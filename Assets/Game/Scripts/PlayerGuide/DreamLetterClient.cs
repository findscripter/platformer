using System.Collections;
using System.IO;
using System.Text;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 调用 DeepSeek 生成梦境提取和终点梦笺。参赛版优先使用随游戏打包的内置 Key。
/// </summary>
public static class DreamLetterClient
{
    public const string EmbeddedSettingsPath = "DreamLetter/connection";
    // The provider retired deepseek-chat; keep settings and gameplay on the same supported model.
    public const string ModelName = "deepseek-flash";
    private const float AnalyzeTimeout = 12f;
    public const float LetterTotalTimeout = 35f;

    public static IEnumerator Analyze(GameContext context)
    {
        if (context?.DreamRun == null)
            yield break;
        if (context.DreamRun.AnalyzeStarted)
            yield break;

        DreamRunRecorder recording = context.DreamRun;
        recording.AnalyzeStarted = true;
        recording.AnalysisStartedAt = Time.realtimeSinceStartupAsDouble;
        recording.AnalysisFailureReason = null;
        string dream = context.PlayerDreamInput ?? string.Empty;
        string user = "只返回JSON。输入数据：\n{\"dream_text\":\"" + DreamRunRecorder.Escape(dream) + "\"}";
        string json = null;
        try
        {
            yield return Chat("analyze_system", user, 0.2f, 600, AnalyzeTimeout,
                text => json = text, error => recording.AnalysisFailureReason = error, recording,
                isCurrent: () => ReferenceEquals(context.DreamRun, recording) && context.PlayerDreamInput == dream);
            if (!ReferenceEquals(context.DreamRun, recording) || context.PlayerDreamInput != dream) yield break;
            JObject analysis = ParseObject(json);
            if (analysis?["dream_summary"]?.Type == JTokenType.String)
                recording.AnalysisJson = analysis.ToString(Formatting.None);
            else if (recording.AnalysisFailureReason == null) recording.AnalysisFailureReason = "analysis_schema";
        }
        finally { recording.AnalyzeFinished = true; }
    }

