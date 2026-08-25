using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerAnimatorAssetSetup
{
    private const string OutputFolder = "Assets/Game/Resources/Player";
    private const string ControllerPath = OutputFolder + "/PlayerAnimator.controller";
    private const string StateParameter = "State";

    private static readonly string[] RequiredStateNames =
    {
        "Idle",
        "Run",
        "Jump Up",
        "Jump Down",
        "Landing",
        "Attack"
    };

    private static bool isBuilding;

    static PlayerAnimatorAssetSetup()
    {
        EditorApplication.delayCall += EnsureAnimatorAssets;
    }

    [MenuItem("Tools/Platformer/Rebuild Player Animator")]
    public static void RebuildAnimatorAssets()
    {
        if (isBuilding)
            return;

        isBuilding = true;
        try
        {
            EnsureFolders();
            BuildAnimatorAssets(forceRebuildController: true);
        }
        finally
        {
            isBuilding = false;
        }
    }

    private static void EnsureAnimatorAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || isBuilding)
            return;

        if (IsAnimatorSetupComplete())
            return;

        isBuilding = true;
        try
        {
            EnsureFolders();
            BuildAnimatorAssets(forceRebuildController: false);
        }
        finally
        {
            isBuilding = false;
        }
    }

    private static void BuildAnimatorAssets(bool forceRebuildController)
    {
        // 使用 Resources 目录下的动画
        AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Resources/Player/player_idle.anim");
        AnimationClip run = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Resources/Player/player_run.anim");
        AnimationClip jumpClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Player/player_jump.anim");
        AnimationClip attack = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Player/player_attack.anim");

        if (idle == null || run == null || jumpClip == null)
        {
            Debug.LogError("Player animations not found! Run 'Generate Character Animations' first.");
            return;
        }

        // 从 jump 动画中分离 Up/Down/Landing
        if (!TryCreateJumpVariants(jumpClip, out AnimationClip jumpUp, out AnimationClip jumpDown, out AnimationClip landing))
            return;

        // attack clip 的真源在 Animations/ 下，这里派生一份到 Resources 供运行时 Resources.Load 读取
        if (attack != null)
        {
            attack = MirrorClipToResources(attack, "PlayerAttack");
        }

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null && !forceRebuildController && HasAllRequiredStates(controller))
        {
            AssetDatabase.SaveAssets();
            return;
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(StateParameter, AnimatorControllerParameterType.Int);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = AddState(stateMachine, "Idle", idle, new Vector3(250f, 20f));
            AnimatorState runState = AddState(stateMachine, "Run", run, new Vector3(500f, 20f));
            AnimatorState jumpUpState =
                AddState(stateMachine, "Jump Up", jumpUp, new Vector3(250f, 150f));
            AnimatorState jumpDownState =
                AddState(stateMachine, "Jump Down", jumpDown, new Vector3(500f, 150f));
            AnimatorState landingState =
                AddState(stateMachine, "Landing", landing, new Vector3(750f, 150f));

            stateMachine.defaultState = idleState;
            AddAnyStateTransition(stateMachine, idleState, 0);
            AddAnyStateTransition(stateMachine, runState, 1);
            AddAnyStateTransition(stateMachine, jumpUpState, 2);
            AddAnyStateTransition(stateMachine, jumpDownState, 3);
            AddAnyStateTransition(stateMachine, landingState, 4);

            // Attack 状态值须与 PlayerAnimationStateController.PlayerAnimState.Attack 一致
            if (attack != null)
            {
                AnimatorState attackState =
                    AddState(stateMachine, "Attack", attack, new Vector3(750f, 20f));
                AddAnyStateTransition(stateMachine, attackState, 5);
            }
            else
            {
                Debug.LogWarning(
                    "player_attack.anim not found — Attack state skipped. " +
                    "Run 'Generate Character Animations' then rebuild.");
            }

            EditorUtility.SetDirty(controller);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Player Animator rebuilt using new sprite sequences!");
    }

    private static bool TryCreateJumpVariants(
        AnimationClip sourceJump,
        out AnimationClip jumpUp,
        out AnimationClip jumpDown,
        out AnimationClip landing)
    {
        // 获取 jump 动画的所有帧
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes =
            AnimationUtility.GetObjectReferenceCurve(sourceJump, binding);

        if (keyframes == null || keyframes.Length < 3)
        {
            Debug.LogError("Jump animation doesn't have enough frames!");
            jumpUp = jumpDown = landing = null;
            return false;
        }

        float frameRate = sourceJump.frameRate;

        // 分配帧数：前 2 帧 JumpUp，第 3 帧静止 JumpDown，后续帧 Landing
        int upFrames = Mathf.Min(2, keyframes.Length / 3);
        int downFrame = upFrames;
        int landingStart = downFrame + 1;

        // JumpUp: 前 2 帧循环
        jumpUp = CreateClipFromKeyframes("player_up", keyframes.Take(upFrames).ToArray(), frameRate, true);

        // JumpDown: 单帧静止
        jumpDown = CreateClipFromKeyframes("player_down", new[] { keyframes[downFrame] }, frameRate, false);

        // Landing: 剩余帧
        landing = CreateClipFromKeyframes("PlayerLanding", keyframes.Skip(landingStart).ToArray(), frameRate, false);

        return jumpUp != null && jumpDown != null && landing != null;
    }

    private static AnimationClip CreateClipFromKeyframes(
        string clipName,
        ObjectReferenceKeyframe[] keyframes,
        float frameRate,
        bool loop)
    {
        if (keyframes == null || keyframes.Length == 0)
            return null;

        string clipPath = $"{OutputFolder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = clipName };
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = frameRate;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        // 重新计算时间轴
        var retimedKeyframes = keyframes.Select((kf, index) => new ObjectReferenceKeyframe
        {
            time = index / frameRate,
            value = kf.value
        }).ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, retimedKeyframes);
        SetLoopTime(clip, loop);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    /// <summary>
    /// 把 Animations/ 下的源 clip 复刻到 Resources/Player/，保持源文件为唯一真源。
    /// 运行时 PlayerAnimationStateController 通过 Resources.Load 读取复刻件测量时长。
    /// </summary>
    private static AnimationClip MirrorClipToResources(AnimationClip source, string clipName)
    {
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes =
            AnimationUtility.GetObjectReferenceCurve(source, binding);

        if (keyframes == null || keyframes.Length == 0)
        {
            Debug.LogWarning(
                $"{source.name} has no sprite keyframes on the root SpriteRenderer — " +
                "using source clip directly instead of mirroring to Resources.");
            return source;
        }

        AnimationClip mirrored = CreateClipFromKeyframes(
            clipName,
            keyframes,
            source.frameRate > 0f ? source.frameRate : 20f,
            loop: false);

        return mirrored ?? source;
    }

    private static AnimatorState AddState(
        AnimatorStateMachine stateMachine,
        string name,
        Motion motion,
        Vector3 position)
    {
        AnimatorState state = stateMachine.AddState(name, position);
        state.motion = motion;
        return state;
    }

    private static void AddAnyStateTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        int stateValue)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.05f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.Equals, stateValue, StateParameter);
    }

    private static void SetLoopTime(AnimationClip clip, bool loop)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        SerializedProperty loopTime = settings?.FindPropertyRelative("m_LoopTime");

        if (loopTime == null)
            return;

        loopTime.boolValue = loop;
        serializedClip.ApplyModifiedProperties();
    }

    private static void EnsureFolders()
    {
        string currentPath = "Assets";
        foreach (string folder in OutputFolder.Split('/').Skip(1))
        {
            string nextPath = $"{currentPath}/{folder}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folder);
            }

            currentPath = nextPath;
        }
    }

    private static bool IsAnimatorSetupComplete()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        return controller != null && HasAllRequiredStates(controller);
    }

    private static bool HasAllRequiredStates(AnimatorController controller)
    {
        foreach (string stateName in RequiredStateNames)
        {
            if (!HasState(controller, stateName))
                return false;
        }

        return true;
    }

    private static bool HasState(AnimatorController controller, string stateName)
    {
        return controller.layers
            .SelectMany(layer => layer.stateMachine.states)
            .Any(childState => childState.state.name == stateName);
    }
}
