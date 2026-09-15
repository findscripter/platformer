using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>Consumes complete SSE responses without exposing incomplete JSON as a letter.</summary>
public static class DreamLetterReply
{
    public static bool TryRead(string response, out string content, out string finish, out string error, bool anthropic = false, bool responses = false)
    {
        if (responses) return TryResponses(response, out content, out finish, out error);
        if (anthropic) return TryAnthropic(response, out content, out finish, out error);
        content = null; finish = null; error = null;
        if (string.IsNullOrWhiteSpace(response)) { error = "empty_response"; return false; }
        try
        {
            if (response.TrimStart().StartsWith("{"))
            {
                var choice = (JObject.Parse(response)["choices"] as JArray)?.First as JObject;
                var message = choice?["message"] as JObject;
                content = message?["content"]?.Type == JTokenType.String ? message["content"].Value<string>() : null;
                finish = choice?["finish_reason"]?.Type == JTokenType.String ? choice["finish_reason"].Value<string>() : null;
            }
            else
            {
                var text = new StringBuilder();
                bool done = false;
                foreach (string raw in response.Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(":")) continue; // Provider queue heartbeat.
                    if (!line.StartsWith("data:")) continue;
                    string data = line.Substring(5).Trim();
                    if (data.Length == 0) continue;
                    if (data == "[DONE]") { done = true; break; }
                    var choice = (JObject.Parse(data)["choices"] as JArray)?.First as JObject;
                    var delta = choice?["delta"] as JObject;
                    if (delta?["content"]?.Type == JTokenType.String) text.Append(delta["content"].Value<string>());
                    if (choice?["finish_reason"]?.Type == JTokenType.String) finish = choice["finish_reason"].Value<string>();
                }
                if (!done) { error = "stream_interrupted"; return false; }
                content = text.ToString();
            }
            if (finish != "stop" || string.IsNullOrWhiteSpace(content))
            {
                error = finish == "length" ? "response_truncated" : "incomplete_response";
                return false;
            }
            return true;
        }
        catch (Exception e) when (e is JsonException || e is InvalidCastException || e is InvalidOperationException || e is ArgumentException)
        { error = "invalid_response"; return false; }
    }

    // A terminal event may be larger than a network chunk. Do not close until its complete JSON line arrives.
    public static bool HasResponseEnd(string stream)
    {
        int end = stream.LastIndexOf('\n');
        if (end < 0) return false;
        foreach (string raw in stream.Substring(0, end).Split('\n'))
        {
            string line = raw.Trim();
            if (!line.StartsWith("data:") || !(line.Contains("response.completed") || line.Contains("response.failed") || line.Contains("response.incomplete"))) continue;
            try
            {
                string type = (JObject.Parse(line.Substring(5))["type"] as JValue)?.Value as string;
                if (type == "response.completed" || type == "response.failed" || type == "response.incomplete") return true;
            }
            catch (JsonException) { }
        }
        return false;
    }

    private static string Text(JToken value) => value?.Type == JTokenType.String ? value.Value<string>() : null;
    private static bool TryResponses(string response, out string content, out string finish, out string error)
    {
        content = null; finish = null; error = null;
        if (string.IsNullOrWhiteSpace(response)) { error = "empty_response"; return false; }
        try
        {
            var chunks = new StringBuilder();
            JObject document = null;
            if (response.TrimStart().StartsWith("{")) document = JObject.Parse(response);
            else foreach (string raw in response.Split('\n'))
            {
                string line = raw.Trim();
                if (!line.StartsWith("data:")) continue;
                string data = line.Substring(5).Trim();
                if (data.Length == 0) continue;
                JObject item = JObject.Parse(data);
                string type = Text(item["type"]);
                if (type == "response.output_text.delta") chunks.Append(Text(item["delta"]));
                if (type == "error") { error = "invalid_response"; return false; }
                if (type == "response.completed" || type == "response.failed" || type == "response.incomplete")
                { document = item["response"] as JObject; break; }
            }
            if (document == null) { error = "stream_interrupted"; return false; }
            finish = Text(document["status"]);
            if (finish != "completed")
            {
                string code = Text((document["error"] as JObject)?["code"]);
                error = finish == "incomplete" ? "response_truncated" : code == "invalid_request_error" ? "http_400" : "invalid_response";
                return false;
            }
            var final = new StringBuilder();
            foreach (JToken output in document["output"] as JArray ?? new JArray())
                if (output is JObject message && Text(message["type"]) == "message")
                    foreach (JToken item in message["content"] as JArray ?? new JArray())
                        if (item is JObject block && Text(block["type"]) == "output_text") final.Append(Text(block["text"]));
            content = final.Length > 0 ? final.ToString() : chunks.ToString();
            if (string.IsNullOrWhiteSpace(content)) { error = "empty_response"; return false; }
            return true;
        }
        catch (Exception e) when (e is JsonException || e is InvalidCastException || e is InvalidOperationException || e is ArgumentException)
        { error = "invalid_response"; return false; }
    }

    private static bool TryAnthropic(string response, out string content, out string finish, out string error)
    {
        content = null; finish = null; error = null;
        if (string.IsNullOrWhiteSpace(response)) { error = "empty_response"; return false; }
        try
        {
            var text = new StringBuilder();
            bool done = false;
            if (response.TrimStart().StartsWith("{"))
            {
                JObject document = JObject.Parse(response);
                if (document["error"] != null) { error = "provider_overloaded"; return false; }
                foreach (JObject block in document["content"] as JArray ?? new JArray())
                    if (block["type"]?.Value<string>() == "text") text.Append(block["text"]?.Value<string>());
                finish = document["stop_reason"]?.Value<string>(); done = true;
            }
            else foreach (string raw in response.Split('\n'))
            {
                string line = raw.Trim();
                if (!line.StartsWith("data:")) continue;
                string data = line.Substring(5).Trim();
                if (data.Length == 0) continue;
                JObject item = JObject.Parse(data);
                string type = item["type"]?.Value<string>();
                if (type == "error") { error = "provider_overloaded"; return false; }
                if (type == "content_block_start" && item["content_block"]?["type"]?.Value<string>() == "text") text.Append(item["content_block"]["text"]?.Value<string>());
                if (type == "content_block_delta" && item["delta"]?["type"]?.Value<string>() == "text_delta") text.Append(item["delta"]["text"]?.Value<string>());
                if (type == "message_delta") finish = item["delta"]?["stop_reason"]?.Value<string>() ?? finish;
                if (type == "message_stop") { done = true; break; }
            }
            if (!done) { error = "stream_interrupted"; return false; }
            if (finish != "end_turn" || text.Length == 0) { error = finish == "max_tokens" ? "response_truncated" : "incomplete_response"; return false; }
            content = text.ToString(); return true;
        }
        catch (Exception e) when (e is JsonException || e is InvalidCastException || e is InvalidOperationException || e is ArgumentException)
        { error = "invalid_response"; return false; }
    }
}
