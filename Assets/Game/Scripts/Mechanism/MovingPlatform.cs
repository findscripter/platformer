using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-100)]
public sealed class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 pointA;
    [SerializeField] private Vector2 pointB;
    [SerializeField] private float speed = 1.6f;
    [SerializeField] private float waitTime = 0.35f;

    private Rigidbody2D body;
    private Vector2 lastPosition;
    private bool towardB = true;
    private float waitRemaining;

    public Vector2 FrameDelta { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.useFullKinematicContacts = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        lastPosition = body.position;
        if (pointA == Vector2.zero && pointB == Vector2.zero)
        {
            pointA = body.position;
            pointB = body.position + Vector2.up * 2f;
        }
    }

    private void FixedUpdate()
    {
        Vector2 current = body.position;
        if (waitRemaining > 0f)
        {
            waitRemaining -= Time.fixedDeltaTime;
            FrameDelta = Vector2.zero;
            lastPosition = current;
            return;
        }

        Vector2 target = towardB ? pointB : pointA;
        Vector2 next = Vector2.MoveTowards(current, target, speed * Time.fixedDeltaTime);
        FrameDelta = next - current;
        body.MovePosition(next);
        lastPosition = next;

        if ((target - next).sqrMagnitude <= 0.0004f)
        {
            towardB = !towardB;
            waitRemaining = waitTime;
        }
    }

    public void Configure(Vector2 a, Vector2 b, float moveSpeed)
    {
        pointA = a;
        pointB = b;
        speed = Mathf.Max(0.2f, moveSpeed);
        if (body != null)
            lastPosition = body.position;
    }
}
