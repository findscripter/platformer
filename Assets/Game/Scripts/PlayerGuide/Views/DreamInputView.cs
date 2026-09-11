using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Frame 7-8 写梦输入面板。可自己写，也可使用腓腓拾来的示例梦境。
/// </summary>
public class DreamInputView : MonoBehaviour
{
    public const string SampleDream =
        "我梦见自己走在回家的路上，熟悉的街道却怎么也走不到尽头。\n"
        + "身后像有什么在催我，我想跑，脚步却很沉。\n"
        + "后来我停了下来，才发现墙边有一条窄窄的小路。\n"
        + "远处有一盏暖黄色的灯。\n"
        + "我不知道它通向哪里，但这次，我想自己选一条路。";

    private static readonly Color Ink = new Color(0.12f, 0.12f, 0.15f, 1f);
    private static readonly Color PlaceholderInk = new Color(0.35f, 0.35f, 0.38f, 1f);
    private static readonly Color GrayButton = new Color(0.62f, 0.62f, 0.64f, 1f);
    private static readonly Color GrayButtonDisabled = new Color(0.72f, 0.72f, 0.73f, 1f);
    private static readonly Color BlueButton = new Color(0.18f, 0.48f, 0.95f, 1f);

    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField dreamInputField;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private Button submitButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text followUpHintText;

    public event Action OnSubmit;

    public bool UsedSampleDream { get; private set; }

    private Button sampleButton;
    private Image sampleBodyPanel;
    private TMP_Text sampleBodyText;
    private bool chromeReady;
    private bool awaitingSubmit;
    private bool showingSample;
    private bool followUpMode;
    private string draftText = string.Empty;
    private float nextSubmitAllowedAt;
    private const float SubmitGestureGuardSeconds = 0.5f;

    private void OnDisable()
    {
        UnbindSubmit();
    }

    private void Update()
    {
        if (awaitingSubmit)
            RefreshButtons();
    }

