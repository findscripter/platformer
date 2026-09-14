using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主梦泡视图。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 mainBubbleVisual / mainBubbleButton 的
/// 出现动画、点击、弱引导发光提示、震动与消散逻辑（Frame 2-4）。
/// </summary>
public class MainBubbleView : MonoBehaviour
{
    [SerializeField] private PlayerGuideMainBubbleVisual mainBubbleVisual;
    [SerializeField] private Button mainBubbleButton;
    [SerializeField] private TMP_Text echoText;

    [Header("Frame 1 舞台")]
    [SerializeField] private Vector2 stageSize = new Vector2(192f, 192f);
    [SerializeField] private Vector2 stageNorm = new Vector2(0.39f, 0.45f);

    [Header("Frame 2 强调")]
    [Tooltip("Frame 2 主梦泡放大强调的目标倍率（设计稿：延续 Frame 1 布局，放大强调）")]
    [SerializeField] private float emphasisScale = 1.15f;

    [Header("Frame 3 台词配置")]
    [SerializeField] private string echoDialogue = "「我一直在找回家的路……」";
    [SerializeField] private float echoFadeInDuration = 0.3f;
    [SerializeField] private float echoFadeOutDuration = 0.4f;

    public event Action OnClicked;

    private RectTransform clickHint;
    private Image bubbleImage;

    private float EmphasisScale => emphasisScale > 1f ? emphasisScale : 1.15f;

    private void OnDestroy()
    {
        if (mainBubbleButton != null)
        {
            mainBubbleButton.onClick.RemoveListener(HandleClicked);
        }
    }

    /// <summary>
    /// Figma Frame 1：主梦泡中偏左 39%/45%，192×192，蓝描边强调色由预制体承担。
    /// </summary>
    public void ApplyFigmaStagePlacement()
    {
        if (mainBubbleVisual == null)
            return;

        var rect = mainBubbleVisual.transform as RectTransform;
        if (rect == null)
            return;

        var parent = rect.parent as RectTransform;
        float width = parent != null ? Mathf.Max(1f, parent.rect.width) : 1920f;
        float height = parent != null ? Mathf.Max(1f, parent.rect.height) : 1080f;
        rect.sizeDelta = stageSize.x > 0f ? stageSize : new Vector2(192f, 192f);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2((stageNorm.x - 0.5f) * width, (0.5f - stageNorm.y) * height);
    }

