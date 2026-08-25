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

    private IEnumerator FloatSmallBubble(RectTransform bubbleRect, float startX, float startY, float floatSpeed, float floatAmplitude)
    {
        float time = 0f;
        float randomPhase = Random.Range(0f, Mathf.PI * 2f);

        while (bubbleRect != null && gameObject.activeInHierarchy)
        {
            time += Time.deltaTime;

            float offsetY = Mathf.Sin(time + randomPhase) * floatAmplitude;
            float offsetX = (time * floatSpeed) % 100f - 50f;

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
