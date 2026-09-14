using UnityEngine;

/// <summary>补给只回 1 血，不计入梦核碎片。</summary>
public sealed class DreamSupplyChest : MonoBehaviour
{
    private bool used;

    private void OnTriggerEnter2D(Collider2D other) => TryUse(other);

    private void OnTriggerStay2D(Collider2D other) => TryUse(other);

    private void TryUse(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (used || player == null || !player.TryHealOne())
            return;
        used = true;
        gameObject.SetActive(false);
    }
}

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
    public bool BelongsToSceneA { get; private set; } = true;
    private bool registered;

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
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        if (IsCollected || other.GetComponentInParent<PlayerController>() == null)
            return;

        ResolveTracker();
        if (tracker != null)
            tracker.Collect(this, value);
        else
            MarkCollected();
    }

    public void ConfigureRegion(bool sceneA)
    {
        BelongsToSceneA = sceneA;
        ResolveTracker();
        if (tracker == null || registered || IsCollected)
            return;

        tracker.Register(this, sceneA);
        registered = true;
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

        tracker = CollectibleTracker.Active;
        if (tracker == null)
            tracker = GetComponentInParent<CollectibleTracker>();
        if (tracker == null)
            tracker = FindAnyObjectByType<CollectibleTracker>();
    }
}
