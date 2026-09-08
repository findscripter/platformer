#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in frame timing probe for a complete tarot interaction; excluded from builds.</summary>
public sealed class TarotFrameProbe : MonoBehaviour
{
    private readonly List<float> milliseconds = new List<float>(2048);
    private int skipFrames = 3;
    private TarotDrawController target;
    private System.Reflection.FieldInfo displayProgress;
    private bool pauseAtResults;
    public bool Finished { get; private set; }
    public int Samples => milliseconds.Count;
    public float MaximumMs { get; private set; }
    public float P95Ms { get; private set; }

    public static TarotFrameProbe Begin(bool pauseForScreenshot = false)
    {
        var go = new GameObject("TarotFrameProbe");
        DontDestroyOnLoad(go);
        var probe = go.AddComponent<TarotFrameProbe>();
        probe.target = Object.FindAnyObjectByType<TarotDrawController>();
        probe.displayProgress = typeof(TarotDrawController).GetField("resultDisplayProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        probe.pauseAtResults = pauseForScreenshot;
        return probe;
    }

    private void Update()
    {
        if (Finished) return;
        if (pauseAtResults && target != null && (float)displayProgress.GetValue(target) >= 1f)
        {
            pauseAtResults = false;
            UnityEditor.EditorApplication.isPaused = true;
        }
        if (SceneManager.GetActiveScene().name != "PlayerGuide")
        {
            Finished = true;
            milliseconds.Sort();
            if (milliseconds.Count > 0)
            {
                MaximumMs = milliseconds[milliseconds.Count - 1];
                P95Ms = milliseconds[Mathf.FloorToInt((milliseconds.Count - 1) * 0.95f)];
            }
            return;
        }
        if (skipFrames-- > 0) return;
        milliseconds.Add(Time.unscaledDeltaTime * 1000f);
    }
}
#endif
