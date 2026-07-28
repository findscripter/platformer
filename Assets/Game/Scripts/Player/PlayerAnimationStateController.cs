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
        Landing = 4
    }

    private const string ControllerResourcePath = "Player/PlayerAnimator";
    private const string LandingClipResourcePath = "Player/PlayerLanding";
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

    public AnimationState CurrentState => currentState;
    public bool IsLandingAnimationPlaying =>
        landingLocked && landingTimeRemaining > 0f;
    public bool IsMovementLocked =>
        landingLocked && landingTimeRemaining > landingFrameDuration;

    public void ResetState()
    {
        landingLocked = false;
        hasValidAirbornePhase = false;
        airborneTime = 0f;
        landingTimeRemaining = 0f;
        currentState = (AnimationState)(-1);
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
