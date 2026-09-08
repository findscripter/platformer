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
    [SerializeField, Min(0)] private int extraAirJumps = 1;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.5f;

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0f)] private float invulnerabilityDuration = 1f;

    [Header("Attack")]
    [SerializeField] private Vector2 attackHitboxSize = new Vector2(1.1f, 0.9f);
    [SerializeField] private float attackHitboxForwardOffset = 0.7f;
    [SerializeField] private Vector2 attackHitboxCenterOffset = Vector2.zero;
    [SerializeField, Min(1)] private int attackDamage = 1;
    [SerializeField, Range(0f, 1f)] private float attackHitTimeNormalized = 0.4f;
    // 敌人 prefab 根节点与 Visual 均在 Enemy 层（index 9）
    [SerializeField] private LayerMask attackTargetLayers = 1 << 9;
    [SerializeField, Min(0f)] private float attackCooldown = 0.1f;

    private bool allowMoveLeft = true;
    private float attackRangeMultiplier = 1f;
    private int extraAirJumpsRemaining;
    private int airWalkLocks;
    private float defaultGravityScale = 1f;
    private AerialAttackMode aerialAttackMode = AerialAttackMode.Normal;
    private RigidbodyType2D storedBodyType = RigidbodyType2D.Dynamic;
    private bool physicsFrozen;

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

    private int currentHealth;
    private float invulnerabilityRemaining;
    private float attackCooldownRemaining;
    private readonly Collider2D[] attackHits = new Collider2D[8];
    private ContactFilter2D attackFilter;

    public bool IsGrounded { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInputEnabled { get; private set; } = true;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvulnerable => invulnerabilityRemaining > 0f;

    public enum AerialAttackMode
    {
        Normal = 0,
        Execute = 1,
        Nullify = 2
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animationController = GetComponent<PlayerAnimationStateController>();
        defaultGravityScale = rb != null ? rb.gravityScale : 1f;
        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useTriggers = false;

        // 敌人碰撞体是 trigger，命中检测必须允许 trigger 参与。
        attackFilter = new ContactFilter2D();
        attackFilter.SetLayerMask(attackTargetLayers);
        attackFilter.useTriggers = true;

        currentHealth = maxHealth;
        extraAirJumpsRemaining = extraAirJumps;

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

        if (input.AttackPressed)
            TryAttack();

        if (animationController.IsMovementLocked)
        {
            moveX = 0f;
            return;
        }

        moveX = input.MoveX;
        if (!allowMoveLeft && moveX < 0f)
            moveX = 0f;
    }

    public void SetMoveLeftAllowed(bool allowed)
    {
        allowMoveLeft = allowed;
        if (!allowed && moveX < 0f)
            moveX = 0f;
    }

    public bool TryAttack()
    {
        if (IsDead || !IsInputEnabled || attackCooldownRemaining > 0f)
            return false;
        if (animationController == null || !animationController.TryStartAttack())
            return false;
        attackCooldownRemaining = attackCooldown;
        return true;
    }

    public void SetAttackRangeMultiplier(float multiplier)
    {
        attackRangeMultiplier = Mathf.Max(1f, multiplier);
    }

    public void SetExtraAirJumps(int count)
    {
        int capacity = Mathf.Max(0, count);
        int addedCapacity = capacity - extraAirJumps;
        extraAirJumps = capacity;
        // Reapplying a card (including returning from pause) must not refill spent jumps.
        extraAirJumpsRemaining = IsGrounded
            ? capacity
            : Mathf.Clamp(extraAirJumpsRemaining + addedCapacity, 0, capacity);
    }

    public void SetMaxHealth(int value, bool fill)
    {
        maxHealth = Mathf.Max(1, value);
        if (fill)
            currentHealth = maxHealth;
        else
            currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    public void SetAerialAttackMode(AerialAttackMode mode)
    {
        aerialAttackMode = mode;
    }

    public void AddAirWalkLock()
    {
        airWalkLocks++;
        ApplyAirWalkGravity();
    }

    public void RemoveAirWalkLock()
    {
        airWalkLocks = Mathf.Max(0, airWalkLocks - 1);
        ApplyAirWalkGravity();
    }

    public void ClearAirWalkLocks()
    {
        airWalkLocks = 0;
        ApplyAirWalkGravity();
    }

    private bool IsAirWalking => airWalkLocks > 0;

    private void ApplyAirWalkGravity()
    {
        if (rb == null)
            return;
        rb.gravityScale = IsAirWalking ? 0f : defaultGravityScale;
    }

    public void FixedTick(float fixedDeltaTime)
    {
        if (IsDead || !IsInputEnabled || physicsFrozen)
            return;

        UpdateCombatTimers(fixedDeltaTime);

        CheckGround();
        if (IsAirWalking)
        {
            IsGrounded = true;
            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        UpdateFallingTime(fixedDeltaTime);
        UpdateJumpAvailability();
        UpdateCoyoteTime(fixedDeltaTime);

        float effectiveMoveX = GetEffectiveMoveX();
        animationController.UpdateAnimation(effectiveMoveX, fixedDeltaTime);

        // 动画状态在 UpdateAnimation 里推进，故命中窗口查询紧随其后。
        if (animationController.ConsumeAttackHitWindow(attackHitTimeNormalized))
        {
            PerformAttackHitDetection();
        }

        if (animationController.IsMovementLocked)
        {
            moveX = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            RideMovingPlatform();
            UpdateJumpBuffer(fixedDeltaTime);
            return;
        }

        Move(effectiveMoveX);
        RideMovingPlatform();

        if (!TryJump())
        {
            UpdateJumpBuffer(fixedDeltaTime);
        }
    }

    private void CheckGround()
    {
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
                RaycastHit2D hit = groundHits[i];
                float supportVelocityY = hit.rigidbody != null ? hit.rigidbody.linearVelocity.y : 0f;
                MovingPlatform platform = hit.collider.GetComponentInParent<MovingPlatform>();
                if (platform != null && Time.fixedDeltaTime > 0f)
                    supportVelocityY = platform.FrameDelta.y / Time.fixedDeltaTime;

                // An upward-moving lift carries the player upward without making them airborne.
                if (hit.normal.y >= minimumGroundNormalY && rb.linearVelocity.y <= supportVelocityY + 0.05f)
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

        IsGrounded = rb.linearVelocity.y <= 0.05f && Physics2D.Raycast(
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
        return allowMoveLeft ? moveX : Mathf.Max(0f, moveX);
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
        if (IsGrounded && (leftGroundAfterJump || rb.linearVelocity.y <= 0.05f))
        {
            jumpConsumed = false;
            leftGroundAfterJump = false;
            extraAirJumpsRemaining = extraAirJumps;
            return;
        }

        if (jumpConsumed && !IsGrounded)
            leftGroundAfterJump = true;
    }

    private bool CanJump()
    {
        if (IsGrounded || coyoteTimeRemaining > 0f)
            return true;

        return extraAirJumpsRemaining > 0;
    }

    private bool TryJump()
    {
        if (jumpBufferTimeRemaining <= 0f || !CanJump())
            return false;

        bool airJump = !IsGrounded && coyoteTimeRemaining <= 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferTimeRemaining = 0f;
        coyoteTimeRemaining = 0f;
        jumpConsumed = true;
        leftGroundAfterJump = !IsGrounded;
        if (airJump)
            extraAirJumpsRemaining--;
        return true;
    }

    public void ClearJumpBuffer()
    {
        jumpBufferTimeRemaining = 0f;
    }

    /// <summary>
    /// 对玩家造成伤害。无敌帧期间被忽略。血量归零时自动调用 <see cref="TryKill"/>。
    /// </summary>
    /// <returns>是否真正造成了伤害（无敌帧或已死返回 false）。</returns>
    public bool TakeDamage(int amount)
    {
        if (IsDead || IsInvulnerable || amount <= 0)
            return false;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        invulnerabilityRemaining = invulnerabilityDuration;

        Debug.Log($"Player took {amount} damage, health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            TryKill();
        }

        return true;
    }

    /// <summary>
    /// 立即致死，忽略血量与无敌帧。用于坠落深渊等环境即死。
    /// </summary>
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
        UnfreezePhysics();
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
        currentHealth = maxHealth;
        extraAirJumpsRemaining = extraAirJumps;
        airWalkLocks = 0;
        ApplyAirWalkGravity();
        invulnerabilityRemaining = 0f;
        attackCooldownRemaining = 0f;
        animationController.ResetState();
        animationController.UpdateAnimation(0f);
    }

    private void UpdateCombatTimers(float fixedDeltaTime)
    {
        if (invulnerabilityRemaining > 0f)
        {
            invulnerabilityRemaining = Mathf.Max(0f, invulnerabilityRemaining - fixedDeltaTime);
        }

        if (attackCooldownRemaining > 0f)
        {
            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - fixedDeltaTime);
        }
    }

    private Vector2 GetAttackHitboxSize()
    {
        return attackHitboxSize * attackRangeMultiplier;
    }

    private Vector2 GetAttackHitboxCenter()
    {
        float facing = animationController != null && animationController.IsFacingRight ? 1f : -1f;
        float range = attackRangeMultiplier;
        Vector2 offset = new Vector2(
            (attackHitboxForwardOffset * facing * range) + (attackHitboxCenterOffset.x * facing),
            attackHitboxCenterOffset.y);

        return (Vector2)transform.position + offset;
    }

    private void PerformAttackHitDetection()
    {
        Vector2 center = GetAttackHitboxCenter();
        int count = Physics2D.OverlapBox(center, GetAttackHitboxSize(), 0f, attackFilter, attackHits);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = attackHits[i];
            if (hit == null)
                continue;

            if (hit.transform.IsChildOf(transform))
                continue;

            if (IsBlockedByWall(center, hit))
                continue;

            PatrolEnemy enemy = hit.GetComponentInParent<PatrolEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(ResolveAttackDamage());
            }
        }
    }

    private int ResolveAttackDamage()
    {
        bool falling = !IsGrounded && rb != null && rb.linearVelocity.y < 0f;
        if (!falling)
            return attackDamage;

        if (aerialAttackMode == AerialAttackMode.Execute)
            return 999;
        if (aerialAttackMode == AerialAttackMode.Nullify)
            return 0;
        return attackDamage;
    }

    private void RideMovingPlatform()
    {
        if (!IsGrounded || bodyCollider == null)
            return;

        int hitCount = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, groundCheckRadius + 0.05f);
        for (int i = 0; i < hitCount; i++)
        {
            MovingPlatform platform = groundHits[i].collider.GetComponentInParent<MovingPlatform>();
            if (platform == null || groundHits[i].normal.y < minimumGroundNormalY)
                continue;

            rb.position += platform.FrameDelta;
            // Carry once through position, not again through last tick's collision velocity.
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            return;
        }
    }

    private bool IsBlockedByWall(Vector2 origin, Collider2D target)
    {
        Vector2 destination = target.bounds.center;
        RaycastHit2D block = Physics2D.Linecast(origin, destination, groundLayer);
        if (block.collider == null)
            return false;

        return !block.collider.transform.IsChildOf(target.transform)
            && !block.collider.transform.IsChildOf(transform);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GetAttackHitboxCenter(), GetAttackHitboxSize());
    }

    public void StopMovement()
    {
        IsInputEnabled = false;
        moveX = 0f;
        coyoteTimeRemaining = 0f;
        jumpBufferTimeRemaining = 0f;
        jumpConsumed = false;
        leftGroundAfterJump = false;
        fallingTime = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (animationController != null)
        {
            animationController.ResetState();
            animationController.UpdateAnimation(0f);
        }
    }

    /// <summary>
    /// 过场/通关时关掉动力学，避免站在 END 上仍被重力拖下深渊。
    /// </summary>
    public void FreezePhysics()
    {
        StopMovement();
        if (rb == null || physicsFrozen)
            return;

        storedBodyType = rb.bodyType;
        physicsFrozen = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void UnfreezePhysics()
    {
        if (rb != null && physicsFrozen)
        {
            rb.bodyType = storedBodyType;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        physicsFrozen = false;
        IsInputEnabled = true;
    }
}
