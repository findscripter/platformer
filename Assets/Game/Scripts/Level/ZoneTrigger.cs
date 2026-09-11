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
        // 镜头跟随整段 A/B 区域，不再按 8 个平台切房间。
        // 旧房间触发条只有 0.4 宽，跳一下就会错过，相机会卡在上一段右沿。
    }
}
