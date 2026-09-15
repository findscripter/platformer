using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>DeepSeek starts first; a slow request gets a bounded backup using identical run evidence.</summary>
public static class DreamLetterGateway
{
    public const string PrimaryEndpoint = "https://api.deepseek.com/chat/completions";
    public const float SlowPrimaryBackupDelay = 8f;
    public sealed class Provider
    {
        public readonly string Id, Endpoint, Model, Key;
        public readonly bool Anthropic, Responses;
        public Provider(string id, string endpoint, string model, string key, bool anthropic = false, bool responses = false)
        { Id = id; Endpoint = endpoint; Model = model; Key = key; Anthropic = anthropic; Responses = responses; }
        public bool Ready => !string.IsNullOrWhiteSpace(Key) && !string.IsNullOrWhiteSpace(Model)
            && Uri.TryCreate(Endpoint, UriKind.Absolute, out Uri uri) && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
    }
    public sealed class Result
    {
        public string Content, Error, ProviderId, RejectedContent;
        public bool PrimaryFailed;
        public readonly List<string> Diagnostics = new List<string>();
    }
    public delegate Task<DreamLetterHttp.Response> Sender(Provider provider, string json, int seconds, CancellationToken cancellation);

    public static JObject BuildRequest(Provider provider, string system, string user, float temperature, int maxTokens, string responseType = null)
    {
        if (provider.Responses)
        {
            var responseRequest = new JObject {
                ["model"] = provider.Model, ["stream"] = true, ["store"] = false,
                ["max_output_tokens"] = maxTokens, ["reasoning"] = new JObject { ["effort"] = "none" },
                ["instructions"] = system,
                ["input"] = new JArray(new JObject { ["type"] = "message", ["role"] = "user",
                    ["content"] = new JArray(new JObject { ["type"] = "input_text", ["text"] = user }) })
            };
            JObject format = ResponseFormat(responseType);
            if (format != null) responseRequest["text"] = new JObject { ["format"] = format };
            return responseRequest;
        }
        var request = new JObject { ["model"] = provider.Model, ["max_tokens"] = maxTokens, ["stream"] = true, ["temperature"] = temperature };
        if (provider.Anthropic)
        {
            request["system"] = system;
            request["messages"] = new JArray(new JObject { ["role"] = "user", ["content"] = user });
        }
        else
        {
            request["messages"] = new JArray(new JObject { ["role"] = "system", ["content"] = system }, new JObject { ["role"] = "user", ["content"] = user });
            if (provider.Id == "deepseek")
            {
                request["thinking"] = new JObject { ["type"] = "disabled" };
                request["response_format"] = new JObject { ["type"] = "json_object" };
            }
            else if (provider.Model == "gpt-5.4-mini" || provider.Model == "gpt-5.4")
            {
                request.Remove("max_tokens");
                request.Remove("temperature");
                request["max_completion_tokens"] = maxTokens;
                request["reasoning_effort"] = "none";
                request["response_format"] = new JObject { ["type"] = "json_object" };
            }
        }
        return request;
    }

    private static JObject StringSchema() => new JObject { ["type"] = "string" };
    private static JObject StringsSchema(int max = -1)
    {
        var schema = new JObject { ["type"] = "array", ["items"] = StringSchema() };
        if (max >= 0) schema["maxItems"] = max;
        return schema;
    }
    private static JObject ObjectSchema(JObject properties)
    {
        var required = new JArray();
        foreach (var property in properties.Properties()) required.Add(property.Name);
        return new JObject { ["type"] = "object", ["properties"] = properties, ["required"] = required, ["additionalProperties"] = false };
    }
    private static JObject ResponseFormat(string name)
    {
        JObject schema;
        if (name == "analyze_system")
            schema = ObjectSchema(new JObject { ["dream_summary"] = StringSchema(), ["core_tension"] = StringSchema(),
                ["emotion_tags"] = StringsSchema(3), ["imagery_keywords"] = StringsSchema(4), ["uncertainty_notes"] = StringsSchema() });
        else if (name == "letter_system")
        {
            var card = ObjectSchema(new JObject { ["card"] = StringSchema(), ["orientation"] = new JObject { ["type"] = "string", ["enum"] = new JArray("正位", "逆位", "未记录") },
                ["dream_evidence"] = StringSchema(), ["behavior_event_ids"] = StringsSchema(5), ["contribution"] = StringSchema() });
            schema = ObjectSchema(new JObject { ["core_interpretation"] = StringSchema(),
                ["card_readings"] = new JObject { ["type"] = "array", ["items"] = card, ["minItems"] = 4, ["maxItems"] = 4 },
                ["used_behavior_event_ids"] = StringsSchema(5), ["unsupported_claims"] = StringsSchema(0), ["report_body"] = StringSchema() });
        }
        else if (name == "letter_ending_system")
            schema = ObjectSchema(new JObject { ["ending"] = new JObject { ["type"] = "string", ["minLength"] = 6, ["maxLength"] = 32 } });
        else return null;
        return new JObject { ["type"] = "json_schema", ["name"] = name, ["strict"] = true, ["schema"] = schema };
    }

