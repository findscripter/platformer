using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace Game.PlayerGuide.Views
{
    public class DreamInputView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject inputPanel;
        [SerializeField] private TMP_InputField dreamInputField;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private Button submitButton;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Events")]
        public UnityEvent<string> OnSubmit = new UnityEvent<string>();

        private void Awake()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(HandleSubmit);
            }

            if (dreamInputField != null)
            {
                dreamInputField.onSubmit.AddListener(HandleInputFieldSubmit);
            }
        }

        private void OnDestroy()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(HandleSubmit);
            }

            if (dreamInputField != null)
            {
                dreamInputField.onSubmit.RemoveListener(HandleInputFieldSubmit);
            }
        }

        public void Show()
        {
            if (inputPanel != null)
            {
                inputPanel.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (dreamInputField != null)
            {
                dreamInputField.Select();
                dreamInputField.ActivateInputField();
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (inputPanel != null)
            {
                inputPanel.SetActive(false);
            }
        }

        public void Clear()
        {
            if (dreamInputField != null)
            {
                dreamInputField.text = string.Empty;
            }
        }

        public string GetInputText()
        {
            return dreamInputField != null ? dreamInputField.text : string.Empty;
        }

        private void HandleSubmit()
        {
            string inputText = GetInputText();
            if (!string.IsNullOrWhiteSpace(inputText))
            {
                OnSubmit?.Invoke(inputText);
            }
        }

        private void HandleInputFieldSubmit(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                OnSubmit?.Invoke(text);
            }
        }
    }
}
