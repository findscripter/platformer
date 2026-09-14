using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CameraTargetFollow : MonoBehaviour
{
    public Transform player;

    public float minX = 0f;
    public float minY = 0f;
    public float maxX = 500f;
    public float maxY = 16f;
    [SerializeField] private Vector3 followOffset = new Vector3(0.6f, 1.05f, 0f);
    [SerializeField] private float verticalLookAhead = 1.5f;

    private Camera cam;
    private Vector3 lastPlayerPosition;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (transform.parent != null)
            transform.SetParent(null, true);
        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        FollowCurrentRegion();
        ResolveViewCamera();

        float halfH = cam != null && cam.orthographic ? cam.orthographicSize : 5.5f;
        float halfW = halfH * (cam != null ? cam.aspect : 16f / 9f);

        float verticalOffset = 0f;
        if (lastPlayerPosition != Vector3.zero && Time.deltaTime > 0.0001f)
        {
            float verticalVelocity = (player.position.y - lastPlayerPosition.y) / Time.deltaTime;
            if (verticalVelocity > 0.1f)
                verticalOffset = Mathf.Clamp(verticalVelocity * 0.2f, 0f, verticalLookAhead);
            else if (verticalVelocity < -0.1f)
                verticalOffset = Mathf.Clamp(verticalVelocity * 0.15f, -verticalLookAhead, 0f);
        }
        lastPlayerPosition = player.position;

        Vector3 pos = player.position + followOffset + new Vector3(0f, verticalOffset, 0f);
        pos.z = transform.position.z;
        ClampToFollowBounds(ref pos, halfW, halfH);
        KeepBPlayerVisible(ref pos, halfW, halfH);
        transform.position = pos;
    }

    /// <summary>
    /// 立即瞬移相机到玩家位置（无平滑），用于重生等需要立即同步的场景
    /// </summary>
    public void Snap()
    {
        if (player == null)
            return;

        FollowCurrentRegion();
        ResolveViewCamera();

        float halfH = cam != null && cam.orthographic ? cam.orthographicSize : 5.5f;
        float halfW = halfH * (cam != null ? cam.aspect : 16f / 9f);

        Vector3 pos = player.position + followOffset;
        pos.z = transform.position.z;
        ClampToFollowBounds(ref pos, halfW, halfH);
        KeepBPlayerVisible(ref pos, halfW, halfH);
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
        maxY = Mathf.Max(roomMaxY, roomMinY + 24f);
    }

    /// <summary>
    /// A/B 各跟一整段区域，不用紧凑稿里那几格小房间当镜头边界。
    /// </summary>
    public void FollowCurrentRegion()
    {
        if (player == null)
            return;

        float origin = 0f;
        float length = DreamremainsExpandedLayout.ALength;
        if (player.position.x >= DreamremainsLevelBootstrap.SceneBOrigin - 1f)
        {
            origin = DreamremainsLevelBootstrap.SceneBOrigin;
            length = DreamremainsExpandedLayout.BLength;
        }

        SetRoom(origin, origin + length, -2f, 28f);
    }

    private void ClampToFollowBounds(ref Vector3 pos, float halfW, float halfH)
    {
        float xMin = minX + halfW;
        float xMax = maxX - halfW;
        if (xMin <= xMax)
            pos.x = Mathf.Clamp(pos.x, xMin, xMax);

        // 视口约 11 高时，用整屏 halfH 夹紧会把 Y 锁死在中线。
        // 垂直跟随时只留一点边，让镜头能跟着跳跃和高台走。
        float yPad = Mathf.Min(halfH * 0.25f, 1.2f);
        float yMin = minY + yPad;
        float yMaxBound = maxY - yPad;
        if (yMin <= yMaxBound)
            pos.y = Mathf.Clamp(pos.y, yMin, yMaxBound);
    }

    private void ResolveViewCamera()
    {
        if (cam != null && cam.isActiveAndEnabled)
            return;

        cam = Camera.main;
        if (cam == null)
            cam = GetComponent<Camera>();
    }

    private void KeepBPlayerVisible(ref Vector3 position, float halfW, float halfH)
    {
        if (player == null || player.position.x < DreamremainsLevelBootstrap.SceneBOrigin || player.position.y < -0.5f)
            return;

        var controller = player.GetComponent<PlayerController>();
        if (controller != null && controller.IsDead)
            return;

        var body = player.GetComponent<Collider2D>();
        Bounds bounds = body != null ? body.bounds : new Bounds(player.position, Vector3.one);
        foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>())
        {
            if (renderer.enabled)
                bounds.Encapsulate(renderer.bounds);
        }

        float left = bounds.max.x - halfW + 0.35f;
        float right = bounds.min.x + halfW - 0.35f;
        float bottom = bounds.max.y - halfH + 0.4f;
        float top = bounds.min.y + halfH - 0.4f;
        if (left <= right)
            position.x = Mathf.Clamp(position.x, left, right);
        if (bottom <= top)
            position.y = Mathf.Clamp(position.y, bottom, top);
    }
}