    public static IEnumerator ComposeLetter(GameContext context)
    {
        if (context?.DreamRun == null)
            yield break;
        DreamRunRecorder recording = context.DreamRun;
        if (!string.IsNullOrEmpty(recording.LetterBody))
            yield break;
        if (context.DreamRun.LetterStarted)
        {
            double until = recording.LetterStartedAt + LetterTotalTimeout + 2f;
            while (ReferenceEquals(context.DreamRun, recording) && recording.LetterStarted && string.IsNullOrEmpty(recording.LetterBody) && Time.realtimeSinceStartupAsDouble < until)
            {
                yield return null;
            }
            if (ReferenceEquals(context.DreamRun, recording) && string.IsNullOrEmpty(recording.LetterBody))
            {
                recording.LetterRequestVersion++;
                SetUnavailable(recording, context, "request_interrupted");
                recording.LetterStarted = false;
            }
            yield break;
        }

        recording.LetterStarted = true;
        int requestVersion = ++recording.LetterRequestVersion;
        recording.LetterStartedAt = Time.realtimeSinceStartupAsDouble;
        recording.LetterFailureReason = null;
        recording.LastLetterCandidateJson = null;
        var letterContext = new GameContext
        {
            PlayerDreamInput = context.PlayerDreamInput,
            TarotResult = context.TarotResult,
            DreamRun = recording
        };
        bool IsCurrent() => ReferenceEquals(context.DreamRun, recording)
            && recording.LetterRequestVersion == requestVersion
            && context.PlayerDreamInput == letterContext.PlayerDreamInput
            && ReferenceEquals(context.TarotResult, letterContext.TarotResult);
        try
        {
            // Dream text is the authoritative evidence. A pending optional extraction must not
            // add another request before the end-of-run letter; use it only if already available.
            if (!IsCurrent()) yield break;

            if (letterContext.TarotResult == null || !letterContext.TarotResult.IsComplete())
            {
                SetUnavailable(recording, letterContext, "missing_drawn_cards");
                yield break;
            }
            string user = "只返回JSON。输入数据：\n" + BuildLetterPayload(letterContext);
            // Use the supplied writing prompt for both long and short dreams. Its sparse
            // input section handles brevity without swapping in a second writing style.
            string prosePrompt = null;
            LetterEvidence evidence = CaptureEvidence(letterContext);
            bool AcceptCandidate(string candidate)
            {
                string issue = null;
                string body = ValidateLetterEvidence(candidate, evidence, reason => issue = reason);
                return body != null || issue == "ending_advice";
            }
            string json = null;
            yield return Chat("letter_system", user, 0.65f, 4096, LetterTotalTimeout,
                text => json = text, error => recording.LetterFailureReason = error, recording, prosePrompt, IsCurrent, AcceptCandidate);
            if (!IsCurrent()) yield break;
            if (json != null) recording.LastLetterCandidateJson = json;
            // Keep the real rejection reason for diagnostics instead of replacing it with
            // missing_report_body after the gateway rejects a completed response.
            string report = ValidateLetter(json ?? recording.LastLetterCandidateJson, letterContext);
            if (json == null) report = null;
            if (json != null && report == null && recording.LetterValidationFailure == "ending_advice")
            {
                // A bad closing caption must not throw away an otherwise valid personalized letter.
                // Keep the model's dream/card/behavior analysis and substitute only the local title,
                // then validate the entire document again before publishing anything.
                EnsureLocalLetter(letterContext);
                string localBody = recording.LocalLetterBody;
                if (!string.IsNullOrWhiteSpace(localBody))
                {
                    string[] lines = localBody.TrimEnd().Split('\n');
                    string repaired = ReplaceEnding(json, new JObject { ["ending"] = lines[lines.Length - 1].Trim() }.ToString(Formatting.None));
                    string validated = ValidateLetter(repaired, letterContext);
                    if (validated != null)
                    {
                        json = repaired; report = validated;
                        Debug.Log("[DreamLetter] Validated model body retained with local closing caption.");
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(report))
                SetUnavailable(recording, letterContext, recording.LetterFailureReason ?? recording.LetterValidationFailure ?? "letter_schema");
            else
            {
                recording.LetterIsFallback = false;
                recording.LetterFailureReason = null;
                recording.LetterSource = "model";
                recording.LetterJson = json;
                recording.LetterBody = report;
                Debug.Log("[DreamLetter] Letter ready, characters=" + report.Length + ", seconds=" +
                    (Time.realtimeSinceStartupAsDouble - recording.LetterStartedAt).ToString("F1", CultureInfo.InvariantCulture));
            }
        }
        finally
        {
            if (IsCurrent())
            {
                if (string.IsNullOrEmpty(recording.LetterBody))
                    SetUnavailable(recording, letterContext, recording.LetterFailureReason ?? "request_interrupted");
                recording.LetterStarted = false;
            }
            else if (recording.LetterRequestVersion == requestVersion) recording.LetterStarted = false;
        }
    }

    private static void SetUnavailable(DreamRunRecorder recording, GameContext context, string reason)
    {
        recording.LetterIsFallback = true;
        recording.LetterFailureReason = reason;
        EnsureLocalLetter(context);
        recording.LetterSource = string.IsNullOrEmpty(recording.LocalLetterBody) ? "unavailable" : "local";
        recording.LetterJson = null;
        recording.LetterBody = recording.LocalLetterBody ?? DreamNoteWriter.Compose(context.PlayerDreamInput, context.TarotResult);
        Debug.LogWarning("[DreamLetter] Letter unavailable: " + reason);
    }

    public static void CancelPendingLetter(GameContext context)
    {
        if (context?.DreamRun == null) return;
        DreamRunRecorder recording = context.DreamRun;
        recording.LetterRequestVersion++;
        recording.PendingLetterCancellation?.Cancel();
        recording.LetterStarted = false;
    }

    public static void EnsureLocalLetter(GameContext context)
    {
        if (context?.DreamRun == null || !string.IsNullOrEmpty(context.DreamRun.LocalLetterBody)) return;
        JObject document = LocalDreamLetterComposer.ComposeDocument(context);
        if (document == null) return;
        context.DreamRun.LocalLetterJson = document.ToString(Formatting.None);
        context.DreamRun.LocalLetterBody = document["report_body"].Value<string>();
    }

    public static void ShowLocalLetterWhilePending(GameContext context)
    {
        if (context?.DreamRun == null || !string.IsNullOrEmpty(context.DreamRun.LetterBody)) return;
        EnsureLocalLetter(context);
        context.DreamRun.LetterBody = context.DreamRun.LocalLetterBody;
        context.DreamRun.LetterSource = "local";
        context.DreamRun.LetterIsFallback = true;
    }

    private static bool IsTransientFailure(string error)
    {
        return error == "timeout" || error == "stream_interrupted" || error == "response_truncated"
            || error == "empty_response" || error == "invalid_response" || error == "incomplete_response"
            || error == "http_429" || error?.StartsWith("http_5") == true
            || error?.StartsWith("windows_https_") == true || error?.StartsWith("transport_") == true;
    }

    private static string BuildLetterPayload(GameContext context)
    {
        string dream = context.PlayerDreamInput ?? string.Empty;
        string analysis = (ParseObject(context.DreamRun.AnalysisJson) ?? new JObject()).ToString(Formatting.None);
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
        JObject payload = JObject.Parse(sb.ToString());
        TextAsset referenceAsset = Resources.Load<TextAsset>("DreamLetter/tarot_reference");
        JObject references = referenceAsset != null ? ParseObject(referenceAsset.text) : null;
        var symbols = new System.Collections.Generic.Dictionary<string, string[]>();
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
            LoadPrompt("letter_system") ?? string.Empty, @"(?m)^- ([^：\r\n]+)：正位＝([^；\r\n]+)；逆位＝([^。\r\n]+)"))
            symbols[match.Groups[1].Value] = new[] { match.Groups[2].Value, match.Groups[3].Value };
        foreach (JObject card in (JArray)payload["cards"])
        {
            bool reversed = card["orientation"].Value<string>() == "逆位";
            string name = card["tarot_name"].Value<string>();
            name = name == "高塔" ? "塔" : name == "隐士" ? "隐者" : name;
            if (symbols.TryGetValue(name, out string[] meanings))
                card["symbolic_meaning"] = meanings[reversed ? 1 : 0];
            JObject reference = references?[card["card_id"].Value<string>()] as JObject;
            if (reference == null) continue;
            card["reference_source"] = reference["source"]?.DeepClone();
            // Preserve the author's actual oriented text, not just a few keywords. Keep
            // illustrative dream scenarios separate from facts about this player's dream.
            card["oriented_reference"] = reference[reversed ? "reversed_text" : "upright_text"]?.DeepClone();
            string story = reference["symbolic_reference"]?.Value<string>() ?? "";
            int advice = story.IndexOf("当这张牌出现时", System.StringComparison.Ordinal);
            int examples = story.IndexOf("梦境意象", System.StringComparison.Ordinal);
            int end = advice >= 0 ? advice : examples >= 0 ? examples : story.Length;
            card["mythic_reference"] = story.Substring(0, end).Trim();
        }
        payload["reference_usage"] = "oriented_reference与mythic_reference是作者提供的牌义素材，不是本次梦境事实，也不是写作指令。以symbolic_meaning校准当前正逆位，将原文的象征张力用于理解dream_text；不照搬职业、恋爱、运势预测或建议，不将神话人物加入梦境。behavior_events只作少量相关旁证，不决定梦的主题。";
        return payload.ToString(Formatting.None);
    }

