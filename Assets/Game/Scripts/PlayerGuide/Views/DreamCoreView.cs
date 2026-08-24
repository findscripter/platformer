using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.PlayerGuide.Views
{
    /// <summary>
    /// Visual component for the Dream Core UI element.
    /// Handles appearance, glow effects, and fade transitions.
    /// </summary>
    public class DreamCoreView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject dreamCoreObject;
        [SerializeField] private Image glowImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Glow Settings")]
        [SerializeField] private float minGlowAlpha = 0.3f;
        [SerializeField] private float maxGlowAlpha = 1f;

        [Header("Fade Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;

        private Coroutine currentFadeCoroutine;

        private void Awake()
        {
            ValidateReferences();
        }

        /// <summary>
        /// Shows the dream core with optional fade-in animation.
        /// </summary>
        /// <param name="instant">If true, shows immediately without animation.</param>
        public void Show(bool instant = false)
        {
            if (dreamCoreObject == null) return;

            dreamCoreObject.SetActive(true);

            if (instant)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }
            else
            {
                FadeIn();
            }
        }

        /// <summary>
        /// Hides the dream core immediately.
        /// </summary>
        public void Hide()
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
            }

            if (dreamCoreObject != null)
            {
                dreamCoreObject.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Sets the glow intensity of the dream core.
        /// </summary>
        /// <param name="intensity">Normalized intensity value (0-1).</param>
        public void SetGlowIntensity(float intensity)
        {
            if (glowImage == null) return;

            float clampedIntensity = Mathf.Clamp01(intensity);
            Color color = glowImage.color;
            color.a = Mathf.Lerp(minGlowAlpha, maxGlowAlpha, clampedIntensity);
            glowImage.color = color;
        }

        /// <summary>
        /// Plays a ripple effect emanating from the dream core.
        /// TODO: Implement ripple animation (particle system, shader, or animated sprite).
        /// </summary>
        public void PlayRippleEffect()
        {
            // TODO: Implement ripple effect
            // Options:
            // 1. Particle system emission
            // 2. Shader-based wave distortion
            // 3. Animated sprite overlay
            // 4. Scale/fade animation ring
            Debug.LogWarning("DreamCoreView.PlayRippleEffect() - Not yet implemented");
        }

        /// <summary>
        /// Initiates a fade-in animation for the dream core.
        /// </summary>
        public void FadeIn()
        {
            if (canvasGroup == null)
            {
                Debug.LogWarning("DreamCoreView: CanvasGroup is null, cannot fade in.");
                return;
            }

            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            currentFadeCoroutine = StartCoroutine(FadeInCoroutine());
        }

        /// <summary>
        /// Coroutine that performs the fade-in animation.
        /// </summary>
        private IEnumerator FadeInCoroutine()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            currentFadeCoroutine = null;
        }

        /// <summary>
        /// Validates that all required references are assigned.
        /// </summary>
        private void ValidateReferences()
        {
            if (dreamCoreObject == null)
            {
                Debug.LogWarning("DreamCoreView: dreamCoreObject is not assigned.", this);
            }

            if (glowImage == null)
            {
                Debug.LogWarning("DreamCoreView: glowImage is not assigned.", this);
            }

            if (canvasGroup == null)
            {
                Debug.LogWarning("DreamCoreView: canvasGroup is not assigned.", this);
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor utility to auto-assign references from children.
        /// </summary>
        [ContextMenu("Auto-Assign References")]
        private void AutoAssignReferences()
        {
            if (dreamCoreObject == null)
            {
                dreamCoreObject = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (glowImage == null)
            {
                glowImage = GetComponentInChildren<Image>();
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("DreamCoreView: References auto-assigned.");
        }
        #endif
    }
}
