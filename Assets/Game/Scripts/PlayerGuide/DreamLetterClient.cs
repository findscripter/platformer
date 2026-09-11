using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 调用 DeepSeek 生成梦境提取和终点梦笺。Key 只从本机 .env / 环境变量读取，不写进代码。
/// </summary>
public static class DreamLetterClient
{
    private const string DeepSeekUrl = "https://api.deepseek.com/chat/completions";
    private const string ProxyUrl = "http://127.0.0.1:8787";
    private const float AnalyzeTimeout = 25f;
    private const float LetterTimeout = 60f;

    public static IEnumerator Analyze(GameContext context)
    {
        if (context?.DreamRun == null)
            yield break;
        if (context.DreamRun.AnalyzeStarted)
            yield break;

        context.DreamRun.AnalyzeStarted = true;
        string dream = context.PlayerDreamInput ?? string.Empty;
        string user = "只返回JSON。输入数据：\n{\"dream_text\":\"" + DreamRunRecorder.Escape(dream) + "\"}";
        string json = null;
        yield return Chat("analyze_system", user, 0.2f, 600, AnalyzeTimeout, text => json = text);
        if (!string.IsNullOrEmpty(json))
            context.DreamRun.AnalysisJson = UnwrapModelJson(json);
    }

    public static IEnumerator ComposeLetter(GameContext context)
    {
        if (context?.DreamRun == null)
            yield break;
        if (!string.IsNullOrEmpty(context.DreamRun.LetterBody))
            yield break;
        if (context.DreamRun.LetterStarted)
        {
            float wait = 0f;
            while (string.IsNullOrEmpty(context.DreamRun.LetterBody) && wait < LetterTimeout)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            yield break;
        }

        context.DreamRun.LetterStarted = true;
        string user = "只返回JSON。输入数据：\n" + BuildLetterPayload(context);
        string json = null;
        yield return Chat("letter_system", user, 0.9f, 2800, LetterTimeout, text => json = text);

        string report = ExtractJsonString(UnwrapModelJson(json), "report_body");
        if (string.IsNullOrWhiteSpace(report))
            report = DreamNoteWriter.Compose(context.PlayerDreamInput, context.TarotResult);
        context.DreamRun.LetterBody = report;
    }

    private static string BuildLetterPayload(GameContext context)
    {
        string dream = context.PlayerDreamInput ?? string.Empty;
        string analysis = string.IsNullOrWhiteSpace(context.DreamRun.AnalysisJson)
            ? "{}"
            : context.DreamRun.AnalysisJson;
        string quality = CountMeaningful(dream) < 8 ? "sparse" : "normal";
        var sb = new StringBuilder();
        sb.Append("{\"dream_text\":\"").Append(DreamRunRecorder.Escape(dream)).Append("\",");
        sb.Append("\"dream_analysis\":").Append(analysis).Append(',');
        sb.Append("\"input_quality\":\"").Append(quality).Append("\",");
        sb.Append("\"cards\":[");
        TarotResultData run = context.TarotResult;
        for (int i = 0; i < TarotResultData.SlotCount; i++)
        {
            if (i > 0)
                sb.Append(',');
            TarotCardData card = run != null ? run.GetCard(i) : null;
            bool reversed = run != null && run.IsReversed(i);
            string id = card != null ? card.CardId : string.Empty;
            string shan = TarotCatalog.GetDisplayName(card);
            string tarot = string.Empty;
            if (card != null && TarotCatalog.TryGet(id, out TarotCatalog.Entry entry))
                tarot = entry.TarotName;
            sb.Append("{\"card_id\":\"").Append(DreamRunRecorder.Escape(id))
                .Append("\",\"shanhaijing_name\":\"").Append(DreamRunRecorder.Escape(shan))
                .Append("\",\"tarot_name\":\"").Append(DreamRunRecorder.Escape(tarot))
                .Append("\",\"orientation\":\"").Append(reversed ? "逆位" : "正位")
                .Append("\"}");
        }
        sb.Append("],\"behavior_events\":").Append(context.DreamRun.EventsJson()).Append('}');
        return sb.ToString();
    }

