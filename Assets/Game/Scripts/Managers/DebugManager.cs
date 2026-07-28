using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Text performanceText;

    [Header("Sampling")]
    [SerializeField, Min(0.25f)] private float sampleInterval = 1f;
    [SerializeField, Min(1f)] private float managedMemorySampleInterval = 5f;
    [SerializeField] private bool visibleOnStart;

    private readonly Dictionary<string, string> customMetrics = new();
    private readonly StringBuilder displayBuilder = new(256);

    private GameObject debugCanvasRoot;
    private string lastDisplayText;
    private float sampleTimer;
    private float managedMemoryTimer;
    private int frameCount;
    private float frameTimeTotal;
    private float fps;
    private float frameTimeMs;
    private float processMemoryMb;
    private float managedMemoryMb;
    private bool isVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveDebugCanvasRoot();
        SetVisible(visibleOnStart);
    }

    private void Update()
    {
        if (Keyboard.current?.f3Key.wasPressedThisFrame == true)
        {
            ToggleVisible();
        }

        if (!isVisible)
        {
            return;
        }

        frameCount++;
        frameTimeTotal += Time.unscaledDeltaTime;
        sampleTimer += Time.unscaledDeltaTime;
        managedMemoryTimer += Time.unscaledDeltaTime;

        if (sampleTimer < sampleInterval)
        {
            return;
        }

        SamplePerformance(managedMemoryTimer >= managedMemorySampleInterval);
        if (managedMemoryTimer >= managedMemorySampleInterval)
        {
            managedMemoryTimer = 0f;
        }

        RefreshDisplay();
        ResetSamplingWindow();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ToggleVisible()
    {
        SetVisible(!isVisible);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (debugCanvasRoot != null)
        {
            debugCanvasRoot.SetActive(visible);
        }
        else if (debugPanel != null)
        {
            debugPanel.SetActive(visible);
        }

        lastDisplayText = null;

        if (visible)
        {
            ResetSamplingWindow();
            managedMemoryTimer = managedMemorySampleInterval;
        }
    }

    public void SetMetric(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        customMetrics[key] = value ?? string.Empty;
        lastDisplayText = null;
    }

    public void RemoveMetric(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            customMetrics.Remove(key);
            lastDisplayText = null;
        }
    }

    public void ClearMetrics()
    {
        customMetrics.Clear();
        lastDisplayText = null;
    }

    public void Log(string message)
    {
        Debug.Log($"[DebugManager] {message}");
    }

    private void ResolveDebugCanvasRoot()
    {
        if (debugPanel == null)
        {
            return;
        }

        var canvas = debugPanel.GetComponentInParent<Canvas>();
        debugCanvasRoot = canvas != null ? canvas.gameObject : debugPanel;
    }

    private void SamplePerformance(bool sampleManagedMemory)
    {
        var elapsed = Mathf.Max(frameTimeTotal, 0.0001f);
        fps = frameCount / elapsed;
        frameTimeMs = elapsed * 1000f / Mathf.Max(frameCount, 1);
        processMemoryMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

        if (sampleManagedMemory)
        {
            managedMemoryMb = GC.GetTotalMemory(false) / (1024f * 1024f);
        }
    }

    private void ResetSamplingWindow()
    {
        frameCount = 0;
        frameTimeTotal = 0f;
        sampleTimer = 0f;
    }

    private void RefreshDisplay()
    {
        if (performanceText == null)
        {
            return;
        }

        displayBuilder.Clear();
        displayBuilder.AppendLine("PERFORMANCE");
        AppendFixed1(displayBuilder, "FPS        ", fps);
        displayBuilder.AppendLine();
        AppendFixed1(displayBuilder, "FRAME      ", frameTimeMs);
        displayBuilder.AppendLine(" ms");
        AppendFixed1(displayBuilder, "ALLOCATED  ", processMemoryMb);
        displayBuilder.AppendLine(" MB");
        AppendFixed1(displayBuilder, "MANAGED    ", managedMemoryMb);
        displayBuilder.AppendLine(" MB");
        displayBuilder.Append("SCENE      ").Append(SceneManager.GetActiveScene().name);

        foreach (var metric in customMetrics)
        {
            displayBuilder.AppendLine();
            displayBuilder.Append(metric.Key.ToUpperInvariant()).Append("    ").Append(metric.Value);
        }

        var text = displayBuilder.ToString();
        if (text == lastDisplayText)
        {
            return;
        }

        lastDisplayText = text;
        performanceText.text = text;
    }

    private static void AppendFixed1(StringBuilder builder, string label, float value)
    {
        builder.Append(label);
        var scaled = Mathf.RoundToInt(value * 10f);
        if (scaled < 0)
        {
            builder.Append('-');
            scaled = -scaled;
        }

        var whole = scaled / 10;
        var fraction = scaled % 10;
        builder.Append(whole).Append('.').Append(fraction);
    }
}
