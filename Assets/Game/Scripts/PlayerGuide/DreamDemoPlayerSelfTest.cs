using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Release-Player QA entry. Completely dormant unless --dream-ai-selftest is explicitly supplied.
/// Tests only synthetic contexts and never writes player saves or connection settings.
/// </summary>
public sealed class DreamDemoPlayerSelfTest : MonoBehaviour
{
    static readonly string[] Dreams = {
        "牙齿掉了",
        "牙齿掉了",
        "我梦见有人在很长的走廊里追我，我看不清他的脸，一直跑到一扇门前，可是怎么也打不开门。",
        "我梦见回学校考试，已经迟到了，可是楼梯和走廊一直在变，我怎么也找不到自己的教室。",
        "我从很高的地方往下掉，开始非常害怕，后来发现自己像羽毛一样慢慢落在草地上，心里反而平静了。",
        "我梦见已经去世的奶奶坐在旧厨房里剥豆子。我坐在旁边帮她，她问我最近累不累，我还没回答就醒了。",
        DreamInputView.SampleDream,
        "我在海边看见一只蓝色的猫，它陪我坐着看了一会儿日落，我觉得很平静。"
    };
    static readonly string[] RequiredResources = {
        "DreamLetter/connection", "DreamLetter/analyze_system", "DreamLetter/letter_system",
        "DreamLetter/letter_sparse_system", "DreamLetter/letter_ending_system", "DreamLetter/card_lenses",
        "DreamLetter/tarot_reference", "DreamLetter/local_sample_lenses"
    };
    readonly JArray cases = new JArray();
    readonly List<string> failed = new List<string>();
    readonly List<string> diagnostics = new List<string>();
    readonly Stack<IEnumerator> iterators = new Stack<IEnumerator>();
    JObject summary;
    string output;
    bool finished;
    double started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (Application.isEditor || !Environment.GetCommandLineArgs().Contains("--dream-ai-selftest")) return;
        var host = new GameObject("DreamDemoPlayerSelfTest");
        DontDestroyOnLoad(host);
        host.AddComponent<DreamDemoPlayerSelfTest>();
    }

    IEnumerator Start()
    {
        output = Argument("--dream-ai-selftest-output") ?? Path.Combine(Application.persistentDataPath, "AI-SelfTest");
        output = Path.GetFullPath(output);
        started = Time.realtimeSinceStartupAsDouble;
        Application.logMessageReceived += OnLog;
        Application.runInBackground = true;
        summary = new JObject { ["utc"] = DateTime.UtcNow.ToString("O"), ["unity_version"] = Application.unityVersion,
            ["platform"] = Application.platform.ToString(), ["editor"] = Application.isEditor,
            ["development_build"] = Debug.isDebugBuild, ["data_path"] = Application.dataPath,
            ["synthetic_inputs_only"] = true, ["secret_values_written"] = false,
            ["cases"] = cases, ["failures"] = new JArray() };
        try { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "started.json"), summary.ToString()); }
        catch (Exception error) { Debug.LogError("[DemoPlayerQA] Cannot write report: " + error.GetType().Name); Application.Quit(1); yield break; }
        yield return null;
        foreach (string path in RequiredResources)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text)) failed.Add("missing_resource:" + path);
        }
        var writingPrompt = Resources.Load<TextAsset>("DreamLetter/letter_system")?.text ?? "";
        summary["author_prompt_sparse_rule_present"] = writingPrompt.Contains("sparse 为300至450字");
        summary["author_prompt_unknown_rule_present"] = writingPrompt.Contains("不能据此解读为失联");
        if (!writingPrompt.Contains("sparse 为300至450字") || !writingPrompt.Contains("不能据此解读为失联")) failed.Add("stale_writing_prompt");
        if (Application.platform != RuntimePlatform.WindowsPlayer) failed.Add("not_windows_player");
        if (!DreamLetterClient.HasEmbeddedApiKey) failed.Add("embedded_primary_missing");
        if (DreamLetterClient.GetBackupProvider()?.Ready != true) failed.Add("embedded_backup_missing");
        var deck = TarotCatalog.LoadDeck();
        if (deck.Length != 22) failed.Add("tarot_deck_count:" + deck.Length);
        if (failed.Count > 0) { Finish(); yield break; }

        int count = Dreams.Length;
        if (int.TryParse(Argument("--dream-ai-selftest-count"), out int requested)) count = Mathf.Clamp(requested, 1, Dreams.Length);
        int offset = 0;
        if (int.TryParse(Argument("--dream-ai-selftest-offset"), out int requestedOffset)) offset = Mathf.Clamp(requestedOffset, 0, Dreams.Length - 1);
        bool localOnly = Environment.GetCommandLineArgs().Contains("--dream-ai-selftest-local");
        summary["local_only"] = localOnly;
        for (int n = 0; n < count; n++)
        {
            int index = (offset + n) % Dreams.Length;
            var cards = Enumerable.Range(0, 4).Select(i => deck[(index * 3 + i) % deck.Length]).ToArray();
            var reversed = Enumerable.Range(0, 4).Select(i => ((index + 5) & (1 << i)) != 0).ToArray();
            var tarot = new TarotResultData(); tarot.BeginRun(cards, reversed);
            var context = new GameContext { PlayerDreamInput = Dreams[index], TarotResult = tarot, DreamRun = new DreamRunRecorder() };
            if (index % 4 == 1 || index % 4 == 3)
            {
                context.DreamRun.Log("death", "途中受阻倒下"); context.DreamRun.Log("retry", "从刚才停下的地方再走一次");
            }
            if (index % 4 == 2 || index % 4 == 3)
            {
                context.DreamRun.Log("route_choice", "在分岔处选择下层道路，并走到这一段的汇合处。");
                context.DreamRun.Log("collect", "拾取梦核碎片");
            }
            diagnostics.Clear();
            double before = Time.realtimeSinceStartupAsDouble;
            double analysisSeconds = 0;
            double composeStarted = before;
            if (localOnly)
            {
                DreamLetterClient.EnsureLocalLetter(context);
                context.DreamRun.LetterBody = context.DreamRun.LocalLetterBody;
                context.DreamRun.LetterJson = context.DreamRun.LocalLetterJson;
                context.DreamRun.LetterSource = "local";
                context.DreamRun.LetterIsFallback = true;
            }
            else
            {
                // Match the real game's submission path: optional analysis runs before the
                // completed run is composed. Keep its timing separate from the final wait.
                yield return RunRequest(DreamLetterClient.Analyze(context), 20, index, "analysis");
                analysisSeconds = Time.realtimeSinceStartupAsDouble - before;
                composeStarted = Time.realtimeSinceStartupAsDouble;
                iterators.Push(DreamLetterClient.ComposeLetter(context));
                while (iterators.Count > 0)
                {
                    if (Time.realtimeSinceStartupAsDouble - composeStarted > DreamLetterClient.LetterTotalTimeout + 10)
                    {
                        failed.Add("case_deadline:" + index); DisposeIterators(); DreamLetterHttp.CancelAll(); break;
                    }
                    bool pause = false;
                    try
                    {
                        for (int step = 0; step < 64 && iterators.Count > 0; step++)
                        {
                            var iterator = iterators.Peek();
                            if (!iterator.MoveNext()) { (iterators.Pop() as IDisposable)?.Dispose(); continue; }
                            if (iterator.Current is IEnumerator child) { iterators.Push(child); continue; }
                            pause = true; break;
                        }
                    }
                    catch (Exception error)
                    {
                        failed.Add("case_exception:" + index + ":" + error.GetType().Name);
                        DisposeIterators(); DreamLetterHttp.CancelAll();
                    }
                    if (pause || iterators.Count > 0) yield return null;
                }
            }
            var run = context.DreamRun;
            string issue = DreamLetterClient.PresentationIssue(run.LetterBody);
            if (string.IsNullOrWhiteSpace(run.LetterBody)) failed.Add("empty_letter:" + index);
            if (issue != null) failed.Add("presentation:" + index + ":" + issue);
            if (!localOnly && (run.LetterIsFallback || run.LetterSource != "model")) failed.Add("online_unavailable:" + index);
            JObject documentChecks = CheckDocument(run.LetterJson ?? run.LocalLetterJson, context, index);
            cases.Add(new JObject { ["case"] = index, ["dream"] = context.PlayerDreamInput,
                ["seconds"] = Math.Round(Time.realtimeSinceStartupAsDouble - before, 2),
                ["analysis_seconds"] = Math.Round(analysisSeconds, 2),
                ["compose_seconds"] = Math.Round(Time.realtimeSinceStartupAsDouble - composeStarted, 2),
                ["compose_budget_seconds"] = DreamLetterClient.LetterTotalTimeout,
                ["analysis"] = run.AnalysisJson, ["analysis_error"] = run.AnalysisFailureReason, ["analysis_provider"] = run.AnalysisProvider,
                ["cards"] = new JArray(cards.Select((c, i) => new JObject { ["id"] = c.CardId, ["reversed"] = reversed[i] })),
                ["events"] = JArray.Parse(run.EventsJson()), ["source"] = run.LetterSource, ["provider"] = run.LetterProvider,
                ["fallback"] = run.LetterIsFallback, ["error"] = run.LetterFailureReason,
                ["validation_failure"] = run.LetterValidationFailure, ["presentation_issue"] = issue,
                ["rejected_candidate"] = run.LetterIsFallback ? run.LastLetterCandidateJson : null,
                ["letter"] = run.LetterBody, ["document"] = run.LetterJson ?? run.LocalLetterJson, ["document_checks"] = documentChecks,
                ["diagnostics"] = new JArray(diagnostics) });
            WriteProgress();
            Debug.Log("[DemoPlayerQA] case=" + index + " source=" + run.LetterSource + " seconds=" + (Time.realtimeSinceStartupAsDouble - before).ToString("F1"));
            yield return null;
        }
        Finish();
    }

    IEnumerator RunRequest(IEnumerator request, double timeout, int index, string phase)
    {
        double phaseStarted = Time.realtimeSinceStartupAsDouble;
        iterators.Push(request);
        while (iterators.Count > 0)
        {
            if (Time.realtimeSinceStartupAsDouble - phaseStarted > timeout)
            {
                failed.Add(phase + "_deadline:" + index); DisposeIterators(); DreamLetterHttp.CancelAll(); yield break;
            }
            bool pause = false;
            try
            {
                for (int step = 0; step < 64 && iterators.Count > 0; step++)
                {
                    var iterator = iterators.Peek();
                    if (!iterator.MoveNext()) { (iterators.Pop() as IDisposable)?.Dispose(); continue; }
                    if (iterator.Current is IEnumerator child) { iterators.Push(child); continue; }
                    pause = true; break;
                }
            }
            catch (Exception error)
            {
                failed.Add(phase + "_exception:" + index + ":" + error.GetType().Name);
                DisposeIterators(); DreamLetterHttp.CancelAll();
            }
            if (pause || iterators.Count > 0) yield return null;
        }
    }

    JObject CheckDocument(string json, GameContext context, int index)
    {
        var checks = new JObject();
        void Check(string name, bool ok) { checks[name] = ok; if (!ok) failed.Add("document:" + index + ":" + name); }
        try
        {
            JObject doc = JObject.Parse(json ?? "{}");
            Check("report_body_matches_display", doc["report_body"]?.Value<string>()?.Trim() == context.DreamRun.LetterBody?.Trim());
            var readings = doc["card_readings"] as JArray;
            Check("four_cards", readings?.Count == 4);
            var recordedIds = new HashSet<string>(context.DreamRun.Events.Select(e => e.EventId));
            var linkedIds = new HashSet<string>();
            for (int i = 0; i < 4; i++)
            {
                JObject reading = readings != null && i < readings.Count ? readings[i] as JObject : null;
                string name = TarotCatalog.GetDisplayName(context.TarotResult.GetCard(i));
                string orientation = context.TarotResult.IsReversed(i) ? "逆位" : "正位";
                Check("card_" + i + "_identity", reading?["card"]?.Value<string>()?.Trim() == name);
                Check("card_" + i + "_orientation", reading?["orientation"]?.Value<string>() == orientation);
                Check("card_" + i + "_dream_evidence", !string.IsNullOrWhiteSpace(reading?["dream_evidence"]?.Value<string>()));
                JArray ids = reading?["behavior_event_ids"] as JArray;
                Check("card_" + i + "_recorded_events_only", ids != null && ids.All(id => id.Type == JTokenType.String && recordedIds.Contains(id.Value<string>())));
                if (ids != null) foreach (JToken id in ids) linkedIds.Add(id.ToString());
            }
            JArray used = doc["used_behavior_event_ids"] as JArray;
            Check("used_events_recorded", used != null && used.All(id => id.Type == JTokenType.String && recordedIds.Contains(id.Value<string>())));
            Check("event_links_agree", used != null && linkedIds.SetEquals(used.Select(id => id.ToString())));
            Check("unsupported_claims_empty", doc["unsupported_claims"] is JArray claims && claims.Count == 0);
            Check("no_transport_or_prompt_text", DreamLetterClient.PresentationIssue(context.DreamRun.LetterBody) == null);
        }
        catch (Exception error) { Check("parse_" + error.GetType().Name, false); }
        return checks;
    }

    void OnLog(string message, string stack, LogType type)
    {
        // Only service timing/error codes; never serialize connection objects or request headers.
        if (message.StartsWith("[DreamLetter]", StringComparison.Ordinal)) diagnostics.Add(message);
    }
    void WriteProgress()
    {
        summary["failures"] = new JArray(failed);
        summary["elapsed_seconds"] = Math.Round(Time.realtimeSinceStartupAsDouble - started, 2);
        summary["online_count"] = cases.Count(c => c["source"]?.Value<string>() == "model");
        summary["fallback_count"] = cases.Count(c => c["fallback"]?.Value<bool>() == true);
        File.WriteAllText(Path.Combine(output, "results.json"), summary.ToString());
    }
    void Finish()
    {
        if (finished) return;
        finished = true;
        DisposeIterators(); DreamLetterHttp.CancelAll();
        summary["completed"] = true;
        summary["active_requests_at_exit"] = DreamLetterHttp.ActiveRequestCount;
        if (DreamLetterHttp.ActiveRequestCount != 0) failed.Add("active_requests_at_exit");
        WriteProgress();
        Application.logMessageReceived -= OnLog;
        Application.Quit(failed.Count == 0 ? 0 : 1);
    }
    void DisposeIterators()
    {
        while (iterators.Count > 0) (iterators.Pop() as IDisposable)?.Dispose();
    }
    static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
