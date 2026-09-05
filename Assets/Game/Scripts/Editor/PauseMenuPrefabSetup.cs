using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class PauseMenuPrefabSetup
{
    private const string PrefabPath = "Assets/Game/Prefabs/Managers/PersistentRoot.prefab";

    private static readonly Color OverlayColor = new(0.015f, 0.025f, 0.045f, 0.78f);
    private static readonly Color PanelColor = new(0.065f, 0.09f, 0.14f, 0.98f);
    private static readonly Color PrimaryColor = new(0.16f, 0.58f, 0.86f, 1f);
    private static readonly Color SecondaryColor = new(0.15f, 0.19f, 0.27f, 1f);
    private static readonly Color TextColor = new(0.94f, 0.97f, 1f, 1f);
    private static readonly Color MutedTextColor = new(0.58f, 0.64f, 0.72f, 1f);

    [MenuItem("Tools/UI/Build Pause Menu")]
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
            var canvas = root.transform.Find("UI/Canvas_Menu");
            if (canvas == null)
            {
                throw new System.InvalidOperationException("Missing UI/Canvas_Menu in PersistentRoot prefab.");
            }

            var pauseMenu = canvas.Find("PauseMenu");
            if (pauseMenu != null && pauseMenu is not RectTransform)
            {
                Object.DestroyImmediate(pauseMenu.gameObject);
                pauseMenu = null;
            }

            if (pauseMenu == null)
            {
                pauseMenu = CreateUiObject("PauseMenu", canvas).transform;
            }

            ClearChildren(pauseMenu);
            Stretch((RectTransform)pauseMenu);

            var controller = pauseMenu.GetComponent<PauseMenuUIController>() ??
                             pauseMenu.gameObject.AddComponent<PauseMenuUIController>();

            var overlay = pauseMenu.GetComponent<Image>() ?? pauseMenu.gameObject.AddComponent<Image>();
            overlay.color = OverlayColor;
            overlay.raycastTarget = true;

            var panel = CreateImage("PausePanel", pauseMenu, PanelColor);
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(500f, 520f), new Vector2(0.5f, 0.5f));

            var title = CreateText("Title", panel.transform, "游戏暂停", 48, FontStyle.Bold, TextColor);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -85f), new Vector2(400f, 70f), new Vector2(0.5f, 0.5f));

            var tip = CreateText("Tip", panel.transform, "按 Esc 可继续游戏", 20, FontStyle.Normal, MutedTextColor);
            SetRect(tip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -142f), new Vector2(400f, 38f), new Vector2(0.5f, 0.5f));

            var resumeButton = CreateButton(panel.transform, "ResumeButton", "继续游戏", PrimaryColor, TextColor);
            SetRect(resumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -245f), new Vector2(340f, 72f), new Vector2(0.5f, 0.5f));
            UnityEventTools.AddPersistentListener(resumeButton.onClick, controller.ResumeGame);

            var restartButton = CreateButton(panel.transform, "RestartButton", "重新开始（开发中）",
                SecondaryColor, MutedTextColor);
            SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -335f), new Vector2(340f, 62f), new Vector2(0.5f, 0.5f));
            restartButton.interactable = false;

            var menuButton = CreateButton(panel.transform, "MainMenuButton", "返回主菜单", SecondaryColor, TextColor);
            SetRect(menuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -425f), new Vector2(340f, 64f), new Vector2(0.5f, 0.5f));
            UnityEventTools.AddPersistentListener(menuButton.onClick, controller.ReturnToMainMenu);

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            var uiManager = root.transform.Find("Managers/UIManager")?.GetComponent<UIManager>();
            if (uiManager == null)
            {
                throw new System.InvalidOperationException("Missing Managers/UIManager in PersistentRoot prefab.");
            }

            var serializedUiManager = new SerializedObject(uiManager);
            serializedUiManager.FindProperty("pausePanel").objectReferenceValue = pauseMenu.gameObject;
            serializedUiManager.ApplyModifiedPropertiesWithoutUndo();

            pauseMenu.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("Pause menu UI built successfully.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, Color textColor)
    {
        var image = CreateImage(name, parent, color);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        button.colors = colors;

        var text = CreateText("Label", image.transform, label, 25, FontStyle.Bold, textColor);
        Stretch(text.rectTransform);
        UIButtonAnimationUtility.Apply(button);
        return button;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var gameObject = CreateUiObject(name, parent);
        var image = gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Color color)
    {
        var gameObject = CreateUiObject(name, parent);
        var text = gameObject.AddComponent<Text>();
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
