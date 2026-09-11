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

    public static readonly Color DialogueInk = new Color(0.12f, 0.12f, 0.16f, 1f);

    /// <summary>
    /// Dialogue.png 的 PPU 是 128，默认 9-slice 边只有几像素，金扇会被拉扁。
    /// 把 multiplier 调到 1 纹理像素 = 1 画布单位，左右 293px 的扇才能保住形状。
    /// </summary>
    public static void ApplyDialogueBanner(Image image)
    {
        if (image == null)
            return;

        Sprite banner = GuideArt.DialogueBanner;
        if (banner != null)
        {
            image.sprite = banner;
            image.color = Color.white;
        }

        image.type = Image.Type.Sliced;
        image.fillCenter = true;
        image.preserveAspect = false;
        image.raycastTarget = false;

        Canvas canvas = image.canvas != null ? image.canvas.rootCanvas : null;
        float canvasPpu = canvas != null ? canvas.referencePixelsPerUnit : 100f;
        float spritePpu = image.sprite != null ? image.sprite.pixelsPerUnit : canvasPpu;
        image.pixelsPerUnitMultiplier = canvasPpu / Mathf.Max(1f, spritePpu);
    }

    /// <summary>底栏对白：横幅 9-slice，正文落在左右金扇之间。</summary>
    public static void LayoutDialogueBar(RectTransform panel, TMP_Text speaker, TMP_Text content)
    {
        Stretch(panel, new Vector2(0.05f, 0.045f), new Vector2(0.95f, 0.26f));
        ApplyDialogueBanner(panel != null ? panel.GetComponent<Image>() : null);

        if (speaker != null)
        {
            Stretch(speaker.rectTransform, new Vector2(0.24f, 0.64f), new Vector2(0.76f, 0.88f));
            ReadableText(speaker, 26f, DialogueInk);
            speaker.alignment = TextAlignmentOptions.Center;
            speaker.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (content != null)
        {
            Stretch(content.rectTransform, new Vector2(0.24f, 0.16f), new Vector2(0.76f, 0.62f));
            ReadableText(content, 36f, DialogueInk);
            content.alignment = TextAlignmentOptions.Center;
            content.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}
