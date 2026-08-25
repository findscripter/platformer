using System;
using System.Collections;
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

    public IEnumerator PlayAppearAnimation(float duration)
    {
        if (mainBubbleVisual == null)
            yield break;

        mainBubbleVisual.gameObject.SetActive(true);

        var parentImage = mainBubbleVisual.GetComponent<Image>();
        if (parentImage != null)
        {
            var color = parentImage.color;
            color.a = 1f;
            parentImage.color = color;
        }

        Transform target = mainBubbleVisual.transform;
        Vector3 from = Vector3.zero;
        Vector3 to = Vector3.one;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = EaseOutCubic(t);
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

        Transform bubbleTransform = mainBubbleVisual.transform;
        Vector3 originalScale = bubbleTransform.localScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float vibration = Mathf.Sin(elapsed * 10f) * 0.05f;
            bubbleTransform.localScale = originalScale * (1f + vibration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        bubbleTransform.localScale = originalScale;
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
