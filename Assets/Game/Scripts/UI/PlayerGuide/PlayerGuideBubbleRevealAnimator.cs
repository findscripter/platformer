using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerGuideBubbleRevealAnimator
{
    public static void PrepareHidden(
        GameObject bubble,
        float startScale,
        RectTransform scaleTarget = null,
        bool disableAnimator = false,
        bool keepRootScaleAtOne = false)
    {
        RectTransform rootRect = bubble != null ? bubble.GetComponent<RectTransform>() : null;
        RectTransform targetRect = scaleTarget != null ? scaleTarget : rootRect;
        CanvasGroup canvasGroup = EnsureCanvasGroup(bubble);

        if (disableAnimator)
            SetAnimatorEnabled(bubble, false);

        if (keepRootScaleAtOne && rootRect != null)
            rootRect.localScale = Vector3.one;

        if (targetRect != null)
            targetRect.localScale = Vector3.one * startScale;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        bubble.SetActive(true);
    }

    public static IEnumerator Reveal(
        GameObject bubble,
        float duration,
        float startScale,
        RectTransform scaleTarget = null,
        bool keepRootScaleAtOne = false)
    {
        if (bubble == null)
            yield break;

        RectTransform rootRect = bubble.GetComponent<RectTransform>();
        RectTransform targetRect = scaleTarget != null ? scaleTarget : rootRect;
        CanvasGroup canvasGroup = EnsureCanvasGroup(bubble);
        Vector3 fromScale = Vector3.one * startScale;
        Vector3 toScale = Vector3.one;

        if (duration <= 0f)
        {
            ApplyFinalState(rootRect, targetRect, canvasGroup, toScale, keepRootScaleAtOne);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            if (keepRootScaleAtOne && rootRect != null)
                rootRect.localScale = Vector3.one;

            if (targetRect != null)
                targetRect.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);

            canvasGroup.alpha = t;
            yield return null;
        }

        ApplyFinalState(rootRect, targetRect, canvasGroup, toScale, keepRootScaleAtOne);
    }

    public static void ApplyScale(RectTransform target, float scale)
    {
        if (target != null)
            target.localScale = Vector3.one * scale;
    }

    public static void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    public static IEnumerator RevealScale(
        RectTransform target,
        float fromScale,
        float toScale,
        float duration,
        RectTransform rootRect = null,
        bool keepRootScaleAtOne = false)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            if (keepRootScaleAtOne && rootRect != null)
                rootRect.localScale = Vector3.one;

            ApplyScale(target, toScale);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float scale = Mathf.LerpUnclamped(fromScale, toScale, t);

            if (keepRootScaleAtOne && rootRect != null)
                rootRect.localScale = Vector3.one;

            ApplyScale(target, scale);
            yield return null;
        }

        if (keepRootScaleAtOne && rootRect != null)
            rootRect.localScale = Vector3.one;

        ApplyScale(target, toScale);
    }

    public static IEnumerator RevealGraphicColorAlpha(
        Graphic graphic,
        float fromAlpha,
        float toAlpha,
        float duration)
    {
        if (graphic == null)
            yield break;

        Color color = graphic.color;

        if (duration <= 0f)
        {
            color.a = toAlpha;
            graphic.color = color;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            color.a = Mathf.LerpUnclamped(fromAlpha, toAlpha, t);
            graphic.color = color;
            yield return null;
        }

        color.a = toAlpha;
        graphic.color = color;
    }

    public static void SetAnimatorEnabled(GameObject bubble, bool enabled)
    {
        if (bubble == null)
            return;

        Animator animator = bubble.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = enabled;
    }

    public static void EnableButtonAnimator(GameObject bubble)
    {
        if (bubble == null)
            return;

        RectTransform rootRect = bubble.GetComponent<RectTransform>();
        if (rootRect != null)
            rootRect.localScale = Vector3.one;

        Animator animator = bubble.GetComponent<Animator>();
        if (animator == null)
            return;

        animator.enabled = true;
        animator.Play("Normal", 0, 0f);
        animator.Update(0f);
    }

    public static void ShowInstant(GameObject target)
    {
        if (target == null)
            return;

        RemoveCanvasGroup(target);
        target.SetActive(true);
    }

    public static void PrepareFadeHidden(GameObject target, bool blockRaycasts = false)
    {
        if (target == null)
            return;

        CanvasGroup canvasGroup = EnsureCanvasGroup(target);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = blockRaycasts;
        canvasGroup.interactable = blockRaycasts;
        target.SetActive(true);
    }

    public static void ConfigureFadeForInteraction(GameObject target)
    {
        if (target == null)
            return;

        RemoveCanvasGroup(target);

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = true;
    }

    public static void ConfigureDialogueBoxInteraction(GameObject dialogBox)
    {
        if (dialogBox == null)
            return;

        RemoveCanvasGroup(dialogBox);

        foreach (Graphic graphic in dialogBox.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = graphic.gameObject == dialogBox;

        Button button = dialogBox.GetComponent<Button>();
        if (button == null)
            return;

        button.interactable = true;
        button.transition = Selectable.Transition.None;
        SetAnimatorEnabled(dialogBox, false);
    }

    public static void DisableRaycasts(GameObject target)
    {
        if (target == null)
            return;

        foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public static void RemoveCanvasGroup(GameObject target)
    {
        if (target == null)
            return;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            Object.Destroy(canvasGroup);
    }

    public static void SetFadeAlpha(GameObject target, float alpha, bool blockRaycasts)
    {
        if (target == null)
            return;

        CanvasGroup canvasGroup = EnsureCanvasGroup(target);
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = blockRaycasts;
        canvasGroup.interactable = blockRaycasts;
    }

    public static IEnumerator RevealFade(
        GameObject target,
        float duration,
        bool blockRaycastsAtEnd = true)
    {
        if (target == null)
            yield break;

        CanvasGroup canvasGroup = EnsureCanvasGroup(target);

        if (duration <= 0f)
        {
            ApplyFadeFinalState(canvasGroup, blockRaycastsAtEnd);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            canvasGroup.alpha = t;
            yield return null;
        }

        ApplyFadeFinalState(canvasGroup, blockRaycastsAtEnd);

        if (!blockRaycastsAtEnd)
            Object.Destroy(canvasGroup);
    }

    private static void ApplyFadeFinalState(CanvasGroup canvasGroup, bool blockRaycasts)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = blockRaycasts;
        canvasGroup.interactable = !blockRaycasts;
    }

    private static void ApplyFinalState(
        RectTransform rootRect,
        RectTransform targetRect,
        CanvasGroup canvasGroup,
        Vector3 toScale,
        bool keepRootScaleAtOne)
    {
        if (keepRootScaleAtOne && rootRect != null)
            rootRect.localScale = Vector3.one;

        if (targetRect != null)
            targetRect.localScale = toScale;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject bubble)
    {
        CanvasGroup canvasGroup = bubble.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = bubble.AddComponent<CanvasGroup>();

        return canvasGroup;
    }
}
