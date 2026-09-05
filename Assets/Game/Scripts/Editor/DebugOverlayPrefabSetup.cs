using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DebugOverlayPrefabSetup
{
    private const string PrefabPath = "Assets/Game/Prefabs/Managers/PersistentRoot.prefab";

    [MenuItem("Tools/UI/Build Debug Overlay")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void BuildBatch()
    {
        try
        {
            Build();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    private static void Build()
    {
        if (!File.Exists(PrefabPath))
        {
            throw new FileNotFoundException($"Missing prefab: {PrefabPath}");
        }

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var managers = root.transform.Find("Managers");
            var canvas = root.transform.Find("UI/Canvas_Debug");
            if (managers == null || canvas == null)
            {
                throw new System.InvalidOperationException("PersistentRoot requires Managers and UI/Canvas_Debug.");
            }

            var managerObject = managers.Find("DebugManager")?.gameObject;
            if (managerObject == null)
            {
                managerObject = new GameObject("DebugManager");
                managerObject.transform.SetParent(managers, false);
            }

            var manager = managerObject.GetComponent<DebugManager>() ??
                          managerObject.AddComponent<DebugManager>();

            SetupCanvas(canvas.gameObject);
            ClearChildren(canvas);

            var panel = CreateImage("PerformancePanel", canvas, new Color(0.015f, 0.025f, 0.04f, 0.82f));
            SetRect(
                panel.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -18f),
                new Vector2(330f, 245f),
                Vector2.one);

            var border = CreateImage("Accent", panel.transform, new Color(0.16f, 0.58f, 0.86f, 1f));
            SetRect(
                border.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(5f, 0f),
                new Vector2(0f, 0.5f));

            var textObject = CreateUiObject("PerformanceText", panel.transform);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = new Color(0.9f, 0.95f, 1f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.lineSpacing = 1.15f;
            text.text = "PERFORMANCE\nFPS        --\nFRAME      -- ms\nCPU        -- %\nMEMORY     -- MB\nMANAGED    -- MB\nSCENE      Boot";

            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 14f);
            textRect.offsetMax = new Vector2(-14f, -14f);
            textRect.localScale = Vector3.one;

            var hint = CreateUiObject("ToggleHint", panel.transform).AddComponent<Text>();
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.fontSize = 13;
            hint.color = new Color(0.5f, 0.6f, 0.7f, 1f);
            hint.alignment = TextAnchor.LowerRight;
            hint.raycastTarget = false;
            hint.text = "F3  HIDE";
            SetRect(
                hint.rectTransform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-12f, 8f),
                new Vector2(100f, 24f),
                new Vector2(1f, 0f));

            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("debugPanel").objectReferenceValue = panel.gameObject;
            serializedManager.FindProperty("performanceText").objectReferenceValue = text;
            serializedManager.FindProperty("sampleInterval").floatValue = 1f;
            serializedManager.FindProperty("visibleOnStart").boolValue = false;
            serializedManager.FindProperty("managedMemorySampleInterval").floatValue = 5f;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("Debug overlay UI built successfully.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetupCanvas(GameObject canvasObject)
    {
        var rect = canvasObject.transform as RectTransform;
        if (rect == null)
        {
            throw new System.InvalidOperationException("Canvas_Debug requires a RectTransform.");
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var image = CreateUiObject(name, parent).AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void ClearChildren(Transform parent)
    {
        for (var index = parent.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(parent.GetChild(index).gameObject);
        }
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
    }
}
