using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class CollectibleItem : MonoBehaviour
{
    [SerializeField, Min(0)] private int value = 1;
    [SerializeField, Min(0f)] private float bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float bobFrequency = 1.5f;
    [SerializeField] private CollectibleTracker tracker;

    private Vector3 startLocalPosition;

    public bool IsCollected { get; private set; }
    public int Value => value;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        ResolveTracker();
    }

    private void Reset()
    {
        Collider2D itemCollider = GetComponent<Collider2D>();
        itemCollider.isTrigger = true;
    }

    private void Update()
    {
        if (IsCollected || bobHeight <= 0f || bobFrequency <= 0f)
            return;

        float offset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobHeight;
        transform.localPosition = startLocalPosition + Vector3.up * offset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsCollected || other.GetComponentInParent<PlayerController>() == null)
            return;

        ResolveTracker();
        tracker?.Collect(this, value);
    }

    public void MarkCollected()
    {
        if (IsCollected)
            return;

        IsCollected = true;
        gameObject.SetActive(false);
    }

    private void ResolveTracker()
    {
        if (tracker != null)
            return;

        tracker = GetComponentInParent<CollectibleTracker>();
        if (tracker == null)
        {
            tracker = FindAnyObjectByType<CollectibleTracker>();
        }
    }
}
