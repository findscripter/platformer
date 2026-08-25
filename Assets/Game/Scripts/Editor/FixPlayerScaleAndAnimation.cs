using UnityEditor;
using UnityEngine;

public class FixPlayerScaleAndAnimation : EditorWindow
{
    [MenuItem("Tools/GameJam/12. Fix Player Scale and Animation")]
    static void Fix()
    {
        var player = GameObject.Find("test_player");
        if (player == null)
        {
            Debug.LogError("未找到 test_player");
            return;
        }

        // 调整缩放（原始精灵 834x1112 像素太大，缩小到合理尺寸）
        player.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
        Debug.Log("✓ 玩家缩放调整为 0.2（原 6.5x8.7 单位 → 1.3x1.7 单位）");

        // 调整 BoxCollider2D 尺寸以匹配缩放后的角色
        var collider = player.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = new Vector2(1f, 2.2f);
            collider.offset = new Vector2(0, 1.1f);
            Debug.Log("✓ BoxCollider2D 尺寸调整为 1x2.2，offset (0, 1.1)");
        }

        // 调整 GroundCheck 位置
        var groundCheck = player.transform.Find("GroundCheck");
        if (groundCheck != null)
        {
            groundCheck.localPosition = new Vector3(0, -1.2f, 0);
            Debug.Log("✓ GroundCheck 位置调整为 (0, -1.2, 0)");
        }

        // 检查动画控制器
        var animator = player.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log($"✓ Animator 已关联控制器: {animator.runtimeAnimatorController.name}");

            // 检查当前播放状态
            if (Application.isPlaying)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"  当前动画状态: {stateInfo.shortNameHash}");
            }
        }

        // 检查 PlayerAnimationStateController
        var animController = player.GetComponent<PlayerAnimationStateController>();
        if (animController != null)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var animatorField = typeof(PlayerAnimationStateController).GetField("animator", flags);
            var spriteRendererField = typeof(PlayerAnimationStateController).GetField("spriteRenderer", flags);

            var animatorValue = animatorField?.GetValue(animController);
            var spriteRendererValue = spriteRendererField?.GetValue(animController);

            Debug.Log($"✓ PlayerAnimationStateController.animator: {(animatorValue != null ? "已关联" : "NULL")}");
            Debug.Log($"✓ PlayerAnimationStateController.spriteRenderer: {(spriteRendererValue != null ? "已关联" : "NULL")}");
        }

        EditorUtility.SetDirty(player);
        Debug.Log("✓ 玩家缩放和碰撞体修正完成");
    }
}
