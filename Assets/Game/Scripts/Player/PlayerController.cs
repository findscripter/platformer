using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]

    public bool baseFaceRight = true;
    public bool enableAnimation = true;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField, Range(0f, 1f)] private float minimumAirControlMultiplier = 0.6f;
    [SerializeField, Range(0.01f, 1f)] private float airControlFallPortion = 1f / 3f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Jump Assist")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.2f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.5f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private PlayerAnimationStateController animationController;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
    private ContactFilter2D groundFilter;

    [SerializeField] private float moveX;
    private float coyoteTimeRemaining;
    private float jumpBufferTimeRemaining;
    private bool jumpConsumed;
    private bool leftGroundAfterJump;
    private float fallingTime;

    public bool IsGrounded { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInputEnabled { get; private set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animationController = GetComponent<PlayerAnimationStateController>();
        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useTriggers = false;

        if (animationController == null)
        {
            animationController = gameObject.AddComponent<PlayerAnimationStateController>();
        }

        animationController.Initialize(this, rb);
    }

    public void SetInputEnabled(bool enabled)
    {
        IsInputEnabled = enabled;

        if (!enabled)
        {
            moveX = 0f;
            ClearJumpBuffer();
        }
    }

    public void HandleInput(InputManager input)
    {
        if (IsDead || !IsInputEnabled)
            return;

        if (input.JumpPressed)
        {
            jumpBufferTimeRemaining = jumpBufferTime;
        }

        if (animationController.IsMovementLocked)
        {
            moveX = 0f;
            return;
        }

        moveX = input.MoveX;
    }

    public void FixedTick(float fixedDeltaTime)
    {
        if (IsDead || !IsInputEnabled)
            return;

        CheckGround();
        UpdateFallingTime(fixedDeltaTime);
        UpdateJumpAvailability();
        UpdateCoyoteTime(fixedDeltaTime);

        float effectiveMoveX = GetEffectiveMoveX();
        animationController.UpdateAnimation(effectiveMoveX, fixedDeltaTime);

        if (animationController.IsMovementLocked)
        {
            moveX = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Move(effectiveMoveX);

        if (animationController.IsLandingAnimationPlaying)
            return;

        if (!TryJump())
        {
            UpdateJumpBuffer(fixedDeltaTime);
        }
    }

    private void CheckGround()
    {
        if (rb.linearVelocity.y > 0f)
        {
            IsGrounded = false;
            return;
        }

        if (bodyCollider != null)
        {
            int hitCount = bodyCollider.Cast(
                Vector2.down,
                groundFilter,
                groundHits,
                groundCheckRadius);

            IsGrounded = false;
            for (int i = 0; i < hitCount; i++)
            {
                if (groundHits[i].normal.y >= minimumGroundNormalY)
                {
                    IsGrounded = true;
                    break;
                }
            }

            return;
        }

        if (groundCheck == null)
        {
            IsGrounded = false;
            return;
        }

        IsGrounded = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckRadius,
            groundLayer
        );
    }

    private void Move(float effectiveMoveX)
    {
        float controlMultiplier = GetHorizontalControlMultiplier();
        rb.linearVelocity = new Vector2(
            effectiveMoveX * moveSpeed * controlMultiplier,
            rb.linearVelocity.y);
    }

    private void UpdateFallingTime(float deltaTime)
    {
        if (IsGrounded || rb.linearVelocity.y >= 0f)
        {
            fallingTime = 0f;
            return;
        }

        fallingTime += deltaTime;
    }

    private float GetHorizontalControlMultiplier()
    {
        if (IsGrounded || rb.linearVelocity.y >= 0f)
            return 1f;

        float gravityMagnitude =
            Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        if (gravityMagnitude <= Mathf.Epsilon)
            return minimumAirControlMultiplier;

        float estimatedFallDuration = jumpForce / gravityMagnitude;
        float fadeDuration = estimatedFallDuration * airControlFallPortion;
        float fallProgress = Mathf.Clamp01(fallingTime / fadeDuration);

        return Mathf.Lerp(1f, minimumAirControlMultiplier, fallProgress);
    }

    private float GetEffectiveMoveX()
    {
        return moveX;
    }

    private void UpdateCoyoteTime(float deltaTime)
    {
        if (IsGrounded)
        {
            coyoteTimeRemaining = coyoteTime;
            return;
        }

        coyoteTimeRemaining = Mathf.Max(0f, coyoteTimeRemaining - deltaTime);
    }

    private void UpdateJumpBuffer(float deltaTime)
    {
        if (jumpBufferTimeRemaining <= 0f)
            return;

        jumpBufferTimeRemaining = Mathf.Max(0f, jumpBufferTimeRemaining - deltaTime);
    }

    private void UpdateJumpAvailability()
    {
        if (!jumpConsumed)
            return;

        if (!IsGrounded)
        {
            leftGroundAfterJump = true;
            return;
        }

        if (leftGroundAfterJump)
        {
            jumpConsumed = false;
            leftGroundAfterJump = false;
        }
    }

    private bool CanJump()
    {
        return !jumpConsumed && (IsGrounded || coyoteTimeRemaining > 0f);
    }

    private bool TryJump()
    {
        if (jumpBufferTimeRemaining <= 0f || !CanJump())
            return false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferTimeRemaining = 0f;
        coyoteTimeRemaining = 0f;
        jumpConsumed = true;
        leftGroundAfterJump = !IsGrounded;
        return true;
    }

    public void ClearJumpBuffer()
    {
        jumpBufferTimeRemaining = 0f;
    }

    public bool TryKill()
    {
        if (IsDead)
            return false;

        IsDead = true;
        animationController.UpdateAnimation(0f);
        Debug.Log("player killed");
        return true;
    }

    public void Kill()
    {
        TryKill();
    }

    public void Respawn(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector2.zero;

        IsDead = false;
        IsInputEnabled = true;
        moveX = 0f;
        coyoteTimeRemaining = 0f;
        jumpBufferTimeRemaining = 0f;
        jumpConsumed = false;
        leftGroundAfterJump = false;
        fallingTime = 0f;
        animationController.ResetState();
        animationController.UpdateAnimation(0f);
    }

    public void StopMovement()
    {
        IsInputEnabled = true;
        moveX = 0f;
        coyoteTimeRemaining = 0f;
        jumpBufferTimeRemaining = 0f;
        jumpConsumed = false;
        leftGroundAfterJump = false;
        fallingTime = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        animationController.ResetState();
        animationController.UpdateAnimation(0f);
    }
}
