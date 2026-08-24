using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MonsterAnimatorSetup
{
    [MenuItem("Tools/Platformer/Setup Monster Animator")]
    public static void SetupMonsterAnimator()
    {
        string controllerPath = "Assets/Game/Resources/Enemy/PatrolEnemy.controller";

        // 加载已生成的动画
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Monster/monster_walk.anim");
        AnimationClip hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Monster/monster_hit.anim");

        if (walkClip == null || hitClip == null)
        {
            Debug.LogError("Monster animations not found! Run 'Generate Character Animations' first.");
            return;
        }

        // 加载或创建控制器
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // 清空现有状态
        var layer = controller.layers[0];
        var stateMachine = layer.stateMachine;

        foreach (var state in stateMachine.states)
        {
            stateMachine.RemoveState(state.state);
        }

        // 添加参数
        if (!HasParameter(controller, "isHit"))
        {
            controller.AddParameter("isHit", AnimatorControllerParameterType.Bool);
        }

        // 创建状态
        var walkState = stateMachine.AddState("Walk", new Vector3(300, 100));
        walkState.motion = walkClip;

        var hitState = stateMachine.AddState("Hit", new Vector3(300, 250));
        hitState.motion = hitClip;

        // 设置默认状态
        stateMachine.defaultState = walkState;

        // Walk -> Hit
        var toHit = walkState.AddTransition(hitState);
        toHit.hasExitTime = false;
        toHit.duration = 0.1f;
        toHit.AddCondition(AnimatorConditionMode.If, 0, "isHit");

        // Hit -> Walk (动画结束后自动回到 Walk)
        var toWalk = hitState.AddTransition(walkState);
        toWalk.hasExitTime = true;
        toWalk.exitTime = 0.9f;
        toWalk.duration = 0.1f;
        toWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "isHit");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("Monster Animator setup complete!");
    }

    private static bool HasParameter(AnimatorController controller, string paramName)
    {
        foreach (var param in controller.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}
