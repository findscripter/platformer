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
        if (placeholderText != null)
        {
            placeholderText.text = text;
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
    }
}