    public void SetActive(bool active)
    {
        if (mainBubbleVisual != null)
        {
            mainBubbleVisual.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// 把主梦泡作为 Frame 1 舞台元素直接显示（常态尺寸、不播入场动画）。
    /// 可重复调用；alpha 与 scale 都会被重置回常态，避免上一轮消散残留状态。
    /// </summary>
    public void ShowAsStageElement()
    {
        if (mainBubbleVisual == null)
            return;

        mainBubbleVisual.gameObject.SetActive(true);
        EnsureBubbleArt();
        mainBubbleVisual.transform.localScale = Vector3.one;
        ApplyFigmaStagePlacement();
        EnsureEchoText();

        var parentImage = mainBubbleVisual.GetComponent<Image>();
        if (parentImage != null)
        {
            var color = parentImage.color;
            color.a = 1f;
            parentImage.color = color;
        }

        // Frame 4 的消散会留下 alpha<1 的 CanvasGroup，重新入场时必须复位。
        // 这里先置 0，由 Frame 1 的 FadeIn 跟着背景一起淡入。
        var bubbleGroup = EnsureCanvasGroup();
        if (bubbleGroup != null)
        {
            bubbleGroup.alpha = 0f;
        }
    }

    private void EnsureBubbleArt()
    {
        if (mainBubbleVisual == null)
            return;

        var visual = PlayerGuideMainBubbleVisual.EnsureStructure(mainBubbleVisual.gameObject);
        EnsureCircularMask(mainBubbleVisual.gameObject);
        RectTransform target = visual.ScaleTarget;
        if (target == null)
            return;
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
        bubbleImage = target.GetComponent<Image>();
        if (bubbleImage == null)
            bubbleImage = target.gameObject.AddComponent<Image>();

        Sprite art = GuideArt.Bubble;
        if (art != null)
        {
            bubbleImage.sprite = art;
            bubbleImage.overrideSprite = null;
        }
        bubbleImage.material = null;
        bubbleImage.type = Image.Type.Simple;
        bubbleImage.useSpriteMesh = false;
        bubbleImage.preserveAspect = true;
        bubbleImage.color = Color.white;
        bubbleImage.enabled = true;
        bubbleImage.raycastTarget = true;

        var rootImage = mainBubbleVisual.GetComponent<Image>();
        if (rootImage != null && rootImage != bubbleImage)
        {
            // 根节点 Image 同时承担圆形 Mask 的模板，不能关闭；Mask.showMaskGraphic
            // 已经负责隐藏它本身的白色遮罩图形。
            rootImage.enabled = true;
            rootImage.raycastTarget = false;
        }
        if (mainBubbleButton != null)
        {
            // The supplied cutout already contains its highlight and alpha. Keep the
            // button from multiplying that artwork with disabled/hover tints.
            mainBubbleButton.transition = Selectable.Transition.None;
            mainBubbleButton.targetGraphic = bubbleImage;
        }
        bubbleImage.canvasRenderer.SetColor(Color.white);
    }

    /// <summary>
    /// S01/0.png 的原图带方形透明画布和四角外发光；主梦泡显示时用现成圆形贴图做 UI 遮罩，
    /// 保留玻璃内容与光晕，同时裁掉四角，避免出现明显的方形切割痕迹。
    /// </summary>
    private void EnsureCircularMask(GameObject root)
    {
        if (root == null)
            return;

        Image maskImage = root.GetComponent<Image>();
        if (maskImage == null)
            maskImage = root.AddComponent<Image>();

        Sprite circle = GuideArt.Circle;
        if (circle != null)
            maskImage.sprite = circle;
        maskImage.type = Image.Type.Simple;
        maskImage.preserveAspect = false;
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;
        maskImage.enabled = true;

        Mask mask = root.GetComponent<Mask>();
        if (mask == null)
            mask = root.AddComponent<Mask>();
        mask.showMaskGraphic = false;
    }

    /// <summary>
    /// 取得（必要时创建）主梦泡的 CanvasGroup，用于整体淡入淡出。
    /// </summary>
    private CanvasGroup EnsureCanvasGroup()
    {
        if (mainBubbleVisual == null)
            return null;

        var group = mainBubbleVisual.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = mainBubbleVisual.gameObject.AddComponent<CanvasGroup>();
        }
        return group;
    }

    /// <summary>
    /// Frame 1 舞台淡入：主梦泡不在 BackgroundCanvasGroup 之下，
    /// 必须自己跟着背景一起从透明淡入，否则会在背景还没亮起时就整个「闪」出来。
    /// </summary>
    public IEnumerator FadeIn(float duration)
    {
        var group = EnsureCanvasGroup();
        if (group == null)
            yield break;

        group.alpha = 0f;

        if (duration <= 0f)
        {
            group.alpha = 1f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            group.alpha = elapsed / duration;
            elapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = 1f;
    }

    /// <summary>
    /// Frame 2 强调动画：从当前尺寸放大到 emphasisScale 并停在该尺寸，
    /// 表示"这个梦泡可以点"。不改变可见性（梦泡在 Frame 1 已显示）。
    /// </summary>
    public IEnumerator PlayEmphasis(float duration)
    {
        if (mainBubbleVisual == null)
            yield break;

        // 防御：若因故未经 Frame 1 直接进入 Frame 2，这里补上显示
        if (!mainBubbleVisual.gameObject.activeSelf)
        {
            ShowAsStageElement();
        }

        Transform target = mainBubbleVisual.transform;
        Vector3 from = target.localScale;
        Vector3 to = Vector3.one * EmphasisScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = EaseOutCubic(elapsed / duration);
            target.localScale = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = to;
    }

    public void EnableClick()
    {
        if (mainBubbleButton != null)
        {
            mainBubbleButton.interactable = true;
            mainBubbleButton.onClick.RemoveListener(HandleClicked);
            mainBubbleButton.onClick.AddListener(HandleClicked);
        }
    }

    public void DisableClick()
    {
        if (mainBubbleButton != null)
        {
            mainBubbleButton.onClick.RemoveListener(HandleClicked);
            mainBubbleButton.interactable = false;
        }
    }

    private void HandleClicked()
    {
        OnClicked?.Invoke();
    }

    public void ShowClickHint(bool visible)
    {
        EnsureClickHint();
        if (clickHint != null)
            clickHint.gameObject.SetActive(visible);
    }

    public IEnumerator PlayHintGlow(float glowIntensity)
    {
        if (mainBubbleVisual == null) yield break;

        if (bubbleImage == null)
            EnsureBubbleArt();
        Color originalColor = bubbleImage != null ? bubbleImage.color : Color.white;
        float bright = glowIntensity > 1f ? glowIntensity : 1.3f;
        Color lit = new Color(
            Mathf.Min(1f, originalColor.r * bright),
            Mathf.Min(1f, originalColor.g * bright),
            Mathf.Min(1f, originalColor.b * bright),
            originalColor.a);

        Transform bubbleTransform = mainBubbleVisual.transform;
        Vector3 originalScale = bubbleTransform.localScale;
        Vector3 targetScale = originalScale * 1.06f;

        float elapsed = 0f;
        float duration = 0.35f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            bubbleTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            if (bubbleImage != null)
                bubbleImage.color = Color.Lerp(originalColor, lit, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            bubbleTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            if (bubbleImage != null)
                bubbleImage.color = Color.Lerp(lit, originalColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        bubbleTransform.localScale = originalScale;
        if (bubbleImage != null)
            bubbleImage.color = originalColor;
    }

    public IEnumerator PlayVibration(float duration)
    {
        if (mainBubbleVisual == null)
            yield break;

        ShowClickHint(false);
        EnsureEchoText();
        Image silhouette = EnsureEchoSilhouette();

        // 显示台词
        if (echoText != null)
        {
            echoText.text = echoDialogue;
            echoText.gameObject.SetActive(true);
            yield return FadeText(echoText, 0f, 1f, echoFadeInDuration);
        }

        if (silhouette != null)
        {
            silhouette.gameObject.SetActive(true);
            yield return FadeGraphic(silhouette, 0f, 0.55f, 0.35f);
        }

        // 衰减震动（残响效果）
        Transform bubbleTransform = mainBubbleVisual.ScaleTarget;
        if (bubbleTransform == null)
            bubbleTransform = mainBubbleVisual.transform;

        Vector3 originalScale = bubbleTransform.localScale;

        float textHoldDuration = Mathf.Max(0f, duration - echoFadeOutDuration);
        bool echoFadeOutStarted = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 衰减包络：余音渐弱
            float decay = 1f - (elapsed / duration);
            float vibration = Mathf.Sin(elapsed * 10f) * 0.05f * decay;
            bubbleTransform.localScale = originalScale * (1f + vibration);

            // 在最后 0.4s 开始淡出文本
            if (!echoFadeOutStarted && echoText != null && elapsed >= textHoldDuration)
            {
                echoFadeOutStarted = true;
                StartCoroutine(FadeText(echoText, 1f, 0f, echoFadeOutDuration));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        bubbleTransform.localScale = originalScale;

        if (silhouette != null)
        {
            yield return FadeGraphic(silhouette, silhouette.color.a, 0f, echoFadeOutDuration);
            silhouette.gameObject.SetActive(false);
        }

        // 确保文本已隐藏
        if (echoText != null)
        {
            echoText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
    {
        if (graphic == null)
            yield break;

        Color color = graphic.color;
        color.a = from;
        graphic.color = color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            color.a = Mathf.Lerp(from, to, elapsed / duration);
            graphic.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = to;
        graphic.color = color;
    }

    /// <summary>
    /// TMP 文本 alpha 渐变
    /// </summary>
    private IEnumerator FadeText(TMP_Text text, float from, float to, float duration)
    {
        if (text == null) yield break;

        Color color = text.color;

        if (duration <= 0f)
        {
            color.a = to;
            text.color = color;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            color.a = Mathf.Lerp(from, to, elapsed / duration);
            text.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = to;
        text.color = color;
    }

    public IEnumerator PlayDissolve(float duration)
    {
        if (mainBubbleVisual == null)
            yield break;

        duration = Mathf.Max(0.05f, duration);
        ShowClickHint(false);

        CanvasGroup bubbleGroup = EnsureCanvasGroup();
        Vector3 startScale = mainBubbleVisual.transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (bubbleGroup != null)
                bubbleGroup.alpha = 1f - t;
            mainBubbleVisual.transform.localScale = startScale * (1f - t * 0.4f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (bubbleGroup != null)
            bubbleGroup.alpha = 0f;
        mainBubbleVisual.gameObject.SetActive(false);
    }

    private void EnsureEchoText()
    {
        if (echoText != null)
        {
            echoText.color = new Color(0.94f, 0.93f, 1f, 0f);
            echoText.transform.SetAsLastSibling();
            return;
        }

        if (mainBubbleVisual == null)
            return;

        var existing = mainBubbleVisual.transform.Find("EchoText");
        GameObject textGo;
        if (existing != null)
        {
            textGo = existing.gameObject;
        }
        else
        {
            textGo = new GameObject("EchoText", typeof(RectTransform));
            textGo.transform.SetParent(mainBubbleVisual.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.2f);
            rect.anchorMax = new Vector2(0.95f, 0.8f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        echoText = textGo.GetComponent<TMP_Text>();
        if (echoText == null)
            echoText = textGo.AddComponent<TextMeshProUGUI>();

        echoText.fontSize = 28;
        echoText.alignment = TextAlignmentOptions.Center;
        echoText.textWrappingMode = TextWrappingModes.Normal;
        echoText.color = new Color(0.94f, 0.93f, 1f, 0f);
        echoText.raycastTarget = false;
        GuideUiFont.Apply(echoText);
        textGo.SetActive(false);
    }

    private Image EnsureEchoSilhouette()
    {
        if (mainBubbleVisual == null)
            return null;

        Transform existing = mainBubbleVisual.transform.Find("EchoSilhouette");
        GameObject go = existing != null ? existing.gameObject : new GameObject("EchoSilhouette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null)
            go.transform.SetParent(mainBubbleVisual.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.18f, 0.12f);
        rect.anchorMax = new Vector2(0.82f, 0.78f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        var monster = GuideArt.MonsterWalk;
        image.sprite = monster != null ? monster : GuideArt.PlayerIdle;
        image.preserveAspect = true;
        image.color = new Color(0.64f, 0.60f, 0.78f, 0f);
        image.raycastTarget = false;

        Transform playerGhost = go.transform.Find("PlayerGhost");
        if (playerGhost == null && GuideArt.PlayerIdle != null)
        {
            var ghost = new GameObject("PlayerGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ghost.transform.SetParent(go.transform, false);
            var ghostRect = ghost.GetComponent<RectTransform>();
            ghostRect.anchorMin = new Vector2(0.55f, 0.05f);
            ghostRect.anchorMax = new Vector2(0.98f, 0.7f);
            ghostRect.offsetMin = Vector2.zero;
            ghostRect.offsetMax = Vector2.zero;
            var ghostImage = ghost.GetComponent<Image>();
            ghostImage.sprite = GuideArt.PlayerIdle;
            ghostImage.preserveAspect = true;
            ghostImage.color = new Color(0.64f, 0.66f, 0.8f, 0.55f);
            ghostImage.raycastTarget = false;
        }

        // 暗色内芯不透明，残响放在梦泡之上、台词之下。
        go.transform.SetSiblingIndex(mainBubbleVisual.ScaleTarget.GetSiblingIndex() + 1);
        go.SetActive(false);
        return image;
    }

    private void EnsureClickHint()
    {
        if (clickHint != null || mainBubbleVisual == null)
            return;

        var existing = mainBubbleVisual.transform.Find("ClickHint");
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject("ClickHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(mainBubbleVisual.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.08f);
            rect.sizeDelta = new Vector2(22f, 22f);
            rect.anchoredPosition = Vector2.zero;
            var image = go.GetComponent<Image>();
            var hint = GuideArt.HintDot;
            if (hint != null)
                image.sprite = hint;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        clickHint = go.GetComponent<RectTransform>();
        go.SetActive(false);
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
