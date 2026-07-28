using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LevelClearPrefabSetup
{
    private const string PrefabPath = "Assets/Game/Scripts/Managers/PersistentRoot.prefab";

    private static readonly Color OverlayColor = new(0.015f, 0.025f, 0.045f, 0.82f);
    private static readonly Color PanelColor = new(0.06f, 0.1f, 0.14f, 0.98f);
    private static readonly Color AccentColor = new(0.2f, 0.72f, 0.42f, 1f);
    private static readonly Color ButtonColor = new(0.14f, 0.52f, 0.78f, 1f);
    private static readonly Color TextColor = new(0.94f, 0.98f, 1f, 1f);
    private static readonly Color MutedTextColor = new(0.65f, 0.74f, 0.8f, 1f);

    static LevelClearPrefabSetup()
    {
        EditorApplication.delayCall += BuildIfNeeded;
    }

    [MenuItem("Tools/UI/Build Level Clear Menu")]
    public static void BuildFromMenu()
    {
        Build();
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(PrefabPath))
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null && prefab.GetComponentInChildren<LevelClearUIController>(true) != null)
            return;

        Build();
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
                throw new System.InvalidOperationException("Missing UI/Canvas_Menu in PersistentRoot prefab.");

            Transform resultPanel = canvas.Find("ResultPanel");
            if (resultPanel != null && resultPanel is not RectTransform)
            {
                Object.DestroyImmediate(resultPanel.gameObject);
                resultPanel = null;
            }

            if (resultPanel == null)
            {
                resultPanel = CreateUiObject("ResultPanel", canvas).transform;
            }

            ClearChildren(resultPanel);
            Stretch((RectTransform)resultPanel);

            LevelClearUIController controller =
                resultPanel.GetComponent<LevelClearUIController>() ??
                resultPanel.gameObject.AddComponent<LevelClearUIController>();

            Image overlay = resultPanel.GetComponent<Image>() ??
                            resultPanel.gameObject.AddComponent<Image>();
            overlay.color = OverlayColor;
            overlay.raycastTarget = true;

            Image card = CreateImage("ClearCard", resultPanel, PanelColor);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600f, 430f), new Vector2(0.5f, 0.5f));

            Image accent = CreateImage("Accent", card.transform, AccentColor);
            SetRect(accent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -5f), new Vector2(0f, 10f), new Vector2(0.5f, 0.5f));

            Text title = CreateText("Title", card.transform, "关卡通过", 54, FontStyle.Bold, TextColor);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -105f), new Vector2(500f, 80f), new Vector2(0.5f, 0.5f));

            Text message = CreateText(
                "Message",
                card.transform,
                "你已成功到达终点",
                24,
                FontStyle.Normal,
                MutedTextColor);
            SetRect(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -180f), new Vector2(500f, 45f), new Vector2(0.5f, 0.5f));

            Button mainMenuButton =
                CreateButton(card.transform, "MainMenuButton", "返回主菜单", ButtonColor, TextColor);
            SetRect(mainMenuButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 85f), new Vector2(360f, 72f), new Vector2(0.5f, 0.5f));
            UnityEventTools.AddPersistentListener(mainMenuButton.onClick, controller.ReturnToMainMenu);

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("mainMenuButton").objectReferenceValue = mainMenuButton;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            UIManager uiManager = root.transform.Find("Managers/UIManager")?.GetComponent<UIManager>();
            if (uiManager == null)
                throw new System.InvalidOperationException("Missing Managers/UIManager in PersistentRoot prefab.");

            SerializedObject serializedUiManager = new SerializedObject(uiManager);
            serializedUiManager.FindProperty("resultPanel").objectReferenceValue = resultPanel.gameObject;
            serializedUiManager.ApplyModifiedPropertiesWithoutUndo();

            resultPanel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("Level clear UI built successfully.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Color color,
        Color textColor)
    {
        Image image = CreateImage(name, parent, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.23f, 0.64f, 0.9f, 1f);
        colors.pressedColor = new Color(0.1f, 0.4f, 0.65f, 1f);
        button.colors = colors;

        Text text = CreateText("Label", image.transform, label, 26, FontStyle.Bold, textColor);
        Stretch(text.rectTransform);
        UIButtonAnimationUtility.Apply(button);
        return button;
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
