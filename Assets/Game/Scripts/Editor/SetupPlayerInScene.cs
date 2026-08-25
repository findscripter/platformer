using UnityEditor;
using UnityEngine;

public class SetupPlayerInScene : EditorWindow
{
    [MenuItem("Tools/GameJam/10. Setup Player Animation Components")]
    static void Setup()
    {
        var player = GameObject.Find("test_player");
        if (player == null)
        {
            Debug.LogError("未找到 test_player GameObject");
            return;
        }

        var animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            animator = player.AddComponent<Animator>();
            Debug.Log("✓ 添加 Animator 组件");
        }

        var controller = Resources.Load<RuntimeAnimatorController>("Player/PlayerAnimator");
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
            Debug.Log("✓ 关联 PlayerAnimator controller");
        }
        else
        {
            Debug.LogWarning("⚠ 未找到 Resources/Player/PlayerAnimator.controller");
        }

        var animController = player.GetComponent<PlayerAnimationStateController>();
        if (animController == null)
        {
            animController = player.AddComponent<PlayerAnimationStateController>();
            Debug.Log("✓ 添加 PlayerAnimationStateController 组件");
        }

        var spriteRenderer = player.GetComponent<SpriteRenderer>();

        // 使用反射关联私有字段
        var animControllerType = typeof(PlayerAnimationStateController);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        animControllerType.GetField("animator", flags)?.SetValue(animController, animator);
        animControllerType.GetField("spriteRenderer", flags)?.SetValue(animController, spriteRenderer);
        Debug.Log("✓ 关联 animator 和 spriteRenderer 字段");
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            var idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Characters/player/idle/1.png");
            if (idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
                Debug.Log("✓ 设置初始精灵（idle/1.png）");
            }
        }

        EditorUtility.SetDirty(player);
        Debug.Log("✓ test_player 动画组件配置完成");
    }
}
