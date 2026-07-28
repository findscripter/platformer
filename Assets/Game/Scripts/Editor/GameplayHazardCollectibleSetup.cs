using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GameplayHazardCollectibleSetup
{
    private const string TrapTexturePath =
        "Assets/Game/Art/Characters/images/trap.png";
    private const string CollectibleTexturePath =
        "Assets/Game/Art/Characters/images/collectedItem.png";
    private const string EnemyTexturePath =
        "Assets/Game/Art/Characters/images/enemy_move.png";

    private const string SpikePrefabPath =
        "Assets/Game/Prefabs/Mechanism/SpikeTrap.prefab";
    private const string CollectiblePrefabPath =
        "Assets/Game/Prefabs/Collectible/CollectibleItem.prefab";
    private const string EnemyPrefabPath =
        "Assets/Game/Prefabs/Enemy/PatrolEnemy.prefab";
    private const string EnemyAnimationFolder =
        "Assets/Game/Resources/Enemy";
    private const string EnemyClipPath =
        EnemyAnimationFolder + "/enemy_move.anim";
    private const string EnemyControllerPath =
        EnemyAnimationFolder + "/PatrolEnemy.controller";
    private const string GameplayScenePath =
        "Assets/Game/Scenes/Gameplay.unity";

    static GameplayHazardCollectibleSetup()
    {
        EditorApplication.delayCall += BuildMissingAssets;
    }

    [MenuItem("Tools/Platformer/Rebuild Hazards, Collectibles and Enemies")]
    public static void RebuildFromMenu()
    {
        BuildAll();
    }

    public static void SetupBatch()
    {
        try
        {
            BuildAll();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Gameplay object setup failed: {exception}");
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    private static void BuildMissingAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject spikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePrefabPath);
        GameObject collectiblePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(CollectiblePrefabPath);
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (HasCorrectVisualLayer(spikePrefab) &&
            HasCorrectVisualLayer(collectiblePrefab) &&
            HasCorrectVisualLayer(enemyPrefab))
        {
            return;
        }

        BuildAll();
    }

    private static void BuildAll()
    {
        EnsureRequiredFolders();
        ConfigureTexture(TrapTexturePath, 1024f, FilterMode.Point);
        ConfigureTexture(CollectibleTexturePath, 512f, FilterMode.Point);
        ConfigureTexture(EnemyTexturePath, 300f, FilterMode.Point);

        Sprite trapSprite = LoadSprites(TrapTexturePath).FirstOrDefault();
        Sprite collectibleSprite = LoadSprites(CollectibleTexturePath).FirstOrDefault();
        Sprite[] enemySprites = LoadSprites(EnemyTexturePath);

        if (trapSprite == null || collectibleSprite == null || enemySprites.Length != 6)
        {
            throw new InvalidOperationException(
                "Expected one trap sprite, one collectible sprite and six enemy sprites.");
        }

        AnimatorController enemyController = BuildEnemyAnimation(enemySprites);
        BuildSpikePrefab(trapSprite);
        BuildCollectiblePrefab(collectibleSprite);
        BuildEnemyPrefab(enemySprites[0], enemyController);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        PopulateGameplayScene();
        Debug.Log("Spike trap, collectible and patrol enemy assets were rebuilt successfully.");
    }

    private static void ConfigureTexture(string path, float pixelsPerUnit, FilterMode filterMode)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new FileNotFoundException($"Missing texture importer: {path}");
        }

        bool changed =
            !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit) ||
            importer.filterMode != filterMode ||
            importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed;

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filterMode;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(sprite => sprite.rect.width >= 64f && sprite.rect.height >= 64f)
            .OrderBy(sprite => sprite.rect.x)
            .ToArray();
    }

    private static AnimatorController BuildEnemyAnimation(Sprite[] sprites)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(EnemyClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "enemy_move", frameRate = 10f };
            AssetDatabase.CreateAsset(clip, EnemyClipPath);
        }

        clip.frameRate = 10f;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "Visual",
            propertyName = "m_Sprite"
        };
        ObjectReferenceKeyframe[] frames = sprites
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprite
            })
            .ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings =
            serializedClip.FindProperty("m_AnimationClipSettings");
        SerializedProperty loopTime = settings?.FindPropertyRelative("m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = true;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(clip);

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(EnemyControllerPath);
        }

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(EnemyControllerPath);
        AnimatorState state = controller.layers[0].stateMachine.AddState("Move");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void BuildSpikePrefab(Sprite sprite)
    {
        GameObject root = new GameObject("SpikeTrap");
        try
        {
            SetLayerRecursively(root, GameLayers.MechanismLayer);
            CreateVisual(root.transform, sprite, new Vector3(2f, 2f, 1f));

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.8f, 0.75f);
            collider.offset = new Vector2(0f, 0.15f);
            root.AddComponent<SpikeTrap>();

            PrefabUtility.SaveAsPrefabAsset(root, SpikePrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildCollectiblePrefab(Sprite sprite)
    {
        GameObject root = new GameObject("CollectibleItem");
        try
        {
            SetLayerRecursively(root, GameLayers.CollectibleLayer);
            CreateVisual(root.transform, sprite, Vector3.one);

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            root.AddComponent<CollectibleItem>();

            PrefabUtility.SaveAsPrefabAsset(root, CollectiblePrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildEnemyPrefab(Sprite sprite, RuntimeAnimatorController controller)
    {
        GameObject root = new GameObject("PatrolEnemy");
        try
        {
            SetLayerRecursively(root, GameLayers.EnemyLayer);
            SpriteRenderer renderer = CreateVisual(root.transform, sprite, Vector3.one);

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.85f, 0.95f);

            PatrolEnemy enemy = root.AddComponent<PatrolEnemy>();
            SerializedObject serializedEnemy = new SerializedObject(enemy);
            serializedEnemy.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static SpriteRenderer CreateVisual(
        Transform parent,
        Sprite sprite,
        Vector3 scale)
    {
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = scale;
        visual.layer = parent.gameObject.layer;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = GameLayers.InteractiveUpSorting;
        renderer.sortingOrder = 10;

        visual.transform.localPosition =
            -Vector3.Scale((Vector3)sprite.bounds.center, scale);
        return renderer;
    }

    private static bool HasCorrectVisualLayer(GameObject prefab)
    {
        if (prefab == null)
            return false;

        Transform visual = prefab.transform.Find("Visual");
        return visual != null && visual.gameObject.layer == prefab.layer;
    }

    private static void PopulateGameplayScene()
    {
        if (!File.Exists(GameplayScenePath))
        {
            throw new FileNotFoundException($"Missing scene: {GameplayScenePath}");
        }

        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        GameObject trapsRoot =
            FindSceneObject(scene, "SceneRoot/GameplayLayer/Runtime_Traps");
        GameObject collectiblesRoot =
            FindSceneObject(scene, "SceneRoot/GameplayLayer/Runtime_Collectibles");
        GameObject enemiesRoot =
            FindSceneObject(scene, "SceneRoot/GameplayLayer/Runtime_Enemies");

        CollectibleTracker tracker = collectiblesRoot.GetComponent<CollectibleTracker>();
        if (tracker == null)
        {
            tracker = collectiblesRoot.AddComponent<CollectibleTracker>();
        }

        ReplaceInstance(
            trapsRoot.transform,
            "Test_SpikeTrap",
            SpikePrefabPath,
            new Vector3(3.2f, -1.45f, 0f));
        GameObject collectible = ReplaceInstance(
            collectiblesRoot.transform,
            "Test_Collectible",
            CollectiblePrefabPath,
            new Vector3(6f, 0.25f, 0f));
        SerializedObject serializedCollectible =
            new SerializedObject(collectible.GetComponent<CollectibleItem>());
        serializedCollectible.FindProperty("tracker").objectReferenceValue = tracker;
        serializedCollectible.ApplyModifiedPropertiesWithoutUndo();

        GameObject enemy = ReplaceInstance(
            enemiesRoot.transform,
            "Test_PatrolEnemy",
            EnemyPrefabPath,
            new Vector3(15f, -1.15f, 0f));
        SerializedObject serializedEnemy =
            new SerializedObject(enemy.GetComponent<PatrolEnemy>());
        serializedEnemy.FindProperty("patrolStartOffset").vector2Value =
            new Vector2(-2f, 0f);
        serializedEnemy.FindProperty("patrolEndOffset").vector2Value =
            new Vector2(2f, 0f);
        serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject ReplaceInstance(
        Transform parent,
        string name,
        string prefabPath,
        Vector3 position)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new FileNotFoundException($"Missing generated prefab: {prefabPath}");
        }

        GameObject instance =
            (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.position = position;
        return instance;
    }

    private static GameObject FindSceneObject(Scene scene, string path)
    {
        string[] segments = path.Split('/');
        Transform current = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == segments[0])
            ?.transform;

        for (int index = 1; index < segments.Length && current != null; index++)
        {
            current = current.Find(segments[index]);
        }

        if (current == null)
        {
            throw new InvalidOperationException($"Missing scene path: {path}");
        }

        return current.gameObject;
    }

    private static void EnsureRequiredFolders()
    {
        EnsureFolder("Assets/Game/Prefabs/Mechanism");
        EnsureFolder("Assets/Game/Prefabs/Collectible");
        EnsureFolder("Assets/Game/Prefabs/Enemy");
        EnsureFolder(EnemyAnimationFolder);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = $"{current}/{parts[index]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }
            current = next;
        }
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        if (layer >= 0)
        {
            gameObject.layer = layer;
        }

        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
