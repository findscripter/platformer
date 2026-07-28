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
        "Landing"
    };

    private static readonly string[] RequiredClipNames =
    {
        "player_idle",
        "player_run",
        "player_up",
        "player_down",
        "PlayerLanding"
    };

    private const string IdleSheet =
        "Assets/Game/Art/Characters/images/player_idle.png";
    private const string RunSheet =
        "Assets/Game/Art/Characters/images/player_run.png";
    private const string JumpUpSheet =
        "Assets/Game/Art/Characters/images/player_up.png";
    private const string JumpDownSheet =
        "Assets/Game/Art/Characters/images/player_down.png";

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
        AnimationClip idle = CreateClip("player_idle", IdleSheet, 6f, true);
        AnimationClip run = CreateClip("player_run", RunSheet, 10f, true);
        AnimationClip jumpUp = CreateClip("player_up", JumpUpSheet, 12f, false);

        if (!TryCreateJumpDownAndLandingClips(out AnimationClip jumpDown, out AnimationClip landing))
            return;

        if (idle == null || run == null || jumpUp == null || jumpDown == null || landing == null)
        {
            Debug.LogError("Player Animator 创建失败：一个或多个序列图没有可用 Sprite。");
            return;
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

            EditorUtility.SetDirty(controller);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Player Animator 已生成：Idle / Run / Jump Up / Jump Down / Landing。");
    }

    private static bool TryCreateJumpDownAndLandingClips(
        out AnimationClip jumpDown,
        out AnimationClip landing)
    {
        Sprite[] jumpDownSprites = LoadSprites(JumpDownSheet);
        if (jumpDownSprites.Length < 2)
        {
            Debug.LogError(
                "Player Animator 创建失败：Jump Down 序列图至少需要 2 帧（第 1 帧用于下落静止，其余帧用于 Landing）。");
            jumpDown = null;
            landing = null;
            return false;
        }

        // Jump Down: hold first frame only. Landing: play remaining frames.
        jumpDown = CreateClip("player_down", JumpDownSheet, 12f, false, 0, 1);
        landing = CreateClip("PlayerLanding", JumpDownSheet, 12f, false, 1);
        return jumpDown != null && landing != null;
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

    private static AnimationClip CreateClip(
        string clipName,
        string spriteSheetPath,
        float frameRate,
        bool loop,
        int startFrame = 0,
        int frameCount = int.MaxValue)
    {
        Sprite[] sprites = LoadSprites(spriteSheetPath, startFrame, frameCount);
        if (sprites.Length == 0)
            return null;

        return CreateClipFromSprites(clipName, sprites, frameRate, loop);
    }

    private static AnimationClip CreateClipFromSprites(
        string clipName,
        Sprite[] sprites,
        float frameRate,
        bool loop)
    {
        if (sprites == null || sprites.Length == 0)
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

        ObjectReferenceKeyframe[] keyframes = sprites
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / frameRate,
                value = sprite
            })
            .ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        SetLoopTime(clip, loop);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static Sprite[] LoadSprites(
        string spriteSheetPath,
        int startFrame = 0,
        int frameCount = int.MaxValue)
    {
        return AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
            .OfType<Sprite>()
            .Where(sprite => sprite.rect.width >= 64f && sprite.rect.height >= 64f)
            .OrderBy(sprite => sprite.rect.x)
            .Skip(startFrame)
            .Take(frameCount)
            .ToArray();
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
        if (controller == null || !HasAllRequiredStates(controller))
            return false;

        foreach (string clipName in RequiredClipNames)
        {
            if (!IsClipValid($"{OutputFolder}/{clipName}.anim"))
                return false;
        }

        return true;
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

    private static bool IsClipValid(string clipPath)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
            return false;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes =
            AnimationUtility.GetObjectReferenceCurve(clip, binding);
        return keyframes != null && keyframes.Length > 0;
    }

    private static bool HasState(AnimatorController controller, string stateName)
    {
        return controller.layers
            .SelectMany(layer => layer.stateMachine.states)
            .Any(childState => childState.state.name == stateName);
    }
}
