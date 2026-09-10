using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private int zoneIndex;
    [SerializeField] private float roomMinX;
    [SerializeField] private float roomMaxX;
    [SerializeField] private float roomMinY;
    [SerializeField] private float roomMaxY;

    private bool isPlayerInside;

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

        isPlayerInside = true;
        UpdateCameraRoom();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        // 每帧检查，确保相机边界正确（处理玩家往回走或重生的情况）
        if (!isPlayerInside)
        {
            isPlayerInside = true;
            UpdateCameraRoom();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        isPlayerInside = false;
    }

    private void UpdateCameraRoom()
    {
        TarotZoneQuery.EnterZone(zoneIndex);
        CameraTargetFollow follow = Object.FindFirstObjectByType<CameraTargetFollow>();
        follow?.SetRoom(roomMinX, roomMaxX, roomMinY, roomMaxY);
    }
}
