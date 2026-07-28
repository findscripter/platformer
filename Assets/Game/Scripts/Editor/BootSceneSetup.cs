using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class BootSceneSetup
{
    private const string BootScenePath = "Assets/Game/Scenes/Boot.unity";
    private const string GameplayScenePath = "Assets/Game/Scenes/Gameplay.unity";
    private const string MainMenuScenePath = "Assets/Game/Scenes/MainMenu.unity";
    private const string PlayerGuideScenePath = "Assets/Game/Scenes/PlayerGuide.unity";
    private const string LoadingScenePath = "Assets/Game/Scenes/Loading.unity";
    private const string InputActionsPath = "Assets/Game/Configs/GameInputActions.inputactions";

    private static readonly string[] PersistentUiCanvasNames =
    {
        "Canvas_HUD",
        "Canvas_Menu",
        "Canvas_Map",
        "Canvas_Prompt",
        "Canvas_Transition",
        "Canvas_Debug"
    };

    [MenuItem("Tools/Project Structure/Setup Boot Scene")]
    public static void SetupFromMenu()
    {
        Setup();
    }

    [MenuItem("Tools/Project Structure/Rebind Boot Managers")]
    public static void RebindBootManagersFromMenu()
    {
        RebindBootManagers();
    }

    public static void RebindBootManagersBatch()
    {
        try
        {
            RebindBootManagers();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Boot manager rebind failed: {exception}");
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    public static void SetupBatch()
    {
        try
        {
            Setup();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Boot scene setup failed: {exception}");
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    private static void Setup()
    {
        if (!File.Exists(GameplayScenePath))
        {
            throw new FileNotFoundException($"Missing scene: {GameplayScenePath}");
        }

        var bootScene = EnsureBootScene();
        var gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);

        var persistentRoot = EnsureRootInScene(bootScene, "PersistentRoot");
        var managersParent = ConsolidatePersistentRoot(gameplayScene, bootScene, persistentRoot, "Managers");
        var uiParent = ConsolidatePersistentRoot(gameplayScene, bootScene, persistentRoot, "UI");

        EnsureGameLoopManagerComponents(managersParent);

        var bootRoot = EnsureRootInScene(bootScene, "BootRoot");
        var bootstrap = EnsureComponent<BootBootstrap>(bootRoot);
        AssignBootstrap(bootstrap, persistentRoot);
        WirePersistentManagers(managersParent, uiParent);

        SetupGameplayScene(gameplayScene);

        EditorSceneManager.MarkSceneDirty(bootScene);
        EditorSceneManager.MarkSceneDirty(gameplayScene);
        EditorSceneManager.SaveScene(bootScene);
        EditorSceneManager.SaveScene(gameplayScene);
        EditorSceneManager.CloseScene(gameplayScene, true);

        UpdateBuildSettings();

        Debug.Log("Boot scene setup completed.");
    }

    private static void RebindBootManagers()
    {
        if (!File.Exists(BootScenePath))
        {
            throw new FileNotFoundException($"Missing scene: {BootScenePath}");
        }

        var bootScene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
        var persistentRoot = FindInScene(bootScene, "PersistentRoot");
        if (persistentRoot == null)
        {
            throw new System.InvalidOperationException("Missing PersistentRoot in Boot scene.");
        }

        var managersParent = persistentRoot.transform.Find("Managers")?.gameObject;
        if (managersParent == null)
        {
            throw new System.InvalidOperationException("Missing PersistentRoot/Managers in Boot scene.");
        }

        var uiParent = persistentRoot.transform.Find("UI")?.gameObject;
        if (uiParent == null)
        {
            throw new System.InvalidOperationException("Missing PersistentRoot/UI in Boot scene.");
        }

        DeduplicateNamedManagers(managersParent.transform);
        EnsureGameLoopManagerComponents(managersParent);
        WirePersistentManagers(managersParent, uiParent);

        var bootRoot = FindInScene(bootScene, "BootRoot");
        if (bootRoot != null)
        {
            AssignBootstrap(EnsureComponent<BootBootstrap>(bootRoot), persistentRoot);
        }

        EditorSceneManager.MarkSceneDirty(bootScene);
        EditorSceneManager.SaveScene(bootScene);

        Debug.Log("Boot manager references rebound.");
    }

    private static void DeduplicateNamedManagers(Transform managersParent)
    {
        DeduplicateNamedChild(managersParent, "GameLoop", typeof(GameLoop));
        DeduplicateNamedChild(managersParent, "InputManager", typeof(InputManager));
        DeduplicateNamedChild(managersParent, "UIManager", typeof(UIManager));
        DeduplicateNamedChild(managersParent, "LevelManager", typeof(LevelManager));
    }

    private static void DeduplicateNamedChild(Transform parent, string objectName, System.Type preferredComponent)
    {
        GameObject keeper = null;
        var duplicates = new List<GameObject>();

        foreach (Transform child in parent)
        {
            if (child.name != objectName)
            {
                continue;
            }

            if (keeper == null && child.GetComponent(preferredComponent) != null)
            {
                keeper = child.gameObject;
            }
            else
            {
                duplicates.Add(child.gameObject);
            }
        }

        if (keeper == null)
        {
            foreach (Transform child in parent)
            {
                if (child.name == objectName)
                {
                    keeper = child.gameObject;
                    break;
                }
            }
        }

        foreach (var duplicate in duplicates)
        {
            if (duplicate != keeper)
            {
                Object.DestroyImmediate(duplicate);
            }
        }
    }

    private static Scene EnsureBootScene()
    {
        if (File.Exists(BootScenePath))
        {
            return EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, BootScenePath);
        return scene;
    }

    private static GameObject ConsolidatePersistentRoot(
        Scene gameplayScene,
        Scene bootScene,
        GameObject persistentRoot,
        string rootName)
    {
        var persistentChild = persistentRoot.transform.Find(rootName)?.gameObject;
        if (persistentChild == null)
        {
            persistentChild = EnsureChildInScene(bootScene, persistentRoot, rootName);
        }

        var gameplayRoot = FindInScene(gameplayScene, rootName);
        if (gameplayRoot != null && gameplayRoot != persistentChild)
        {
            MoveAllChildren(gameplayRoot.transform, persistentChild.transform);
            Object.DestroyImmediate(gameplayRoot);
        }

        return persistentChild;
    }

    private static void MoveAllChildren(Transform source, Transform destination)
    {
        var children = new List<Transform>();
        foreach (Transform child in source)
        {
            children.Add(child);
        }

        foreach (var child in children)
        {
            MoveToScene(child.gameObject, destination.gameObject.scene, destination);
        }
    }

    private static void EnsureGameLoopManagerComponents(GameObject managersParent)
    {
        EnsureManagerComponent(managersParent, "GameLoop", typeof(GameLoop));
        EnsureManagerComponent(managersParent, "InputManager", typeof(InputManager));
        EnsureManagerComponent(managersParent, "UIManager", typeof(UIManager));
        EnsureManagerComponent(managersParent, "LevelManager", typeof(LevelManager));
    }

    private static Component EnsureManagerComponent(GameObject managersParent, string objectName, System.Type componentType)
    {
        var managerTransform = managersParent.transform.Find(objectName);
        if (managerTransform == null)
        {
            var created = new GameObject(objectName);
            created.transform.SetParent(managersParent.transform, false);
            return created.AddComponent(componentType);
        }

        StripForeignComponents(managerTransform.gameObject, componentType);
        var existing = managerTransform.GetComponent(componentType);
        return existing != null ? existing : managerTransform.gameObject.AddComponent(componentType);
    }

    private static void StripForeignComponents(GameObject gameObject, System.Type allowedType)
    {
        var components = gameObject.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component == null || component is Transform)
            {
                continue;
            }

            if (component.GetType() != allowedType)
            {
                Object.DestroyImmediate(component);
            }
        }
    }

    private static void WirePersistentManagers(GameObject managersParent, GameObject uiParent)
    {
        var inputManager = GetDirectChildComponent<InputManager>(managersParent.transform, "InputManager");
        var uiManager = GetDirectChildComponent<UIManager>(managersParent.transform, "UIManager");
        var levelManager = GetDirectChildComponent<LevelManager>(managersParent.transform, "LevelManager");
        var gameLoop = GetDirectChildComponent<GameLoop>(managersParent.transform, "GameLoop");

        if (inputManager == null || uiManager == null || levelManager == null || gameLoop == null)
        {
            throw new System.InvalidOperationException("Missing persistent manager components under PersistentRoot/Managers.");
        }

        var mainMenuPanel = FindUiObject(uiParent.transform, "Canvas_Menu/MainMenuPanel");
        if (mainMenuPanel == null)
        {
            var canvasMenu = uiParent.transform.Find("Canvas_Menu")?.gameObject ?? uiParent;
            mainMenuPanel = EnsureChildInScene(uiParent.scene, canvasMenu, "MainMenuPanel");
        }

        var gameplayPanel = FindUiObject(uiParent.transform, "Canvas_HUD");
        var pausePanel = FindUiObject(uiParent.transform, "Canvas_Menu/PauseMenu");
        var resultPanel = FindUiObject(uiParent.transform, "Canvas_Menu/ResultPanel");
        if (resultPanel == null)
        {
            var canvasMenu = uiParent.transform.Find("Canvas_Menu")?.gameObject ?? uiParent;
            resultPanel = EnsureChildInScene(uiParent.scene, canvasMenu, "ResultPanel");
        }

        AssignInputActions(inputManager);
        AssignGameLoop(gameLoop, inputManager, uiManager, levelManager);
        AssignUIManager(uiManager, mainMenuPanel, gameplayPanel, pausePanel, resultPanel);
        AssignLevelManagerBoot(levelManager);

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(gameplayPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(resultPanel, false);

        foreach (var canvasName in PersistentUiCanvasNames)
        {
            FixCanvasScale(uiParent.transform.Find(canvasName)?.gameObject);
        }
    }

    private static T GetDirectChildComponent<T>(Transform parent, string childName) where T : Component
    {
        foreach (Transform child in parent)
        {
            if (child.name != childName)
            {
                continue;
            }

            var component = child.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
        }

        return parent.Find(childName)?.GetComponent<T>();
    }

    private static GameObject FindUiObject(Transform uiRoot, string path)
    {
        var found = uiRoot.Find(path);
        if (found != null)
        {
            return found.gameObject;
        }

        var segments = path.Split('/');
        foreach (Transform child in uiRoot)
        {
            if (child.name != segments[0])
            {
                continue;
            }

            if (segments.Length == 1)
            {
                return child.gameObject;
            }

            var nested = child.Find(segments[1]);
            if (nested != null)
            {
                return nested.gameObject;
            }
        }

        return null;
    }

    private static void SetupGameplayScene(Scene gameplayScene)
    {
        SceneManager.SetActiveScene(gameplayScene);
        ProjectLayerSetup.Setup();

        var spawnPoint = EnsurePathInScene(gameplayScene, "SceneRoot/GameplayLayer/SpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 1f, 0f);

        var goalTrigger = EnsurePathInScene(gameplayScene, "SceneRoot/GameplayLayer/Runtime_Exits/GoalTrigger");
        var goalCollider = EnsureComponent<BoxCollider2D>(goalTrigger);
        goalCollider.isTrigger = true;
        goalCollider.size = new Vector2(2f, 2f);
        SetLayerRecursively(goalTrigger, LayerMask.NameToLayer(GameLayers.Trigger));

        var player = EnsurePathInScene(gameplayScene, "SceneRoot/GameplayLayer/Runtime_Player");
        SetupPlayer(player);

        var bridgeRoot = EnsurePathInScene(gameplayScene, "SceneRoot");
        var bridge = EnsureComponent<GameplaySceneBridge>(bridgeRoot);
        AssignGameplayBridge(bridge, player, spawnPoint.transform, goalCollider);
    }

    private static void AssignBootstrap(BootBootstrap bootstrap, GameObject persistentRoot)
    {
        var serializedBootstrap = new SerializedObject(bootstrap);
        serializedBootstrap.FindProperty("persistentRoot").objectReferenceValue = persistentRoot;
        serializedBootstrap.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
        serializedBootstrap.FindProperty("gameplaySceneName").stringValue = "Gameplay";
        serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignGameLoop(
        GameLoop gameLoop,
        InputManager inputManager,
        UIManager uiManager,
        LevelManager levelManager)
    {
        var serializedLoop = new SerializedObject(gameLoop);
        serializedLoop.FindProperty("inputManager").objectReferenceValue = inputManager;
        serializedLoop.FindProperty("uiManager").objectReferenceValue = uiManager;
        serializedLoop.FindProperty("levelManager").objectReferenceValue = levelManager;
        serializedLoop.FindProperty("player").objectReferenceValue = null;
        serializedLoop.FindProperty("playerSpawnPoint").objectReferenceValue = null;
        serializedLoop.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignGameplayBridge(
        GameplaySceneBridge bridge,
        GameObject player,
        Transform spawnPoint,
        BoxCollider2D goalTrigger)
    {
        var serializedBridge = new SerializedObject(bridge);
        serializedBridge.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
        serializedBridge.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        serializedBridge.FindProperty("goalTrigger").objectReferenceValue = goalTrigger;
        serializedBridge.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignInputActions(InputManager inputManager)
    {
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions == null)
        {
            throw new FileNotFoundException($"Missing input actions asset: {InputActionsPath}");
        }

        var serializedInput = new SerializedObject(inputManager);
        serializedInput.FindProperty("inputActions").objectReferenceValue = inputActions;
        serializedInput.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignUIManager(
        UIManager uiManager,
        GameObject mainMenuPanel,
        GameObject gameplayPanel,
        GameObject pausePanel,
        GameObject resultPanel)
    {
        var serializedUi = new SerializedObject(uiManager);
        serializedUi.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
        serializedUi.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        serializedUi.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        serializedUi.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        serializedUi.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignLevelManagerBoot(LevelManager levelManager)
    {
        var serializedLevel = new SerializedObject(levelManager);
        serializedLevel.FindProperty("defaultSpawnPoint").objectReferenceValue = null;
        serializedLevel.FindProperty("goalTrigger").objectReferenceValue = null;
        serializedLevel.FindProperty("deathY").floatValue = -10f;
        serializedLevel.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupPlayer(GameObject player)
    {
        var rigidbody = EnsureComponent<Rigidbody2D>(player);
        rigidbody.gravityScale = 3f;
        rigidbody.freezeRotation = true;

        SetLayerRecursively(player, LayerMask.NameToLayer(GameLayers.Player));

        var collider = EnsureComponent<BoxCollider2D>(player);
        collider.size = new Vector2(0.8f, 1.2f);

        var groundCheckTransform = player.transform.Find("GroundCheck");
        if (groundCheckTransform == null)
        {
            var groundCheckObject = new GameObject("GroundCheck");
            groundCheckObject.transform.SetParent(player.transform, false);
            groundCheckObject.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            groundCheckTransform = groundCheckObject.transform;
        }

        var controller = EnsureComponent<PlayerController>(player);
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("groundCheck").objectReferenceValue = groundCheckTransform;
        serializedController.FindProperty("groundCheckRadius").floatValue = 0.1f;
        serializedController.FindProperty("groundLayer").intValue = GameLayers.GroundCheckMask.value;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        gameObject.layer = layer;

        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void UpdateBuildSettings()
    {
        var scenePaths = new[]
        {
            BootScenePath,
            MainMenuScenePath,
            PlayerGuideScenePath,
            LoadingScenePath,
            GameplayScenePath
        };

        var scenes = new List<EditorBuildSettingsScene>();
        foreach (var scenePath in scenePaths)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"Build settings skipped missing scene: {scenePath}");
                continue;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void MoveToScene(GameObject gameObject, Scene targetScene, Transform parent)
    {
        if (gameObject.transform.parent != null)
        {
            gameObject.transform.SetParent(null, false);
        }

        SceneManager.MoveGameObjectToScene(gameObject, targetScene);
        gameObject.transform.SetParent(parent, false);
    }

    private static GameObject EnsureRootInScene(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        var created = new GameObject(name);
        SceneManager.MoveGameObjectToScene(created, scene);
        return created;
    }

    private static GameObject EnsureChildInScene(Scene scene, GameObject parent, string name)
    {
        var existing = parent.transform.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static GameObject EnsurePathInScene(Scene scene, string path)
    {
        var segments = path.Split('/');
        GameObject current = null;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == segments[0])
            {
                current = root;
                break;
            }
        }

        if (current == null)
        {
            current = new GameObject(segments[0]);
            SceneManager.MoveGameObjectToScene(current, scene);
        }

        for (var index = 1; index < segments.Length; index++)
        {
            var childTransform = current.transform.Find(segments[index]);
            if (childTransform == null)
            {
                var childObject = new GameObject(segments[index]);
                childObject.transform.SetParent(current.transform, false);
                current = childObject;
            }
            else
            {
                current = childTransform.gameObject;
            }
        }

        return current;
    }

    private static GameObject FindInScene(Scene scene, string path)
    {
        var segments = path.Split('/');
        Transform current = null;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == segments[0])
            {
                current = root.transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (var index = 1; index < segments.Length; index++)
        {
            current = current.Find(segments[index]);
            if (current == null)
            {
                return null;
            }
        }

        return current.gameObject;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static void FixCanvasScale(GameObject canvasObject)
    {
        if (canvasObject == null)
        {
            return;
        }

        var rectTransform = canvasObject.GetComponent<RectTransform>();
        if (rectTransform != null && rectTransform.localScale == Vector3.zero)
        {
            rectTransform.localScale = Vector3.one;
        }
    }
}