    private static IEnumerator Chat(string promptName, string user, float temperature, int maxTokens, float timeout, System.Action<string> onDone)
    {
        string key = LoadApiKey();
        string system = LoadPrompt(promptName);
        if (string.IsNullOrEmpty(system))
        {
            onDone?.Invoke(null);
            yield break;
        }

        if (!string.IsNullOrEmpty(key))
        {
            string request = "{\"model\":\"deepseek-chat\",\"temperature\":" + temperature.ToString("0.##") +
                             ",\"max_tokens\":" + maxTokens +
                             ",\"response_format\":{\"type\":\"json_object\"},\"messages\":[" +
                             "{\"role\":\"system\",\"content\":\"" + DreamRunRecorder.Escape(system) + "\"}," +
                             "{\"role\":\"user\",\"content\":\"" + DreamRunRecorder.Escape(user) + "\"}]}";
            for (int attempt = 0; attempt < 2; attempt++)
            {
                using (var req = new UnityWebRequest(DeepSeekUrl, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.SetRequestHeader("Authorization", "Bearer " + key);
                    req.timeout = Mathf.CeilToInt(timeout);
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        string content = ExtractJsonString(req.downloadHandler.text, "content");
                        onDone?.Invoke(string.IsNullOrEmpty(content) ? req.downloadHandler.text : content);
                        yield break;
                    }
                }
            }
        }

        string path = promptName == "analyze_system" ? "/v1/dream/analyze" : "/v1/dream/letter";
        using (var proxy = new UnityWebRequest(ProxyUrl + path, "POST"))
        {
            proxy.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(user.Contains("{") ? user.Substring(user.IndexOf('{')) : "{}"));
            proxy.downloadHandler = new DownloadHandlerBuffer();
            proxy.SetRequestHeader("Content-Type", "application/json");
            proxy.timeout = 8;
            yield return proxy.SendWebRequest();
            if (proxy.result == UnityWebRequest.Result.Success)
            {
                onDone?.Invoke(proxy.downloadHandler.text);
                yield break;
            }
        }

        onDone?.Invoke(null);
    }

    private static string LoadApiKey()
    {
        string env = System.Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        string[] paths =
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Tools", "DreamLetterProxy", ".env"),
            Path.Combine(Application.dataPath, "..", "Tools", "DreamLetterProxy", ".env")
        };
        for (int i = 0; i < paths.Length; i++)
        {
            if (!File.Exists(paths[i]))
                continue;
            foreach (string line in File.ReadAllLines(paths[i], Encoding.UTF8))
            {
                if (!line.StartsWith("DEEPSEEK_API_KEY="))
                    continue;
                return line.Substring("DEEPSEEK_API_KEY=".Length).Trim().Trim('"');
            }
        }

        return null;
    }

    private static string LoadPrompt(string name)
    {
        TextAsset asset = Resources.Load<TextAsset>("DreamLetter/" + name);
        if (asset != null && !string.IsNullOrEmpty(asset.text))
            return asset.text;

        string[] paths =
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Tools", "DreamLetterProxy", "prompts", name + ".txt"),
            Path.Combine(Application.dataPath, "..", "Tools", "DreamLetterProxy", "prompts", name + ".txt")
        };
        for (int i = 0; i < paths.Length; i++)
        {
            if (File.Exists(paths[i]))
                return File.ReadAllText(paths[i], Encoding.UTF8);
        }

        return null;
    }

    private static string UnwrapModelJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return "{}";
        string trimmed = json.Trim();
        if (trimmed.StartsWith("{"))
            return trimmed;
        int obj = trimmed.IndexOf('{');
        int end = trimmed.LastIndexOf('}');
        if (obj >= 0 && end > obj)
            return trimmed.Substring(obj, end - obj + 1);
        return "{}";
    }

    private static int CountMeaningful(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;
        int n = 0;
        foreach (char c in value)
        {
            if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c))
                n++;
        }
        return n;
    }

    public static string ExtractJsonString(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            return null;
        string needle = "\"" + key + "\"";
        int i = json.IndexOf(needle, System.StringComparison.Ordinal);
        if (i < 0)
            return null;
        int colon = json.IndexOf(':', i + needle.Length);
        if (colon < 0)
            return null;
        int q = json.IndexOf('"', colon + 1);
        if (q < 0)
            return null;
        var sb = new StringBuilder();
        for (int p = q + 1; p < json.Length; p++)
        {
            char c = json[p];
            if (c == '\\' && p + 1 < json.Length)
            {
                char n = json[p + 1];
                if (n == 'n')
                    sb.Append('\n');
                else if (n == '"')
                    sb.Append('"');
                else if (n == '\\')
                    sb.Append('\\');
                else
                    sb.Append(n);
                p++;
                continue;
            }
            if (c == '"')
                break;
            sb.Append(c);
        }
        return sb.ToString();
    }
}