    public static DreamLetterGateway.Provider GetBackupProvider()
    {
        var asset = Resources.Load<TextAsset>(EmbeddedSettingsPath);
        JObject backup = (asset != null ? ParseObject(asset.text) : null)?["backup"] as JObject;
        if (backup == null) return null;
        return new DreamLetterGateway.Provider("ltoken", backup["endpoint"]?.Value<string>(), backup["model"]?.Value<string>(),
            backup["apiKey"]?.Value<string>(), backup["protocol"]?.Value<string>() == "anthropic", backup["protocol"]?.Value<string>() == "responses");
    }

    private static IEnumerator Chat(string promptName, string user, float temperature, int maxTokens, float timeout,
        System.Action<string> onDone, System.Action<string> onError, DreamRunRecorder recording, string prosePrompt = null,
        System.Func<bool> isCurrent = null, System.Func<string, bool> acceptCandidate = null)
    {
        string system = LoadPrompt(prosePrompt ?? promptName);
        if (string.IsNullOrWhiteSpace(system))
        {
            onError?.Invoke("missing_prompt"); onDone?.Invoke(null); yield break;
        }
        var primary = new DreamLetterGateway.Provider("deepseek", DreamLetterGateway.PrimaryEndpoint, ModelName, LoadApiKey());
        var backup = GetBackupProvider();
        double start = Time.realtimeSinceStartupAsDouble;
        bool skipPrimary = backup?.Ready == true && (recording.PrimaryUnavailableUntil > start
            || (promptName == "letter_ending_system" && recording.LetterProvider == "ltoken"));
        Debug.Log("[DreamLetter] " + promptName + " started; " + (skipPrimary ? "temporary backup route" : "DeepSeek first") + ".");
        using (var cancellation = new System.Threading.CancellationTokenSource())
        {
            if (promptName == "letter_system") recording.PendingLetterCancellation = cancellation;
            if (promptName == "analyze_system") acceptCandidate = candidate => ParseObject(candidate)?["dream_summary"]?.Type == JTokenType.String;
            var task = DreamLetterGateway.RequestAsync(primary, backup, system, user, temperature, maxTokens, timeout, cancellation.Token, skipPrimary, promptName, acceptCandidate);
            try
            {
                while (!task.IsCompleted && Time.realtimeSinceStartupAsDouble - start < timeout + 1f)
                {
                    if (isCurrent != null && !isCurrent()) yield break;
                    yield return null;
                }
                if (isCurrent != null && !isCurrent()) yield break;
                string failure = "timeout";
                if (task.IsFaulted || task.IsCanceled) failure = "transport_failed";
                else if (task.IsCompleted)
                {
                    DreamLetterGateway.Result result = task.Result;
                    if (promptName == "letter_system" && result.RejectedContent != null) recording.LastLetterCandidateJson = result.RejectedContent;
                    foreach (string diagnostic in result.Diagnostics) Debug.Log("[DreamLetter] " + diagnostic);
                    if (result.PrimaryFailed) recording.PrimaryUnavailableUntil = Time.realtimeSinceStartupAsDouble + 60f;
                    else if (result.ProviderId == "deepseek" && result.Content != null) recording.PrimaryUnavailableUntil = 0;
                    if (promptName == "analyze_system") recording.AnalysisProvider = result.ProviderId;
                    else recording.LetterProvider = result.ProviderId;
                    if (result.Content != null)
                    {
                        Debug.Log("[DreamLetter] " + promptName + " received from " + result.ProviderId + ", seconds=" +
                            (Time.realtimeSinceStartupAsDouble - start).ToString("F1", CultureInfo.InvariantCulture));
                        onDone?.Invoke(result.Content); yield break;
                    }
                    failure = result.Error;
                }
                Debug.LogWarning("[DreamLetter] " + promptName + " failed: " + failure);
                onError?.Invoke(failure); onDone?.Invoke(null);
            }
            finally
            {
                cancellation.Cancel();
                if (ReferenceEquals(recording.PendingLetterCancellation, cancellation)) recording.PendingLetterCancellation = null;
            }
        }
    }

