using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 在现有PlayerGuide场景基础上添加游戏场景元素
/// 创建平台、玩家、收集品、触发器等
/// </summary>
public static class PlayerGuideGameplaySetup
{
    private const string ScenePath = "Assets/Game/Scenes/PlayerGuide.unity";

    [MenuItem("Tools/GameJam/4. Setup PlayerGuide Gameplay")]
    public static void Setup()
    {
        var startTime = System.DateTime.Now;
        Debug.Log("=== 开始搭建PlayerGuide游戏场景 ===");

        if (!File.Exists(ScenePath))
        {
            Debug.LogError($"场景不存在: {ScenePath}");
            return;
        }

        ProjectLayerSetup.Setup();

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        CreateSceneHierarchy();
        CreatePlayer();
        CreatePlatforms();
        CreateCollectibles();
        CreateNPC();
        CreateTutorialTriggers();
        CreateCamera();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        var duration = (System.DateTime.Now - startTime).TotalSeconds;
        Debug.Log($"=== PlayerGuide场景搭建完成！耗时 {duration:F1} 秒 ===");
        Debug.Log("提示：现在可以使用Tools菜单中的对话生成工具创建教学对话");
    }

    private static void CreateSceneHierarchy()
    {
        Debug.Log("创建场景层级结构...");

        var sceneRoot = EnsurePath("SceneRoot");

        var environment = EnsurePath("SceneRoot/Environment");
        EnsurePath("SceneRoot/Environment/Background");
        EnsurePath("SceneRoot/Environment/Platforms");
        EnsurePath("SceneRoot/Environment/Decorations");

        var gameplay = EnsurePath("SceneRoot/GameplayLayer");
        EnsurePath("SceneRoot/GameplayLayer/Player");
        EnsurePath("SceneRoot/GameplayLayer/NPCs");
        EnsurePath("SceneRoot/GameplayLayer/Collectibles");
        EnsurePath("SceneRoot/GameplayLayer/Triggers");

        EnsurePath("SceneRoot/Lighting");

        Debug.Log("✓ 场景层级结构创建完成");
    }

    private static void CreatePlayer()
    {
        Debug.Log("创建玩家对象...");

        var playerRoot = GameObject.Find("SceneRoot/GameplayLayer/Player");
        if (playerRoot == null)
        {
            Debug.LogError("未找到Player父节点");
            return;
        }

        // 清空现有Player子对象
        for (int i = playerRoot.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(playerRoot.transform.GetChild(i).gameObject);
        }

        var player = new GameObject("PlayerCharacter");
        player.transform.SetParent(playerRoot.transform, false);
        player.transform.position = new Vector3(-8f, 1f, 0f);
        player.layer = LayerMask.NameToLayer(GameLayers.Player);

        // SpriteRenderer
        var spriteRenderer = player.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Player";
        spriteRenderer.sortingOrder = 0;

        // 尝试加载第一帧Idle动画作为默认显示
        var idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/Idle/1.png");
        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }

