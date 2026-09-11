using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            if (active)
            {
                GuideUiLayout.ConfigureCanvas(this);
                ApplyFullWidthReadableLayout();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }
        }
    }

    public void ApplyFullWidthReadableLayout()
    {
        GuideUiLayout.LayoutDialogueBar(dialoguePanel != null ? dialoguePanel.transform as RectTransform : null,
            speakerText, contentText);
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

    /// <summary>逐字显示，约 0.05s/字；空文本立即返回。</summary>
    public IEnumerator PlayTypewriter(string content, float secondsPerChar)
    {
        if (contentText == null)
            yield break;

        if (string.IsNullOrEmpty(content) || secondsPerChar <= 0f)
        {
            contentText.text = content ?? string.Empty;
            yield break;
        }

        contentText.text = string.Empty;
        for (int i = 0; i < content.Length; i++)
        {
            contentText.text = content.Substring(0, i + 1);
            yield return new WaitForSeconds(secondsPerChar);
        }

        contentText.text = content;
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return FadeCanvasGroup(0f, 1f, duration);
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return FadeCanvasGroup(1f, 0f, duration);
    }

    /// <summary>第一次相遇：玩家在左下三分之一，站在对白条上方。</summary>
    public void ShowPlayerStandIn(bool visible)
    {
        if (dialoguePanel == null)
            return;

        Transform parent = dialoguePanel.transform.parent != null ? dialoguePanel.transform.parent : dialoguePanel.transform;
        Transform existing = parent.Find("PlayerStandIn");
        if (existing == null)
            existing = dialoguePanel.transform.Find("PlayerStandIn");
        if (!visible)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        GameObject go = existing != null ? existing.gameObject : new GameObject("PlayerStandIn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null)
            go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.18f, 0.46f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360f, 400f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        var image = go.GetComponent<Image>();
        var player = GuideArt.PlayerIdle;
        if (player != null)
            image.sprite = player;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;

        Transform labelTf = go.transform.Find("Label");
        if (labelTf != null)
            labelTf.gameObject.SetActive(false);

        int barIndex = dialoguePanel.transform.GetSiblingIndex();
        go.transform.SetSiblingIndex(Mathf.Max(0, barIndex));
        go.SetActive(true);
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
