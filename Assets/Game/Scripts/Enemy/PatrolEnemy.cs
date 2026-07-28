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

    private Rigidbody2D body;
    private Vector2 patrolOrigin;
    private bool movingToEnd = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

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
        TryKillPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private static void TryKillPlayer(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.TryKill();
        }
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        float direction = movingToEnd
            ? patrolEndOffset.x - patrolStartOffset.x
            : patrolStartOffset.x - patrolEndOffset.x;
        spriteRenderer.flipX = direction < 0f;
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
