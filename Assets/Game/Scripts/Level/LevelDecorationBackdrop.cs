using UnityEngine;

/// <summary>等比 cover 当前视口；读取镜头，不改变镜头约束或关卡坐标。</summary>
[DefaultExecutionOrder(1000)]
public sealed class LevelDecorationBackdrop : MonoBehaviour
{
    private SpriteRenderer visual;
    private Camera viewCamera;
    private CameraTargetFollow follow;
    private float roomMin;
    private float roomMax;

    public void Configure(SpriteRenderer renderer, float min, float max)
    {
        visual = renderer;
        roomMin = min;
        roomMax = max;
        // 相机尚未初始化时，先等比覆盖整个区域，包括 B 的 66–67 末端平台。
        Fit(new Vector3((min + max) * 0.5f, 4f, 1f), max - min, 8f);
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (visual == null || visual.sprite == null)
            return;
        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
        {
            viewCamera = Camera.main;
            follow = viewCamera != null ? viewCamera.GetComponent<CameraTargetFollow>() : null;
        }
        if (viewCamera == null)
            return;

        // 由所跟随的角色所在大区选择固定底图，超宽视口也不会露出另一大区底图。
        float referenceX = follow != null && follow.player != null ? follow.player.position.x : viewCamera.transform.position.x;
        bool inSceneA = referenceX < 32f;
        visual.enabled = inSceneA == (roomMin < 1f);
        if (!visual.enabled)
            return;

        float height = viewCamera.orthographic ? viewCamera.orthographicSize * 2f
            : 2f * Mathf.Abs(viewCamera.transform.position.z - 1f) * Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        height = Mathf.Max(0.1f, height);
        float width = height * Mathf.Max(0.1f, viewCamera.aspect);
        // 留 12% 余量，仅在 cover 的安全范围里作轻微横向取景变化。
        Vector3 center = viewCamera.transform.position;
        center.z = 1f;
        float progress = Mathf.InverseLerp(roomMin, roomMax, center.x);
        center.x += Mathf.Lerp(width * 0.025f, -width * 0.025f, progress);
        Fit(center, width * 1.12f, height * 1.12f);
    }

    private void Fit(Vector3 center, float width, float height)
    {
        if (visual == null || visual.sprite == null)
            return;
        Bounds art = visual.sprite.bounds;
        float scale = Mathf.Max(width / Mathf.Max(0.001f, art.size.x), height / Mathf.Max(0.001f, art.size.y));
        Vector3 inherited = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(scale / inherited.x, scale / inherited.y, 1f);
        transform.position = center - art.center * scale;
    }
}
