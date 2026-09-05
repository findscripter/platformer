using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private int zoneIndex;
    [SerializeField] private float roomMinX;
    [SerializeField] private float roomMaxX;
    [SerializeField] private float roomMinY;
    [SerializeField] private float roomMaxY;

    public void Configure(int zone, float minX, float maxX, float minY, float maxY)
    {
        zoneIndex = zone;
        roomMinX = minX;
        roomMaxX = maxX;
        roomMinY = minY;
        roomMaxY = maxY;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        TarotZoneQuery.EnterZone(zoneIndex);
        CameraTargetFollow follow = Object.FindFirstObjectByType<CameraTargetFollow>();
        follow?.SetRoom(roomMinX, roomMaxX, roomMinY, roomMaxY);
    }
}
