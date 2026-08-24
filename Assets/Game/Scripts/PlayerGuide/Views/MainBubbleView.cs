using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PlayerGuide.Views
{
    /// <summary>
    /// Main bubble view controller - handles visual state, animations, and user interaction
    /// for the primary player guide bubble.
    /// </summary>
    [RequireComponent(typeof(PlayerGuideMainBubbleVisual))]
    [RequireComponent(typeof(Button))]
    public class MainBubbleView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private PlayerGuideMainBubbleVisual visual;
        [SerializeField] private Button button;

        [Header("Animation Settings")]
        [SerializeField] private float scaleAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField] private float glowPulseDuration = 1.5f;
        [SerializeField] private float glowMinAlpha = 0.3f;
        [SerializeField] private float glowMaxAlpha = 1f;

        [SerializeField] private float dissolveAnimationDuration = 0.4f;

        [Header("Events")]
        [SerializeField] private UnityEvent onClick = new UnityEvent();

        private Coroutine activeScaleCoroutine;
        private Coroutine activeGlowCoroutine;
        private Coroutine activeDissolveCoroutine;
        private Material materialInstance;

        public UnityEvent OnClick => onClick;

        private void Awake()
        {
            if (visual == null)
                visual = GetComponent<PlayerGuideMainBubbleVisual>();

            if (button == null)
                button = GetComponent<Button>();

            button.onClick.AddListener(HandleClick);

            // Create material instance for dissolve/glow effects
            Image targetImage = visual.ScaleTarget.GetComponent<Image>();
            if (targetImage != null && targetImage.material != null)
            {
                materialInstance = new Material(targetImage.material);
                targetImage.material = materialInstance;
            }
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);

            if (materialInstance != null)
                Destroy(materialInstance);
        }

        private void HandleClick()
        {
            onClick?.Invoke();
        }

        /// <summary>
        /// Shows the bubble with optional scale animation.
        /// </summary>
        /// <param name="animated">If true, scales from 0 to 1</param>
        public void Show(bool animated = true)
        {
            gameObject.SetActive(true);

            if (animated)
            {
                StopScaleAnimation();
                activeScaleCoroutine = StartCoroutine(ScaleAnimation(0f, 1f));
            }
            else
            {
                visual.ScaleTarget.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Hides the bubble with optional scale animation.
        /// </summary>
        /// <param name="animated">If true, scales from 1 to 0</param>
        public void Hide(bool animated = true)
        {
            if (animated)
            {
                StopScaleAnimation();
                activeScaleCoroutine = StartCoroutine(ScaleAnimationThenHide());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Starts a pulsing glow effect to hint at interactivity.
        /// </summary>
        public void StartHintGlowPulse()
        {
            StopHintGlowPulse();
            activeGlowCoroutine = StartCoroutine(HintGlowPulse());
        }

        /// <summary>
        /// Stops the pulsing glow effect.
        /// </summary>
        public void StopHintGlowPulse()
        {
            if (activeGlowCoroutine != null)
            {
                StopCoroutine(activeGlowCoroutine);
                activeGlowCoroutine = null;
            }

            // Reset alpha to max
            if (materialInstance != null && materialInstance.HasProperty("_Color"))
            {
                Color color = materialInstance.GetColor("_Color");
                color.a = glowMaxAlpha;
                materialInstance.SetColor("_Color", color);
            }
        }

        /// <summary>
        /// Plays a dissolve animation (typically for transitions or destruction).
        /// </summary>
        /// <param name="reversed">If true, animates from dissolved to solid</param>
        public void PlayDissolveAnimation(bool reversed = false)
        {
            StopDissolveAnimation();
            activeDissolveCoroutine = StartCoroutine(DissolveAnimation(reversed));
        }

        /// <summary>
        /// Stops any active dissolve animation.
        /// </summary>
        public void StopDissolveAnimation()
        {
            if (activeDissolveCoroutine != null)
            {
                StopCoroutine(activeDissolveCoroutine);
                activeDissolveCoroutine = null;
            }
        }

        /// <summary>
        /// Sets whether the bubble can be interacted with.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private void StopScaleAnimation()
        {
            if (activeScaleCoroutine != null)
            {
                StopCoroutine(activeScaleCoroutine);
                activeScaleCoroutine = null;
            }
        }

        /// <summary>
        /// Animates the bubble scale from startScale to endScale.
        /// </summary>
        private IEnumerator ScaleAnimation(float startScale, float endScale)
        {
            float elapsed = 0f;
            RectTransform target = visual.ScaleTarget;

            while (elapsed < scaleAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaleAnimationDuration);
                float curveValue = scaleCurve.Evaluate(t);
                float scale = Mathf.Lerp(startScale, endScale, curveValue);

                target.localScale = Vector3.one * scale;

                yield return null;
            }

            target.localScale = Vector3.one * endScale;
            activeScaleCoroutine = null;
        }

        /// <summary>
        /// Scales down and then hides the GameObject.
        /// </summary>
        private IEnumerator ScaleAnimationThenHide()
        {
            yield return ScaleAnimation(1f, 0f);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Continuously pulses the bubble alpha to hint at interactivity.
        /// </summary>
        private IEnumerator HintGlowPulse()
        {
            if (materialInstance == null || !materialInstance.HasProperty("_Color"))
                yield break;

            while (true)
            {
                float elapsed = 0f;

                // Fade out
                while (elapsed < glowPulseDuration / 2f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (glowPulseDuration / 2f);
                    float alpha = Mathf.Lerp(glowMaxAlpha, glowMinAlpha, t);

                    Color color = materialInstance.GetColor("_Color");
                    color.a = alpha;
                    materialInstance.SetColor("_Color", color);

                    yield return null;
                }

                elapsed = 0f;

                // Fade in
                while (elapsed < glowPulseDuration / 2f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (glowPulseDuration / 2f);
                    float alpha = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, t);

                    Color color = materialInstance.GetColor("_Color");
                    color.a = alpha;
                    materialInstance.SetColor("_Color", color);

                    yield return null;
                }
            }
        }

        /// <summary>
        /// Animates a dissolve effect using a shader property.
        /// </summary>
        /// <param name="reversed">If true, animates from 1 (dissolved) to 0 (solid)</param>
        private IEnumerator DissolveAnimation(bool reversed)
        {
            if (materialInstance == null || !materialInstance.HasProperty("_DissolveAmount"))
                yield break;

            float startValue = reversed ? 1f : 0f;
            float endValue = reversed ? 0f : 1f;
            float elapsed = 0f;

            while (elapsed < dissolveAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dissolveAnimationDuration);
                float dissolveValue = Mathf.Lerp(startValue, endValue, t);

                materialInstance.SetFloat("_DissolveAmount", dissolveValue);

                yield return null;
            }

            materialInstance.SetFloat("_DissolveAmount", endValue);
            activeDissolveCoroutine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (visual == null)
                visual = GetComponent<PlayerGuideMainBubbleVisual>();

            if (button == null)
                button = GetComponent<Button>();
        }
#endif
    }
}
