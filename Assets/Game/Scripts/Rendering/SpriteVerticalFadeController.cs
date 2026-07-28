using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteVerticalFadeController : MonoBehaviour
{
    private static readonly int FadeTopId = Shader.PropertyToID("_FadeTop");
    private static readonly int FadeBottomId = Shader.PropertyToID("_FadeBottom");
    private static readonly int FadeSoftnessId = Shader.PropertyToID("_FadeSoftness");

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Range(0f, 1f)] private float fadeTop = 1f;
    [SerializeField, Range(0f, 1f)] private float fadeBottom = 0f;
    [SerializeField, Range(0f, 1f)] private float fadeSoftness = 0f;

    private MaterialPropertyBlock propertyBlock;

    public float FadeTop
    {
        get => fadeTop;
        set
        {
            fadeTop = Mathf.Clamp01(value);
            Apply();
        }
    }

    public float FadeBottom
    {
        get => fadeBottom;
        set
        {
            fadeBottom = Mathf.Clamp01(value);
            Apply();
        }
    }

    public float FadeSoftness
    {
        get => fadeSoftness;
        set
        {
            fadeSoftness = Mathf.Clamp01(value);
            Apply();
        }
    }

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Apply();
    }

    public void Apply()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(FadeTopId, fadeTop);
        propertyBlock.SetFloat(FadeBottomId, fadeBottom);
        propertyBlock.SetFloat(FadeSoftnessId, fadeSoftness);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}
