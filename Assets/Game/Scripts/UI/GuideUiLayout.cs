using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>运行时引导 UI 的共同尺寸与排版规则，不修改场景或美术资源。</summary>
public static class GuideUiLayout
{
    public static TMP_FontAsset LoadFont()
    {
        return GuideUiFont.Load();
    }

    public static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        if (rect == null)
            return;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    public static void ConfigureCanvas(Component owner)
    {
        var canvas = owner.GetComponentInParent<Canvas>();
        var scaler = canvas != null ? canvas.rootCanvas.GetComponent<CanvasScaler>() : null;
        if (scaler == null)
            return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // 保证设计画布始终放得进窗口，多出来的视口交给背景覆盖。
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }

    // 只对独立背景 Image 使用；含 UI 子节点的面板应另建背景子节点。
    public static void Cover(Image image, Sprite sprite)
    {
        if (image == null)
            return;
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.color = sprite != null ? Color.white : new Color(0.025f, 0.025f, 0.035f, 1f);
        Stretch(image.rectTransform, Vector2.zero, Vector2.one);
        var fitter = image.GetComponent<AspectRatioFitter>();
        if (sprite == null)
        {
            if (fitter != null)
                fitter.enabled = false;
            return;
        }
        if (fitter == null)
            fitter = image.gameObject.AddComponent<AspectRatioFitter>();
        fitter.enabled = true;
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
    }

    public static void ReadableText(TMP_Text text, float size, Color color)
    {
        if (text == null)
            return;
        var font = LoadFont();
        if (font != null)
            text.font = font;
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        // TMP's material setters require Awake/OnEnable to have initialized its renderer.
        // Inactive panels can be laid out before their first activation.
        if (text.isActiveAndEnabled)
            text.outlineWidth = 0f;
        text.rectTransform.localScale = Vector3.one;
    }
}
