using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SpikeGrowth : MonoBehaviour
{
    [SerializeField] private int zoneIndex = -1;
    [SerializeField] private float extraHeight = 0.7f;

    private BoxCollider2D box;
    private Vector2 restSize;
    private Vector2 restOffset;
    private bool grown;

    public void Configure(int zone, float growth)
    {
        zoneIndex = zone;
        extraHeight = growth;
    }

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        restSize = box.size;
        restOffset = box.offset;
    }

    private void Update()
    {
        bool shouldGrow = TarotZoneQuery.HasEffect(zoneIndex, TarotEffectId.E14);
        if (shouldGrow == grown)
            return;

        grown = shouldGrow;
        if (grown)
        {
            box.size = new Vector2(restSize.x, restSize.y + extraHeight);
            box.offset = new Vector2(restOffset.x, restOffset.y + extraHeight * 0.5f);
        }
        else
        {
            box.size = restSize;
            box.offset = restOffset;
        }
    }
}
