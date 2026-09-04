using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 梦核视图。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 dreamCoreObject / dreamCoreGlow 的显示与光效逻辑。
/// </summary>
public class DreamCoreView : MonoBehaviour
{
    [SerializeField] private GameObject dreamCoreObject;
    [SerializeField] private Image dreamCoreGlow;

    public void SetActive(bool active)
    {
        if (dreamCoreObject != null)
        {
            dreamCoreObject.SetActive(active);
        }
    }

    /// <summary>Figma Frame 4：梦核 64–72px。</summary>
    public void ApplyFigmaSize()
    {
        if (dreamCoreObject == null)
            return;

        var rect = dreamCoreObject.transform as RectTransform;
        if (rect == null)
            return;

        rect.sizeDelta = new Vector2(72f, 96f);
        var image = dreamCoreObject.GetComponent<Image>();
        if (image != null)
        {
            var core = GuideArt.DreamCore;
            if (core != null)
                image.sprite = core;
            image.preserveAspect = true;
            image.color = Color.white;
        }
    }

    public void AttachTo(Transform parent, Vector2 localPos)
    {
        if (dreamCoreObject == null || parent == null)
            return;

        var rect = dreamCoreObject.transform as RectTransform;
        if (rect == null)
            return;

        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = localPos;
        rect.SetAsLastSibling();
    }

    public IEnumerator PlayRipple()
    {
        if (dreamCoreObject == null)
            yield break;

        Transform target = dreamCoreObject.transform;
        Vector3 original = target.localScale;
        Vector3 up = original * 1.28f;
        const float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float wave = Mathf.Sin(t * Mathf.PI);
            target.localScale = Vector3.Lerp(original, up, wave);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = original;
    }

    /// <summary>Frame 5 结果：梦核从躁动变安静（光晕回落到稳定微光）。</summary>
    public IEnumerator CalmFromAgitated(float duration)
    {
        if (dreamCoreGlow == null)
            yield break;

        Color start = dreamCoreGlow.color;
        Color end = start;
        end.a = 0.45f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            dreamCoreGlow.color = Color.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        dreamCoreGlow.color = end;
    }

    public IEnumerator FadeIn(float duration)
    {
        if (dreamCoreObject == null)
            yield break;

        CanvasGroup coreGroup = dreamCoreObject.GetComponent<CanvasGroup>();
        if (coreGroup == null)
        {
            coreGroup = dreamCoreObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            coreGroup.alpha = Mathf.Lerp(0f, 1f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        coreGroup.alpha = 1f;
    }

    public void SetGlowAlpha(float alpha)
    {
        if (dreamCoreGlow == null)
            return;

        Color glowColor = dreamCoreGlow.color;
        glowColor.a = alpha;
        dreamCoreGlow.color = glowColor;
    }

    public IEnumerator PulseGlowToFull(float duration)
    {
        if (dreamCoreGlow == null)
            yield break;

        Color startColor = dreamCoreGlow.color;
        Color endColor = startColor;
        endColor.a = 1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            dreamCoreGlow.color = Color.Lerp(startColor, endColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        dreamCoreGlow.color = endColor;
    }
}
