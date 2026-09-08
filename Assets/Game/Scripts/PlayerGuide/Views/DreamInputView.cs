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

    private bool chromeReady;
    private bool awaitingSubmit;
    private float nextSubmitAllowedAt;
    private const float SubmitGestureGuardSeconds = 0.5f;

    private void OnDisable()
    {
        UnbindSubmit();
    }

    private void Update()
    {
        if (awaitingSubmit && submitButton != null)
            submitButton.interactable = Time.unscaledTime >= nextSubmitAllowedAt;
    }

    private void OnDestroy()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleSubmit);
        }
    }

    public void SetPanelActive(bool active)
    {
        if (active)
            EnsureChrome();
        else
            UnbindSubmit();
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
        SetInputFieldAnchors(new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.8f));
    }

    /// <summary>
    /// Figma Frame 8 状态 B：原位补问一次，问题贴着玩家原话。
    /// </summary>
    public void ShowFollowUpState(string originalInput)
    {
        EnsureChrome();
        string clipped = ClipOriginal(originalInput);
        SetTitle("腓腓轻声问你");
        SetFollowUpHint(string.IsNullOrEmpty(clipped)
            ? "闭上眼想一想，梦里有什么让你一直记到现在？"
            : $"你说「{clipped}」。那时，你身边发生了什么？");
        SetPlaceholder("哪怕只记得一个小细节，也可以告诉我……");
        SetSubmitLabel("继续");
        SetInputFieldAnchors(new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.62f));
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
        if (inputPanel == null || chromeReady)
            return;

        chromeReady = true;
        GuideUiLayout.ConfigureCanvas(this);
        GuideUiLayout.Stretch(inputPanel.transform as RectTransform,
            new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.78f));
        if (canvasGroup == null)
            canvasGroup = inputPanel.GetComponent<CanvasGroup>();

        if (titleText == null)
        {
            titleText = FindOrCreateLabel(inputPanel.transform, "Title", new Vector2(0.12f, 0.83f), new Vector2(0.88f, 0.95f), 36, FontStyles.Bold);
        }

        if (followUpHintText == null)
        {
            followUpHintText = FindOrCreateLabel(inputPanel.transform, "FollowUpHint", new Vector2(0.12f, 0.64f), new Vector2(0.88f, 0.82f), 28, FontStyles.Normal);
            followUpHintText.gameObject.SetActive(false);
        }

        if (placeholderText == null && dreamInputField != null)
        {
            placeholderText = dreamInputField.placeholder as TMP_Text;
        }

        var ink = new Color(0.12f, 0.12f, 0.15f, 1f);
        if (titleText != null)
            GuideUiLayout.Stretch(titleText.rectTransform, new Vector2(0.12f, 0.83f), new Vector2(0.88f, 0.95f));
        if (followUpHintText != null)
            GuideUiLayout.Stretch(followUpHintText.rectTransform, new Vector2(0.12f, 0.64f), new Vector2(0.88f, 0.82f));
        GuideUiLayout.ReadableText(titleText, 36f, ink);
        GuideUiLayout.ReadableText(followUpHintText, 28f, ink);
        if (followUpHintText != null)
            followUpHintText.richText = false;
        if (dreamInputField != null)
        {
            bool fieldWasEnabled = dreamInputField.enabled;
            bool installingViewport = dreamInputField.textViewport == null;
            if (installingViewport)
                dreamInputField.enabled = false;
            // 场景原来未指定 viewport 且是单行输入；补齐裁切区，让长梦境可换行滚动。
            if (installingViewport)
            {
                var viewport = new GameObject("TextViewport", typeof(RectTransform), typeof(RectMask2D));
                viewport.transform.SetParent(dreamInputField.transform, false);
                var viewportRect = (RectTransform)viewport.transform;
                GuideUiLayout.Stretch(viewportRect, Vector2.zero, Vector2.one);
                viewportRect.offsetMin = new Vector2(18f, 14f);
                viewportRect.offsetMax = new Vector2(-18f, -14f);
                dreamInputField.textViewport = viewportRect;
                if (dreamInputField.textComponent != null)
                    dreamInputField.textComponent.transform.SetParent(viewportRect, false);
                if (dreamInputField.placeholder != null)
                    dreamInputField.placeholder.transform.SetParent(viewportRect, false);
                // 已激活的 TMP 会提前建立 Caret；它必须跟文字处于同一坐标空间。
                var caret = dreamInputField.GetComponentInChildren<TMP_SelectionCaret>(true);
                if (caret != null)
                {
                    caret.transform.SetParent(viewportRect, false);
                    caret.transform.SetAsFirstSibling();
                }
            }
            dreamInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            dreamInputField.richText = false;
            var inputText = dreamInputField.textComponent;
            GuideUiLayout.ReadableText(inputText, 30f, ink);
            if (inputText != null)
            {
                inputText.richText = false;
                inputText.alignment = TextAlignmentOptions.TopLeft;
                GuideUiLayout.Stretch(inputText.rectTransform, Vector2.zero, Vector2.one);
            }
            GuideUiLayout.ReadableText(placeholderText, 30f, new Color(0.35f, 0.35f, 0.38f, 1f));
            if (placeholderText != null)
            {
                placeholderText.alignment = TextAlignmentOptions.TopLeft;
                GuideUiLayout.Stretch(placeholderText.rectTransform, Vector2.zero, Vector2.one);
            }
            // 让 TMP.OnEnable 重新缓存 viewport 的 RectMask2D 和裁切范围。
            if (installingViewport)
                dreamInputField.enabled = fieldWasEnabled;
        }
        if (submitButton != null)
        {
            var rect = submitButton.transform as RectTransform;
            GuideUiLayout.Stretch(rect, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f));
            if (rect != null)
                rect.sizeDelta = new Vector2(200f, 64f);
            GuideUiLayout.ReadableText(submitButton.GetComponentInChildren<TMP_Text>(true), 30f, ink);
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
        tmp.font = GuideUiLayout.LoadFont();
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
        awaitingSubmit = true;
        if (submitButton != null)
        {
            submitButton.interactable = Time.unscaledTime >= nextSubmitAllowedAt;
            submitButton.onClick.RemoveListener(HandleSubmit);
            submitButton.onClick.AddListener(HandleSubmit);
        }
    }

    public void UnbindSubmit()
    {
        awaitingSubmit = false;
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleSubmit);
            submitButton.interactable = false;
        }
    }

    private void HandleSubmit()
    {
        if (!awaitingSubmit || !isActiveAndEnabled || Time.unscaledTime < nextSubmitAllowedAt)
            return;
        awaitingSubmit = false;
        // Frame7→Frame8 会在相邻帧重新 Bind；同一次双击不能顺手提交空补问。
        nextSubmitAllowedAt = Time.unscaledTime + SubmitGestureGuardSeconds;
        if (submitButton != null)
            submitButton.interactable = false;
        OnSubmit?.Invoke();
    }

    private System.Collections.IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        canvasGroup.alpha = from;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = to > 0.01f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = to > 0.01f;
        canvasGroup.interactable = to > 0.01f;
    }
}
