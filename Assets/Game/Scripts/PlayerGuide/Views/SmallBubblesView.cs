using System.Collections;
using UnityEngine;

/// <summary>
/// 小梦泡容器视图。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 SpawnSmallBubbles / FloatSmallBubble 的生成与漂浮逻辑。
/// 所有配置由 Controller 从 PlayerGuideView 读取后传入，避免与根视图字段重复。
/// </summary>
public class SmallBubblesView : MonoBehaviour
{
    public void SpawnSmallBubbles(Transform container, GameObject prefab, int count, Vector2 sizeRange, float floatSpeed, float floatAmplitude)
    {
        if (container == null || prefab == null)
            return;

        RectTransform containerRect = container as RectTransform;
        if (containerRect == null)
        {
            Debug.LogWarning("[PlayerGuide] smallBubblesContainer 不是 RectTransform，跳过小梦泡生成");
            return;
        }

        float width = containerRect.rect.width;
        float height = containerRect.rect.height;

        for (int i = 0; i < count; i++)
        {
            GameObject bubble = Instantiate(prefab, containerRect);
            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            if (bubbleRect == null) continue;

            float size = Random.Range(sizeRange.x, sizeRange.y);
            bubbleRect.sizeDelta = new Vector2(size, size);

            float margin = size * 0.5f + 50f;
            float x = Random.Range(-width * 0.5f + margin, width * 0.5f - margin);
            float y = Random.Range(-height * 0.5f + margin, height * 0.5f - margin);
            bubbleRect.anchoredPosition = new Vector2(x, y);

            StartCoroutine(FloatSmallBubble(bubbleRect, x, y, floatSpeed, floatAmplitude));
        }
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
