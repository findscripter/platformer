using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小梦泡容器视图。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 SpawnSmallBubbles / FloatSmallBubble 的生成与漂浮逻辑。
/// 所有配置由 Controller 从 PlayerGuideView 读取后传入，避免与根视图字段重复。
/// </summary>
public class SmallBubblesView : MonoBehaviour
{
    /// <summary>
    /// Figma Frame 1 小梦泡归一化位置（原点左上，对应 1920×1080 主帧舞台）。
    /// 主梦泡在 (0.39, 0.45) 192px，不在此列。
    /// </summary>
    private static readonly Vector2[] FigmaSmallBubbleNorm =
    {
        new Vector2(0.36f, 0.20f),
        new Vector2(0.20f, 0.32f),
        new Vector2(0.56f, 0.32f),
        new Vector2(0.73f, 0.24f),
        new Vector2(0.18f, 0.60f),
        new Vector2(0.64f, 0.56f)
    };

    private static readonly float[] FigmaSmallBubbleSizes = { 110f, 98f, 120f, 94f, 128f, 104f };

    public void SpawnSmallBubbles(Transform container, GameObject prefab, int count, Vector2 sizeRange, float floatSpeed, float floatAmplitude)
    {
        SpawnFigmaLayout(container, prefab, floatSpeed, floatAmplitude);
    }

    /// <summary>
    /// 按 Figma Frame 1 舞台散布 6 个小梦泡（64–128px），绕固定点缓慢漂浮。
    /// </summary>
    public void SpawnFigmaLayout(Transform container, GameObject prefab, float floatSpeed, float floatAmplitude)
    {
        if (container == null || prefab == null)
            return;

        RectTransform containerRect = container as RectTransform;
        if (containerRect == null)
        {
            Debug.LogWarning("[PlayerGuide] smallBubblesContainer 不是 RectTransform，跳过小梦泡生成");
            return;
        }

        ClearBubbles(container);

        float width = Mathf.Max(1f, containerRect.rect.width);
        float height = Mathf.Max(1f, containerRect.rect.height);
        Sprite bubbleArt = GuideArt.SmallBubble;

        for (int i = 0; i < FigmaSmallBubbleNorm.Length; i++)
        {
            GameObject bubble = Instantiate(prefab, containerRect);
            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            if (bubbleRect == null)
                continue;

            float size = FigmaSmallBubbleSizes[i];
            bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRect.pivot = new Vector2(0.5f, 0.5f);
            bubbleRect.localScale = Vector3.one;
            bubbleRect.localRotation = Quaternion.identity;
            bubbleRect.sizeDelta = new Vector2(size, size);

            // 直接使用素材包中132px的中号梦泡切图，覆盖当前94–128px显示尺寸。
            Image image = bubble.GetComponent<Image>();
            if (image == null)
                image = bubble.AddComponent<Image>();
            if (bubbleArt != null)
            {
                image.sprite = bubbleArt;
                image.overrideSprite = null;
            }
            image.material = null;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            image.enabled = true;

            Vector2 norm = FigmaSmallBubbleNorm[i];
            float x = (norm.x - 0.5f) * width;
            float y = (0.5f - norm.y) * height;
            bubbleRect.anchoredPosition = new Vector2(x, y);

            var group = bubble.GetComponent<CanvasGroup>();
            if (group == null)
                group = bubble.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            StartCoroutine(FloatSmallBubble(bubbleRect, x, y, floatSpeed, floatAmplitude));
        }
    }

    public void SetAllAlpha(Transform container, float alpha)
    {
        if (container == null)
            return;

        foreach (Transform child in container)
        {
            var group = child.GetComponent<CanvasGroup>();
            if (group == null)
                group = child.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            group.blocksRaycasts = false;
        }
    }

    public IEnumerator DimAll(Transform container, float targetAlpha, float duration)
    {
        if (container == null)
            yield break;

        var groups = new System.Collections.Generic.List<CanvasGroup>();
        var starts = new System.Collections.Generic.List<float>();
        foreach (Transform child in container)
        {
            var group = child.GetComponent<CanvasGroup>();
            if (group == null)
                group = child.gameObject.AddComponent<CanvasGroup>();
            groups.Add(group);
            starts.Add(group.alpha);
        }

        if (duration <= 0f)
        {
            for (int i = 0; i < groups.Count; i++)
                groups[i].alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            for (int i = 0; i < groups.Count; i++)
                groups[i].alpha = Mathf.Lerp(starts[i], targetAlpha, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < groups.Count; i++)
            groups[i].alpha = targetAlpha;
    }

    /// <summary>
    /// 小梦泡容器整体淡入。容器是 BackgroundCanvasGroup 的兄弟节点，
    /// 不会随背景淡入，必须自己跑一条同时长的淡入，否则会整批「闪」出来。
    /// </summary>
    public IEnumerator FadeInContainer(Transform container, float duration)
    {
        if (container == null)
            yield break;

        var group = container.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = container.gameObject.AddComponent<CanvasGroup>();
        }

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

    private IEnumerator FloatSmallBubble(RectTransform bubbleRect, float startX, float startY, float floatSpeed, float floatAmplitude)
    {
        float time = 0f;
        float phaseY = Random.Range(0f, Mathf.PI * 2f);
        float phaseX = Random.Range(0f, Mathf.PI * 2f);

        // 水平漂移改为正弦往返：原先的 (time * speed) % 100 是锯齿波，
        // 每 100px 会瞬间跳回起点，视觉上表现为梦泡"重置位置"。
        // 用周期 = 100 / speed 的正弦保持同样的漂移幅度与速率，但首尾连续。
        float driftRange = 50f;
        float angularSpeed = floatSpeed * Mathf.PI / 100f;

        // 相位是随机的，若直接把 sin 结果加到 startX/startY 上，第一帧就会从生成点
        // 瞬移最多 ±driftRange。这里减掉 t=0 时的初值，让曲线从生成点平滑起步。
        float baseOffsetX = Mathf.Sin(phaseX) * driftRange;
        float baseOffsetY = Mathf.Sin(phaseY) * floatAmplitude;

        while (bubbleRect != null && gameObject.activeInHierarchy)
        {
            time += Time.deltaTime;

            float offsetY = Mathf.Sin(time + phaseY) * floatAmplitude - baseOffsetY;
            float offsetX = Mathf.Sin(time * angularSpeed + phaseX) * driftRange - baseOffsetX;

            bubbleRect.anchoredPosition = new Vector2(startX + offsetX, startY + offsetY);

            yield return null;
        }
    }

    public void ClearBubbles(Transform container)
    {
        if (container == null)
            return;

        foreach (Transform child in container)
        {
            if (child.gameObject != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
