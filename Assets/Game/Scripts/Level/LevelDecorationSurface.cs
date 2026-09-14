using UnityEngine;

/// <summary>只读物理根节点，把平台/地刺美术对齐到原有碰撞面。</summary>
public sealed class LevelDecorationSurface : MonoBehaviour
{
    [SerializeField] private BoxCollider2D shape;
    [SerializeField] private SpriteRenderer source;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private bool isSpike;
    [SerializeField] private Color tint;
    [SerializeField] private bool useContentRect;
    [SerializeField] private Rect contentRect;
    private bool orangeBlue;
    private SpriteRenderer edge;
    private SpriteRenderer halo;
    private static Sprite edgeSprite;
    public void EnableOrangeBlueEdge()
    {
        orangeBlue=true;
        if(edge!=null)return;
        if(edgeSprite==null)edgeSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f),1);
        edge=CreateEdge("StandingEdge",12);halo=CreateEdge("EdgeHalo",11);
    }
    private SpriteRenderer CreateEdge(string name,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(shape.transform,false);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=edgeSprite;
        renderer.sortingLayerName=GameLayers.InteractiveDownSorting;renderer.sortingOrder=order;
        return renderer;
    }

    // Pixel coordinates from the upper-left of the original canvas; no importer changes.
    public void SetContentRect(Rect pixels)
    {
        contentRect = pixels;
        useContentRect = true;
        LateUpdate();
    }

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
        if(orangeBlue)visual.color=new Color(1,1,1,source.color.a);
        Vector3 inherited = shape.transform.lossyScale;
        Vector2 physicalSize = Vector2.Scale(shape.size, new Vector2(Mathf.Abs(inherited.x), Mathf.Abs(inherited.y)));
        Bounds art = visual.sprite.bounds;
        if (useContentRect && !isSpike)
        {
            Sprite s = visual.sprite;
            Vector2 c = new Vector2(contentRect.center.x, s.rect.height - contentRect.center.y);
            art = new Bounds((c - s.pivot) / s.pixelsPerUnit,
                new Vector3(contentRect.width, contentRect.height, 0f) / s.pixelsPerUnit);
        }
        float sx = physicalSize.x / Mathf.Max(0.001f, art.size.x);
        float sy = sx;
        Vector3 center = shape.transform.TransformPoint(shape.offset);
        if(orangeBlue && edge!=null)
        {
            bool warning=source.color.b<.5f && source.color.r>.9f;
            Color light=warning?new Color(1f,.84f,.5f):new Color(.8f,.86f,.94f);
            UpdateEdge(edge,center,physicalSize,inherited,.03f,light,source.color.a*(warning?.6f:.55f));
            UpdateEdge(halo,center,physicalSize,inherited,.10f,light,source.color.a*(warning?.10f:.09f));
        }
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
    private void UpdateEdge(SpriteRenderer renderer,Vector3 centre,Vector2 size,Vector3 inherited,float height,Color colour,float alpha)
    {
        renderer.enabled=source.enabled && shape.enabled;
        colour.a=alpha;renderer.color=colour;
        renderer.transform.position=centre+Vector3.up*(size.y*.5f);
        Vector2 native=renderer.sprite.bounds.size;
        renderer.transform.localScale=new Vector3(size.x/(native.x*Mathf.Abs(inherited.x)),height/(native.y*Mathf.Abs(inherited.y)),1);
    }
}
