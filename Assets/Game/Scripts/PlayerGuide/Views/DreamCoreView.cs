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
