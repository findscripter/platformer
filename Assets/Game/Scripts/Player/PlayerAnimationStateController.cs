using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAnimationStateController : MonoBehaviour
{
    public enum AnimationState
    {
        Idle = 0,
        Run = 1,
        JumpUp = 2,
        JumpDown = 3,
        Landing = 4,
        Attack = 5
    }

    private const string ControllerResourcePath = "Player/PlayerAnimator";
    private const string LandingClipResourcePath = "Player/PlayerLanding";
    private const string AttackClipResourcePath = "Player/PlayerAttack";
    private static readonly int StateParameter = Animator.StringToHash("State");

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
    [SerializeField, Min(0f)] private float verticalThreshold = 0.05f;
    [SerializeField, Min(0f)] private float minimumAirborneTime = 0.08f;

    private PlayerController player;
    private Rigidbody2D rb;
    private AnimationState currentState = (AnimationState)(-1);
    private bool landingLocked;
    private bool hasValidAirbornePhase;
    private float airborneTime;
    private float landingTimeRemaining;
    private float landingClipLength = 0.5f;
    private float landingFrameDuration = 1f / 12f;
    private bool attackLocked;
    private float attackTimeRemaining;
    private float attackClipLength = 0.8f;
    private bool attackHitResolved;

    public AnimationState CurrentState => currentState;
    public bool IsLandingAnimationPlaying =>
        landingLocked && landingTimeRemaining > 0f;

    /// <summary>攻击动画是否正在播放（整段时长内为 true）。</summary>
    public bool IsAttackAnimationPlaying =>
        attackLocked && attackTimeRemaining > 0f;

    /// <summary>攻击动画总时长，供判定窗口按比例换算。</summary>
    public float AttackClipLength => attackClipLength;

    /// <summary>
    /// 当前朝向是否为右。唯一真源是 <c>spriteRenderer.flipX</c>，
    /// 并按 <see cref="PlayerController.baseFaceRight"/> 还原素材的原始朝向。
    /// </summary>
    public bool IsFacingRight
    {
        get
        {
            if (spriteRenderer == null || player == null)
                return true;

            return player.baseFaceRight ? !spriteRenderer.flipX : spriteRenderer.flipX;
        }
    }

    public bool IsMovementLocked =>
        (landingLocked && landingTimeRemaining > landingFrameDuration) ||
        IsAttackAnimationPlaying;

    public void ResetState()
    {
        landingLocked = false;
        hasValidAirbornePhase = false;
        airborneTime = 0f;
        landingTimeRemaining = 0f;
        attackLocked = false;
        attackTimeRemaining = 0f;
        attackHitResolved = false;
        currentState = (AnimationState)(-1);
    }

    /// <summary>
    /// 启动攻击锁。攻击期间移动被 <see cref="IsMovementLocked"/> 抑制、朝向冻结。
    /// </summary>
    /// <returns>是否成功进入攻击（已在攻击或落地锁中会被拒绝）。</returns>
    public bool TryStartAttack()
    {
        if (attackLocked || IsMovementLocked)
            return false;

        attackLocked = true;
        attackTimeRemaining = attackClipLength;
        attackHitResolved = false;
        return true;
    }

    /// <summary>
    /// 查询本次攻击的判定窗口是否刚刚到达。每段攻击只会返回 true 一次，
    /// 由调用方在该帧执行命中检测——取代脆弱的 AnimationEvent 绑定。
    /// </summary>
    /// <param name="hitTimeNormalized">判定时点占动画总长的比例（0-1）。</param>
    public bool ConsumeAttackHitWindow(float hitTimeNormalized)
    {
        if (!attackLocked || attackHitResolved)
            return false;

        float elapsed = attackClipLength - attackTimeRemaining;
        if (elapsed < attackClipLength * hitTimeNormalized)
            return false;

        attackHitResolved = true;
        return true;
    }

    public void Initialize(PlayerController playerController, Rigidbody2D rigidbody2D)
    {
        player = playerController;
        rb = rigidbody2D;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (!player.enableAnimation)
            return;

        if (animator == null && spriteRenderer != null)
        {
            animator = spriteRenderer.GetComponent<Animator>();
            if (animator == null)
            {
                animator = spriteRenderer.gameObject.AddComponent<Animator>();
            }
        }

        if (animator != null && animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController =
                Resources.Load<RuntimeAnimatorController>(ControllerResourcePath);
        }

        CacheLandingClipTiming();
        CacheAttackClipTiming();
        UpdateAnimation(0f);
    }

    private void CacheLandingClipTiming()
    {
        AnimationClip landingClip = Resources.Load<AnimationClip>(LandingClipResourcePath);
        if (landingClip == null)
            return;

        if (landingClip.frameRate > 0f)
        {
            landingFrameDuration = 1f / landingClip.frameRate;
        }

        if (landingClip.length > 0f)
        {
            landingClipLength = landingClip.length;
        }
    }

    private void CacheAttackClipTiming()
    {
        AnimationClip attackClip = Resources.Load<AnimationClip>(AttackClipResourcePath);
        if (attackClip != null && attackClip.length > 0f)
        {
            attackClipLength = attackClip.length;
        }
    }

    public void UpdateAnimation(float horizontalInput, float deltaTime = 0f)
    {
        if (player == null || rb == null)
            return;

        if (spriteRenderer != null &&
            !IsMovementLocked &&
            Mathf.Abs(horizontalInput) > movementThreshold)
        {
            spriteRenderer.flipX = player.baseFaceRight ? horizontalInput < 0f : horizontalInput > 0f;
        }

        if (!player.enableAnimation)
            return;

        AnimationState nextState = ResolveState(horizontalInput, deltaTime);
        if (nextState == currentState)
            return;

        currentState = nextState;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetInteger(StateParameter, (int)currentState);
        }
    }

    private AnimationState ResolveState(float horizontalInput, float deltaTime)
    {
        if (attackLocked)
        {
            attackTimeRemaining -= deltaTime;
            if (attackTimeRemaining > 0f)
            {
                return AnimationState.Attack;
            }

            attackLocked = false;
            attackTimeRemaining = 0f;
            attackHitResolved = false;
        }

        if (landingLocked)
        {
            landingTimeRemaining -= deltaTime;
            if (landingTimeRemaining > 0f)
            {
                return AnimationState.Landing;
            }

            landingLocked = false;
            landingTimeRemaining = 0f;
        }

        if (!player.IsGrounded)
        {
            airborneTime += deltaTime;
            if (airborneTime >= minimumAirborneTime)
            {
                hasValidAirbornePhase = true;
            }

            return rb.linearVelocity.y > verticalThreshold
                ? AnimationState.JumpUp
                : AnimationState.JumpDown;
        }

        if (hasValidAirbornePhase)
        {
            hasValidAirbornePhase = false;
            airborneTime = 0f;
            landingLocked = true;
            landingTimeRemaining = landingClipLength;
            return AnimationState.Landing;
        }

        airborneTime = 0f;

        return Mathf.Abs(horizontalInput) > movementThreshold
            ? AnimationState.Run
            : AnimationState.Idle;
    }
}