    private void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(HandleLeftButton);
        if (sampleButton != null)
            sampleButton.onClick.RemoveListener(HandleRightButton);
        if (dreamInputField != null)
            dreamInputField.onValueChanged.RemoveListener(OnDraftChanged);
    }

    public void SetPanelActive(bool active)
    {
        if (active)
            EnsureChrome();
        else
            UnbindSubmit();
        if (inputPanel != null)
            inputPanel.SetActive(active);
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
            placeholderText.text = text;

        if (dreamInputField != null && dreamInputField.placeholder is TMP_Text tmpPlaceholder)
            tmpPlaceholder.text = text;
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
        SetButtonLabel(submitButton, text);
    }

    /// <summary>
    /// Figma Frame 8 状态 A：写下梦境，可改用示例模板。
    /// </summary>
    public void ShowWriteState()
    {
        EnsureChrome();
        showingSample = false;
        followUpMode = false;
        UsedSampleDream = false;
        SetTitle("写下你还记得的梦境");
        SetFollowUpHint("想不起梦也没关系，可以先使用示例梦境。");
        SetPlaceholder("一个画面、一种感觉，或一小段经历，都可以。");
        SetButtonLabel(submitButton, "写好了，继续");
        SetButtonLabel(sampleButton, "使用示例梦境");
        SetInputFieldAnchors(new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.8f));
        ApplyWriteLayout();
        if (dreamInputField != null)
        {
            dreamInputField.gameObject.SetActive(true);
            dreamInputField.readOnly = false;
            if (!string.IsNullOrEmpty(draftText) && string.IsNullOrEmpty(dreamInputField.text))
                dreamInputField.text = draftText;
        }
        SetSampleBodyVisible(false);
        StyleGray(submitButton);
        StyleBlue(sampleButton);
        RefreshButtons();
    }

    public void ShowSampleState()
    {
        EnsureChrome();
        showingSample = true;
        followUpMode = false;
        if (dreamInputField != null)
            draftText = dreamInputField.text;
        SetTitle("腓腓为你拾来一场梦");
        SetFollowUpHint("暂时想不起也没关系。借这场梦，开始你的旅程吧。");
        SetButtonLabel(submitButton, "返回自己写");
        SetButtonLabel(sampleButton, "就用这场梦");
        ApplyWriteLayout();
        if (followUpHintText != null)
            GuideUiLayout.Stretch(followUpHintText.rectTransform, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.84f));
        if (dreamInputField != null)
            dreamInputField.gameObject.SetActive(false);
        if (sampleBodyText != null)
            sampleBodyText.text = SampleDream;
        SetSampleBodyVisible(true);
        StyleGray(submitButton);
        StyleBlue(sampleButton);
        RefreshButtons();
    }

    /// <summary>
    /// Figma Frame 8 状态 B：原位补问一次，问题贴着玩家原话。
    /// </summary>
    public void ShowFollowUpState(string originalInput)
    {
        EnsureChrome();
        showingSample = false;
        followUpMode = true;
        UsedSampleDream = false;
        string clipped = ClipOriginal(originalInput);
        SetTitle("腓腓轻声问你");
        SetFollowUpHint(string.IsNullOrEmpty(clipped)
            ? "闭上眼想一想，梦里有什么让你一直记到现在？"
            : $"你说「{clipped}」。那时，你身边发生了什么？");
        SetPlaceholder("哪怕只记得一个小细节，也可以告诉我……");
        SetButtonLabel(submitButton, "继续");
        SetInputFieldAnchors(new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.62f));
        ApplyFollowUpLayout();
        if (dreamInputField != null)
        {
            dreamInputField.gameObject.SetActive(true);
            dreamInputField.readOnly = false;
        }
        SetSampleBodyVisible(false);
        StyleBlue(submitButton);
        RefreshButtons();
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
            titleText = FindOrCreateLabel(inputPanel.transform, "Title", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f), 36, FontStyles.Bold);

        if (followUpHintText == null)
        {
            followUpHintText = FindOrCreateLabel(inputPanel.transform, "FollowUpHint", new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.32f), 26, FontStyles.Normal);
            followUpHintText.gameObject.SetActive(false);
        }

        if (placeholderText == null && dreamInputField != null)
            placeholderText = dreamInputField.placeholder as TMP_Text;

        if (titleText != null)
            GuideUiLayout.Stretch(titleText.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f));
        if (followUpHintText != null)
            GuideUiLayout.Stretch(followUpHintText.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.32f));
        GuideUiLayout.ReadableText(titleText, 36f, Ink);
        GuideUiLayout.ReadableText(followUpHintText, 26f, Ink);
        if (followUpHintText != null)
            followUpHintText.richText = false;
        if (dreamInputField != null)
        {
            bool fieldWasEnabled = dreamInputField.enabled;
            bool installingViewport = dreamInputField.textViewport == null;
            if (installingViewport)
                dreamInputField.enabled = false;
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
            GuideUiLayout.ReadableText(inputText, 30f, Ink);
            if (inputText != null)
            {
                inputText.richText = false;
                inputText.alignment = TextAlignmentOptions.TopLeft;
                GuideUiLayout.Stretch(inputText.rectTransform, Vector2.zero, Vector2.one);
            }
            GuideUiLayout.ReadableText(placeholderText, 30f, PlaceholderInk);
            if (placeholderText != null)
            {
                placeholderText.alignment = TextAlignmentOptions.TopLeft;
                GuideUiLayout.Stretch(placeholderText.rectTransform, Vector2.zero, Vector2.one);
            }
            if (installingViewport)
                dreamInputField.enabled = fieldWasEnabled;
            dreamInputField.onValueChanged.RemoveListener(OnDraftChanged);
            dreamInputField.onValueChanged.AddListener(OnDraftChanged);
        }

        if (submitButton != null)
            EnsureButtonTarget(submitButton);

        sampleButton = FindOrCreateButton(inputPanel.transform, "SampleButton", "使用示例梦境");
        sampleBodyPanel = FindOrCreateSampleBody(inputPanel.transform);
        sampleBodyText = sampleBodyPanel != null
            ? sampleBodyPanel.GetComponentInChildren<TMP_Text>(true)
            : null;
        ApplyWriteLayout();
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
        draftText = string.Empty;
        if (dreamInputField != null)
        {
            dreamInputField.text = "";
            if (dreamInputField.gameObject.activeInHierarchy)
                dreamInputField.ActivateInputField();
        }
        RefreshButtons();
    }

    public string GetInputText()
    {
        if (showingSample)
            return SampleDream;
        return dreamInputField != null ? dreamInputField.text.Trim() : string.Empty;
    }

    public void BindSubmit()
    {
        awaitingSubmit = true;
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleLeftButton);
            submitButton.onClick.AddListener(HandleLeftButton);
        }
        if (sampleButton != null)
        {
            sampleButton.onClick.RemoveListener(HandleRightButton);
            sampleButton.onClick.AddListener(HandleRightButton);
        }
        RefreshButtons();
    }

    public void UnbindSubmit()
    {
        awaitingSubmit = false;
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleLeftButton);
            submitButton.interactable = false;
        }
        if (sampleButton != null)
        {
            sampleButton.onClick.RemoveListener(HandleRightButton);
            sampleButton.interactable = false;
        }
    }

    private void OnDraftChanged(string _)
    {
        draftText = dreamInputField != null ? dreamInputField.text : string.Empty;
        RefreshButtons();
    }

    private void HandleLeftButton()
    {
        if (!awaitingSubmit || !isActiveAndEnabled || Time.unscaledTime < nextSubmitAllowedAt)
            return;

        if (showingSample)
        {
            ShowWriteState();
            if (dreamInputField != null)
            {
                dreamInputField.text = draftText ?? string.Empty;
                dreamInputField.ActivateInputField();
            }
            RefreshButtons();
            return;
        }

        if (string.IsNullOrWhiteSpace(GetInputText()))
            return;

        FireSubmit(false);
    }

    private void HandleRightButton()
    {
        if (!awaitingSubmit || !isActiveAndEnabled || Time.unscaledTime < nextSubmitAllowedAt)
            return;

        if (followUpMode)
            return;

        if (showingSample)
        {
            if (dreamInputField != null)
                dreamInputField.text = SampleDream;
            FireSubmit(true);
            return;
        }

        ShowSampleState();
    }

    private void FireSubmit(bool usedSample)
    {
        UsedSampleDream = usedSample;
        awaitingSubmit = false;
        nextSubmitAllowedAt = Time.unscaledTime + SubmitGestureGuardSeconds;
        if (submitButton != null)
            submitButton.interactable = false;
        if (sampleButton != null)
            sampleButton.interactable = false;
        OnSubmit?.Invoke();
    }

    private void RefreshButtons()
    {
        bool guarded = Time.unscaledTime >= nextSubmitAllowedAt;
        if (submitButton != null)
        {
            if (showingSample)
                submitButton.interactable = guarded;
            else if (followUpMode)
                submitButton.interactable = guarded;
            else
                submitButton.interactable = guarded && !string.IsNullOrWhiteSpace(GetInputText());
        }
        if (sampleButton != null)
            sampleButton.interactable = !followUpMode && guarded;
    }

    private void ApplyWriteLayout()
    {
        PlacePairButton(submitButton, true);
        PlacePairButton(sampleButton, false);
        if (sampleButton != null)
            sampleButton.gameObject.SetActive(true);
        if (followUpHintText != null)
            GuideUiLayout.Stretch(followUpHintText.rectTransform, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.3f));
    }

    private void ApplyFollowUpLayout()
    {
        if (sampleButton != null)
            sampleButton.gameObject.SetActive(false);
        if (submitButton == null)
            return;
        var rect = submitButton.transform as RectTransform;
        if (rect == null)
            return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.09f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 56f);
        rect.anchoredPosition = Vector2.zero;
        if (followUpHintText != null)
            GuideUiLayout.Stretch(followUpHintText.rectTransform, new Vector2(0.12f, 0.64f), new Vector2(0.88f, 0.82f));
    }

    private void SetSampleBodyVisible(bool visible)
    {
        if (sampleBodyPanel != null)
            sampleBodyPanel.gameObject.SetActive(visible);
    }

    private static void PlacePairButton(Button button, bool left)
    {
        if (button == null)
            return;
        var rect = button.transform as RectTransform;
        if (rect == null)
            return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.09f);
        rect.pivot = new Vector2(left ? 1f : 0f, 0.5f);
        rect.sizeDelta = new Vector2(248f, 56f);
        rect.anchoredPosition = new Vector2(left ? -12f : 12f, 0f);
    }

    private static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
            return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = text;
    }

    private static void StyleGray(Button button)
    {
        if (button == null)
            return;
        ApplyTint(button, GrayButton, GrayButtonDisabled);
        SetLabelColor(button, Ink);
    }

    private static void StyleBlue(Button button)
    {
        if (button == null)
            return;
        ApplyTint(button, BlueButton, BlueButton * 0.7f);
        SetLabelColor(button, Color.white);
    }

    private static void ApplyTint(Button button, Color normal, Color disabled)
    {
        var animator = button.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;
        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = Color.white;
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Color.white, 0.08f);
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.12f);
        colors.selectedColor = normal;
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
    }

    private static void SetLabelColor(Button button, Color color)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.color = color;
    }

    private static void EnsureButtonTarget(Button button)
    {
        if (button == null)
            return;
        if (button.targetGraphic == null)
            button.targetGraphic = button.GetComponent<Image>();
    }

    private Button FindOrCreateButton(Transform parent, string name, string label)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (existing == null)
            go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;
        var button = go.GetComponent<Button>();
        EnsureButtonTarget(button);

        Transform labelTransform = go.transform.Find("Label");
        if (labelTransform == null)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.font = GuideUiLayout.LoadFont();
            tmp.fontSize = 28f;
            tmp.text = label;
            tmp.color = Color.white;
            GuideUiLayout.Stretch(tmp.rectTransform, Vector2.zero, Vector2.one);
        }
        else
        {
            var tmp = labelTransform.GetComponent<TMP_Text>();
            if (tmp != null)
                tmp.text = label;
        }

        return button;
    }

    private static Image FindOrCreateSampleBody(Transform parent)
    {
        Transform existing = parent.Find("SampleBody");
        GameObject go = existing != null ? existing.gameObject : new GameObject("SampleBody", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null)
            go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        GuideUiLayout.Stretch(rect, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.74f));
        var image = go.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        Transform textTransform = go.transform.Find("Text");
        TMP_Text tmp;
        if (textTransform == null)
        {
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            tmp = textGo.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            tmp = textTransform.GetComponent<TMP_Text>();
            if (tmp == null)
                tmp = textTransform.gameObject.AddComponent<TextMeshProUGUI>();
        }

        GuideUiLayout.Stretch(tmp.rectTransform, Vector2.zero, Vector2.one);
        tmp.rectTransform.offsetMin = new Vector2(22f, 16f);
        tmp.rectTransform.offsetMax = new Vector2(-22f, -16f);
        tmp.font = GuideUiLayout.LoadFont();
        tmp.fontSize = 30f;
        tmp.color = Ink;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.richText = false;
        go.SetActive(false);
        return image;
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
