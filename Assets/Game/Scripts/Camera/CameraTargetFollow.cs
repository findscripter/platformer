using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CameraTargetFollow : MonoBehaviour
{
    public Transform player;

    public float minX = 0f;
    public float minY = 0f;
    public float maxX = 68f;
    public float maxY = 8f;
    [SerializeField] private Vector3 followOffset = new Vector3(0.6f, 0.35f, 0f);

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        if (cam == null)
            cam = GetComponent<Camera>();

        float halfH = cam != null && cam.orthographic ? cam.orthographicSize : 2.8f;
        float halfW = halfH * (cam != null ? cam.aspect : 16f / 9f);

        Vector3 pos = player.position + followOffset;
        pos.z = transform.position.z;

        float xMin = minX + halfW;
        float xMax = maxX - halfW;
        if (xMin <= xMax)
            pos.x = Mathf.Clamp(pos.x, xMin, xMax);

        float yMin = minY + halfH;
        float yMax = maxY - halfH;
        if (yMin <= yMax)
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

        transform.position = pos;
    }

    public void SetRoom(float roomMinX, float roomMaxX, float roomMinY, float roomMaxY)
    {
        minX = roomMinX;
        maxX = roomMaxX;
        minY = roomMinY;
        maxY = roomMaxY;
    }
}
