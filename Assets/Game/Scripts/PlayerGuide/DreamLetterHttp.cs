using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Cancelable Windows HTTPS with normal system certificate and hostname checks.</summary>
public static class DreamLetterHttp
{
    public sealed class Response
    {
        public int StatusCode;
        public string Body;
        public string Error;
        public string Stage;
        public int ReceivedBytes;
        public double ElapsedSeconds, FirstByteSeconds = -1;
        public bool Success => Error == null && StatusCode >= 200 && StatusCode < 300;
    }
    public const string Backend = "Windows HTTPS";
    private static readonly object ActiveGate = new object();
    private static readonly HashSet<Operation> Active = new HashSet<Operation>();
    public static int ActiveRequestCount { get { lock (ActiveGate) return Active.Count; } }

    static DreamLetterHttp()
    {
#if UNITY_5_3_OR_NEWER
        UnityEngine.Application.quitting += CancelAll;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += CancelAll;
        UnityEditor.EditorApplication.quitting += CancelAll;
#endif
#endif
    }
    public static Task<Response> PostAsync(string json, string key, int timeoutSeconds, bool streaming = false,
        string endpoint = "https://api.deepseek.com/chat/completions", bool anthropic = false, bool responses = false)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Contains("\r") || key.Contains("\n"))
            return Task.FromResult(new Response { Error = "missing_or_invalid_key" });
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri destination) || destination.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(destination.UserInfo) || !string.IsNullOrEmpty(destination.Fragment)
            || !string.IsNullOrEmpty(destination.Query))
            return Task.FromResult(new Response { Error = "invalid_endpoint" });
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        var operation = new Operation(streaming, anthropic, responses);
        lock (ActiveGate) Active.Add(operation);
        operation.Begin(json, key, Math.Max(1, timeoutSeconds), destination);
        return operation.Completion.Task;
#else
        return Task.FromResult(new Response { Error = "windows_build_required" });
