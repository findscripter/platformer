using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本局语义行为与梦境提取结果。只记说明里的六类事件，供终点梦笺使用。
/// </summary>
public sealed class DreamRunRecorder
{
    public string AnalysisJson { get; set; } = "{}";
    public string LetterBody { get; set; }
    public bool AnalyzeStarted { get; set; }
    public bool LetterStarted { get; set; }
    public bool AnalyzeFinished { get; set; }
    public bool LetterIsFallback { get; set; }
    public string LetterSource { get; set; }
    public string LetterJson { get; set; }
    // Kept in memory for local diagnostics only; never displayed as a completed letter.
    public string LastLetterCandidateJson { get; set; }
    public string LetterValidationFailure { get; set; }
    public string LocalLetterBody { get; set; }
    public string LocalLetterJson { get; set; }
    public string AnalysisFailureReason { get; set; }
    public string LetterFailureReason { get; set; }
    public double AnalysisStartedAt { get; set; }
    public double LetterStartedAt { get; set; }
    public int LetterRequestVersion { get; set; }
    public double PrimaryUnavailableUntil { get; set; }
    public string AnalysisProvider { get; set; }
    public string LetterProvider { get; set; }
    internal System.Threading.CancellationTokenSource PendingLetterCancellation { get; set; }

    public void PrepareLetterRetry()
    {
        if (LetterStarted) return;
        LetterBody = null;
        LetterJson = null;
        LetterFailureReason = null;
        LetterIsFallback = false;
        if (!string.IsNullOrEmpty(AnalysisFailureReason))
        {
            AnalyzeStarted = false;
            AnalyzeFinished = false;
        }
    }

    private readonly List<Entry> events = new List<Entry>();
    private int nextId = 1;
    private readonly HashSet<string> onceKeys = new HashSet<string>();

    public readonly struct Entry
    {
        public readonly string EventId;
        public readonly string Stage;
        public readonly string Type;
        public readonly string Description;
        public readonly int Count;

        public Entry(string eventId, string stage, string type, string description, int count)
        {
            EventId = eventId;
            Stage = stage;
            Type = type;
            Description = description;
            Count = count;
        }

        public Entry WithCount(int count)
        {
            return new Entry(EventId, Stage, Type, Description, count);
        }
    }

    public IReadOnlyList<Entry> Events => events;

    public void Log(string type, string description, bool mergeSame = true)
    {
        if (string.IsNullOrEmpty(type))
            return;

        string stage = CurrentStage();
        if (mergeSame)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Type == type && events[i].Description == description && events[i].Stage == stage)
                {
                    events[i] = events[i].WithCount(events[i].Count + 1);
                    return;
                }
            }
        }

        events.Add(new Entry("e" + nextId.ToString("D3"), stage, type, description, 1));
        nextId++;
    }

    public void LogOnce(string key, string type, string description)
    {
        if (!onceKeys.Add(key))
            return;
        Log(type, description, mergeSame: false);
    }

    public string EventsJson()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (int i = 0; i < events.Count; i++)
        {
            if (i > 0)
                sb.Append(',');
            Entry e = events[i];
            sb.Append("{\"event_id\":\"").Append(Escape(e.EventId))
                .Append("\",\"stage\":\"").Append(Escape(e.Stage))
                .Append("\",\"type\":\"").Append(Escape(e.Type))
                .Append("\",\"description\":\"").Append(Escape(e.Description))
                .Append("\",\"count\":").Append(e.Count).Append('}');
        }
        sb.Append(']');
        return sb.ToString();
    }

    public static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        return json.Substring(1, json.Length - 2);
    }

    public static string CurrentStage()
    {
        int zone = TarotZoneQuery.CurrentZone;
        if (zone < 0)
            return "guide";
        return zone < 4 ? "scene_a" : "scene_b";
    }
}