    private static string LoadApiKey()
    {
        string embedded = LoadEmbeddedApiKey();
        if (!string.IsNullOrWhiteSpace(embedded)) return embedded;
#if UNITY_EDITOR
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
            foreach (string raw in File.ReadAllLines(paths[i], Encoding.UTF8))
            {
                string line = raw.Trim().TrimStart('\uFEFF');
                if (!line.StartsWith("DEEPSEEK_API_KEY="))
                    continue;
                return line.Substring("DEEPSEEK_API_KEY=".Length).Trim().Trim('"', '\'');
            }
        }

#endif
        return null;
    }

    public static bool HasEmbeddedApiKey => !string.IsNullOrWhiteSpace(LoadEmbeddedApiKey());

    private static string LoadEmbeddedApiKey()
    {
        var asset = Resources.Load<TextAsset>(EmbeddedSettingsPath);
        JObject settings = asset != null ? ParseObject(asset.text) : null;
        JToken value = settings?["apiKey"];
        return value?.Type == JTokenType.String ? value.Value<string>().Trim() : null;
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
        JToken token = ParseObject(json)?[key];
        return token?.Type == JTokenType.String ? token.Value<string>() : null;
    }

    private static JObject ParseObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JObject.Parse(UnwrapModelJson(json)); }
        catch (JsonException) { return null; }
    }

    private static string ReplaceEnding(string original, string reply)
    {
        JObject document = ParseObject(original), correction = ParseObject(reply);
        string body = document?["report_body"]?.Type == JTokenType.String ? document["report_body"].Value<string>().TrimEnd() : null;
        string ending = correction?["ending"]?.Type == JTokenType.String ? correction["ending"].Value<string>().Trim() : null;
        if (body == null || ending == null || !ending.StartsWith("——") || ending.Length < 6 || ending.Length > 32
            || ending.Contains("\n") || ending.Contains("\r")) return null;
        int line = body.LastIndexOf('\n');
        if (line < 0 || !body.Substring(line + 1).TrimStart().StartsWith("——")) return null;
        document["report_body"] = body.Substring(0, line) + "\n" + ending;
        return document.ToString(Formatting.None);
    }

    private static string ValidateLetter(string json, GameContext context)
    {
        context.DreamRun.LetterValidationFailure = null;
        return ValidateLetterEvidence(json, CaptureEvidence(context), reason => context.DreamRun.LetterValidationFailure = reason);
    }

    private sealed class LetterEvidence
    {
        public string Dream;
        public readonly string[] Names = new string[TarotResultData.SlotCount];
        public readonly bool[] Reversed = new bool[TarotResultData.SlotCount];
        public System.Collections.Generic.IReadOnlyList<DreamRunRecorder.Entry> Events;
    }

    private static LetterEvidence CaptureEvidence(GameContext context)
    {
        var evidence = new LetterEvidence { Dream = context.PlayerDreamInput ?? string.Empty,
            Events = new System.Collections.Generic.List<DreamRunRecorder.Entry>(context.DreamRun.Events) };
        for (int i = 0; i < TarotResultData.SlotCount; i++)
        {
            evidence.Names[i] = TarotCatalog.GetDisplayName(context.TarotResult?.GetCard(i));
            evidence.Reversed[i] = context.TarotResult != null && context.TarotResult.IsReversed(i);
        }
        return evidence;
    }

    // This validator runs on provider completion threads, so it uses captured plain values only.
    // It neither reads Unity objects nor mutates the live run's failure state while routes compete.
    private static string ValidateLetterEvidence(string json, LetterEvidence evidence, System.Action<string> onRejected)
    {
        string Reject(string reason) { onRejected?.Invoke(reason); return null; }
        JObject letter = ParseObject(json);
        string body = ExtractJsonString(json, "report_body");
        if (string.IsNullOrWhiteSpace(body) || letter?["error"] != null) return Reject("missing_report_body");
        string presentationIssue = PresentationIssue(body);
        if (presentationIssue != null) return Reject(presentationIssue);
        string[] paragraphs = body.Trim().Split('\n');
        string ending = paragraphs[paragraphs.Length - 1].Trim();
        if (!ending.StartsWith("——") || ending.Length < 6) return Reject("missing_ending");
        bool endingAdvice = System.Text.RegularExpressions.Regex.IsMatch(ending, @"先[\s\S]{0,10}(走|迈|再)|继续去|试着|你可以|你应该|你需要|你必须");
        int endingLine = body.LastIndexOf('\n');
        string proseForAdvice = endingAdvice ? (endingLine < 0 ? string.Empty : body.Substring(0, endingLine)) : body;
        string[] disallowed = { "这说明你", "这意味着你", "你应该", "你需要", "你必须", "你的性格", "潜意识", "行为数据显示", "事件ID", "触发器" };
        foreach (string phrase in disallowed)
            if (proseForAdvice.Contains(phrase) && !evidence.Dream.Contains(phrase)) return Reject("unsupported_advice");
        string sparseDream = System.Text.RegularExpressions.Regex.Replace(evidence.Dream, @"[\s\p{P}]", string.Empty);
        if (System.Text.RegularExpressions.Regex.IsMatch(sparseDream, @"^(我)?(梦到|梦见)?((牙齿|牙)(都|全|全部|突然)?(掉了|脱落了?)|掉牙了?)$"))
        {
            string[] invented = { "一颗牙", "少了一颗", "满口牙", "流血", "长辈", "亲人", "父母", "吃饭", "再长", "长出来", "嘴里空", "空出一处" };
            foreach (string detail in invented)
                if (body.Contains(detail) && !evidence.Dream.Contains(detail)) return Reject("ungrounded_short_dream");
        }
        if (!(letter["card_readings"] is JArray readings) || readings.Count != TarotResultData.SlotCount) return Reject("card_count");
        if (!(letter["unsupported_claims"] is JArray claims) || claims.Count != 0) return Reject("unsupported_claims");
        if (!(letter["used_behavior_event_ids"] is JArray used) || used.Count > 5) return Reject("behavior_count");
        var recordedIds = new System.Collections.Generic.HashSet<string>();
        var recordedTypes = new System.Collections.Generic.Dictionary<string, string>();
        var usedIds = new System.Collections.Generic.HashSet<string>();
        var perCardIds = new System.Collections.Generic.HashSet<string>();
        foreach (var entry in evidence.Events) { recordedIds.Add(entry.EventId); recordedTypes[entry.EventId] = entry.Type; }
        foreach (JToken id in used)
        {
            if (id.Type != JTokenType.String || !recordedIds.Contains(id.Value<string>()) || !usedIds.Add(id.Value<string>())) return Reject("unknown_behavior");
        }
        if (CountMeaningful(evidence.Dream) < 8)
        {
            int fragments = usedIds.Count;
            bool death = false, retry = false;
            foreach (string id in usedIds)
            {
                death |= recordedTypes[id] == "death";
                retry |= recordedTypes[id] == "retry";
            }
            if (death && retry) fragments--;
            if (fragments > 1) return Reject("sparse_behavior_count");
            if (CountMeaningful(body) > 520) return Reject("sparse_letter_too_long");
        }
        for (int i = 0; i < TarotResultData.SlotCount; i++)
        {
            string name = evidence.Names[i];
            string orientation = evidence.Reversed[i] ? "逆位" : "正位";
            if (!(readings[i] is JObject reading) || string.IsNullOrEmpty(name)
                || reading["card"]?.Type != JTokenType.String
                || reading["orientation"]?.Type != JTokenType.String
                || reading["card"]?.Value<string>()?.Trim() != name
                || reading["orientation"]?.Value<string>() != orientation
                || !MentionsCardOrientation(body, name, orientation)) return Reject("card_mismatch_" + i);
            if (reading["dream_evidence"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace(reading["dream_evidence"].Value<string>())) return Reject("missing_dream_evidence_" + i);
            if (!(reading["behavior_event_ids"] is JArray ids)) return Reject("missing_behavior_links_" + i);
            foreach (JToken id in ids)
            {
                if (id.Type != JTokenType.String || !recordedIds.Contains(id.Value<string>())) return Reject("unknown_card_behavior_" + i);
                perCardIds.Add(id.Value<string>());
            }
        }
        if (!usedIds.SetEquals(perCardIds)) return Reject("behavior_links_disagree");
        if (endingAdvice) return Reject("ending_advice");
        return body.Trim();
    }

    // Run on the extracted prose, never on the transport JSON or the player's original input.
    public static string PresentationIssue(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "missing_report_body";
        if (body.IndexOf('\uFFFD') >= 0 || System.Text.RegularExpressions.Regex.IsMatch(body, @"\\[nrt]|\\u[0-9a-fA-F]{4}"))
            return "escaped_or_corrupt_prose";
        if (System.Text.RegularExpressions.Regex.IsMatch(body,
            @"report_body|card_readings|dream_evidence|behavior_event_ids|unsupported_claims|input_quality|shanhaijing_name|```|<\|(?:im_start|im_end|system)\|>"))
            return "structured_data_in_prose";
        string[] instructions = { "提示词", "只返回JSON", "只返回 JSON", "正文必须", "正文改为", "先在内部完成", "信息权重必须", "内部核对维度", "输出格式", "无直接梦境证据", "原文证据", "全篇唯一核心理解", "玩家", "原文没有", "原文支持", "不能替梦", "不能替你写", "没有更多行为", "证据不足", "输入信息不足" };
        foreach (string instruction in instructions)
            if (body.Contains(instruction)) return "writing_instructions_in_prose";
        return null;
    }

    private static bool MentionsCardOrientation(string body, string name, string orientation)
    {
        // 女娲 and 伏羲女娲 are distinct cards; a longer card name cannot supply this card's orientation.
        foreach (TarotCatalog.Entry entry in TarotCatalog.All)
            if (entry.DisplayName.Length > name.Length && entry.DisplayName.Contains(name))
                body = body.Replace(entry.DisplayName, new string('_', entry.DisplayName.Length));
        // Both “共工触山（逆位）” and “逆位的「共工触山」” are valid prose.
        // Do not let an orientation elsewhere in the paragraph satisfy this card.
        string separator = @"(?:[^\S\r\n]|[（）()「」『』“”\""的·：:]){0,5}";
        string escaped = System.Text.RegularExpressions.Regex.Escape(name);
        string opposite = orientation == "正位" ? "逆位" : "正位";
        return System.Text.RegularExpressions.Regex.IsMatch(body,
            escaped + separator + orientation + "|" + orientation + separator + escaped)
            && !System.Text.RegularExpressions.Regex.IsMatch(body,
                escaped + separator + opposite + "|" + opposite + separator + escaped);
    }
}
