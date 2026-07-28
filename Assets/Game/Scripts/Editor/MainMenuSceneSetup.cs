using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSceneSetup
{
    private const string ScenePath = "Assets/Game/Scenes/MainMenu.unity";

    private static readonly Color BackgroundColor = new(0.035f, 0.055f, 0.09f, 1f);
    private static readonly Color PanelColor = new(0.075f, 0.105f, 0.16f, 0.96f);
    private static readonly Color PrimaryColor = new(0.16f, 0.58f, 0.86f, 1f);
    private static readonly Color SecondaryColor = new(0.15f, 0.19f, 0.27f, 1f);
    private static readonly Color TextColor = new(0.94f, 0.97f, 1f, 1f);
    private static readonly Color MutedTextColor = new(0.55f, 0.62f, 0.72f, 1f);

    [InitializeOnLoadMethod]
    private static void BuildWhenNeeded()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess())
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath) ||
                File.ReadAllText(ScenePath).Contains("m_Name: StartButton"))
            {
                return;
            }

            Build();
        };
    }

    [MenuItem("Tools/UI/Build Main Menu")]
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
        if (!File.Exists(ScenePath))
        {
            throw new FileNotFoundException($"Missing scene: {ScenePath}");
        }

        var previousActiveScene = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var openedForSetup = !scene.isLoaded;
        if (openedForSetup)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        SceneManager.SetActiveScene(scene);
        var root = FindOrCreateRoot(scene, "MainMenuRoot");
        ClearChildren(root.transform);

        var controller = root.GetComponent<MainMenuUIController>();
        if (controller == null)
        {
            controller = root.AddComponent<MainMenuUIController>();
        }

        var canvasObject = FindOrCreateRoot(scene, "UI");
        ClearChildren(canvasObject.transform);

        var canvasRoot = CreateUiObject("Canvas_MainMenu", canvasObject.transform);
        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasRoot.AddComponent<GraphicRaycaster>();
        Stretch(canvasRoot.GetComponent<RectTransform>());

        var background = CreateImage("Background", canvasRoot.transform, BackgroundColor);
        Stretch(background.rectTransform);

        var accent = CreateImage("Accent", background.transform, PrimaryColor);
        SetRect(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -7f), new Vector2(520f, 7f), new Vector2(0.5f, 1f));

        var panel = CreateImage("MenuPanel", background.transform, PanelColor);
        SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(520f, 650f), new Vector2(0.5f, 0.5f));

        var title = CreateText("Title", panel.transform, "PLATFORMER", 54, FontStyle.Bold, TextColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -92f), new Vector2(440f, 80f), new Vector2(0.5f, 0.5f));

        var subtitle = CreateText("Subtitle", panel.transform, "探索 · 跳跃 · 抵达终点", 22, FontStyle.Normal, MutedTextColor);
        SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -150f), new Vector2(440f, 42f), new Vector2(0.5f, 0.5f));

        var startButton = CreateButton(panel.transform, "StartButton", "开始游戏", PrimaryColor, TextColor);
        SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -270f), new Vector2(360f, 72f), new Vector2(0.5f, 0.5f));
        UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartGame);

        var continueButton = CreateButton(panel.transform, "ContinueButton", "继续游戏（开发中）", SecondaryColor, MutedTextColor);
        SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -364f), new Vector2(360f, 64f), new Vector2(0.5f, 0.5f));
        continueButton.interactable = false;

        var settingsButton = CreateButton(panel.transform, "SettingsButton", "设置（开发中）", SecondaryColor, MutedTextColor);
        SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -448f), new Vector2(360f, 64f), new Vector2(0.5f, 0.5f));
        settingsButton.interactable = false;

        var quitButton = CreateButton(panel.transform, "QuitButton", "退出游戏", SecondaryColor, TextColor);
        SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -540f), new Vector2(360f, 64f), new Vector2(0.5f, 0.5f));
        UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame);

        var footer = CreateText("Footer", background.transform, "WASD / 方向键移动  ·  Space 跳跃  ·  Esc 暂停",
            18, FontStyle.Normal, MutedTextColor);
        SetRect(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 42f), new Vector2(760f, 36f), new Vector2(0.5f, 0.5f));

        SetupEventSystem(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }

        if (openedForSetup)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log("Main menu UI built successfully.");
    }

    private static void SetupEventSystem(Scene scene)
    {
        var eventSystemObject = FindOrCreateRoot(scene, "EventSystem");
        var eventSystem = eventSystemObject.GetComponent<EventSystem>() ?? eventSystemObject.AddComponent<EventSystem>();
        var oldModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
        {
            Object.DestroyImmediate(oldModule);
        }

        var module = eventSystemObject.GetComponent<InputSystemUIInputModule>() ??
                     eventSystemObject.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
        eventSystem.firstSelectedGameObject = null;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, Color textColor)
    {
        var image = CreateImage(name, parent, color);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        colors.colorMultiplier = 1f;
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
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject FindOrCreateRoot(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        var gameObject = new GameObject(name);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
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
