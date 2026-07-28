using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerGuideSceneSetup
{
    private const string ScenePath = "Assets/Game/Scenes/PlayerGuide.unity";

    private static readonly Color BackgroundColor = new(0.03f, 0.07f, 0.12f, 1f);
    private static readonly Color PanelColor = new(0.08f, 0.12f, 0.2f, 0.96f);
    private static readonly Color PrimaryColor = new(0.16f, 0.58f, 0.86f, 1f);
    private static readonly Color SecondaryColor = new(0.15f, 0.19f, 0.27f, 1f);
    private static readonly Color TextColor = new(0.94f, 0.97f, 1f, 1f);
    private static readonly Color MutedTextColor = new(0.55f, 0.62f, 0.72f, 1f);

    [MenuItem("Tools/UI/Build Player Guide Scene")]
    public static void BuildFromMenu()
    {
        Build();
    }

    [InitializeOnLoadMethod]
    private static void BuildWhenNeeded()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess())
            return;

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath))
            {
                Build();
                return;
            }

            if (!File.ReadAllText(ScenePath).Contains("PlayerGuideUIController"))
                Build();
        };
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
        EnsureSceneAssetExists();

        var previousActiveScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        var root = FindOrCreateRoot(scene, "PlayerGuideRoot");
        ClearChildren(root.transform);

        var controller = root.GetComponent<PlayerGuideUIController>();
        if (controller == null)
            controller = root.AddComponent<PlayerGuideUIController>();

        var canvasObject = FindOrCreateRoot(scene, "UI");
        ClearChildren(canvasObject.transform);

        var canvasRoot = CreateUiObject("Canvas_PlayerGuide", canvasObject.transform);
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

        var panel = CreateImage("GuidePanel", background.transform, PanelColor);
        SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(860f, 560f), new Vector2(0.5f, 0.5f));

        var title = CreateText("Title", panel.transform, "新手引导", 42, FontStyle.Bold, TextColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -56f), new Vector2(760f, 56f), new Vector2(0.5f, 0.5f));

        var pageContainer = CreateUiObject("PageContainer", panel.transform);
        Stretch(pageContainer.GetComponent<RectTransform>());

        var page1 = CreateGuidePage(
            pageContainer.transform,
            "Page_Welcome",
            "欢迎来到平台跳跃世界",
            "在这里你将学习基础操作与交互方式。\n本场景仅用于引导，不包含核心战斗与关卡逻辑。");
        var page2 = CreateGuidePage(
            pageContainer.transform,
            "Page_Movement",
            "移动与跳跃",
            "WASD / 方向键：左右移动\nSpace：跳跃\n可在空中进行有限制移动。");
        var page3 = CreateGuidePage(
            pageContainer.transform,
            "Page_Interaction",
            "交互与目标",
            "靠近可交互物体时按 E 进行交互。\n收集物品、避开陷阱，抵达终点完成关卡。");

        var indicator = CreateText("PageIndicator", panel.transform, "1 / 3", 18, FontStyle.Normal, MutedTextColor);
        SetRect(indicator.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 88f), new Vector2(200f, 32f), new Vector2(0.5f, 0.5f));

        var nextButton = CreateButton(panel.transform, "NextButton", "下一步", PrimaryColor, TextColor);
        SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(80f, 28f), new Vector2(220f, 56f), new Vector2(0.5f, 0f));
        UnityEventTools.AddPersistentListener(nextButton.onClick, controller.OnNextClicked);

        var skipButton = CreateButton(panel.transform, "SkipButton", "跳过引导", SecondaryColor, MutedTextColor);
        SetRect(skipButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-80f, 28f), new Vector2(220f, 56f), new Vector2(0.5f, 0f));
        UnityEventTools.AddPersistentListener(skipButton.onClick, controller.ContinueToGameplay);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("pages").arraySize = 3;
        serializedController.FindProperty("pages").GetArrayElementAtIndex(0).objectReferenceValue = page1;
        serializedController.FindProperty("pages").GetArrayElementAtIndex(1).objectReferenceValue = page2;
        serializedController.FindProperty("pages").GetArrayElementAtIndex(2).objectReferenceValue = page3;
        serializedController.FindProperty("pageIndicatorText").objectReferenceValue = indicator;
        serializedController.FindProperty("nextButton").objectReferenceValue = nextButton;
        serializedController.FindProperty("skipButton").objectReferenceValue = skipButton;
        serializedController.FindProperty("nextButtonLabel").objectReferenceValue =
            nextButton.GetComponentInChildren<Text>();
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        SetupEventSystem(scene, nextButton.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded && previousActiveScene.path != ScenePath)
            SceneManager.SetActiveScene(previousActiveScene);

        Debug.Log("Player guide scene built successfully.");
    }

    private static void EnsureSceneAssetExists()
    {
        if (File.Exists(ScenePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Game/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static GameObject CreateGuidePage(Transform parent, string name, string heading, string body)
    {
        var page = CreateUiObject(name, parent);
        Stretch(page.GetComponent<RectTransform>());

        var headingText = CreateText("Heading", page.transform, heading, 30, FontStyle.Bold, TextColor);
        SetRect(headingText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -120f), new Vector2(720f, 48f), new Vector2(0.5f, 0.5f));

        var bodyText = CreateText("Body", page.transform, body, 24, FontStyle.Normal, MutedTextColor);
        SetRect(bodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 20f), new Vector2(720f, 220f), new Vector2(0.5f, 0.5f));
        bodyText.alignment = TextAnchor.UpperLeft;

        return page;
    }

    private static void SetupEventSystem(Scene scene, GameObject firstSelected)
    {
        var eventSystemObject = FindOrCreateRoot(scene, "EventSystem");
        var eventSystem = eventSystemObject.GetComponent<EventSystem>() ?? eventSystemObject.AddComponent<EventSystem>();
        var oldModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
            Object.DestroyImmediate(oldModule);

        var module = eventSystemObject.GetComponent<InputSystemUIInputModule>() ??
                     eventSystemObject.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
        eventSystem.firstSelectedGameObject = firstSelected;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, Color textColor)
    {
        var image = CreateImage(name, parent, color);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var text = CreateText("Label", image.transform, label, 22, FontStyle.Bold, textColor);
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
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
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
                return root;
        }

        var gameObject = new GameObject(name);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static void ClearChildren(Transform parent)
    {
        for (var index = parent.childCount - 1; index >= 0; index--)
            Object.DestroyImmediate(parent.GetChild(index).gameObject);
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
