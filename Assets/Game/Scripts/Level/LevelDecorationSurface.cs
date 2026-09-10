using UnityEngine;

/// <summary>只读物理根节点，把平台/地刺美术对齐到原有碰撞面。</summary>
public sealed class LevelDecorationSurface : MonoBehaviour
{
    private BoxCollider2D shape;
    private SpriteRenderer source;
    private SpriteRenderer visual;
    private bool isSpike;
    private Color tint;

    public void Configure(BoxCollider2D collider, SpriteRenderer stateSource, SpriteRenderer renderer, bool spike, Color color)
    {
        shape = collider;
        source = stateSource;
        visual = renderer;
        isSpike = spike;
        tint = color;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (shape == null || source == null || visual == null || visual.sprite == null)
            return;

        // FadingPlatform 只认识根 renderer；隐藏时仍保留它的 15% 残影和恢复提示。
        visual.enabled = source.enabled;
        visual.color = source.color * tint;
        Vector3 inherited = shape.transform.lossyScale;
        Vector2 physicalSize = Vector2.Scale(shape.size, new Vector2(Mathf.Abs(inherited.x), Mathf.Abs(inherited.y)));
        Bounds art = visual.sprite.bounds;
        float sx = physicalSize.x / Mathf.Max(0.001f, art.size.x);
        float sy = sx;
        Vector3 center = shape.transform.TransformPoint(shape.offset);
        if (isSpike)
        {
            // S01/11 顶部 12/88 是透明边；尖端、底边均对应危险碰撞边缘。
            // E14 扩张后只重排 Visual，绝不回写 Collider 的 size/offset。
            Sprite sprite = visual.sprite;
            Vector2 canvas = sprite.rect.size / sprite.pixelsPerUnit;
            sx = physicalSize.x / Mathf.Max(0.001f, canvas.x);
            sy = physicalSize.y / Mathf.Max(0.001f, canvas.y * (76f / 88f));
            Vector2 bottom = new Vector2(canvas.x * 0.5f, 0f) - sprite.pivot / sprite.pixelsPerUnit;
            center.y -= physicalSize.y * 0.5f;
            transform.localScale = new Vector3(sx / inherited.x, sy / inherited.y, 1f);
            transform.position = center - Vector3.Scale(bottom, new Vector3(sx, sy, 1f));
            return;
        }
        else
        {
            // 平台保持原图比例，左右边界及上表面与实际落点一致。
            center.y += physicalSize.y * 0.5f - art.extents.y * sy;
        }

        transform.localScale = new Vector3(sx / inherited.x, sy / inherited.y, 1f);
        transform.position = center - Vector3.Scale(art.center, new Vector3(sx, sy, 1f));
    }
}
