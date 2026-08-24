using System.Collections;
using UnityEngine;

namespace Game.PlayerGuide
{
    /// <summary>
    /// Manages background visual effects for the player guide UI,
    /// including fade transitions and particle effects.
    /// </summary>
    public class BackgroundView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private CanvasGroup backgroundCanvasGroup;
        [SerializeField] private ParticleSystem dreamSpaceParticles;

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine currentFadeCoroutine;

        /// <summary>
        /// Fades the background in over the specified duration.
        /// </summary>
        /// <param name="duration">Duration of the fade. Uses default if not specified.</param>
        public void FadeIn(float? duration = null)
        {
            float actualDuration = duration ?? fadeDuration;
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }
            currentFadeCoroutine = StartCoroutine(FadeInCoroutine(actualDuration));
        }

        /// <summary>
        /// Fades the background out over the specified duration.
        /// </summary>
        /// <param name="duration">Duration of the fade. Uses default if not specified.</param>
        public void FadeOut(float? duration = null)
        {
            float actualDuration = duration ?? fadeDuration;
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }
            currentFadeCoroutine = StartCoroutine(FadeOutCoroutine(actualDuration));
        }

        /// <summary>
        /// Starts playing the dream space particle effect.
        /// </summary>
        public void PlayParticles()
        {
            if (dreamSpaceParticles != null && !dreamSpaceParticles.isPlaying)
            {
                dreamSpaceParticles.Play();
            }
        }

        /// <summary>
        /// Stops playing the dream space particle effect.
        /// </summary>
        public void StopParticles()
        {
            if (dreamSpaceParticles != null && dreamSpaceParticles.isPlaying)
            {
                dreamSpaceParticles.Stop();
            }
        }

        /// <summary>
        /// Coroutine that smoothly fades the background alpha from current to 1.
        /// </summary>
        private IEnumerator FadeInCoroutine(float duration)
        {
            if (backgroundCanvasGroup == null)
            {
                Debug.LogWarning("BackgroundView: CanvasGroup is not assigned.");
                yield break;
            }

            float startAlpha = backgroundCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                backgroundCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            backgroundCanvasGroup.alpha = 1f;
            currentFadeCoroutine = null;
        }

        /// <summary>
        /// Coroutine that smoothly fades the background alpha from current to 0.
        /// </summary>
        private IEnumerator FadeOutCoroutine(float duration)
        {
            if (backgroundCanvasGroup == null)
            {
                Debug.LogWarning("BackgroundView: CanvasGroup is not assigned.");
                yield break;
            }

            float startAlpha = backgroundCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                backgroundCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            backgroundCanvasGroup.alpha = 0f;
            currentFadeCoroutine = null;
        }

        private void OnDisable()
        {
            // Clean up coroutine if component is disabled
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
            }
        }
    }
}
