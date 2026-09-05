using UnityEditor;
using UnityEngine;

public class CreateTestLevel : EditorWindow
{
    [MenuItem("Tools/GameJam/11. Create Simple Test Level")]
    static void CreateLevel()
    {
        // 查找现有地面容器
        var groundsRoot = GameObject.Find("test_grounds");
        if (groundsRoot == null)
        {
            groundsRoot = new GameObject("test_grounds");
            groundsRoot.layer = LayerMask.NameToLayer("Ground");
        }

        // 清空现有地面
        while (groundsRoot.transform.childCount > 0)
        {
            DestroyImmediate(groundsRoot.transform.GetChild(0).gameObject);
        }

        // 创建地面平台（使用 BoxCollider2D + SpriteRenderer）
        CreatePlatform(groundsRoot.transform, "Ground_Main", new Vector3(0, -3, 0), new Vector2(30, 1));
        CreatePlatform(groundsRoot.transform, "Platform_1", new Vector3(-8, -1, 0), new Vector2(4, 0.5f));
        CreatePlatform(groundsRoot.transform, "Platform_2", new Vector3(-4, 1, 0), new Vector2(3, 0.5f));
        CreatePlatform(groundsRoot.transform, "Platform_3", new Vector3(2, 2, 0), new Vector2(4, 0.5f));
        CreatePlatform(groundsRoot.transform, "Platform_4", new Vector3(8, 0, 0), new Vector2(3, 0.5f));
        CreatePlatform(groundsRoot.transform, "Platform_5", new Vector3(12, 3, 0), new Vector2(5, 0.5f));

        // 调整玩家位置
        var player = GameObject.Find("test_player");
        if (player != null)
        {
            player.transform.position = new Vector3(-10, 0, 0);

            // 确保 PlayerController 的 groundCheck 字段有值
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                var groundCheckObj = player.transform.Find("GroundCheck");
                if (groundCheckObj == null)
                {
                    groundCheckObj = new GameObject("GroundCheck").transform;
                    groundCheckObj.SetParent(player.transform);
                    groundCheckObj.localPosition = new Vector3(0, -1.1f, 0);
                }

                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(PlayerController).GetField("groundCheck", flags)?.SetValue(controller, groundCheckObj);
                Debug.Log("✓ 配置 PlayerController.groundCheck");
            }
        }

        // 调整摄像机
        var mainCam = GameObject.Find("Camera/MainCamera");
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 0, -10);

            var cam = mainCam.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographicSize = 6;
            }
        }

        Debug.Log("✓ 测试关卡创建完成：主地面 + 5 个平台，玩家位于 (-10, 0)");
    }

    static void CreatePlatform(Transform parent, string name, Vector3 position, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.layer = LayerMask.NameToLayer("Ground");

        // 添加碰撞体
        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;

        // 添加视觉表现（简单方块精灵）
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = new Color(0.4f, 0.3f, 0.2f); // 棕色地面
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;

        EditorUtility.SetDirty(obj);
    }
}
