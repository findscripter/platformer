using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-100)]
public sealed class RaisePlatform : MonoBehaviour
{
    [SerializeField] private int zoneIndex = -1;
    [SerializeField] private Vector3 restPosition;
    [SerializeField] private Vector3 highPosition;
    [SerializeField] private float speed = 4f;

    private Rigidbody2D body;
    private Vector2 lastPosition;

    public Vector2 FrameDelta { get; private set; }

    public void Configure(int zone, Vector3 rest, Vector3 high)
    {
        zoneIndex = zone;
        restPosition = rest;
        highPosition = high;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.useFullKinematicContacts = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (restPosition == Vector3.zero)
            restPosition = transform.position;
        if (highPosition == Vector3.zero)
            highPosition = restPosition + Vector3.up * 1.6f;
        lastPosition = body.position;
    }

    private void FixedUpdate()
    {
        Vector2 target = TarotZoneQuery.HasEffect(zoneIndex, TarotEffectId.E15)
            ? (Vector2)highPosition
            : (Vector2)restPosition;
        Vector2 current = body.position;
        Vector2 next = Vector2.MoveTowards(current, target, speed * Time.fixedDeltaTime);
        FrameDelta = next - current;
        body.MovePosition(next);
        lastPosition = next;
    }
}
