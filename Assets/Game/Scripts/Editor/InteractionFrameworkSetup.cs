using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InteractionFrameworkSetup
{
    private const string PersistentRootPrefabPath =
        "Assets/Game/Prefabs/Managers/PersistentRoot.prefab";

    [MenuItem("Tools/Platformer/Setup Interaction Framework")]
    public static void SetupFromMenu()
    {
        SetupPersistentRoot();
        AssetDatabase.SaveAssets();
        Debug.Log("Interaction framework setup completed.");
    }

    public static void SetupBatch()
    {
        try
        {
            SetupFromMenu();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Interaction framework setup failed: {exception}");
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    private static void SetupPersistentRoot()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PersistentRootPrefabPath);
        try
        {
            Transform managers = FindChild(prefabRoot.transform, "Managers");
            if (managers == null)
                throw new System.InvalidOperationException("Managers node not found on PersistentRoot.");

            var interactionManager = EnsureComponent<InteractionManager>(
                EnsureChild(managers, "InteractionManager").gameObject);
            var dialogueManager = EnsureComponent<DialogueManager>(
                EnsureChild(managers, "DialogueManager").gameObject);
            var dialogueProgressService = EnsureComponent<DialogueProgressService>(dialogueManager.gameObject);
            var saveSystem = EnsureComponent<SaveSystem>(dialogueManager.gameObject);

            var gameLoop = managers.GetComponentInChildren<GameLoop>(true);
            if (gameLoop == null)
                throw new System.InvalidOperationException("GameLoop not found on PersistentRoot.");

            SerializedObject gameLoopObject = new SerializedObject(gameLoop);
            gameLoopObject.FindProperty("interactionManager").objectReferenceValue = interactionManager;
            gameLoopObject.FindProperty("dialogueManager").objectReferenceValue = dialogueManager;
            gameLoopObject.FindProperty("dialogueProgressService").objectReferenceValue = dialogueProgressService;
            gameLoopObject.FindProperty("saveSystem").objectReferenceValue = saveSystem;
            gameLoopObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject saveSystemObject = new SerializedObject(saveSystem);
            saveSystemObject.FindProperty("dialogueProgressService").objectReferenceValue = dialogueProgressService;
            saveSystemObject.ApplyModifiedPropertiesWithoutUndo();

            SetupPromptUI(prefabRoot.transform);
            SetupDialogueUI(prefabRoot.transform, dialogueManager);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PersistentRootPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void SetupPromptUI(Transform root)
    {
        Transform canvasPrompt = FindChild(root, "UI/Canvas_Prompt");
        if (canvasPrompt == null)
            return;

        Transform prompt = EnsureChild(canvasPrompt, "InteractionPrompt");
        RectTransform promptRect = EnsureComponent<RectTransform>(prompt.gameObject);
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 40f);
        promptRect.sizeDelta = new Vector2(420f, 40f);

        Text promptText = prompt.GetComponentInChildren<Text>(true);
        if (promptText == null)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(prompt, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            promptText = labelObject.GetComponent<Text>();
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            promptText.fontSize = 22;
            promptText.text = "[E] 交互";
        }

        var controller = EnsureComponent<InteractionPromptController>(prompt.gameObject);
        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("root").objectReferenceValue = prompt.gameObject;
        controllerObject.FindProperty("promptText").objectReferenceValue = promptText;
        controllerObject.ApplyModifiedPropertiesWithoutUndo();

        prompt.gameObject.SetActive(false);
    }

    private static void SetupDialogueUI(Transform root, DialogueManager dialogueManager)
    {
        Transform uiRoot = EnsureChild(root, "UI");
        Transform dialogueCanvas = EnsureChild(uiRoot, DialogueUIBuilder.CanvasName);
        DialogueUIBuildResult buildResult = DialogueUIBuilder.Build(uiRoot, dialogueCanvas);

        var bootstrap = EnsureComponent<DialogueUIBootstrap>(dialogueManager.gameObject);
        SerializedObject bootstrapObject = new SerializedObject(bootstrap);
        bootstrapObject.FindProperty("dialogueManager").objectReferenceValue = dialogueManager;
        bootstrapObject.FindProperty("dialogueUI").objectReferenceValue = buildResult.DialogueUI;
        bootstrapObject.FindProperty("choiceUI").objectReferenceValue = buildResult.ChoiceUI;
        bootstrapObject.FindProperty("inputUI").objectReferenceValue = buildResult.InputUI;
        bootstrapObject.ApplyModifiedPropertiesWithoutUndo();

        if (dialogueManager.GetComponent<DialogueUIRuntimeBootstrap>() != null)
            Object.DestroyImmediate(dialogueManager.GetComponent<DialogueUIRuntimeBootstrap>());
    }

    private static Transform FindChild(Transform root, string path)
    {
        string[] parts = path.Split('/');
        Transform current = root;
        foreach (string part in parts)
        {
            current = current.Find(part);
            if (current == null)
                return null;
        }

        return current;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        var childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
