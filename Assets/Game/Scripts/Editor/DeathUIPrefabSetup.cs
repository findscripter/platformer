using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DeathUIPrefabSetup
{
    private const string PrefabPath =
        "Assets/Game/Scripts/Managers/PersistentRoot.prefab";

    private static readonly Color OverlayColor = new(0.08f, 0.005f, 0.01f, 0.72f);
    private static readonly Color PanelColor = new(0.12f, 0.025f, 0.035f, 0.96f);
    private static readonly Color AccentColor = new(0.82f, 0.12f, 0.16f, 1f);
    private static readonly Color TextColor = new(1f, 0.93f, 0.93f, 1f);
    private static readonly Color MutedTextColor = new(0.82f, 0.65f, 0.67f, 1f);

    static DeathUIPrefabSetup()
    {
        EditorApplication.delayCall += BuildIfNeeded;
    }

    [MenuItem("Tools/UI/Build Death UI")]
    public static void BuildFromMenu()
    {
        Build();
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(PrefabPath))
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        UIManager manager = prefab != null
            ? prefab.GetComponentInChildren<UIManager>(true)
            : null;
        Transform deathPanel = prefab != null
            ? prefab.transform.Find("UI/Canvas_Menu/DeathPanel")
            : null;

        if (manager == null || deathPanel == null)
        {
            Build();
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        if (serializedManager.FindProperty("deathPanel").objectReferenceValue == null)
        {
            Build();
        }
    }

    private static void Build()
    {
        if (!File.Exists(PrefabPath))
            throw new FileNotFoundException($"Missing prefab: {PrefabPath}");

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform canvas = root.transform.Find("UI/Canvas_Menu");
            if (canvas == null)
                throw new System.InvalidOperationException(
                    "Missing UI/Canvas_Menu in PersistentRoot prefab.");

            Transform deathPanel = canvas.Find("DeathPanel");
            if (deathPanel != null && deathPanel is not RectTransform)
            {
                Object.DestroyImmediate(deathPanel.gameObject);
                deathPanel = null;
            }

            if (deathPanel == null)
            {
                deathPanel = CreateUiObject("DeathPanel", canvas).transform;
            }

            ClearChildren(deathPanel);
            Stretch((RectTransform)deathPanel);

            Image overlay = deathPanel.GetComponent<Image>() ??
                            deathPanel.gameObject.AddComponent<Image>();
            overlay.color = OverlayColor;
            overlay.raycastTarget = true;

            Image card = CreateImage("DeathCard", deathPanel, PanelColor);
            SetRect(
                card.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(620f, 300f),
                new Vector2(0.5f, 0.5f));

            Image accent = CreateImage("Accent", card.transform, AccentColor);
            SetRect(
                accent.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -5f),
                new Vector2(0f, 10f),
                new Vector2(0.5f, 0.5f));

            Text title = CreateText(
                "Title",
                card.transform,
                "你已死亡",
                58,
                FontStyle.Bold,
                TextColor);
            SetRect(
                title.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -105f),
                new Vector2(520f, 85f),
                new Vector2(0.5f, 0.5f));

            Text message = CreateText(
                "Message",
                card.transform,
                "正在返回最近的出生点…",
                24,
                FontStyle.Normal,
                MutedTextColor);
            SetRect(
                message.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -190f),
                new Vector2(520f, 48f),
                new Vector2(0.5f, 0.5f));

            UIManager uiManager =
                root.transform.Find("Managers/UIManager")?.GetComponent<UIManager>();
            if (uiManager == null)
                throw new System.InvalidOperationException(
                    "Missing Managers/UIManager in PersistentRoot prefab.");

            SerializedObject serializedManager = new SerializedObject(uiManager);
            serializedManager.FindProperty("deathPanel").objectReferenceValue =
                deathPanel.gameObject;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            deathPanel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("Death UI built successfully.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string content,
        int size,
        FontStyle style,
        Color color)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        Text text = gameObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(parent.GetChild(index).gameObject);
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