    private static string NormalizeJson(string content)
    {
        string text = content.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal) && text.EndsWith("```", StringComparison.Ordinal))
        {
            int firstLine = text.IndexOf('\n');
            string fence = firstLine >= 0 ? text.Substring(0, firstLine).Trim() : null;
            if (fence == "```" || string.Equals(fence, "```json", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(firstLine + 1, text.Length - firstLine - 4).Trim();
        }
        return JObject.Parse(text).ToString(Formatting.None);
    }

    public static Task<Result> RequestAsync(Provider primary, Provider backup, string system, string user, float temperature,
        int maxTokens, float seconds, CancellationToken cancellation, bool skipPrimary = false, string responseType = null, Func<string, bool> acceptCandidate = null)
        => RequestWithSenderAsync(primary, backup, system, user, temperature, maxTokens, seconds, cancellation, skipPrimary, Send, responseType, acceptCandidate);

    public static async Task<Result> RequestWithSenderAsync(Provider primary, Provider backup, string system, string user,
        float temperature, int maxTokens, float seconds, CancellationToken cancellation, bool skipPrimary, Sender sender, string responseType = null, Func<string, bool> acceptCandidate = null)
    {
        var result = new Result();
        bool backupReady = backup != null && backup.Ready;
        bool primaryReady = primary != null && primary.Ready && !(skipPrimary && backupReady);
        if (!primaryReady && !backupReady) { result.Error = "missing_key_or_provider"; return result; }
        if (cancellation.IsCancellationRequested) { result.Error = "canceled"; return result; }
        var clock = Stopwatch.StartNew();
        using (var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
            lifetime.CancelAfter(TimeSpan.FromSeconds(Math.Max(0.05f, seconds)));
            Task deadline = Task.Delay(Timeout.Infinite, lifetime.Token);
            var pending = new Dictionary<Task<Attempt>, Provider>();
            bool backupStarted = false;
            void Start(Provider route)
            {
                int limit = Math.Max(1, (int)Math.Ceiling(seconds - clock.Elapsed.TotalSeconds));
                pending.Add(AttemptAsync(route, BuildRequest(route, system, user, temperature, maxTokens, responseType)
                    .ToString(Formatting.None), limit, lifetime.Token, sender, acceptCandidate), route);
                if (route == backup) backupStarted = true;
            }
            if (primaryReady) Start(primary); else Start(backup);
            // Analysis is short; a stalled extraction must not consume the letter's final-screen budget.
            double delaySeconds = responseType == "analyze_system" ? 3 : SlowPrimaryBackupDelay;
            Task hedge = primaryReady && backupReady
                ? Task.Delay(TimeSpan.FromSeconds(Math.Min(delaySeconds, Math.Max(0.05, seconds / 3.0))), lifetime.Token)
                : null;
            try
            {
                while (pending.Count > 0)
                {
                    var waits = new List<Task> { deadline };
                    foreach (var task in pending.Keys) waits.Add(task);
                    if (hedge != null && !backupStarted) waits.Add(hedge);
                    Task finished = await Task.WhenAny(waits).ConfigureAwait(false);
                    if (lifetime.IsCancellationRequested || finished == deadline)
                    { result.Error = cancellation.IsCancellationRequested ? "canceled" : "timeout"; return result; }
                    if (finished == hedge)
                    {
                        result.Diagnostics.Add("Primary still pending; backup started after " + clock.Elapsed.TotalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " seconds.");
                        Start(backup);
                        hedge = null;
                        continue;
                    }
                    var completed = (Task<Attempt>)finished;
                    Provider route = pending[completed];
                    pending.Remove(completed);
                    Attempt attempt = await completed.ConfigureAwait(false);
                    result.ProviderId = route.Id;
                    result.Diagnostics.Add(route.Id + ": " + (attempt.Content != null ? "complete" : attempt.Error)
                        + ", HTTP " + attempt.Response.StatusCode + ", stage=" + attempt.Response.Stage
                        + ", received_bytes=" + attempt.Response.ReceivedBytes + Timing(attempt.Response));
                    if (attempt.Content != null)
                    {
                        result.Content = attempt.Content;
                        result.Error = null;
                        return result;
                    }
                    result.Error = attempt.Error;
                    if (attempt.RejectedContent != null) result.RejectedContent = attempt.RejectedContent;
                    if (route == primary && CanFailOver(result.Error))
                    {
                        result.PrimaryFailed = true;
                        if (backupReady && !backupStarted) { Start(backup); hedge = null; }
                    }
                    // No extra same-provider rewrite: the other route already has the exact evidence,
                    // and the client can repair a closing caption locally without a new network wait.
                }
                return result;
            }
            finally { lifetime.Cancel(); }
        }
    }

    private sealed class Attempt
    {
        public DreamLetterHttp.Response Response;
        public string Content, Error, RejectedContent;
    }

    private static async Task<Attempt> AttemptAsync(Provider provider, string request, int seconds, CancellationToken cancellation, Sender sender, Func<string, bool> acceptCandidate)
    {
        var attempt = new Attempt();
        try { attempt.Response = await sender(provider, request, seconds, cancellation).ConfigureAwait(false); }
        catch (OperationCanceledException) { attempt.Response = new DreamLetterHttp.Response { Error = "canceled" }; }
        catch (Exception exception) { attempt.Response = new DreamLetterHttp.Response { Error = "transport_" + exception.GetType().Name }; }
        var response = attempt.Response ?? new DreamLetterHttp.Response { Error = "empty_response" };
        attempt.Response = response;
        attempt.Error = response.Error ?? (response.Success ? null : "http_" + response.StatusCode);
        if (!response.Success) return attempt;
        if (!DreamLetterReply.TryRead(response.Body, out string content, out _, out string parseError, provider.Anthropic, provider.Responses))
        { attempt.Error = parseError; return attempt; }
        try { attempt.Content = NormalizeJson(content); attempt.Error = null; }
        catch (JsonException) { attempt.Error = "invalid_response"; attempt.RejectedContent = content; }
        if (attempt.Content != null && acceptCandidate != null)
        {
            bool accepted;
            try { accepted = acceptCandidate(attempt.Content); }
            catch (Exception) { accepted = false; }
            if (!accepted)
            { attempt.RejectedContent = attempt.Content; attempt.Content = null; attempt.Error = "validation_rejected"; }
        }
        return attempt;
    }

    public static bool CanFailOver(string error)
        => error == "timeout" || error == "stream_interrupted" || error == "response_truncated" || error == "empty_response"
            || error == "invalid_response" || error == "validation_rejected" || error == "incomplete_response" || error == "provider_overloaded"
            || error == "http_401" || error == "http_402" || error == "http_403" || error == "http_404" || error == "http_429"
            || error?.StartsWith("http_5") == true || error?.StartsWith("windows_https_") == true || error?.StartsWith("transport_") == true;

    private static string Timing(DreamLetterHttp.Response response) => ", seconds=" + response.ElapsedSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
        + ", first_byte_seconds=" + response.FirstByteSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

    private static async Task<DreamLetterHttp.Response> Send(Provider provider, string json, int seconds, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var request = DreamLetterHttp.PostAsync(json, provider.Key, seconds, true, provider.Endpoint, provider.Anthropic, provider.Responses);
        using (cancellation.Register(() => DreamLetterHttp.Cancel(request)))
            return await request.ConfigureAwait(false);
    }
}
