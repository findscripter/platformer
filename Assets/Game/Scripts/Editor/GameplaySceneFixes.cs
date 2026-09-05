using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Gameplay 场景动画/背景/装饰一次性修复（2026-08-25 体检结论的落地）：
/// 1. 玩家 idle/run 动画 clip 改用新原画帧，待机 PPU 统一到 112（与跑跳可见身高一致 ~1.5 单位），
///    碰撞体/GroundCheck 对齐到角色实际脚底
/// 2. 怪物预制体 Animator 从根移到 Visual（修复动画绑定路径断裂），比例调到与玩家匹配，激活场景怪物
/// 3. sky 视差层开启启动对齐相机（配合 ParallaxLayer 预热帧修复背景半屏）
/// 4. 8 个 sprite 丢失的背景装饰改用 S01 场景元件
/// 5. 收集品预制体 Visual 指定 S01 梦滴 sprite
/// 可重复执行。
/// </summary>
public static class GameplaySceneFixes
{
    private const string IdleFolder = "Assets/Game/Art/Characters/player/idle";
    private const string RunFolder = "Assets/Game/Art/Characters/player/run";
    private const string IdleClipPath = "Assets/Game/Resources/Player/player_idle.anim";
    private const string RunClipPath = "Assets/Game/Resources/Player/player_run.anim";
    private const string EnemyPrefabPath = "Assets/Game/Prefabs/Enemy/PatrolEnemy.prefab";
    private const string CollectiblePrefabPath = "Assets/Game/Prefabs/Collectible/CollectibleItem.prefab";
    private const string MonsterWalkFirstFrame = "Assets/Game/Art/Characters/monster/walk/1.png";
    private const string S01 = "Assets/Game/Art/Scenes/S01";

    [MenuItem("Tools/GameJam/10. Fix Gameplay Animations And Decor")]
    public static void RunAll()
    {
        Debug.Log("=== Gameplay 修复开始 ===");

        Fix1_PlayerAnimation();
        Fix2_MonsterPrefabAndScene();
        Fix3_SkyParallaxSnap();
        Fix4_BackgroundDecor();
        Fix5_CollectibleSprite();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("=== Gameplay 修复完成，场景已保存 ===");
    }

    // ---------- 1. 玩家动画 ----------

    private static void Fix1_PlayerAnimation()
    {
        // 待机帧画布 834x1112（角色 835px 高）是走跑跳画布 1668x2388 的一半分辨率，
        // PPU 112 使待机可见身高 = 跑跳的 ~1.49 世界单位（scale 0.2 下）
        SetFolderPpu(IdleFolder, 112f);

        RebuildSpriteClip(IdleClipPath, IdleFolder, 1, 20, 12f);
        RebuildSpriteClip(RunClipPath, RunFolder, 1, 9, 12f);

        var player = GameObject.Find("test_player");
        if (player == null)
        {
            Debug.LogWarning("test_player 未找到，跳过碰撞体修正");
            return;
        }

        // 角色可见范围（scale 0.2）：脚底在枢轴下方 ~0.72 世界单位，身高 ~1.45
        var box = player.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(3.5f, 7.2f);   // 世界 0.7 x 1.44
            box.offset = Vector2.zero;            // 世界底边 = -0.72（脚底）
            EditorUtility.SetDirty(box);
        }

        var groundCheck = player.transform.Find("GroundCheck");
        if (groundCheck != null)
        {
            groundCheck.localPosition = new Vector3(0f, -3.6f, 0f); // 世界 -0.72
            EditorUtility.SetDirty(groundCheck);
        }

