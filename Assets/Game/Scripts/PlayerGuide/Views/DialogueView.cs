using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Frame 6/8 对话面板视图。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 dialoguePanel / dialogueCanvasGroup 的显示逻辑。
/// </summary>
public class DialogueView : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private CanvasGroup canvasGroup;

    public void SetPanelActive(bool active)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(active);
        }
    }

    public void SetSpeaker(string speaker)
    {
        if (speakerText != null)
        {
            speakerText.text = speaker;
        }
    }

    public void SetContent(string content)
    {
        if (contentText != null)
        {
            contentText.text = content;
        }
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return FadeCanvasGroup(0f, 1f, duration);
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return FadeCanvasGroup(1f, 0f, duration);
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
