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
    [SerializeField] private float verticalLookAhead = 1.5f;

    private Camera cam;
    private Vector3 lastPlayerPosition;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        if (cam == null)
            cam = GetComponent<Camera>();

        float halfH = cam != null && cam.orthographic ? cam.orthographicSize : 2.8f;
        float halfW = halfH * (cam != null ? cam.aspect : 16f / 9f);

        // 计算垂直预判：如果玩家向上移动（跳跃），相机稍微往上看
        float verticalOffset = 0f;
        if (lastPlayerPosition != Vector3.zero)
        {
            float verticalVelocity = (player.position.y - lastPlayerPosition.y) / Time.deltaTime;
            if (verticalVelocity > 0.1f)
            {
                verticalOffset = Mathf.Clamp(verticalVelocity * 0.2f, 0f, verticalLookAhead);
            }
        }
        lastPlayerPosition = player.position;

        Vector3 pos = player.position + followOffset + new Vector3(0f, verticalOffset, 0f);
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

    /// <summary>
    /// 立即瞬移相机到玩家位置（无平滑），用于重生等需要立即同步的场景
    /// </summary>
    public void Snap()
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
        lastPlayerPosition = player.position;

        // 同时强制 MainCamera 立即瞬移（绕过 Cinemachine 的平滑混合）
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 camPos = pos;
            camPos.z = mainCam.transform.position.z;
            mainCam.transform.position = camPos;
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
