#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Media;
using UnityEngine;

/// <summary>按需录制真实 Game View；不进入 Player 构建，不修改场景文件。</summary>
public sealed class PlaytestVideoRecorder : MonoBehaviour
{
    [Serializable] public sealed class Sample
    {
        public float seconds;
        public Vector3 playerPosition;
        public string state;
        public string route;
        public bool[] tarotNodes;
    }
    [Serializable] public sealed class Report
    {
        public string video;
        public string mode = "Unity Play Mode, automated movement/jump/interaction, silent video";
        public string stopped;
        public int frames;
        public int fps = 30;
        public int width;
        public int height;
        public bool routeFinished;
        public bool routeFailed;
        public List<Sample> samples = new List<Sample>();
        public List<string> errors = new List<string>();
    }

    public static PlaytestVideoRecorder Current { get; private set; }
    public bool Recording { get; private set; }
    public Report Result { get; private set; }
    private MediaEncoder encoder;
    private RenderTexture target;
    private RenderTexture upright;
    private Texture2D pixels;
    private int previousCaptureRate;
    private int previousTargetRate;
    private bool previousBackground;
    private int maxFrames;
    private int completedAt = -1;
    private string lastRoute;
    private string lastState;

    public static PlaytestVideoRecorder Begin(string path, int maximumSeconds = 260)
    {
        if (Current != null && Current.Recording)
            throw new InvalidOperationException("A playtest is already being recorded.");
        if (!Application.isPlaying)
            throw new InvalidOperationException("Recording requires Play Mode.");
        var owner = new GameObject("PlaytestVideoRecorder");
        DontDestroyOnLoad(owner);
        var recorder = owner.AddComponent<PlaytestVideoRecorder>();
        Current = recorder;
        recorder.Initialize(path, maximumSeconds);
        return recorder;
    }

    private void Initialize(string path, int maximumSeconds)
    {
        Result = new Report { video = Path.GetFullPath(path), width = Screen.width, height = Screen.height };
        Directory.CreateDirectory(Path.GetDirectoryName(Result.video));
        previousCaptureRate = Time.captureFramerate;
        previousTargetRate = Application.targetFrameRate;
        previousBackground = Application.runInBackground;
        try
        {
            encoder = new MediaEncoder(Result.video, new VideoTrackAttributes
            {
                frameRate = new MediaRational(Result.fps),
                width = (uint)Result.width,
                height = (uint)Result.height,
                includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.High
            });
            target = new RenderTexture(Result.width, Result.height, 0, RenderTextureFormat.ARGB32);
            target.Create();
            upright = new RenderTexture(Result.width, Result.height, 0, RenderTextureFormat.ARGB32);
            upright.Create();
            pixels = new Texture2D(Result.width, Result.height, TextureFormat.RGBA32, false);
            maxFrames = maximumSeconds * Result.fps;
            Time.captureFramerate = Result.fps;
            Application.targetFrameRate = Result.fps;
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
            Recording = true;
            StartCoroutine(Capture());
        }
        catch
        {
            StopRecording("initialization failed");
            throw;
        }
    }

    private IEnumerator Capture()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (Recording)
        {
            yield return endOfFrame;
            if (!Recording) yield break;
            try
            {
                if (Screen.width != Result.width || Screen.height != Result.height)
                    throw new InvalidOperationException("Game View resolution changed during recording.");
                ScreenCapture.CaptureScreenshotIntoRenderTexture(target);
                Graphics.Blit(target, upright, new Vector2(1f, SystemInfo.graphicsUVStartsAtTop ? -1f : 1f),
                    new Vector2(0f, SystemInfo.graphicsUVStartsAtTop ? 1f : 0f));
                var previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = upright;
                    pixels.ReadPixels(new Rect(0, 0, Result.width, Result.height), 0, 0, false);
                    pixels.Apply(false);
                }
                finally { RenderTexture.active = previous; }
                if (!encoder.AddFrame(pixels))
                    throw new InvalidOperationException("Video encoder rejected a frame.");
                Result.frames++;
                CaptureState();
                if (Result.frames == 30 || Result.frames % 900 == 0)
                    File.WriteAllBytes(Path.ChangeExtension(Result.video, null) + "-frame-" + Result.frames + ".png", pixels.EncodeToPNG());
                if (Result.frames >= maxFrames || (completedAt >= 0 && Result.frames >= completedAt + 90))
                    StopRecording(completedAt >= 0 ? "route ended" : "time limit");
            }
            catch (Exception exception)
            {
                Result.errors.Add(exception.ToString());
                StopRecording("capture failed");
            }
        }
    }

    private void CaptureState()
    {
        var walker = UnityEngine.Object.FindFirstObjectByType<MainRouteWalker>();
        var context = GameLoop.Instance != null ? GameLoop.Instance.Context : null;
        string route = walker != null ? walker.Status : "none";
        string state = context != null ? context.StateMachine.CurrentStateType.ToString() : "none";
        if (Result.frames % 30 == 0 || lastRoute != route || lastState != state)
        {
            var nodes = new bool[4];
            for (int i = 0; i < nodes.Length; i++)
                nodes[i] = context != null && context.TarotResult != null && context.TarotResult.IsNodeActivated(i);
            Result.samples.Add(new Sample
            {
                seconds = (float)Result.frames / Result.fps,
                playerPosition = context != null && context.Player != null ? context.Player.transform.position : Vector3.zero,
                route = route,
                state = state,
                tarotNodes = nodes
            });
            lastRoute = route;
            lastState = state;
        }
        if (walker != null && walker.Finished)
        {
            Result.routeFinished = true;
            Result.routeFailed = walker.Failed;
            if (completedAt < 0) completedAt = Result.frames;
        }
    }

    private void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Result.errors.Add(message + "\n" + trace);
    }

    public void StopRecording(string reason = "stopped")
    {
        Recording = false;
        Application.logMessageReceived -= OnLog;
        try { if (encoder != null) encoder.Dispose(); }
        finally
        {
            encoder = null;
            Time.captureFramerate = previousCaptureRate;
            Application.targetFrameRate = previousTargetRate;
            Application.runInBackground = previousBackground;
            if (RenderTexture.active == target || RenderTexture.active == upright)
                RenderTexture.active = null;
            if (target != null) { target.Release(); Destroy(target); target = null; }
            if (upright != null) { upright.Release(); Destroy(upright); upright = null; }
            if (pixels != null) { Destroy(pixels); pixels = null; }
        }
        if (Result != null)
        {
            Result.stopped = reason;
            File.WriteAllText(Path.ChangeExtension(Result.video, ".json"), JsonUtility.ToJson(Result, true));
        }
    }

    private void OnDisable() { if (Recording || encoder != null) StopRecording("Play Mode stopped"); }
}
#endif
