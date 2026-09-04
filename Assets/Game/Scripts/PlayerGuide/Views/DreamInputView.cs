using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Frame 7-8 写梦输入面板视图。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 inputPanel / dreamInputField 的显示与提交逻辑。
/// </summary>
public class DreamInputView : MonoBehaviour
{
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField dreamInputField;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private Button submitButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text followUpHintText;

    public event Action OnSubmit;

    private void OnDestroy()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleSubmit);
        }
    }

    public void SetPanelActive(bool active)
    {
        if (inputPanel != null)
        {
            inputPanel.SetActive(active);
        }
    }

    public System.Collections.IEnumerator FadeIn(float duration)
    {
        yield return FadeCanvasGroup(0f, 1f, duration);
    }

    public System.Collections.IEnumerator FadeOut(float duration)
    {
        yield return FadeCanvasGroup(1f, 0f, duration);
    }

    public void SetPlaceholder(string text)
    {
        EnsureChrome();
        if (placeholderText != null)
        {
            placeholderText.text = text;
        }

        if (dreamInputField != null && dreamInputField.placeholder is TMP_Text tmpPlaceholder)
        {
            tmpPlaceholder.text = text;
        }
    }

    public void SetTitle(string text)
    {
        EnsureChrome();
        if (titleText != null)
        {
            titleText.text = text;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }

    public void SetFollowUpHint(string text)
    {
        EnsureChrome();
        if (followUpHintText == null)
            return;

        bool visible = !string.IsNullOrEmpty(text);
        followUpHintText.gameObject.SetActive(visible);
        followUpHintText.text = text ?? string.Empty;
    }

    public void SetSubmitLabel(string text)
    {
        if (submitButton == null)
            return;

        var label = submitButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = text;
    }

    /// <summary>
    /// Figma Frame 8 状态 A：同一问话框写下梦境。
    /// </summary>
    public void ShowWriteState()
    {
        EnsureChrome();
        SetTitle("写下你还记得的梦境");
        SetFollowUpHint(string.Empty);
        SetPlaceholder("写下你还记得的梦境");
        SetSubmitLabel("完成");
        SetInputFieldAnchors(new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.8f));
    }

    /// <summary>
    /// Figma Frame 8 状态 B：原位补问一次，问题贴着玩家原话。
    /// </summary>
    public void ShowFollowUpState(string originalInput)
    {
        EnsureChrome();
        string clipped = ClipOriginal(originalInput);
        SetTitle("腓腓补问（仅一次）");
        SetFollowUpHint(string.IsNullOrEmpty(clipped)
            ? "能再靠近一点吗？多说一点你还记得的意象或情绪。"
            : $"你写了「{clipped}」。能再靠近一点吗？");
        SetPlaceholder("补充你还记得的内容……");
        SetSubmitLabel("继续");
        SetInputFieldAnchors(new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.6f));
    }

    private void SetInputFieldAnchors(Vector2 min, Vector2 max)
    {
        if (dreamInputField == null)
            return;

        var rect = dreamInputField.transform as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string ClipOriginal(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return string.Empty;

        string trimmed = original.Trim();
        if (trimmed.Length <= 18)
            return trimmed;
        return trimmed.Substring(0, 18) + "…";
    }

    private void EnsureChrome()
    {
        if (inputPanel == null)
            return;

        if (titleText == null)
        {
            titleText = FindOrCreateLabel(inputPanel.transform, "Title", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.98f), 28, FontStyles.Bold);
        }

        if (followUpHintText == null)
        {
            followUpHintText = FindOrCreateLabel(inputPanel.transform, "FollowUpHint", new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.82f), 22, FontStyles.Normal);
            followUpHintText.gameObject.SetActive(false);
        }

        if (placeholderText == null && dreamInputField != null)
        {
            placeholderText = dreamInputField.placeholder as TMP_Text;
        }
    }

    private static TMP_Text FindOrCreateLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize, FontStyles style)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null)
            go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var tmp = go.GetComponent<TMP_Text>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshProUGUI>();

        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.color = new Color(0.18f, 0.18f, 0.2f, 1f);
        tmp.raycastTarget = false;
        GuideUiFont.Apply(tmp);
        return tmp;
    }

    /// <summary>
    /// 确保面板处于「玩家可见且可操作」状态。
    /// ActivateInputField / onClick 对 inactive 的 GameObject 是空操作，
    /// 重新征询输入前必须先把面板拉回来，否则等待提交会死锁。
    /// </summary>
    public void EnsureVisible()
    {
        SetPanelActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void ClearAndActivate()
    {
        if (dreamInputField != null)
        {
            dreamInputField.text = "";
            dreamInputField.ActivateInputField();
        }
    }

    public string GetInputText()
    {
        return dreamInputField != null ? dreamInputField.text.Trim() : string.Empty;
    }

    public void BindSubmit()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleSubmit);
            submitButton.onClick.AddListener(HandleSubmit);
        }
    }

    public void UnbindSubmit()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleSubmit);
            submitButton.onClick.RemoveAllListeners();
        }
    }

    private void HandleSubmit()
    {
        OnSubmit?.Invoke();
    }

    private System.Collections.IEnumerator FadeCanvasGroup(float from, float to, float duration)
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
        canvasGroup.blocksRaycasts = to > 0.01f;
        canvasGroup.interactable = to > 0.01f;
    }
}
