using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class PatrolEnemy : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField] private Vector2 patrolStartOffset = new Vector2(-1.5f, 0f);
    [SerializeField] private Vector2 patrolEndOffset = new Vector2(1.5f, 0f);
    [SerializeField, Min(0.001f)] private float endpointTolerance = 0.03f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(1)] private int contactDamage = 1;
    [SerializeField, Min(0f)] private float hitStunDuration = 0.45f;

    private static readonly int IsHitParameter = Animator.StringToHash("isHit");

    private Rigidbody2D body;
    private Animator animator;
    private Vector2 patrolOrigin;
    private bool movingToEnd = true;
    private int currentHealth;
    private float hitStunRemaining;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public bool IsDamageable => IsAlive;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Animator 位于子物体 Visual 上，根物体只有 Transform/Rigidbody2D/Collider2D/本脚本。
        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        patrolOrigin = body.position;
        UpdateFacing();
    }

    private void Reset()
    {
        Rigidbody2D enemyBody = GetComponent<Rigidbody2D>();
        enemyBody.bodyType = RigidbodyType2D.Kinematic;
        enemyBody.gravityScale = 0f;
        enemyBody.freezeRotation = true;

        Collider2D enemyCollider = GetComponent<Collider2D>();
        enemyCollider.isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (hitStunRemaining > 0f)
        {
            hitStunRemaining -= Time.fixedDeltaTime;
            if (hitStunRemaining <= 0f)
            {
                hitStunRemaining = 0f;
                if (animator != null)
                {
                    animator.SetBool(IsHitParameter, false);
                }
            }

            return;
        }

        Vector2 target = patrolOrigin + (movingToEnd ? patrolEndOffset : patrolStartOffset);
        Vector2 nextPosition = Vector2.MoveTowards(
            body.position,
            target,
            moveSpeed * Time.fixedDeltaTime);

        body.MovePosition(nextPosition);

        if ((target - nextPosition).sqrMagnitude <= endpointTolerance * endpointTolerance)
        {
            movingToEnd = !movingToEnd;
            UpdateFacing();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (!IsAlive)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
        }
    }

    /// <summary>
    /// 对敌人造成伤害。返回是否实际命中（已死亡时返回 false）。
    /// </summary>
    public bool TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
            return false;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (currentHealth <= 0)
        {
            Die();
            return true;
        }

        hitStunRemaining = hitStunDuration;
        if (animator != null)
        {
            animator.SetBool(IsHitParameter, true);
        }

        return true;
    }

    private void Die()
    {
        hitStunRemaining = 0f;
        if (animator != null)
        {
            animator.SetBool(IsHitParameter, false);
        }

        // 关掉碰撞体避免尸体继续伤害玩家，再整体隐藏。
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        gameObject.SetActive(false);
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        float direction = movingToEnd
            ? patrolEndOffset.x - patrolStartOffset.x
            : patrolStartOffset.x - patrolEndOffset.x;
        spriteRenderer.flipX = direction > 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? patrolOrigin : (Vector2)transform.position;
        Vector3 start = origin + (Vector3)patrolStartOffset;
        Vector3 end = origin + (Vector3)patrolEndOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.08f);
        Gizmos.DrawWireSphere(end, 0.08f);
    }
}
