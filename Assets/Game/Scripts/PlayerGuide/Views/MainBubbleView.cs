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

    [Header("Frame 2 强调")]
    [Tooltip("Frame 2 主梦泡放大强调的目标倍率（设计稿：延续 Frame 1 布局，放大强调）")]
    [SerializeField] private float emphasisScale = 1.15f;

    [Header("Frame 3 台词配置")]
    [SerializeField] private string echoDialogue = "「我一直在找回家的路……」";
    [SerializeField] private string echoSoundNote = "（风声）";
    [SerializeField] private float echoFadeInDuration = 0.3f;
    [SerializeField] private float echoFadeOutDuration = 0.4f;

    public event Action OnClicked;

    private void OnDestroy()
    {
        if (mainBubbleButton != null)
        {
            mainBubbleButton.onClick.RemoveListener(HandleClicked);
        }
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
        mainBubbleVisual.transform.localScale = Vector3.one;

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
        Vector3 to = Vector3.one * emphasisScale;

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

    public IEnumerator PlayHintGlow(float glowIntensity)
    {
        if (mainBubbleVisual == null) yield break;

        Transform bubbleTransform = mainBubbleVisual.transform;
        Vector3 originalScale = bubbleTransform.localScale;
        Vector3 targetScale = originalScale * glowIntensity;

        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            bubbleTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            bubbleTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        bubbleTransform.localScale = originalScale;
    }

    public IEnumerator PlayVibration(float duration)
    {
        if (mainBubbleVisual == null)
            yield break;

        // 显示台词
        if (echoText != null)
        {
            echoText.text = $"{echoDialogue}\n{echoSoundNote}";
            echoText.gameObject.SetActive(true);
            yield return FadeText(echoText, 0f, 1f, echoFadeInDuration);
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

        // 确保文本已隐藏
        if (echoText != null)
        {
            echoText.gameObject.SetActive(false);
        }
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

        CanvasGroup bubbleGroup = mainBubbleVisual.GetComponent<CanvasGroup>();
        if (bubbleGroup == null)
        {
            bubbleGroup = mainBubbleVisual.gameObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        Vector3 startScale = mainBubbleVisual.transform.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            bubbleGroup.alpha = 1f - t;
            mainBubbleVisual.transform.localScale = startScale * (1f - t * 0.5f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainBubbleVisual.gameObject.SetActive(false);
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