#endif
    }
    public static void CancelAll()
    {
        Operation[] pending;
        lock (ActiveGate) { pending = new Operation[Active.Count]; Active.CopyTo(pending); }
        var tasks = new Task[pending.Length];
        for (int i = 0; i < pending.Length; i++)
        {
            pending[i].Cancel("canceled");
            tasks[i] = pending[i].Completion.Task;
        }
        // Drain HANDLE_CLOSING before domain unload; never wait indefinitely for the network.
        if (tasks.Length > 0) Task.WaitAll(tasks, 1500);
    }
    public static void Cancel(Task<Response> task)
    {
        if (task == null) return;
        Operation match = null;
        lock (ActiveGate)
            foreach (Operation operation in Active)
                if (operation.Completion.Task == task) { match = operation; break; }
        match?.Cancel("canceled");
    }

    private sealed class Operation
    {
        internal readonly TaskCompletionSource<Response> Completion = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object gate = new object();
        private readonly bool streaming;
        private readonly bool anthropic;
        private readonly bool responses;
        private readonly MemoryStream received = new MemoryStream();
        private readonly System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
        private double firstByteSeconds = -1;
        private int responseCheckedBytes;
        private readonly byte[] chunk = new byte[32768];
        private IntPtr session, connection, request, input, output;
        private GCHandle root;
        private Timer deadline;
        private bool callbackRegistered, finishing, released;
        private int statusCode;
        private string stage = "setup";
        private Response result;
        internal Operation(bool isStreaming, bool useAnthropic, bool useResponses) { streaming = isStreaming; anthropic = useAnthropic; responses = useResponses; }

        internal void Begin(string json, string key, int seconds, Uri destination)
        {
            lock (gate)
            {
                if (finishing) return;
                try
                {
                    // Async handles support cancellation; do not wrap a synchronous request in Task.Run.
                    session = WinHttpOpen("Dreamremains/1.0", 4, null, null, 0x10000000);
                    if (session == IntPtr.Zero) { FailNative(); return; }
                    // Some Responses relays buffer long reports before the first body byte. The total deadline still bounds the request.
                    if (!WinHttpSetTimeouts(session, 5000, 8000, 10000, Math.Min(seconds, responses ? 60 : 20) * 1000)) { FailNative(); return; }
                    connection = WinHttpConnect(session, destination.DnsSafeHost, (ushort)destination.Port, 0);
                    if (connection == IntPtr.Zero) { FailNative(); return; }
                    request = WinHttpOpenRequest(connection, "POST", destination.AbsolutePath, null, null, IntPtr.Zero, 0x00800000);
                    if (request == IntPtr.Zero) { FailNative(); return; }
                    uint neverRedirect = 0;
                    if (!WinHttpSetOption(request, 88, ref neverRedirect, 4)) { FailNative(); return; }
                    root = GCHandle.Alloc(this);
                    IntPtr context = GCHandle.ToIntPtr(root);
                    if (!SetContext(request, 45, ref context, (uint)IntPtr.Size)) { FailNative(); return; }
                    if (WinHttpSetStatusCallback(request, Callback, 0x00400000 | 0x00020000 | 0x00040000 | 0x00080000 | 0x00200000 | 0x00000800, UIntPtr.Zero) == new IntPtr(-1)) { FailNative(); return; }
                    callbackRegistered = true;
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    input = Marshal.AllocHGlobal(bytes.Length);
                    Marshal.Copy(bytes, 0, input, bytes.Length);
                    output = Marshal.AllocHGlobal(chunk.Length);
                    deadline = new Timer(_ => Cancel("timeout"), null, seconds * 1000, Timeout.Infinite);
                    stage = "sending";
                    string headers = "Content-Type: application/json; charset=utf-8\r\n" + (anthropic
                        ? "x-api-key: " + key + "\r\nanthropic-version: 2023-06-01\r\n"
                        : "Authorization: Bearer " + key + "\r\n");
                    if (!WinHttpSendRequest(request, headers, (uint)headers.Length, input, (uint)bytes.Length, (uint)bytes.Length, context)) FailNative();
                }
                catch (Exception exception) { Finish(new Response { Error = "transport_" + exception.GetType().Name }); }
            }
        }
        internal void Cancel(string reason) { lock (gate) Finish(new Response { Error = reason }); }
        internal void OnStatus(uint status, IntPtr data, uint length)
        {
            lock (gate)
            {
                if (status == 0x00000800) { Release(); return; }
                if (finishing) return;
                try
                {
                    if (status == 0x00400000)
                    {
                        stage = "response_headers";
                        if (!WinHttpReceiveResponse(request, IntPtr.Zero)) FailNative();
                    }
                    else if (status == 0x00020000)
                    {
                        uint code, size = 4;
                        if (!WinHttpQueryHeaders(request, 19 | 0x20000000, null, out code, ref size, IntPtr.Zero)) { FailNative(); return; }
                        statusCode = (int)code;
                        stage = "response_body";
                        ReadNext();
                    }
                    else if (status == 0x00040000)
                    {
                        if (data == IntPtr.Zero || length < 4) { Finish(new Response { Error = "transport_invalid_available_count" }); return; }
                        uint available = unchecked((uint)Marshal.ReadInt32(data));
                        uint count = available > 0 ? Math.Min(available, (uint)chunk.Length) : 1;
                        if (!WinHttpReadData(request, output, count, IntPtr.Zero)) FailNative();
                    }
                    else if (status == 0x00080000)
                    {
                        if (length == 0) { FinishBody(); return; }
                        if (firstByteSeconds < 0) firstByteSeconds = elapsed.Elapsed.TotalSeconds;
                        if (length > chunk.Length || received.Length + length > 2 * 1024 * 1024) { Finish(new Response { Error = "response_too_large" }); return; }
                        Marshal.Copy(data, chunk, 0, (int)length);
                        received.Write(chunk, 0, (int)length);
                        if (streaming)
                        {
                            int start = Math.Max(0, (int)received.Length - 512);
                            string tail = Encoding.UTF8.GetString(received.GetBuffer(), start, (int)received.Length - start);
                            bool done = responses ? ResponseEnded()
                                : anthropic ? System.Text.RegularExpressions.Regex.IsMatch(tail, @"""type""\s*:\s*""message_stop""\s*}") : tail.Contains("data: [DONE]");
                            if (done) { FinishBody(); return; }
                        }
                        ReadNext();
                    }
                    else if (status == 0x00200000)
                    {
                        int code = data != IntPtr.Zero && length >= IntPtr.Size + 4 ? Marshal.ReadInt32(data, IntPtr.Size) : 0;
                        Finish(new Response { Error = NativeError(code) });
                    }
                }
                catch (Exception exception) { Finish(new Response { Error = "transport_" + exception.GetType().Name }); }
            }
        }
        private void ReadNext()
        {
            // Read only available bytes: filling a fixed buffer delays small SSE events and terminal markers.
            // Queue the next step so synchronously completed small reads cannot recurse through callbacks.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (gate)
                {
                    if (finishing) return;
                    if (!WinHttpQueryDataAvailable(request, IntPtr.Zero)) FailNative();
                }
            });
        }
        private bool ResponseEnded()
        {
            byte[] bytes = received.GetBuffer();
            int end = (int)received.Length - 1;
            while (end >= responseCheckedBytes && bytes[end] != 10) end--;
            if (end < responseCheckedBytes) return false;
            string completeLines = Encoding.UTF8.GetString(bytes, responseCheckedBytes, end - responseCheckedBytes + 1);
            responseCheckedBytes = end + 1;
            return DreamLetterReply.HasResponseEnd(completeLines);
        }
        private void FailNative() { Finish(new Response { Error = NativeError(Marshal.GetLastWin32Error()) }); }
        private void FinishBody() { Finish(new Response { Body = Encoding.UTF8.GetString(received.ToArray()) }); }
        private void Finish(Response response)
        {
            if (finishing) return;
            finishing = true;
            result = response;
            result.StatusCode = statusCode;
            result.Stage = stage;
            result.ReceivedBytes = (int)received.Length;
            result.ElapsedSeconds = elapsed.Elapsed.TotalSeconds;
            result.FirstByteSeconds = firstByteSeconds;
            deadline?.Dispose();
            deadline = null;
            if (request != IntPtr.Zero)
            {
                IntPtr closing = request;
                request = IntPtr.Zero;
                WinHttpCloseHandle(closing);
                if (callbackRegistered) return;
            }
            Release();
        }
        private void Release()
        {
            if (released) return;
            released = true;
            if (connection != IntPtr.Zero) { WinHttpCloseHandle(connection); connection = IntPtr.Zero; }
            if (session != IntPtr.Zero) { WinHttpCloseHandle(session); session = IntPtr.Zero; }
            if (input != IntPtr.Zero) { Marshal.FreeHGlobal(input); input = IntPtr.Zero; }
            if (output != IntPtr.Zero) { Marshal.FreeHGlobal(output); output = IntPtr.Zero; }
            if (root.IsAllocated) root.Free();
            received.Dispose();
            lock (ActiveGate) Active.Remove(this);
            Completion.TrySetResult(result ?? new Response { Error = "canceled", Stage = stage });
        }
    }
    private static string NativeError(int code) => code == 12002 ? "timeout" : "windows_https_" + code;
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void StatusCallback(IntPtr request, IntPtr context, uint status, IntPtr data, uint length);
    private static readonly StatusCallback Callback = OnNativeStatus;
#if UNITY_5_3_OR_NEWER
    [AOT.MonoPInvokeCallback(typeof(StatusCallback))]
#endif
    private static void OnNativeStatus(IntPtr request, IntPtr context, uint status, IntPtr data, uint length)
    {
        if (context == IntPtr.Zero) return;
        // Root is released only by HANDLE_CLOSING, the last callback for this handle.
        var operation = GCHandle.FromIntPtr(context).Target as Operation;
        operation?.OnStatus(status, data, length);
    }
    [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr WinHttpOpen(string agent, uint access, string proxy, string bypass, uint flags);
    [DllImport("winhttp.dll", SetLastError = true)]
    private static extern bool WinHttpSetTimeouts(IntPtr session, int resolve, int connect, int send, int receive);
    [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr WinHttpConnect(IntPtr session, string server, ushort port, uint reserved);
    [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr WinHttpOpenRequest(IntPtr connection, string verb, string path, string version, string referer, IntPtr acceptTypes, uint flags);
    [DllImport("winhttp.dll", SetLastError = true)]
    private static extern bool WinHttpSetOption(IntPtr request, uint option, ref uint value, uint size);
    [DllImport("winhttp.dll", EntryPoint = "WinHttpSetOption", SetLastError = true)]
    private static extern bool SetContext(IntPtr request, uint option, ref IntPtr value, uint size);
    [DllImport("winhttp.dll", SetLastError = true)]
    private static extern IntPtr WinHttpSetStatusCallback(IntPtr request, StatusCallback callback, uint flags, UIntPtr reserved);
    [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool WinHttpSendRequest(IntPtr request, string headers, uint headersLength, IntPtr data, uint length, uint totalLength, IntPtr context);
    [DllImport("winhttp.dll", SetLastError = true)]
    private static extern bool WinHttpReceiveResponse(IntPtr request, IntPtr reserved);
    [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool WinHttpQueryHeaders(IntPtr request, uint info, string name, out uint buffer, ref uint length, IntPtr index);
    [DllImport("winhttp.dll", SetLastError = true)]
    private static extern bool WinHttpReadData(IntPtr request, IntPtr buffer, uint count, IntPtr read);
    [DllImport("winhttp.dll", SetLastError = true)]
    private static extern bool WinHttpQueryDataAvailable(IntPtr request, IntPtr available);
    [DllImport("winhttp.dll", SetLastError = true)]
    private static extern bool WinHttpCloseHandle(IntPtr handle);
}
