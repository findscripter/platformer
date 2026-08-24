using System.Collections;
using TMPro;
using UnityEngine;

namespace PlayerGuide.Views
{
    /// <summary>
    /// Dialogue panel UI component - handles dialogue text display with fade animations.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.3f;

        private Coroutine activeFadeCoroutine;

        private void Awake()
        {
            ValidateReferences();

            if (canvasGroup == null && dialoguePanel != null)
                canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();

            if (dialoguePanel != null && dialoguePanel.activeSelf)
                dialoguePanel.SetActive(false);
        }

        private void ValidateReferences()
        {
            if (dialoguePanel == null)
                Debug.LogWarning($"DialogueView on {gameObject.name}: dialoguePanel is not assigned.");
            if (speakerText == null)
                Debug.LogWarning($"DialogueView on {gameObject.name}: speakerText is not assigned.");
            if (contentText == null)
                Debug.LogWarning($"DialogueView on {gameObject.name}: contentText is not assigned.");
        }

        /// <summary>
        /// Shows the dialogue panel immediately without animation.
        /// </summary>
        public void Show()
        {
            if (dialoguePanel == null)
                return;

            StopActiveFade();
            dialoguePanel.SetActive(true);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Hides the dialogue panel immediately without animation.
        /// </summary>
        public void Hide()
        {
            if (dialoguePanel == null)
                return;

            StopActiveFade();
            dialoguePanel.SetActive(false);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Shows the dialogue panel with fade-in animation.
        /// </summary>
        public void FadeIn()
        {
            if (dialoguePanel == null)
                return;

            StopActiveFade();
            dialoguePanel.SetActive(true);

            if (canvasGroup != null)
                activeFadeCoroutine = StartCoroutine(FadeInCoroutine());
        }

        /// <summary>
        /// Hides the dialogue panel with fade-out animation.
        /// </summary>
        public void FadeOut()
        {
            if (dialoguePanel == null || canvasGroup == null)
            {
                Hide();
                return;
            }

            StopActiveFade();
            activeFadeCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        /// <summary>
        /// Sets the speaker name text.
        /// </summary>
        /// <param name="speaker">Speaker name to display</param>
        public void SetSpeaker(string speaker)
        {
            if (speakerText != null)
                speakerText.text = speaker ?? string.Empty;
        }

        /// <summary>
        /// Sets the dialogue content text.
        /// </summary>
        /// <param name="content">Content text to display</param>
        public void SetContent(string content)
        {
            if (contentText != null)
                contentText.text = content ?? string.Empty;
        }

        /// <summary>
        /// Sets both speaker and content in one call.
        /// </summary>
        /// <param name="speaker">Speaker name</param>
        /// <param name="content">Dialogue content</param>
        public void SetDialogue(string speaker, string content)
        {
            SetSpeaker(speaker);
            SetContent(content);
        }

        private void StopActiveFade()
        {
            if (activeFadeCoroutine != null)
            {
                StopCoroutine(activeFadeCoroutine);
                activeFadeCoroutine = null;
            }
        }

        private IEnumerator FadeInCoroutine()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            activeFadeCoroutine = null;
        }

        private IEnumerator FadeOutCoroutine()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            dialoguePanel.SetActive(false);
            activeFadeCoroutine = null;
        }
    }
}
