using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private Canvas transitionCanvas;
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float holdDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    public bool IsFading { get; private set; }

    private void Awake()
    {
        EnsureFadeSetup();
        HideImmediate();
    }

    public void ShowBlackImmediate()
    {
        EnsureFadeSetup();

        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 1f;
        fadePanel.blocksRaycasts = true;
        fadePanel.interactable = false;
    }

    public void HideImmediate()
    {
        if (fadePanel == null)
        {
            return;
        }

        fadePanel.alpha = 0f;
        fadePanel.blocksRaycasts = false;
        fadePanel.interactable = false;
        fadePanel.gameObject.SetActive(false);
    }

    public IEnumerator PlayGameplayEnterFade()
    {
        ShowBlackImmediate();

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        yield return FadeOutRoutine();
    }

    /// <summary>
    /// 渐入到全黑（alpha 0→1）并保持黑屏，供离场转场使用；
    /// 结束后画面维持全黑，等待后续 ShowBlackImmediate/场景加载接管。
    /// </summary>
    public IEnumerator FadeToBlack(float duration)
    {
        EnsureFadeSetup();

        if (fadePanel == null)
        {
            yield break;
        }

        IsFading = true;
        fadePanel.gameObject.SetActive(true);
        fadePanel.blocksRaycasts = true;
        fadePanel.interactable = false;

        if (duration <= 0f)
        {
            fadePanel.alpha = 1f;
            IsFading = false;
            yield break;
        }

        float startAlpha = fadePanel.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadePanel.alpha = 1f;
        IsFading = false;
    }

    /// <summary>从全黑淡出到画面（alpha 1→0）。</summary>
    public IEnumerator FadeFromBlack(float duration)
    {
        EnsureFadeSetup();

        if (fadePanel == null)
            yield break;

        IsFading = true;
        fadePanel.gameObject.SetActive(true);
        fadePanel.blocksRaycasts = true;
        fadePanel.alpha = 1f;

        if (duration <= 0f)
        {
            HideImmediate();
            IsFading = false;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        HideImmediate();
        IsFading = false;
    }

    private IEnumerator FadeOutRoutine()
    {
        if (fadePanel == null)
        {
            yield break;
        }

        IsFading = true;

        if (fadeOutDuration <= 0f)
        {
            HideImmediate();
            IsFading = false;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        HideImmediate();
        IsFading = false;
    }

    private void EnsureFadeSetup()
    {
        if (transitionCanvas == null)
        {
            transitionCanvas = FindTransitionCanvas();
        }

        if (transitionCanvas == null)
        {
            Debug.LogError("SceneTransitionManager: Canvas_Transition was not found.");
            return;
        }

        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.overrideSorting = true;
        transitionCanvas.sortingOrder = 999;
        transitionCanvas.transform.localScale = Vector3.one;

        if (fadePanel == null)
        {
            Transform fadeTransform = transitionCanvas.transform.Find("FadePanel");
            if (fadeTransform != null)
            {
                fadePanel = fadeTransform.GetComponent<CanvasGroup>();
            }
        }

        if (fadePanel == null)
        {
            var fadeObject = new GameObject("FadePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            fadeObject.transform.SetParent(transitionCanvas.transform, false);
            fadePanel = fadeObject.GetComponent<CanvasGroup>();
        }

        ConfigureFadePanel(fadePanel);
    }

    private static void ConfigureFadePanel(CanvasGroup panel)
    {
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        var image = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        panel.alpha = 0f;
        panel.blocksRaycasts = false;
        panel.interactable = false;
    }

    private Canvas FindTransitionCanvas()
    {
        foreach (var canvas in transform.root.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.name == "Canvas_Transition")
            {
                return canvas;
            }
        }

        return null;
    }
}
