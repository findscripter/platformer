using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PlayerGuideBubbleGenerator
{
    private const string DefaultConfigPath = "Assets/Game/Configs/PlayerGuide/BubbleLayoutConfig.asset";
    private const string PlayerGuideScenePath = "Assets/Game/Scenes/PlayerGuide.unity";

    [MenuItem("Tools/UI/Generate Player Guide Bubbles")]
    public static void GenerateFromMenu()
    {
        var config = GetOrCreateDefaultConfig();
        if (config == null)
            return;

        Generate(config, null);
    }

    [MenuItem("Tools/UI/Generate Player Guide Bubbles", true)]
    private static bool ValidateGenerateFromMenu()
    {
        return !EditorApplication.isPlaying;
    }

    public static bool Generate(PlayerGuideBubbleLayoutConfig config, RectTransform bubblesNodeOverride)
    {
        if (config == null)
        {
            Debug.LogError("PlayerGuideBubbleGenerator: config is missing.");
            return false;
        }

        var bubblesNode = bubblesNodeOverride != null
            ? bubblesNodeOverride
            : FindBubblesNodeInOpenScenes(config.bubblesNodeName);

        if (bubblesNode == null)
        {
            Debug.LogError(
                $"PlayerGuideBubbleGenerator: bubbles node '{config.bubblesNodeName}' was not found. " +
                "Open PlayerGuide scene or select the bubbles_node object.");
            return false;
        }

        Undo.RegisterFullObjectHierarchyUndo(bubblesNode.gameObject, "Generate Player Guide Bubbles");

        if (!TryPrepareTemplates(
                bubblesNode,
                config,
                out var mainBubble,
                out var mediumTemplate,
                out var smallTemplate,
                out var preparationError))
        {
            Debug.LogError($"PlayerGuideBubbleGenerator: {preparationError}");
            return false;
        }

        var mediumPrototype = mediumTemplate != null ? Object.Instantiate(mediumTemplate.gameObject) : null;
        var smallPrototype = smallTemplate != null ? Object.Instantiate(smallTemplate.gameObject) : null;
        if (mediumPrototype != null)
            mediumPrototype.SetActive(false);
        if (smallPrototype != null)
            smallPrototype.SetActive(false);

        ClearOtherBubbles(bubblesNode, config.mainBubbleName);

        var layoutHalfSize = GetLayoutHalfSize(bubblesNode);
        if (!PlayerGuideBubbleLayoutUtility.TryGenerateLayout(
                config,
                mainBubble.anchoredPosition,
                layoutHalfSize,
                out var placements,
                out var layoutError))
        {
            if (mediumPrototype != null)
                Object.DestroyImmediate(mediumPrototype);
            if (smallPrototype != null)
                Object.DestroyImmediate(smallPrototype);
            Debug.LogError($"PlayerGuideBubbleGenerator: {layoutError}");
            return false;
        }

        var mediumCounter = 0;
        var smallCounter = 0;
        foreach (var placement in placements)
        {
            var template = placement.SizeType == PlayerGuideBubbleSizeType.Medium
                ? mediumPrototype
                : smallPrototype;
            var prefix = PlayerGuideBubbleLayoutUtility.GetBubbleNamePrefix(config, placement.SizeType);
            var index = placement.SizeType == PlayerGuideBubbleSizeType.Medium
                ? ++mediumCounter
                : ++smallCounter;
            var bubbleName = index == 1 ? prefix : $"{prefix} ({index - 1})";

            var bubbleObject = Object.Instantiate(template, bubblesNode);
            bubbleObject.name = bubbleName;
            bubbleObject.SetActive(true);

            var rectTransform = bubbleObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = placement.AnchoredPosition;
            Undo.RegisterCreatedObjectUndo(bubbleObject, "Generate Player Guide Bubbles");
        }

        if (mediumPrototype != null)
            Object.DestroyImmediate(mediumPrototype);
        if (smallPrototype != null)
            Object.DestroyImmediate(smallPrototype);

        EditorUtility.SetDirty(bubblesNode);
        EditorSceneManager.MarkSceneDirty(bubblesNode.gameObject.scene);
        Debug.Log(
            $"Player guide bubbles generated: 1 main, {config.mediumCount} medium, {config.smallCount} small.");
        return true;
    }

    public static PlayerGuideBubbleLayoutConfig GetOrCreateDefaultConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<PlayerGuideBubbleLayoutConfig>(DefaultConfigPath);
        if (config != null)
            return config;

        var directory = Path.GetDirectoryName(DefaultConfigPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        config = ScriptableObject.CreateInstance<PlayerGuideBubbleLayoutConfig>();
        AssetDatabase.CreateAsset(config, DefaultConfigPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created default bubble layout config at {DefaultConfigPath}.");
        return config;
    }

    private static RectTransform FindBubblesNodeInOpenScenes(string bubblesNodeName)
    {
        for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            var scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
                continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindChildByName(root.transform, bubblesNodeName);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private static RectTransform FindChildByName(Transform parent, string targetName)
    {
        if (parent.name == targetName)
            return parent as RectTransform;

        for (var index = 0; index < parent.childCount; index++)
        {
            var found = FindChildByName(parent.GetChild(index), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static bool TryPrepareTemplates(
        RectTransform bubblesNode,
        PlayerGuideBubbleLayoutConfig config,
        out RectTransform mainBubble,
        out RectTransform mediumTemplate,
        out RectTransform smallTemplate,
        out string errorMessage)
    {
        mainBubble = null;
        mediumTemplate = null;
        smallTemplate = null;
        errorMessage = null;

        var mainBubbles = new List<RectTransform>();
        var mediumBubbles = new List<RectTransform>();
        var smallBubbles = new List<RectTransform>();

        for (var index = 0; index < bubblesNode.childCount; index++)
        {
            var child = bubblesNode.GetChild(index) as RectTransform;
            if (child == null)
                continue;

            if (child.name == config.mainBubbleName)
            {
                mainBubbles.Add(child);
                continue;
            }

            if (child.name.StartsWith(config.mediumBubbleNamePrefix))
                mediumBubbles.Add(child);
            else if (child.name.StartsWith(config.smallBubbleNamePrefix))
                smallBubbles.Add(child);
        }

        if (mainBubbles.Count != 1)
        {
            errorMessage = $"Expected exactly one '{config.mainBubbleName}', found {mainBubbles.Count}.";
            return false;
        }

        if (config.mediumCount > 0 && mediumBubbles.Count == 0)
        {
            errorMessage = $"Missing medium bubble template with prefix '{config.mediumBubbleNamePrefix}'.";
            return false;
        }

        if (config.smallCount > 0 && smallBubbles.Count == 0)
        {
            errorMessage = $"Missing small bubble template with prefix '{config.smallBubbleNamePrefix}'.";
            return false;
        }

        mainBubble = mainBubbles[0];
        mediumTemplate = mediumBubbles.Count > 0 ? mediumBubbles[0] : null;
        smallTemplate = smallBubbles.Count > 0 ? smallBubbles[0] : null;
        return true;
    }

    private static Vector2 GetLayoutHalfSize(RectTransform bubblesNode)
    {
        var rect = bubblesNode.rect;
        if (rect.width > 1f && rect.height > 1f)
            return rect.size * 0.5f;

        return new Vector2(430f, 280f);
    }

    private static void ClearOtherBubbles(RectTransform bubblesNode, string mainBubbleName)
    {
        for (var index = bubblesNode.childCount - 1; index >= 0; index--)
        {
            var child = bubblesNode.GetChild(index);
            if (child.name == mainBubbleName)
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }
}

public class PlayerGuideBubbleGeneratorWindow : EditorWindow
{
    private PlayerGuideBubbleLayoutConfig config;
    private RectTransform bubblesNodeOverride;
    private Vector2 scrollPosition;

    [MenuItem("Tools/UI/Player Guide Bubble Generator Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<PlayerGuideBubbleGeneratorWindow>("Guide Bubbles");
        window.minSize = new Vector2(360f, 220f);
        window.Show();
    }

    private void OnEnable()
    {
        if (config == null)
            config = PlayerGuideBubbleGenerator.GetOrCreateDefaultConfig();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Player Guide Bubble Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "根据配置文件生成 1 个主泡泡和若干中/小泡泡。中泡泡分布在内层椭圆轨道，小泡泡分布在外层椭圆轨道，轨道长轴跟随屏幕比例。",
            MessageType.Info);

        config = (PlayerGuideBubbleLayoutConfig)EditorGUILayout.ObjectField(
            "Layout Config",
            config,
            typeof(PlayerGuideBubbleLayoutConfig),
            false);
        bubblesNodeOverride = (RectTransform)EditorGUILayout.ObjectField(
            "Bubbles Node Override",
            bubblesNodeOverride,
            typeof(RectTransform),
            true);

        if (config != null)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Main bubble: 1");
            EditorGUILayout.LabelField($"Medium bubbles: {config.mediumCount}");
            EditorGUILayout.LabelField($"Small bubbles: {config.smallCount}");
            EditorGUILayout.LabelField($"Random seed: {(config.randomSeed == 0 ? "Random" : config.randomSeed.ToString())}");
        }

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || config == null))
        {
            if (GUILayout.Button("Generate Bubbles", GUILayout.Height(32f)))
                PlayerGuideBubbleGenerator.Generate(config, bubblesNodeOverride);
        }

        EditorGUILayout.EndScrollView();
    }
}
