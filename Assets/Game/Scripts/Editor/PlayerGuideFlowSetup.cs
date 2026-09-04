using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerGuideFlowSetup
{
    private const string ScenePath = "Assets/Game/Scenes/PlayerGuide.unity";
    private const string GuidePanelName = "GuidePanel";
    private const string BubblesNodeName = "bubbles_node";
    private const string MainBubbleName = "bubbles_main";
    private const string FeifeiNodeName = "feifei_menghe_node";
    private const string InputRowName = "guidepanel-input-row";
    private const string GachaNodeName = "gacha_node";
    private const string DialogBoxName = "dialog_box";

    private const string FeifeiTextPath = "Assets/Game/Configs/PlayerGuide/text_feifei.asset";
    private const string InputTextPath = "Assets/Game/Configs/PlayerGuide/text_input.asset";
    private const string GachaTextPath = "Assets/Game/Configs/PlayerGuide/text_gacha.asset";

    [MenuItem("Tools/UI/Wire Player Guide Flow")]
    public static void WireFromMenu()
    {
        if (!TryWireOpenScene(out string message) && !TryWireSceneAsset(out message))
        {
            Debug.LogError(message);
            return;
        }

        Debug.Log(message);
    }

    public static void WireBatch()
    {
        if (!TryWireSceneAsset(out string message))
        {
            Debug.LogError(message);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log(message);
        EditorApplication.Exit(0);
    }

    private static bool TryWireOpenScene(out string message)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded || scene.path != ScenePath)
                continue;

            if (!TryWireScene(scene, markDirty: true, out message))
                return false;

            return true;
        }

        message = null;
        return false;
    }

    private static bool TryWireSceneAsset(out string message)
    {
        if (!File.Exists(ScenePath))
        {
            message = $"Scene not found: {ScenePath}";
            return false;
        }

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        bool success = TryWireScene(scene, markDirty: true, out message);

        if (success)
            EditorSceneManager.SaveScene(scene);

        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded && previousActiveScene.path != ScenePath)
            EditorSceneManager.OpenScene(previousActiveScene.path, OpenSceneMode.Single);

        return success;
    }

    private static bool TryWireScene(Scene scene, bool markDirty, out string message)
    {
        GameObject guidePanel = FindSceneObject(scene, GuidePanelName);
        if (guidePanel == null)
        {
            message = $"Could not find {GuidePanelName} in {scene.path}.";
            return false;
        }

        GameObject bubblesNode = FindChild(guidePanel.transform, BubblesNodeName);
        GameObject mainBubble = FindChild(bubblesNode != null ? bubblesNode.transform : null, MainBubbleName);
        GameObject feifeiNode = FindChild(guidePanel.transform, FeifeiNodeName);
        GameObject inputRow = FindChild(guidePanel.transform, InputRowName);
        GameObject gachaNode = FindChild(guidePanel.transform, GachaNodeName);
        GameObject dialogBox = FindChild(feifeiNode != null ? feifeiNode.transform : null, DialogBoxName);

        if (bubblesNode == null || mainBubble == null || feifeiNode == null || inputRow == null ||
            gachaNode == null || dialogBox == null)
        {
            message = "PlayerGuide scene is missing one or more required GuidePanel children.";
            return false;
        }

        SequentialTextDisplay dialogueDisplay = dialogBox.GetComponent<SequentialTextDisplay>();
        if (dialogueDisplay == null)
            dialogueDisplay = dialogBox.AddComponent<SequentialTextDisplay>();

        GuidePanelInputFieldController inputController = inputRow.GetComponent<GuidePanelInputFieldController>();
        if (inputController == null)
        {
            message = $"Missing {nameof(GuidePanelInputFieldController)} on {InputRowName}.";
            return false;
        }

        Button mainBubbleButton = mainBubble.GetComponent<Button>();
        if (mainBubbleButton == null)
        {
            message = $"Missing Button on {MainBubbleName}.";
            return false;
        }

        EnsureMainBubbleVisual(mainBubble);
        PlayerGuideMainBubbleVisual mainBubbleVisual = mainBubble.GetComponent<PlayerGuideMainBubbleVisual>();

        SequentialTextConfig feifeiText = AssetDatabase.LoadAssetAtPath<SequentialTextConfig>(FeifeiTextPath);
        SequentialTextConfig inputText = AssetDatabase.LoadAssetAtPath<SequentialTextConfig>(InputTextPath);
        SequentialTextConfig gachaText = AssetDatabase.LoadAssetAtPath<SequentialTextConfig>(GachaTextPath);
        if (feifeiText == null || inputText == null || gachaText == null)
        {
            message = "Could not load one or more SequentialTextConfig assets in Assets/Game/Configs/PlayerGuide/.";
            return false;
        }

        PlayerGuideFlowController flowController = guidePanel.GetComponent<PlayerGuideFlowController>();
        if (flowController == null)
            flowController = guidePanel.AddComponent<PlayerGuideFlowController>();

        SerializedObject serializedFlow = new SerializedObject(flowController);
        serializedFlow.FindProperty("bubblesNode").objectReferenceValue = bubblesNode;
        serializedFlow.FindProperty("feifeiMengheNode").objectReferenceValue = feifeiNode;
        serializedFlow.FindProperty("inputRow").objectReferenceValue = inputRow;
        serializedFlow.FindProperty("gachaNode").objectReferenceValue = gachaNode;
        serializedFlow.FindProperty("mainBubbleButton").objectReferenceValue = mainBubbleButton;
        serializedFlow.FindProperty("mainBubbleRoot").objectReferenceValue = mainBubble;
        serializedFlow.FindProperty("mainBubbleVisual").objectReferenceValue = mainBubbleVisual;
        serializedFlow.FindProperty("dialogueDisplay").objectReferenceValue = dialogueDisplay;
        serializedFlow.FindProperty("feifeiTextConfig").objectReferenceValue = feifeiText;
        serializedFlow.FindProperty("inputTextConfig").objectReferenceValue = inputText;
        serializedFlow.FindProperty("gachaTextConfig").objectReferenceValue = gachaText;
        serializedFlow.FindProperty("inputController").objectReferenceValue = inputController;
        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        feifeiNode.SetActive(false);
        inputRow.SetActive(false);
        gachaNode.SetActive(false);
        bubblesNode.SetActive(true);
        mainBubble.SetActive(false);

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(scene);

        message = $"Wired {nameof(PlayerGuideFlowController)} on {GuidePanelName} in {scene.path}.";
        return true;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildRecursive(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        Transform found = parent.Find(childName);
        return found != null ? found.gameObject : null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void EnsureMainBubbleVisual(GameObject mainBubble)
    {
        const string visualName = PlayerGuideMainBubbleVisual.VisualChildName;
        Transform visualTransform = mainBubble.transform.Find(visualName);
        GameObject visualObject;

        if (visualTransform == null)
        {
            visualObject = new GameObject(visualName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(visualObject, "Create Main Bubble Visual");
            visualObject.transform.SetParent(mainBubble.transform, false);
            StretchRect(visualObject.GetComponent<RectTransform>());

            Image rootImage = mainBubble.GetComponent<Image>();
            if (rootImage != null)
            {
                ComponentUtility.CopyComponent(rootImage);
                ComponentUtility.PasteComponentAsNew(visualObject);
                Undo.DestroyObjectImmediate(rootImage);
            }

            var childrenToMove = new List<Transform>();
            foreach (Transform child in mainBubble.transform)
            {
                if (child.name == visualName)
                    continue;

                childrenToMove.Add(child);
            }

            foreach (Transform child in childrenToMove)
                child.SetParent(visualObject.transform, true);
        }
        else
        {
            visualObject = visualTransform.gameObject;
        }

        Button button = mainBubble.GetComponent<Button>();
        Image targetImage = visualObject.GetComponent<Image>();
        if (button != null && targetImage != null)
        {
            button.targetGraphic = targetImage;
            button.transition = Selectable.Transition.Animation;
        }

        PlayerGuideMainBubbleVisual visual = mainBubble.GetComponent<PlayerGuideMainBubbleVisual>();
        if (visual == null)
            visual = Undo.AddComponent<PlayerGuideMainBubbleVisual>(mainBubble);

        SerializedObject serializedVisual = new SerializedObject(visual);
        serializedVisual.FindProperty("scaleTarget").objectReferenceValue = visualObject.GetComponent<RectTransform>();
        serializedVisual.ApplyModifiedPropertiesWithoutUndo();

        RectTransform rootRect = mainBubble.GetComponent<RectTransform>();
        if (rootRect != null)
            rootRect.localScale = Vector3.one;

        RectTransform visualRect = visualObject.GetComponent<RectTransform>();
        if (visualRect != null)
            visualRect.localScale = Vector3.one;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
    }
}
