using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class SceneHierarchyGenerator
{
    private const string GameplayScenePath = "Assets/Game/Scenes/Gameplay.unity";
    private const string MainMenuScenePath = "Assets/Game/Scenes/MainMenu.unity";
    private const string PlayerGuideScenePath = "Assets/Game/Scenes/PlayerGuide.unity";
    private const string LoadingScenePath = "Assets/Game/Scenes/Loading.unity";

    private static readonly string[] WorkScenePaths =
    {
        "Assets/Game/Editor/WorkScenes/Room_001_WorkScene.unity",
        "Assets/Game/Editor/WorkScenes/Room_002_WorkScene.unity",
        "Assets/Game/Editor/WorkScenes/Room_003_WorkScene.unity",
        "Assets/Game/Editor/WorkScenes/TestRoom_WorkScene.unity"
    };

    [MenuItem("Tools/Project Structure/Generate Final Scene Hierarchy")]
    public static void Generate()
    {
        EnsureFolders();
        ProjectLayerSetup.Setup();
        GenerateGameplayScene();
        GenerateSimpleScene(MainMenuScenePath, "MainMenuRoot", "Canvas_MainMenu");
        GenerateSimpleScene(PlayerGuideScenePath, "PlayerGuideRoot", "Canvas_PlayerGuide");
        GenerateSimpleScene(LoadingScenePath, "LoadingRoot", "Canvas_Loading");

        foreach (var path in WorkScenePaths)
        {
            GenerateWorkScene(path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Final scene hierarchy generation completed.");
    }

    public static void GenerateBatch()
    {
        try
        {
            Generate();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    public static void ValidateBatch()
    {
        try
        {
            ValidateGameplayScene();
            ValidateSimpleScene(MainMenuScenePath, "MainMenuRoot", "Canvas_MainMenu");
            ValidateSimpleScene(PlayerGuideScenePath, "PlayerGuideRoot", "Canvas_PlayerGuide");
            ValidateSimpleScene(LoadingScenePath, "LoadingRoot", "Canvas_Loading");

            foreach (var path in WorkScenePaths)
            {
                ValidateWorkScene(path);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Final scene hierarchy validation completed.");
        EditorApplication.Exit(0);
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Game");
        EnsureFolder("Assets/Game/Scenes");
        EnsureFolder("Assets/Game/Editor");
        EnsureFolder("Assets/Game/Editor/WorkScenes");
    }

    private static void GenerateGameplayScene()
    {
        var scene = CreateFreshScene();

        var managers = EnsureRoot("Managers");
        EnsureChildren(managers, "GameManager", "SceneLoader", "SceneTransitionManager", "PlayerSpawnManager",
            "CameraManager", "UIManager", "AudioManager", "DataCollectionManager");

        var camera = EnsureRoot("Camera");
        var mainCamera = EnsureChild(camera, "MainCamera");
        EnsureCamera(mainCamera, true);
        var cinemachineCamera = EnsureChild(camera, "CinemachineVirtualCamera");
        EnsureOptionalComponent(cinemachineCamera, "Unity.Cinemachine.CinemachineCamera");

        var lighting = EnsureRoot("Lighting");
        var globalLight = EnsureChild(lighting, "GlobalLight2D");
        EnsureOptionalComponent(globalLight, "UnityEngine.Rendering.Universal.Light2D");
        var globalVolume = EnsureChild(lighting, "GlobalVolume");
        EnsureOptionalComponent(globalVolume, "UnityEngine.Rendering.Volume");

        var sceneRoot = EnsureRoot("SceneRoot");
        EnsureNested(sceneRoot, "BackgroundLayer/Runtime_Background");
        EnsureNested(sceneRoot, "MidgroundLayer/Runtime_Midground");
        var gameplayLayer = EnsureChild(sceneRoot, "GameplayLayer");
        EnsureChildren(gameplayLayer, "Runtime_Tilemap", "Runtime_Player", "Runtime_Platforms", "Runtime_Enemies",
            "Runtime_Traps", "Runtime_Collectibles", "Runtime_Interactables", "Runtime_Exits");
        EnsureNested(sceneRoot, "ForegroundLayer/Runtime_Foreground");
        var effectsLayer = EnsureChild(sceneRoot, "EffectsLayer");
        EnsureChildren(effectsLayer, "Runtime_Particles", "Runtime_Lights", "Runtime_Decals", "Runtime_TempEffects");

        var sceneTriggers = EnsureRoot("SceneTriggers");
        EnsureChildren(sceneTriggers, "Runtime_RoomTransitionTriggers", "Runtime_AreaTriggers",
            "Runtime_TutorialTriggers", "Runtime_DataCollectionTriggers");

        var ui = EnsureRoot("UI");
        EnsureCanvas(ui, "Canvas_HUD", "HealthBar", "EnergyBar", "SkillIcons", "CollectibleCounter");
        EnsureCanvas(ui, "Canvas_Map", "MiniMap", "RoomMap", "PlayerMarker");
        EnsureCanvas(ui, "Canvas_Prompt", "InteractionPrompt", "TutorialPrompt", "AreaNameText");
        EnsureCanvas(ui, "Canvas_Transition", "FadePanel", "LoadingIcon", "SceneTitle");
        EnsureCanvas(ui, "Canvas_Menu", "PauseMenu", "SettingsPanel", "ConfirmDialog");
        EnsureCanvas(ui, "Canvas_Debug", "FPSCounter", "PositionText", "SceneIdText", "DataLogText", "HeatmapOverlay");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameplayScenePath);
    }

    private static Scene CreateFreshScene()
    {
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void GenerateSimpleScene(string scenePath, string rootName, string canvasName)
    {
        var scene = OpenOrCreateScene(scenePath);
        EnsureRoot(rootName);
        var camera = EnsureRoot("Camera");
        EnsureCamera(EnsureChild(camera, "MainCamera"), true);
        EnsureCanvas(EnsureRoot("UI"), canvasName);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void GenerateWorkScene(string scenePath)
    {
        var scene = OpenOrCreateScene(scenePath);

        var exportRoot = EnsureRoot("ExportRoot");
        var grid = EnsureChild(exportRoot, "Grid");
        EnsureComponent<Grid>(grid);
        EnsureTilemap(grid, "Tilemap_Background");
        EnsureTilemap(grid, "Tilemap_Midground");
        EnsureTilemap(grid, "Tilemap_Ground");
        EnsureTilemap(grid, "Tilemap_Platform");
        EnsureTilemap(grid, "Tilemap_Foreground");

        var gameplayObjects = EnsureChild(exportRoot, "GameplayObjects");
        EnsureChildren(gameplayObjects, "PlayerSpawn", "Platforms", "MovingPlatforms", "OneWayPlatforms",
            "Enemies", "Mechanisms", "Collectibles", "SceneObjects", "Transitions");

        var visualObjects = EnsureChild(exportRoot, "VisualObjects");
        EnsureChildren(visualObjects, "BackgroundProps", "MidgroundProps", "ForegroundProps");

        var effects = EnsureChild(exportRoot, "Effects");
        EnsureChildren(effects, "Particles", "Lights", "LightBeams", "Decals");

        var bounds = EnsureChild(exportRoot, "Bounds");
        EnsureChildren(bounds, "CameraBounds", "RoomBounds");

        var editorOnly = EnsureRoot("EditorOnly");
        EnsureChildren(editorOnly, "PatrolPoints", "MovePathPoints", "Notes", "Gizmos", "DebugHelpers");

        var preview = EnsureRoot("Preview");
        EnsureCamera(EnsureChild(preview, "PreviewCamera"), false);
        var previewLighting = EnsureChild(preview, "PreviewLighting");
        EnsureOptionalComponent(previewLighting, "UnityEngine.Rendering.Universal.Light2D");
        EnsureChild(preview, "PreviewPlayer");
        EnsureChild(preview, "PreviewUI");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static Scene OpenOrCreateScene(string path)
    {
        if (File.Exists(path))
        {
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, path);
        return scene;
    }

    private static GameObject EnsureRoot(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return new GameObject(name);
    }

    private static GameObject EnsureChild(GameObject parent, string name)
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

    private static GameObject EnsureNested(GameObject parent, string path)
    {
        var current = parent;
        foreach (var segment in path.Split('/'))
        {
            current = EnsureChild(current, segment);
        }

        return current;
    }

    private static void EnsureChildren(GameObject parent, params string[] childNames)
    {
        foreach (var childName in childNames)
        {
            EnsureChild(parent, childName);
        }
    }

    private static void EnsureCamera(GameObject gameObject, bool mainCamera)
    {
        var camera = EnsureComponent<Camera>(gameObject);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        EnsureComponent<AudioListener>(gameObject);

        if (mainCamera)
        {
            gameObject.tag = "MainCamera";
        }
    }

    private static void EnsureCanvas(GameObject parent, string canvasName, params string[] childNames)
    {
        var canvasObject = EnsureChild(parent, canvasName);
        var canvas = EnsureComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        EnsureComponent<CanvasScaler>(canvasObject);
        EnsureComponent<GraphicRaycaster>(canvasObject);
        EnsureChildren(canvasObject, childNames);
    }

    private static void EnsureTilemap(GameObject parent, string name)
    {
        var tilemap = EnsureChild(parent, name);
        EnsureComponent<Tilemap>(tilemap);
        EnsureComponent<TilemapRenderer>(tilemap);
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void EnsureOptionalComponent(GameObject gameObject, string typeName)
    {
        var type = FindComponentType(typeName);
        if (type == null)
        {
            Debug.LogWarning($"Optional component type not found: {typeName}");
            return;
        }

        if (!typeof(Component).IsAssignableFrom(type))
        {
            Debug.LogWarning($"Optional type is not a component: {typeName}");
            return;
        }

        if (gameObject.GetComponent(type) == null)
        {
            gameObject.AddComponent(type);
        }
    }

    private static Type FindComponentType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
        {
            throw new InvalidOperationException($"Invalid folder path: {path}");
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void ValidateGameplayScene()
    {
        if (!File.Exists(GameplayScenePath))
        {
            throw new InvalidOperationException($"Missing scene: {GameplayScenePath}");
        }

        EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        RequireRoots("Managers", "Camera", "Lighting", "SceneRoot", "SceneTriggers", "UI");
        RequirePath("Managers/GameManager");
        RequirePath("Managers/SceneLoader");
        RequirePath("Managers/SceneTransitionManager");
        RequirePath("Managers/PlayerSpawnManager");
        RequirePath("Managers/CameraManager");
        RequirePath("Managers/UIManager");
        RequirePath("Managers/AudioManager");
        RequirePath("Managers/DataCollectionManager");
        RequirePath("SceneRoot/BackgroundLayer/Runtime_Background");
        RequirePath("SceneRoot/MidgroundLayer/Runtime_Midground");
        RequirePath("SceneRoot/GameplayLayer/Runtime_Tilemap");
        RequirePath("SceneRoot/GameplayLayer/Runtime_Player");
        RequirePath("SceneRoot/GameplayLayer/Runtime_Platforms");
        RequirePath("SceneRoot/GameplayLayer/Runtime_Enemies");
        RequirePath("SceneRoot/GameplayLayer/Runtime_Traps");
        RequirePath("SceneRoot/GameplayLayer/Runtime_Collectibles");
        RequirePath("SceneRoot/GameplayLayer/Runtime_Interactables");
        RequirePath("SceneRoot/GameplayLayer/Runtime_Exits");
        RequirePath("SceneRoot/ForegroundLayer/Runtime_Foreground");
        RequirePath("SceneRoot/EffectsLayer/Runtime_Particles");
        RequirePath("SceneRoot/EffectsLayer/Runtime_Lights");
        RequirePath("SceneRoot/EffectsLayer/Runtime_Decals");
        RequirePath("SceneRoot/EffectsLayer/Runtime_TempEffects");
        RequirePath("SceneTriggers/Runtime_RoomTransitionTriggers");
        RequirePath("SceneTriggers/Runtime_AreaTriggers");
        RequirePath("SceneTriggers/Runtime_TutorialTriggers");
        RequirePath("SceneTriggers/Runtime_DataCollectionTriggers");
        RequireComponent<Camera>("Camera/MainCamera");
        RequireComponent<AudioListener>("Camera/MainCamera");
        RequireOptionalComponent("Camera/CinemachineVirtualCamera", "Unity.Cinemachine.CinemachineCamera");
        RequireOptionalComponent("Lighting/GlobalLight2D", "UnityEngine.Rendering.Universal.Light2D");
        RequireOptionalComponent("Lighting/GlobalVolume", "UnityEngine.Rendering.Volume");
        RequireComponent<Canvas>("UI/Canvas_HUD");
        RequireComponent<CanvasScaler>("UI/Canvas_HUD");
        RequireComponent<GraphicRaycaster>("UI/Canvas_HUD");
    }

    private static void ValidateSimpleScene(string path, string rootName, string canvasName)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Missing scene: {path}");
        }

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        RequireRoots(rootName, "Camera", "UI");
        RequireComponent<Camera>("Camera/MainCamera");
        RequireComponent<Canvas>($"UI/{canvasName}");
    }

    private static void ValidateWorkScene(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Missing scene: {path}");
        }

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        RequireRoots("ExportRoot", "EditorOnly", "Preview");
        RequireComponent<Grid>("ExportRoot/Grid");
        RequireTilemap("ExportRoot/Grid/Tilemap_Background");
        RequireTilemap("ExportRoot/Grid/Tilemap_Midground");
        RequireTilemap("ExportRoot/Grid/Tilemap_Ground");
        RequireTilemap("ExportRoot/Grid/Tilemap_Platform");
        RequireTilemap("ExportRoot/Grid/Tilemap_Foreground");
        RequireComponent<Camera>("Preview/PreviewCamera");
        RequireComponent<AudioListener>("Preview/PreviewCamera");
        RequireOptionalComponent("Preview/PreviewLighting", "UnityEngine.Rendering.Universal.Light2D");
    }

    private static void RequireRoots(params string[] names)
    {
        var roots = new HashSet<string>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            roots.Add(root.name);
        }

        foreach (var name in names)
        {
            if (!roots.Contains(name))
            {
                throw new InvalidOperationException($"Missing root object: {name}");
            }
        }
    }

    private static void RequireTilemap(string path)
    {
        RequireComponent<Tilemap>(path);
        RequireComponent<TilemapRenderer>(path);
    }

    private static void RequireComponent<T>(string path) where T : Component
    {
        var gameObject = RequirePath(path);
        if (gameObject.GetComponent<T>() == null)
        {
            throw new InvalidOperationException($"Missing component {typeof(T).Name} on {path}");
        }
    }

    private static void RequireOptionalComponent(string path, string typeName)
    {
        var type = FindComponentType(typeName);
        if (type == null)
        {
            Debug.LogWarning($"Optional component type not found during validation: {typeName}");
            return;
        }

        var gameObject = RequirePath(path);
        if (gameObject.GetComponent(type) == null)
        {
            throw new InvalidOperationException($"Missing component {typeName} on {path}");
        }
    }

    private static GameObject RequirePath(string path)
    {
        var current = default(GameObject);
        var segments = path.Split('/');

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == segments[0])
            {
                current = root;
                break;
            }
        }

        if (current == null)
        {
            throw new InvalidOperationException($"Missing hierarchy path: {path}");
        }

        for (var index = 1; index < segments.Length; index++)
        {
            var child = current.transform.Find(segments[index]);
            if (child == null)
            {
                throw new InvalidOperationException($"Missing hierarchy path: {path}");
            }

            current = child.gameObject;
        }

        return current;
    }
}
