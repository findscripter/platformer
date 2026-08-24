using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.PlayerGuide.Controllers
{
    /// <summary>
    /// Handles Frames 1-5 of PlayerGuide: dream space generation through Feifei entrance.
    /// Frame 1: Dream Space Generation (spawn bubbles, fade in background)
    /// Frame 2: Main Bubble Appear (show bubble, hint system)
    /// Frame 3: Dream Echo Play (audio + vibration)
    /// Frame 4: Bubble Dissolve (bubble fade, core appear)
    /// Frame 5: Feifei Enter (walk in, pick up core)
    /// </summary>
    public class Frame1To5Controller : MonoBehaviour
    {
        private readonly PlayerGuideView _view;
        private readonly MonoBehaviour _coroutineHost;

    // Cached references
    private Transform _smallBubblesContainer;
    private GameObject _smallBubblePrefab;
    private CanvasGroup _backgroundCanvasGroup;
    private ParticleSystem _dreamSpaceParticles;
    private PlayerGuideMainBubbleVisual _mainBubbleVisual;
    private Button _mainBubbleButton;
    private AudioSource _dreamEchoAudioSource;
    private GameObject _dreamCoreObject;
    private Image _dreamCoreGlow;
    private GameObject _feifeiCharacter;
    private Animator _feifeiAnimator;

    // Duration config
    private float _frame1Duration;
    private float _frame3Duration;
    private float _frame4DissolveDuration;
    private float _frame4CoreStabilizeDuration;
    private float _frame5WalkDuration;
    private float _frame5PickupDuration;

    // Frame 2 hint config
    private float _frame2HintDelayFirst;
    private float _frame2HintDelayRepeat;
    private float _frame2HintGlowIntensity;

    // Runtime state
    private Coroutine _currentHintCoroutine;

    public Frame1To5Controller(PlayerGuideView view, MonoBehaviour coroutineHost)
    {
        _view = view;
        _coroutineHost = coroutineHost;

        // Cache all references
        _smallBubblesContainer = view.SmallBubblesContainer;
        _smallBubblePrefab = view.SmallBubblePrefab;
        _backgroundCanvasGroup = view.BackgroundCanvasGroup;
        _dreamSpaceParticles = view.DreamSpaceParticles;
        _mainBubbleVisual = view.MainBubbleVisual;
        _mainBubbleButton = view.MainBubbleButton;
        _dreamEchoAudioSource = view.DreamEchoAudioSource;
        _dreamCoreObject = view.DreamCoreObject;
        _dreamCoreGlow = view.DreamCoreGlow;
        _feifeiCharacter = view.FeifeiCharacter;
        _feifeiAnimator = view.FeifeiAnimator;

        // Cache duration config
        _frame1Duration = view.Frame1Duration;
        _frame3Duration = view.Frame3Duration;
        _frame4DissolveDuration = view.Frame4DissolveDuration;
        _frame4CoreStabilizeDuration = view.Frame4CoreStabilizeDuration;
        _frame5WalkDuration = view.Frame5WalkDuration;
        _frame5PickupDuration = view.Frame5PickupDuration;

        // Cache Frame 2 hint config
        _frame2HintDelayFirst = view.Frame2HintDelayFirst;
        _frame2HintDelayRepeat = view.Frame2HintDelayRepeat;
        _frame2HintGlowIntensity = view.Frame2HintGlowIntensity;
    }

    /// <summary>
    /// Frame 1: Dream Space Generation
    /// - Spawn small floating bubbles
    /// - Fade in background
    /// - Start particle system
    /// </summary>
    public IEnumerator Frame1_DreamSpaceGeneration()
    {
        // Initialize background alpha to 0
        if (_backgroundCanvasGroup != null)
            _backgroundCanvasGroup.alpha = 0f;

        // Spawn small bubbles
        SpawnSmallBubbles();

        // Start particle system
        if (_dreamSpaceParticles != null)
            _dreamSpaceParticles.Play();

        // Fade in background over Frame1Duration
        float elapsed = 0f;
        while (elapsed < _frame1Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _frame1Duration);

            if (_backgroundCanvasGroup != null)
                _backgroundCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // Ensure final state
        if (_backgroundCanvasGroup != null)
            _backgroundCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Frame 2: Main Bubble Appear
    /// - Show main bubble with scale-up animation
    /// - Start hint system (glow pulses to guide user interaction)
    /// </summary>
    public IEnumerator Frame2_MainBubbleAppear()
    {
        if (_mainBubbleVisual == null)
            yield break;

        // Ensure bubble is active
        _mainBubbleVisual.gameObject.SetActive(true);

        // Get scale target
        RectTransform scaleTarget = _mainBubbleVisual.ScaleTarget;
        if (scaleTarget == null)
        {
            Debug.LogWarning("Frame2_MainBubbleAppear: ScaleTarget is null, using root transform");
            scaleTarget = _mainBubbleVisual.transform as RectTransform;
        }

        // Animate scale from 0 to 1
        scaleTarget.localScale = Vector3.zero;

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Elastic ease-out for bounce effect
            float scale = ElasticEaseOut(t);
            scaleTarget.localScale = Vector3.one * scale;

            yield return null;
        }

        // Ensure final scale
        scaleTarget.localScale = Vector3.one;

        // Start hint system
        StartHintSystem();
    }

    /// <summary>
    /// Frame 3: Dream Echo Play
    /// - Play audio
    /// - Trigger vibration (if supported)
    /// - Visual feedback on bubble
    /// </summary>
    public IEnumerator Frame3_DreamEchoPlay()
    {
        // Play audio
        if (_dreamEchoAudioSource != null && _dreamEchoAudioSource.clip != null)
            _dreamEchoAudioSource.Play();

        // Trigger haptic feedback
        TriggerHapticFeedback();

        // Visual pulse on main bubble
        if (_mainBubbleVisual != null)
        {
            RectTransform scaleTarget = _mainBubbleVisual.ScaleTarget;
            Vector3 originalScale = scaleTarget.localScale;

            float elapsed = 0f;
            while (elapsed < _frame3Duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _frame3Duration;

                // Pulse: scale up then back down
                float pulse = Mathf.Sin(t * Mathf.PI);
                float scaleFactor = 1f + pulse * 0.1f;
                scaleTarget.localScale = originalScale * scaleFactor;

                yield return null;
            }

            // Restore original scale
            scaleTarget.localScale = originalScale;
        }
        else
        {
            yield return new WaitForSeconds(_frame3Duration);
        }
    }

    /// <summary>
    /// Frame 4: Bubble Dissolve
    /// - Fade out main bubble
    /// - Reveal dream core with glow effect
    /// </summary>
    public IEnumerator Frame4_BubbleDissolve()
    {
        // Stop hint system
        StopHintSystem();

        // Phase 1: Dissolve bubble
        if (_mainBubbleVisual != null)
        {
            CanvasGroup bubbleCanvasGroup = _mainBubbleVisual.GetComponent<CanvasGroup>();
            if (bubbleCanvasGroup == null)
                bubbleCanvasGroup = _mainBubbleVisual.gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < _frame4DissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _frame4DissolveDuration);

                bubbleCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                yield return null;
            }

            bubbleCanvasGroup.alpha = 0f;
            _mainBubbleVisual.gameObject.SetActive(false);
        }

        // Phase 2: Reveal and stabilize dream core
        if (_dreamCoreObject != null)
        {
            _dreamCoreObject.SetActive(true);

            // Initialize core with glow
            if (_dreamCoreGlow != null)
            {
                Color glowColor = _dreamCoreGlow.color;
                glowColor.a = 0f;
                _dreamCoreGlow.color = glowColor;
            }

            // Fade in core glow
            float elapsed = 0f;
            while (elapsed < _frame4CoreStabilizeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _frame4CoreStabilizeDuration);

                if (_dreamCoreGlow != null)
                {
                    Color glowColor = _dreamCoreGlow.color;
                    glowColor.a = Mathf.Lerp(0f, 1f, t);
                    _dreamCoreGlow.color = glowColor;
                }

                yield return null;
            }

            // Ensure final state
            if (_dreamCoreGlow != null)
            {
                Color glowColor = _dreamCoreGlow.color;
                glowColor.a = 1f;
                _dreamCoreGlow.color = glowColor;
            }
        }
    }

    /// <summary>
    /// Frame 5: Feifei Enter
    /// - Feifei walks onto screen
    /// - Picks up dream core
    /// - Core responds with glow pulse
    /// </summary>
    public IEnumerator Frame5_FeifeiEnter()
    {
        if (_feifeiCharacter == null)
            yield break;

        // Ensure Feifei is active
        _feifeiCharacter.SetActive(true);

        // Phase 1: Walk in
        if (_feifeiAnimator != null)
            _feifeiAnimator.SetBool("isWalking", true);

        RectTransform feifeiRect = _feifeiCharacter.GetComponent<RectTransform>();
        if (feifeiRect != null)
        {
            Vector2 startPos = feifeiRect.anchoredPosition;
            Vector2 targetPos = Vector2.zero; // Walk to center

            float elapsed = 0f;
            while (elapsed < _frame5WalkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _frame5WalkDuration);

                feifeiRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                yield return null;
            }

            feifeiRect.anchoredPosition = targetPos;
        }

        // Stop walking animation
        if (_feifeiAnimator != null)
            _feifeiAnimator.SetBool("isWalking", false);

        // Phase 2: Pick up core
        if (_feifeiAnimator != null)
            _feifeiAnimator.SetTrigger("pickup");

        // Wait for pickup animation
        yield return new WaitForSeconds(_frame5PickupDuration);

        // Core responds with glow pulse
        if (_dreamCoreGlow != null)
        {
            Color originalColor = _dreamCoreGlow.color;
            float pulseDuration = 1f;
            float elapsed = 0f;

            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;

                float pulse = Mathf.Sin(t * Mathf.PI);
                Color glowColor = originalColor;
                glowColor.a = Mathf.Lerp(originalColor.a, 1.5f, pulse);
                _dreamCoreGlow.color = glowColor;

                yield return null;
            }

            _dreamCoreGlow.color = originalColor;
        }
    }

    /// <summary>
    /// Stops the hint system (called when Frame 2 ends)
    /// </summary>
    public void StopHintSystem()
    {
        if (_currentHintCoroutine != null)
        {
            _coroutineHost.StopCoroutine(_currentHintCoroutine);
            _currentHintCoroutine = null;
        }

        // Reset main bubble glow
        if (_mainBubbleVisual != null)
        {
            Image bubbleImage = _mainBubbleVisual.GetComponentInChildren<Image>();
            if (bubbleImage != null)
            {
                Color originalColor = bubbleImage.color;
                originalColor.a = 1f;
                bubbleImage.color = originalColor;
            }
        }
    }

    #region Private Helper Methods

    private void SpawnSmallBubbles()
    {
        if (_smallBubblesContainer == null || _smallBubblePrefab == null)
            return;

        int count = _view.SmallBubbleCount;
        Vector2 sizeRange = _view.SmallBubbleSizeRange;

        for (int i = 0; i < count; i++)
        {
            GameObject bubble = Object.Instantiate(_smallBubblePrefab, _smallBubblesContainer);

            // Randomize size
            float size = Random.Range(sizeRange.x, sizeRange.y);
            RectTransform rect = bubble.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(size, size);

            // Randomize position
            if (rect != null)
            {
                float x = Random.Range(-400f, 400f);
                float y = Random.Range(-300f, 300f);
                rect.anchoredPosition = new Vector2(x, y);
            }

            // Add floating animation component
            SmallBubbleFloater floater = bubble.AddComponent<SmallBubbleFloater>();
            floater.Initialize(_view.SmallBubbleFloatSpeed, _view.SmallBubbleFloatAmplitude);
        }
    }

    private void StartHintSystem()
    {
        if (_currentHintCoroutine != null)
            _coroutineHost.StopCoroutine(_currentHintCoroutine);

        _currentHintCoroutine = _coroutineHost.StartCoroutine(HintCoroutine());
    }

    private IEnumerator HintCoroutine()
    {
        // Wait for first hint
        yield return new WaitForSeconds(_frame2HintDelayFirst);

        // Loop hints
        while (true)
        {
            yield return PlayHintAnimation();
            yield return new WaitForSeconds(_frame2HintDelayRepeat);
        }
    }

    private IEnumerator PlayHintAnimation()
    {
        if (_mainBubbleVisual == null)
            yield break;

        Image bubbleImage = _mainBubbleVisual.GetComponentInChildren<Image>();
        if (bubbleImage == null)
            yield break;

        Color originalColor = bubbleImage.color;
        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Pulse alpha
            float pulse = Mathf.Sin(t * Mathf.PI);
            Color glowColor = originalColor;
            glowColor.a = Mathf.Lerp(1f, _frame2HintGlowIntensity, pulse);
            bubbleImage.color = glowColor;

            yield return null;
        }

        bubbleImage.color = originalColor;
    }

    private void TriggerHapticFeedback()
    {
        #if UNITY_ANDROID || UNITY_IOS
        if (UnityEngine.iOS.Device.generation != UnityEngine.iOS.DeviceGeneration.Unknown)
        {
            // iOS haptic
            Handheld.Vibrate();
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            // Android vibration (requires VIBRATE permission)
            Handheld.Vibrate();
        }
        #endif
    }

    private float ElasticEaseOut(float t)
    {
        if (t == 0f || t == 1f)
            return t;

        float p = 0.3f;
        float s = p / 4f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
    }

    #endregion
}

/// <summary>
/// Component for animating small background bubbles with floating motion
/// </summary>
public class SmallBubbleFloater : MonoBehaviour
{
    private float _speed;
    private float _amplitude;
    private float _phase;
    private Vector2 _startPos;
    private RectTransform _rect;

    public void Initialize(float speed, float amplitude)
    {
        _speed = speed;
        _amplitude = amplitude;
        _phase = Random.Range(0f, Mathf.PI * 2f);
        _rect = GetComponent<RectTransform>();
        _startPos = _rect.anchoredPosition;
    }

    private void Update()
    {
        if (_rect == null)
            return;

        float offset = Mathf.Sin(Time.time * _speed + _phase) * _amplitude;
        _rect.anchoredPosition = _startPos + Vector2.up * offset;
    }
    }
}