        // Animator
        var animator = player.AddComponent<Animator>();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Game/Animations/Player/PlayerAnimatorController.controller");
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        // Rigidbody2D
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // BoxCollider2D
        var collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 1.2f);
        collider.offset = new Vector2(0f, 0f);

        // GroundCheck
        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -0.6f, 0f);

        // PlayerController
        var controller_script = player.AddComponent<PlayerController>();
        var serializedController = new SerializedObject(controller_script);
        serializedController.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        serializedController.FindProperty("groundCheckRadius").floatValue = 0.15f;
        serializedController.FindProperty("groundLayer").intValue = GameLayers.GroundCheckMask.value;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        // PlayerAnimationStateController
        player.AddComponent<PlayerAnimationStateController>();

        Debug.Log("✓ 玩家对象创建完成");
    }

    private static void CreatePlatforms()
    {
        Debug.Log("创建平台...");

        var platformsRoot = GameObject.Find("SceneRoot/Environment/Platforms");
        if (platformsRoot == null)
        {
            Debug.LogError("未找到Platforms父节点");
            return;
        }

        // 清空现有平台
        for (int i = platformsRoot.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(platformsRoot.transform.GetChild(i).gameObject);
        }

        // 起始平台（大）
        CreatePlatform(platformsRoot.transform, "Ground_Start", new Vector3(-8f, 0f, 0f), new Vector2(6f, 1f));

        // 跳跃教学平台（中）
        CreatePlatform(platformsRoot.transform, "Platform_Jump1", new Vector3(-2f, 2f, 0f), new Vector2(3f, 0.5f));

        // 收集品平台（小）
        CreatePlatform(platformsRoot.transform, "Platform_Collect1", new Vector3(2f, 4f, 0f), new Vector2(2f, 0.5f));
        CreatePlatform(platformsRoot.transform, "Platform_Collect2", new Vector3(5f, 3f, 0f), new Vector2(2f, 0.5f));

        // 终点平台（大）
        CreatePlatform(platformsRoot.transform, "Ground_End", new Vector3(9f, 1f, 0f), new Vector2(5f, 1f));

        Debug.Log("✓ 平台创建完成（5个平台）");
    }

    private static void CreatePlatform(Transform parent, string name, Vector3 position, Vector2 size)
    {
        var platform = new GameObject(name);
        platform.transform.SetParent(parent, false);
        platform.transform.position = position;
        platform.layer = LayerMask.NameToLayer(GameLayers.Ground);

        // SpriteRenderer（临时使用白色方块）
        var spriteRenderer = platform.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Environment";
        spriteRenderer.sortingOrder = 0;
        spriteRenderer.color = new Color(0.3f, 0.5f, 0.3f, 1f); // 深绿色

        // 创建临时sprite
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        spriteRenderer.sprite = sprite;
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = size;

        // BoxCollider2D
        var collider = platform.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void CreateCollectibles()
    {
        Debug.Log("创建收集品...");

        var collectiblesRoot = GameObject.Find("SceneRoot/GameplayLayer/Collectibles");
        if (collectiblesRoot == null)
        {
            Debug.LogError("未找到Collectibles父节点");
            return;
        }

        // 清空现有收集品
        for (int i = collectiblesRoot.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(collectiblesRoot.transform.GetChild(i).gameObject);
        }

        CreateCollectible(collectiblesRoot.transform, "Collectible_01", new Vector3(2f, 5f, 0f));
        CreateCollectible(collectiblesRoot.transform, "Collectible_02", new Vector3(5f, 4f, 0f));
        CreateCollectible(collectiblesRoot.transform, "Collectible_03", new Vector3(9f, 2.5f, 0f));

        Debug.Log("✓ 收集品创建完成（3个）");
    }

    private static void CreateCollectible(Transform parent, string name, Vector3 position)
    {
        var collectible = new GameObject(name);
        collectible.transform.SetParent(parent, false);
        collectible.transform.position = position;
        collectible.layer = LayerMask.NameToLayer(GameLayers.Collectible);

        // SpriteRenderer（临时使用黄色圆形）
        var spriteRenderer = collectible.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Collectible";
        spriteRenderer.sortingOrder = 0;
        spriteRenderer.color = new Color(1f, 0.9f, 0.2f, 1f); // 金黄色

        // 创建临时圆形sprite
        var texture = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                var dx = x - 16f;
                var dy = y - 16f;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                texture.SetPixel(x, y, distance < 16 ? Color.white : Color.clear);
            }
        }
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
        spriteRenderer.sprite = sprite;

        // CircleCollider2D
        var collider = collectible.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.4f;

        // CollectibleItem脚本
        collectible.AddComponent<CollectibleItem>();
    }

    private static void CreateNPC()
    {
        Debug.Log("创建NPC（腓腓）...");

        var npcsRoot = GameObject.Find("SceneRoot/GameplayLayer/NPCs");
        if (npcsRoot == null)
        {
            Debug.LogError("未找到NPCs父节点");
            return;
        }

        // 清空现有NPC
        for (int i = npcsRoot.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(npcsRoot.transform.GetChild(i).gameObject);
        }

        var npc = new GameObject("NPC_Feifei");
        npc.transform.SetParent(npcsRoot.transform, false);
        npc.transform.position = new Vector3(11f, 2f, 0f);
        npc.layer = LayerMask.NameToLayer(GameLayers.NPC);

        // SpriteRenderer
        var spriteRenderer = npc.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "NPC";
        spriteRenderer.sortingOrder = 0;

        // 尝试加载腓腓Idle动画第一帧
        var feifeiSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Feifei/Idle/1.png");
        if (feifeiSprite != null)
        {
            spriteRenderer.sprite = feifeiSprite;
        }

        // Animator
        var animator = npc.AddComponent<Animator>();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Game/Animations/Feifei/FeifeiAnimatorController.controller");
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        // BoxCollider2D（用于交互检测）
        var collider = npc.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1.5f);

        // NPCInteractable脚本
        var interactable = npc.AddComponent<NPCInteractable>();
        var serializedInteractable = new SerializedObject(interactable);
        serializedInteractable.FindProperty("npcName").stringValue = "腓腓";
        serializedInteractable.FindProperty("dialogueId").stringValue = "dlg_feifei_greeting";
        serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("✓ NPC创建完成");
    }

    private static void CreateTutorialTriggers()
    {
        Debug.Log("创建教学触发器...");

        var triggersRoot = GameObject.Find("SceneRoot/GameplayLayer/Triggers");
        if (triggersRoot == null)
        {
            Debug.LogError("未找到Triggers父节点");
            return;
        }

        // 清空现有触发器
        for (int i = triggersRoot.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(triggersRoot.transform.GetChild(i).gameObject);
        }

        // 移动教学触发器
        CreateTrigger(triggersRoot.transform, "Trigger_MoveTeach",
            new Vector3(-6f, 1.5f, 0f), new Vector2(2f, 3f), "dlg_tutorial_move");

        // 跳跃教学触发器
        CreateTrigger(triggersRoot.transform, "Trigger_JumpTeach",
            new Vector3(-3f, 1.5f, 0f), new Vector2(2f, 3f), "dlg_tutorial_jump");

        // 收集教学触发器
        CreateTrigger(triggersRoot.transform, "Trigger_CollectTeach",
            new Vector3(1f, 3f, 0f), new Vector2(2f, 3f), "dlg_tutorial_collect");

        // 关卡目标触发器
        CreateGoalTrigger(triggersRoot.transform, "Trigger_LevelGoal", new Vector3(13f, 2f, 0f));

        Debug.Log("✓ 教学触发器创建完成（4个）");
    }

    private static void CreateTrigger(Transform parent, string name, Vector3 position, Vector2 size, string dialogueId)
    {
        var trigger = new GameObject(name);
        trigger.transform.SetParent(parent, false);
        trigger.transform.position = position;
        trigger.layer = LayerMask.NameToLayer(GameLayers.Trigger);

        var collider = trigger.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        // 临时使用简单的触发脚本（需要后续实现TutorialTrigger.cs）
        var triggerScript = trigger.AddComponent<NPCInteractable>();
        var serialized = new SerializedObject(triggerScript);
        serialized.FindProperty("dialogueId").stringValue = dialogueId;
        serialized.FindProperty("autoTriggerOnEnter").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateGoalTrigger(Transform parent, string name, Vector3 position)
    {
        var trigger = new GameObject(name);
        trigger.transform.SetParent(parent, false);
        trigger.transform.position = position;
        trigger.layer = LayerMask.NameToLayer(GameLayers.Trigger);

        var collider = trigger.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2f, 3f);

        // SceneTransitionInteractable
        var transition = trigger.AddComponent<SceneTransitionInteractable>();
        var serialized = new SerializedObject(transition);
        serialized.FindProperty("targetScene").stringValue = "Gameplay";
        serialized.FindProperty("autoTransition").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateCamera()
    {
        Debug.Log("配置相机...");

        var cameraObj = Camera.main?.gameObject;
        if (cameraObj == null)
        {
            Debug.LogWarning("未找到主相机");
            return;
        }

        // 设置相机位置
        cameraObj.transform.position = new Vector3(0f, 2f, -10f);

        // 添加CameraTargetFollow脚本
        var cameraFollow = cameraObj.GetComponent<CameraTargetFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = cameraObj.AddComponent<CameraTargetFollow>();
        }

        // 查找Player并设置为跟随目标
        var player = GameObject.Find("SceneRoot/GameplayLayer/Player/PlayerCharacter");
        if (player != null)
        {
            var serialized = new SerializedObject(cameraFollow);
            serialized.FindProperty("target").objectReferenceValue = player.transform;
            serialized.FindProperty("smoothSpeed").floatValue = 5f;
            serialized.FindProperty("offset").vector3Value = new Vector3(0f, 1f, -10f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log("✓ 相机配置完成");
    }

    private static GameObject EnsurePath(string path)
    {
        var segments = path.Split('/');
        GameObject current = null;

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
            current = new GameObject(segments[0]);
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
}
