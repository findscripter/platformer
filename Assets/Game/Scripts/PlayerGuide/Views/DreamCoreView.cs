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

    private Transform stageParent;
    private Vector2 stagePosition;
    private bool stageRemembered;
    private RectTransform holdParent;

    private void LateUpdate()
    {
        if (holdParent != null && dreamCoreObject != null)
            ApplyHeldSize();
    }

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

        if (!stageRemembered)
        {
            stageParent = rect.parent;
            stagePosition = rect.anchoredPosition;
            stageRemembered = true;
        }
        if (holdParent != null)
        {
            rect.SetParent(stageParent, false);
            holdParent = null;
        }
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = stagePosition;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.sizeDelta = new Vector2(72f, 72f);
        var image = dreamCoreObject.GetComponent<Image>();
        if (image != null)
        {
            var core = GuideArt.DreamCore;
            if (core != null)
            {
                image.sprite = core;
                image.overrideSprite = null;
            }
            image.material = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;

            if (dreamCoreGlow != null && dreamCoreGlow != image)
            {
                // 旧 Glow 没有 Sprite，会画出黄色矩形；沿用梦核的透明轮廓。
                dreamCoreGlow.sprite = core;
                dreamCoreGlow.overrideSprite = null;
                dreamCoreGlow.enabled = core != null;
                dreamCoreGlow.material = null;
                dreamCoreGlow.type = Image.Type.Simple;
                dreamCoreGlow.preserveAspect = true;
                dreamCoreGlow.raycastTarget = false;
                dreamCoreGlow.color = new Color(0.88f, 0.9f, 1f, dreamCoreGlow.color.a);
                RectTransform glowRect = dreamCoreGlow.rectTransform;
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.offsetMin = glowRect.offsetMax = Vector2.zero;
                glowRect.localScale = Vector3.one * 1.12f;
            }
        }
    }

    /// <summary>让梦核在消散的梦泡位置出现，保持同一 Canvas 的世界位置。</summary>
    public void PlaceAt(Transform source)
    {
        if (dreamCoreObject != null && source != null)
            dreamCoreObject.transform.position = source.position;
    }

    public void AttachTo(Transform parent, Vector2 localPos)
    {
        if (dreamCoreObject == null || parent == null)
            return;

        var rect = dreamCoreObject.transform as RectTransform;
        if (rect == null)
            return;

        if (!stageRemembered)
            ApplyFigmaSize();

        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = localPos;
        Vector3 position = rect.localPosition;
        position.z = 0f;
        rect.localPosition = position;
        holdParent = parent as RectTransform;
        ApplyHeldSize();
        rect.SetAsLastSibling();
    }

    private void ApplyHeldSize()
    {
        var rect = dreamCoreObject != null ? dreamCoreObject.transform as RectTransform : null;
        if (rect == null || holdParent == null)
            return;
        // 挂点尺寸与角色实际绘制尺寸一致，240px 会面位和 150px 陪伴位同比缩放。
        float size = Mathf.Max(1f, holdParent.rect.height * 0.28f);
        rect.sizeDelta = new Vector2(size, size);
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
        yield return LerpGlow(1f, duration);
    }

    public void SetGlowPurple(float alpha)
    {
        if (dreamCoreGlow == null)
            return;
        dreamCoreGlow.enabled = true;
        dreamCoreGlow.color = new Color(0.62f, 0.48f, 0.96f, Mathf.Clamp01(alpha));
        dreamCoreGlow.rectTransform.localScale = Vector3.one * 1.18f;
    }

    public IEnumerator LerpGlow(float alpha, float duration)
    {
        if (dreamCoreGlow == null)
            yield break;

        dreamCoreGlow.enabled = true;
        Color start = dreamCoreGlow.color;
        Color end = new Color(0.62f, 0.48f, 0.96f, Mathf.Clamp01(alpha));
        if (duration <= 0f)
        {
            dreamCoreGlow.color = end;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            dreamCoreGlow.color = Color.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        dreamCoreGlow.color = end;
    }

    public IEnumerator PlayPurpleRipple()
    {
        if (dreamCoreGlow != null)
        {
            Transform glow = dreamCoreGlow.transform;
            Vector3 glowFrom = Vector3.one * 1.18f;
            Vector3 glowTo = Vector3.one * 1.85f;
            Color start = new Color(0.62f, 0.48f, 0.96f, 0.7f);
            Color end = new Color(0.62f, 0.48f, 0.96f, 0.2f);
            glow.localScale = glowFrom;
            dreamCoreGlow.color = start;
            const float duration = 0.45f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                glow.localScale = Vector3.Lerp(glowFrom, glowTo, t);
                dreamCoreGlow.color = Color.Lerp(start, end, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            glow.localScale = glowFrom;
            dreamCoreGlow.color = new Color(0.62f, 0.48f, 0.96f, 0.45f);
        }

        yield return PlayRipple();
    }

    public IEnumerator ExpandHalo(float duration)
    {
        if (dreamCoreGlow == null)
            yield break;

        dreamCoreGlow.enabled = true;
        Transform glow = dreamCoreGlow.transform;
        Vector3 from = Vector3.one * 1.2f;
        Vector3 to = Vector3.one * 7.5f;
        Color start = new Color(0.58f, 0.42f, 0.95f, 0.8f);
        Color end = new Color(0.58f, 0.42f, 0.95f, 0.02f);
        glow.localScale = from;
        dreamCoreGlow.color = start;
        float safe = duration > 0.2f ? duration : 0.8f;
        float elapsed = 0f;
        while (elapsed < safe)
        {
            float t = elapsed / safe;
            float eased = 1f - (1f - t) * (1f - t);
            glow.localScale = Vector3.LerpUnclamped(from, to, eased);
            dreamCoreGlow.color = Color.Lerp(start, end, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