        Debug.Log("✓ Fix1 玩家动画：idle PPU=112，idle(20帧)/run(9帧) clip 重建为新原画，碰撞体对齐脚底");
    }

    private static void SetFolderPpu(string folder, float ppu)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || Mathf.Approximately(importer.spritePixelsPerUnit, ppu))
                continue;

            importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();
        }
    }

    private static void RebuildSpriteClip(string clipPath, string folder, int firstFrame, int lastFrame, float fps)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogWarning($"clip 不存在: {clipPath}");
            return;
        }

        clip.frameRate = fps;

        var keys = new List<ObjectReferenceKeyframe>();
        var frame = 0;
        for (var i = firstFrame; i <= lastFrame; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{i}.png");
            if (sprite == null)
            {
                Debug.LogWarning($"缺帧: {folder}/{i}.png");
                continue;
            }

            keys.Add(new ObjectReferenceKeyframe { time = frame / fps, value = sprite });
            frame++;
        }

        var binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite",
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    // ---------- 2. 怪物 ----------

    private static void Fix2_MonsterPrefabAndScene()
    {
        var walkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MonsterWalkFirstFrame);

        var root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        try
        {
            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                Debug.LogWarning("PatrolEnemy.prefab 无 Visual 子节点，跳过");
                return;
            }

            // 动画 clip 绑定 path=''，Animator 必须与 SpriteRenderer 同节点
            var rootAnimator = root.GetComponent<Animator>();
            RuntimeAnimatorController controller = null;
            if (rootAnimator != null)
            {
                controller = rootAnimator.runtimeAnimatorController;
                Object.DestroyImmediate(rootAnimator);
            }

            var visualAnimator = visual.GetComponent<Animator>();
            if (visualAnimator == null)
                visualAnimator = visual.gameObject.AddComponent<Animator>();
            if (controller != null)
                visualAnimator.runtimeAnimatorController = controller;

            // 怪物原画 1280x720@PPU100，角色实际 ~5.5 单位高 → 0.18 缩放后 ~0.98 单位
            visual.localScale = new Vector3(0.18f, 0.18f, 1f);
            var sr = visual.GetComponent<SpriteRenderer>();
            if (sr != null && walkSprite != null)
                sr.sprite = walkSprite;

            var capsule = root.GetComponent<CapsuleCollider2D>();
            if (capsule != null)
            {
                capsule.size = new Vector2(1.16f, 0.95f);
                capsule.offset = new Vector2(0f, -0.02f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        // 场景实例：清掉可能残留的实例覆盖值，激活并放到地面上（脚底贴 Ground_Main 顶面 -2.5）
        var sceneEnemy = FindSceneObjectIncludingInactive("Test_PatrolEnemy");
        if (sceneEnemy != null)
        {
            var sceneVisual = sceneEnemy.transform.Find("Visual");
            if (sceneVisual != null)
            {
                sceneVisual.localScale = new Vector3(0.18f, 0.18f, 1f);
                var sr = sceneVisual.GetComponent<SpriteRenderer>();
                if (sr != null && walkSprite != null)
                    sr.sprite = walkSprite;
            }

            var capsule = sceneEnemy.GetComponent<CapsuleCollider2D>();
            if (capsule != null)
            {
                capsule.size = new Vector2(1.16f, 0.95f);
                capsule.offset = new Vector2(0f, -0.02f);
            }

            sceneEnemy.transform.position = new Vector3(10f, -2.0f, 0f);
            sceneEnemy.SetActive(true);
            EditorUtility.SetDirty(sceneEnemy);
        }

        Debug.Log("✓ Fix2 怪物：Animator 移至 Visual（预制体+场景），比例 0.18，已激活并落地");
    }

    // ---------- 3. sky 视差 ----------

    private static void Fix3_SkyParallaxSnap()
    {
        var sky = GameObject.Find("sky");
        if (sky == null)
        {
            Debug.LogWarning("sky 未找到");
            return;
        }

        var layer = sky.GetComponent<ParallaxLayer>();
        if (layer == null)
        {
            Debug.LogWarning("sky 无 ParallaxLayer");
            return;
        }

        var so = new SerializedObject(layer);
        var prop = so.FindProperty("snapToCameraOnStart");
        if (prop == null)
        {
            Debug.LogWarning("snapToCameraOnStart 字段不存在（ParallaxLayer.cs 未编译？）");
            return;
        }

        prop.boolValue = true;
        so.ApplyModifiedProperties();
        Debug.Log("✓ Fix3 sky：snapToCameraOnStart=true（配合 ParallaxLayer 预热帧修复）");
    }

    // ---------- 4. 背景装饰 ----------

    private struct DecorSpec
    {
        public string Sprite;
        public string Name;
        public Vector3 Position;
        public float Scale;
        public int Order;
    }

    private static void Fix4_BackgroundDecor()
    {
        // mid 层（视差 0.8，中景）：发光山丘/鱼鳍坡，底边贴地面线 -2.5
        var midSpecs = new[]
        {
            new DecorSpec { Sprite = "15.png", Name = "Hill_Glow_Large", Position = new Vector3(-4.5f, -1.3f, 0f), Scale = 1.0f, Order = 5 },
            new DecorSpec { Sprite = "19.png", Name = "Hill_Glow_Small", Position = new Vector3(0.8f, -2.1f, 0f), Scale = 0.6f, Order = 5 },
            new DecorSpec { Sprite = "21.png", Name = "FinSlope_Large", Position = new Vector3(6.3f, -0.9f, 0f), Scale = 0.7f, Order = 4 },
            new DecorSpec { Sprite = "15.png", Name = "Hill_Glow_Far", Position = new Vector3(11.0f, -2.0f, 0f), Scale = 0.4f, Order = 5 },
        };
        ApplyDecor("mid", midSpecs, "Midground");

        // fore_back 层（视差 -0.3，前景）：莲花/玻璃花/蜻蜓
        var foreSpecs = new[]
        {
            new DecorSpec { Sprite = "16.png", Name = "GlassFlowers", Position = new Vector3(-6.8f, -1.6f, 0f), Scale = 0.6f, Order = 0 },
            new DecorSpec { Sprite = "17.png", Name = "Lotus_Large", Position = new Vector3(24.8f, -1.6f, 0f), Scale = 0.7f, Order = 0 },
            new DecorSpec { Sprite = "18.png", Name = "Lotus_Small", Position = new Vector3(0f, -2.0f, 0f), Scale = 0.6f, Order = 1 },
            new DecorSpec { Sprite = "22.png", Name = "Dragonfly_Glow", Position = new Vector3(10.8f, -0.6f, 0f), Scale = 0.5f, Order = 2 },
        };
        ApplyDecor("fore_back", foreSpecs, "Foreground");

        Debug.Log("✓ Fix4 背景装饰：8 个丢失 sprite 的占位换为 S01 元件（mid=山丘/鱼鳍坡，fore_back=莲花/玻璃花/蜻蜓）");
    }

    private static void ApplyDecor(string parentName, DecorSpec[] specs, string sortingLayer)
    {
        var backgrounds = GameObject.Find("test_backgrounds");
        var parent = backgrounds != null ? backgrounds.transform.Find(parentName) : null;
        if (parent == null)
        {
            Debug.LogWarning($"未找到 test_backgrounds/{parentName}");
            return;
        }

        // fore_back 挂了 SortingGroup：组整体的层决定与玩家的前后关系
        var group = parent.GetComponent<SortingGroup>();
        if (group != null)
        {
            group.sortingLayerName = sortingLayer;
            group.sortingOrder = 0;
            EditorUtility.SetDirty(group);
        }

        var count = Mathf.Min(parent.childCount, specs.Length);
        for (var i = 0; i < count; i++)
        {
            var child = parent.GetChild(i);
            var spec = specs[i];

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{S01}/{spec.Sprite}");
            if (sprite == null)
            {
                Debug.LogWarning($"S01 元件缺失: {spec.Sprite}");
                continue;
            }

            child.name = spec.Name;
            child.position = spec.Position;
            child.localScale = new Vector3(spec.Scale, spec.Scale, 1f);

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.sortingLayerName = sortingLayer;
                sr.sortingOrder = spec.Order;
                EditorUtility.SetDirty(sr);
            }

            EditorUtility.SetDirty(child.gameObject);
        }
    }

    // ---------- 5. 收集品 ----------

    private static void Fix5_CollectibleSprite()
    {
        // S01/26 = 虹彩梦滴（120x264@PPU100），0.25 缩放后 ~0.3x0.66 单位
        var dropSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{S01}/26.png");
        if (dropSprite == null)
        {
            Debug.LogWarning("S01/26.png 未找到，跳过收集品修复");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(CollectiblePrefabPath);
        try
        {
            var visual = root.transform.Find("Visual");
            if (visual != null)
            {
                visual.localScale = new Vector3(0.25f, 0.25f, 1f);
                var sr = visual.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = dropSprite;
            }

            PrefabUtility.SaveAsPrefabAsset(root, CollectiblePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var sceneCollectible = FindSceneObjectIncludingInactive("Test_Collectible");
        if (sceneCollectible != null)
        {
            var visual = sceneCollectible.transform.Find("Visual");
            if (visual != null)
            {
                visual.localScale = new Vector3(0.25f, 0.25f, 1f);
                var sr = visual.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = dropSprite;
                    EditorUtility.SetDirty(sr);
                }
            }
        }

        Debug.Log("✓ Fix5 收集品：Visual 指定 S01 虹彩梦滴（预制体+场景）");
    }

    private static GameObject FindSceneObjectIncludingInactive(string name)
    {
        foreach (var rootGo in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (rootGo.name == name)
                return rootGo;

            foreach (var t in rootGo.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t.gameObject;
            }
        }

        return null;
    }
}
