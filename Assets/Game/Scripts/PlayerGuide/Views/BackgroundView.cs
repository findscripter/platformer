using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背景视图（背景 CanvasGroup 淡入淡出、音频残响播放、背景图切换）。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 backgroundCanvasGroup / dreamEchoAudioSource 的逻辑。
/// </summary>
public class BackgroundView : MonoBehaviour
{
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [SerializeField] private AudioSource dreamEchoAudioSource;
    [SerializeField] private Image backgroundImage;

    public void SetAlpha(float alpha)
    {
        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha = alpha;
        }
    }

    public void SetBackgroundSprite(Sprite sprite)
    {
        if (backgroundImage != null && sprite != null)
        {
            backgroundImage.sprite = sprite;
        }
    }

    public IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (backgroundCanvasGroup == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            backgroundCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        backgroundCanvasGroup.alpha = to;
    }

    public void PlayDreamEcho()
    {
        GuideSfx.PlayWind(dreamEchoAudioSource);
    }

    public void StopDreamEcho()
    {
        GuideSfx.StopWind(dreamEchoAudioSource);
    }
}
