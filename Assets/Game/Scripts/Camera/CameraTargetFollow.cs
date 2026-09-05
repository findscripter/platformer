using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CameraTargetFollow : MonoBehaviour
{
    public Transform player;

    public float minX = 1f;
    public float minY = -5f;
    public float maxX = 64f;
    public float maxY = 8f;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.05f, 0f);

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 pos = player.position + followOffset;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if ((transform.position - pos).sqrMagnitude > 0.000001f)
        {
            transform.position = pos;
        }
    }

    public void SetRoom(float roomMinX, float roomMaxX, float roomMinY, float roomMaxY)
    {
        minX = roomMinX;
        maxX = roomMaxX;
        minY = roomMinY;
        maxY = roomMaxY;
    }
}
