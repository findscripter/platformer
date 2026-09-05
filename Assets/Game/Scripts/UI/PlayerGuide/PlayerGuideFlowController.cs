using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerGuideFlowController : MonoBehaviour
{
    public const string PlayerNamePrefsKey = "PlayerGuide.PlayerName";

    private enum GuideStep
    {
        Bubble,
        FeifeiDialogue,
        Input,
        Gacha
    }

    [Header("Nodes")]
    [SerializeField] private GameObject bubblesNode;
    [SerializeField] private GameObject feifeiMengheNode;
    [SerializeField] private GameObject inputRow;
    [SerializeField] private GameObject gachaNode;

    [Header("Bubble")]
    [SerializeField] private Button mainBubbleButton;
    [SerializeField] private GameObject mainBubbleRoot;
    [SerializeField] private PlayerGuideMainBubbleVisual mainBubbleVisual;
    [SerializeField] private TMP_Text mainBubbleText;
    [SerializeField] private float bubbleRevealDuration = 0.45f;
    [SerializeField] private float bubbleStartScale = 0.55f;
    [SerializeField] private float decorativeBubbleRevealInterval = 0.08f;
    [SerializeField] private float mainBubbleExpandScale = 2f;
    [SerializeField] private float mainBubbleExpandDuration = 0.45f;
    [SerializeField] private float mainBubbleTextRevealDuration = 0.35f;

    [Header("Dialogue")]
    [SerializeField] private SequentialTextDisplay dialogueDisplay;
    [SerializeField] private SequentialTextConfig feifeiTextConfig;
    [SerializeField] private SequentialTextConfig inputTextConfig;
    [SerializeField] private SequentialTextConfig gachaTextConfig;

    [Header("Input")]
    [SerializeField] private GuidePanelInputFieldController inputController;

    [Header("Feifei")]
    [SerializeField] private float feifeiRevealDelayAfterMenghe = 2f;
    [SerializeField] private float feifeiLeadRevealDuration = 1f;
    [SerializeField] private float feifeiRevealDuration = 0.45f;
    [SerializeField] private float feifeiRevealInterval = 0.08f;

    [Header("Gacha")]
    [SerializeField] private float gachaTransitionDelay = 0.5f;
    [SerializeField] private float gachaCardRevealDuration = 0.45f;
    [SerializeField] private float gachaCardRevealInterval = 0.08f;

    private static readonly string[] FeifeiRevealOrder = { "menghe", "feifei", "player", "dialog_box" };
    private static readonly HashSet<string> FeifeiInstantRevealNames = new() { "menghe" };
    private static readonly HashSet<string> FeifeiLeadRevealNames = new() { "feifei" };

    private GuideStep currentStep;
    private readonly List<Button> gachaCardButtons = new();
    private Coroutine bubbleRevealRoutine;
    private Coroutine mainBubblePostClickRoutine;
    private Coroutine feifeiRevealRoutine;
    private Coroutine gachaRevealRoutine;
    private Coroutine gachaTransitionRoutine;
    private bool isTransitioningToGameplay;

    private void Awake()
    {
        EnsureMainBubbleVisualReference();
    }

    private void Start()
    {
        BindListeners();
        EnterStep(GuideStep.Bubble);
    }

    private void OnDestroy()
    {
        UnbindListeners();
    }

    private void BindListeners()
    {
        if (mainBubbleButton != null)
            mainBubbleButton.onClick.AddListener(OnMainBubbleClicked);

        if (dialogueDisplay != null)
            dialogueDisplay.SequenceCompleted += OnDialogueSequenceCompleted;

        if (inputController != null)
            inputController.Submitted += OnInputSubmitted;
    }

    private void UnbindListeners()
    {
        if (mainBubbleButton != null)
            mainBubbleButton.onClick.RemoveListener(OnMainBubbleClicked);

        if (dialogueDisplay != null)
            dialogueDisplay.SequenceCompleted -= OnDialogueSequenceCompleted;

        if (inputController != null)
            inputController.Submitted -= OnInputSubmitted;

        UnbindGachaCardButtons();

        if (bubbleRevealRoutine != null)
            StopCoroutine(bubbleRevealRoutine);

        if (mainBubblePostClickRoutine != null)
            StopCoroutine(mainBubblePostClickRoutine);

        if (feifeiRevealRoutine != null)
            StopCoroutine(feifeiRevealRoutine);

        if (gachaRevealRoutine != null)
            StopCoroutine(gachaRevealRoutine);

        if (gachaTransitionRoutine != null)
            StopCoroutine(gachaTransitionRoutine);
    }

    private void EnterStep(GuideStep step)
    {
        currentStep = step;

        switch (step)
        {
            case GuideStep.Bubble:
                EnterBubbleStep();
                break;
            case GuideStep.FeifeiDialogue:
                EnterFeifeiDialogueStep();
                break;
            case GuideStep.Input:
                EnterInputStep();
                break;
            case GuideStep.Gacha:
                EnterGachaStep();
                break;
        }
    }

    private void EnterBubbleStep()
    {
        SetNodeActive(bubblesNode, true);
        SetNodeActive(feifeiMengheNode, false);
        SetNodeActive(inputRow, false);
        SetNodeActive(gachaNode, false);

        ConfigureMainBubbleForReveal();

        if (bubbleRevealRoutine != null)
            StopCoroutine(bubbleRevealRoutine);

        bubbleRevealRoutine = StartCoroutine(RevealBubblesRoutine());
    }

    private IEnumerator RevealBubblesRoutine()
    {
        if (bubblesNode == null)
            yield break;

        var revealOrder = new List<GameObject>();
        RectTransform mainScaleTarget = GetMainBubbleScaleTarget();

        if (mainBubbleRoot != null)
        {
            PlayerGuideBubbleRevealAnimator.PrepareHidden(
                mainBubbleRoot,
                bubbleStartScale,
                mainScaleTarget,
                disableAnimator: true,
                keepRootScaleAtOne: true);
            revealOrder.Add(mainBubbleRoot);
        }

        foreach (Transform child in bubblesNode.transform)
        {
            if (child.gameObject == mainBubbleRoot)
                continue;

            PlayerGuideBubbleRevealAnimator.PrepareHidden(child.gameObject, bubbleStartScale);
            revealOrder.Add(child.gameObject);
        }

        for (int i = 0; i < revealOrder.Count; i++)
        {
            if (i > 0 && decorativeBubbleRevealInterval > 0f)
                yield return new WaitForSeconds(decorativeBubbleRevealInterval);

            GameObject bubble = revealOrder[i];
            bool isMainBubble = bubble == mainBubbleRoot;
            StartCoroutine(RevealBubbleRoutine(bubble, isMainBubble, mainScaleTarget));
        }

        float totalRevealTime = bubbleRevealDuration +
            Mathf.Max(0, revealOrder.Count - 1) * decorativeBubbleRevealInterval;
        if (totalRevealTime > 0f)
            yield return new WaitForSeconds(totalRevealTime);

        bubbleRevealRoutine = null;
    }

    private IEnumerator RevealBubbleRoutine(
        GameObject bubble,
        bool isMainBubble,
        RectTransform mainScaleTarget)
    {
        yield return PlayerGuideBubbleRevealAnimator.Reveal(
            bubble,
            bubbleRevealDuration,
            bubbleStartScale,
            isMainBubble ? mainScaleTarget : null,
            keepRootScaleAtOne: isMainBubble);

        if (!isMainBubble)
            yield break;

        PlayerGuideBubbleRevealAnimator.ApplyScale(mainScaleTarget, 1f);
        PlayerGuideBubbleRevealAnimator.EnableButtonAnimator(mainBubbleRoot);

        if (mainBubbleButton != null)
            mainBubbleButton.interactable = true;
    }

    private void ConfigureMainBubbleForReveal()
    {
        EnsureMainBubbleVisualReference();

        if (mainBubbleButton != null)
            mainBubbleButton.interactable = false;

        PlayerGuideBubbleRevealAnimator.SetAnimatorEnabled(mainBubbleRoot, false);
        PlayerGuideBubbleRevealAnimator.ApplyScale(mainBubbleRoot != null ? mainBubbleRoot.transform as RectTransform : null, 1f);
        PlayerGuideBubbleRevealAnimator.ApplyScale(GetMainBubbleScaleTarget(), bubbleStartScale);
        PrepareMainBubbleTextHidden();
    }

    private void PrepareMainBubbleTextHidden()
    {
        TMP_Text text = GetMainBubbleText();
        if (text != null)
            PlayerGuideBubbleRevealAnimator.SetGraphicAlpha(text, 0f);
    }

    private TMP_Text GetMainBubbleText()
    {
        if (mainBubbleText == null && mainBubbleRoot != null)
            mainBubbleText = mainBubbleRoot.GetComponentInChildren<TMP_Text>(true);

        return mainBubbleText;
    }

    private void HideDecorativeBubbles()
    {
        if (bubblesNode == null || mainBubbleRoot == null)
            return;

        foreach (Transform child in bubblesNode.transform)
        {
            if (child.gameObject == mainBubbleRoot)
                continue;

            child.gameObject.SetActive(false);
        }
    }

    private void EnsureMainBubbleVisualReference()
    {
        if (mainBubbleRoot == null)
            return;

        mainBubbleVisual = PlayerGuideMainBubbleVisual.EnsureStructure(mainBubbleRoot);
    }

    private RectTransform GetMainBubbleScaleTarget()
    {
        EnsureMainBubbleVisualReference();
        return mainBubbleVisual != null ? mainBubbleVisual.ScaleTarget : null;
    }

    private void EnterFeifeiDialogueStep()
    {
        StopRevealCoroutines();

        SetNodeActive(bubblesNode, false);
        SetNodeActive(inputRow, false);
        SetNodeActive(gachaNode, false);

        if (dialogueDisplay != null)
        {
            dialogueDisplay.SetAdvanceEnabled(true);
            dialogueDisplay.SetConfig(feifeiTextConfig, reset: true);
        }

        feifeiRevealRoutine = StartCoroutine(RevealFeifeiRoutine());
    }

    private IEnumerator RevealFeifeiRoutine()
    {
        if (feifeiMengheNode == null)
            yield break;

        SetNodeActive(feifeiMengheNode, true);
        DisableFeifeiBlockingRaycasts();

        List<GameObject> revealTargets = GetOrderedFeifeiRevealTargets();
        foreach (GameObject target in revealTargets)
        {
            if (FeifeiInstantRevealNames.Contains(target.name))
                PlayerGuideBubbleRevealAnimator.ShowInstant(target);
            else
                PlayerGuideBubbleRevealAnimator.PrepareFadeHidden(target);
        }

        var leadTargets = new List<GameObject>();
        var tailTargets = new List<GameObject>();
        foreach (GameObject target in revealTargets)
        {
            if (FeifeiInstantRevealNames.Contains(target.name))
                continue;

            if (FeifeiLeadRevealNames.Contains(target.name))
                leadTargets.Add(target);
            else
                tailTargets.Add(target);
        }

        if (feifeiRevealDelayAfterMenghe > 0f)
            yield return new WaitForSeconds(feifeiRevealDelayAfterMenghe);

        foreach (GameObject target in leadTargets)
        {
            yield return PlayerGuideBubbleRevealAnimator.RevealFade(
                target,
                GetFeifeiRevealDuration(target.name),
                blockRaycastsAtEnd: false);
        }

        bool dialogueInteractionEnabled = false;
        for (int i = 0; i < tailTargets.Count; i++)
        {
            if (i > 0 && feifeiRevealInterval > 0f)
                yield return new WaitForSeconds(feifeiRevealInterval);

            GameObject target = tailTargets[i];
            bool isDialogBox = target.name == "dialog_box";

            if (isDialogBox)
            {
                yield return PlayerGuideBubbleRevealAnimator.RevealFade(
                    target,
                    GetFeifeiRevealDuration(target.name),
                    blockRaycastsAtEnd: false);

                EnableFeifeiDialogueInteraction();
                dialogueInteractionEnabled = true;
            }
            else
            {
                StartCoroutine(PlayerGuideBubbleRevealAnimator.RevealFade(
                    target,
                    GetFeifeiRevealDuration(target.name),
                    blockRaycastsAtEnd: false));
            }
        }

        if (tailTargets.Count > 0)
        {
            float latestTailEndTime = 0f;
            for (int i = 0; i < tailTargets.Count; i++)
            {
                float startTime = i * feifeiRevealInterval;
                latestTailEndTime = Mathf.Max(
                    latestTailEndTime,
                    startTime + GetFeifeiRevealDuration(tailTargets[i].name));
            }

            if (latestTailEndTime > 0f)
                yield return new WaitForSeconds(latestTailEndTime);
        }

        if (!dialogueInteractionEnabled)
            EnableFeifeiDialogueInteraction();

        feifeiRevealRoutine = null;
    }

    private float GetFeifeiRevealDuration(string targetName)
    {
        return FeifeiLeadRevealNames.Contains(targetName) ? feifeiLeadRevealDuration : feifeiRevealDuration;
    }

    private void EnableFeifeiDialogueInteraction()
    {
        EnsureDialogueInteraction();

        if (dialogueDisplay != null)
        {
            dialogueDisplay.SetAdvanceEnabled(true);
            dialogueDisplay.EnsureClickBinding();
        }
    }

    private void DisableFeifeiBlockingRaycasts()
    {
        if (feifeiMengheNode == null)
            return;

        Transform dialogBoxTransform = feifeiMengheNode.transform.Find("dialog_box");
        foreach (Transform child in feifeiMengheNode.transform)
        {
            if (dialogBoxTransform != null && child == dialogBoxTransform)
                continue;

            PlayerGuideBubbleRevealAnimator.DisableRaycasts(child.gameObject);
        }
    }

    private void EnsureDialogueInteraction()
    {
        if (feifeiMengheNode == null)
            return;

        DisableFeifeiBlockingRaycasts();

        Transform dialogBoxTransform = feifeiMengheNode.transform.Find("dialog_box");
        if (dialogBoxTransform == null)
            return;

        PlayerGuideBubbleRevealAnimator.ConfigureDialogueBoxInteraction(dialogBoxTransform.gameObject);
    }

    private List<GameObject> GetOrderedFeifeiRevealTargets()
    {
        var targets = new List<GameObject>();
        if (feifeiMengheNode == null)
            return targets;

        foreach (string childName in FeifeiRevealOrder)
        {
            Transform child = feifeiMengheNode.transform.Find(childName);
            if (child != null)
                targets.Add(child.gameObject);
        }

        return targets;
    }

    private void EnterInputStep()
    {
        SetNodeActive(bubblesNode, false);
        SetNodeActive(feifeiMengheNode, true);
        SetNodeActive(inputRow, true);
        SetNodeActive(gachaNode, false);

        inputController?.ClearInput();

        EnsureDialogueInteraction();

        if (dialogueDisplay != null)
        {
            dialogueDisplay.SetAdvanceEnabled(true);
            dialogueDisplay.EnsureClickBinding();
            dialogueDisplay.SetConfig(inputTextConfig, reset: true);
        }
    }

    private void EnterGachaStep()
    {
        SetNodeActive(bubblesNode, false);
        SetNodeActive(feifeiMengheNode, true);
        SetNodeActive(inputRow, false);

        EnsureDialogueInteraction();

        if (dialogueDisplay != null)
        {
            dialogueDisplay.SetAdvanceEnabled(true);
            dialogueDisplay.EnsureClickBinding();
            dialogueDisplay.SetConfig(gachaTextConfig, reset: true);
        }

        if (gachaRevealRoutine != null)
            StopCoroutine(gachaRevealRoutine);

        gachaRevealRoutine = StartCoroutine(RevealGachaCardsRoutine());
    }

    private IEnumerator RevealGachaCardsRoutine()
    {
        if (gachaNode == null)
            yield break;

        SetNodeActive(gachaNode, true);
        PlayerGuideBubbleRevealAnimator.RemoveCanvasGroup(gachaNode);

        var cardTargets = new List<GameObject>();
        foreach (Transform child in gachaNode.transform)
            cardTargets.Add(child.gameObject);

        foreach (GameObject card in cardTargets)
            PlayerGuideBubbleRevealAnimator.PrepareFadeHidden(card);

        for (int i = 0; i < cardTargets.Count; i++)
        {
            if (i > 0 && gachaCardRevealInterval > 0f)
                yield return new WaitForSeconds(gachaCardRevealInterval);

            StartCoroutine(PlayerGuideBubbleRevealAnimator.RevealFade(
                cardTargets[i],
                gachaCardRevealDuration,
                blockRaycastsAtEnd: false));
        }

        float latestEndTime = 0f;
        for (int i = 0; i < cardTargets.Count; i++)
        {
            float startTime = i * gachaCardRevealInterval;
            latestEndTime = Mathf.Max(latestEndTime, startTime + gachaCardRevealDuration);
        }

        if (latestEndTime > 0f)
            yield return new WaitForSeconds(latestEndTime);

        BindGachaCardButtons();
        gachaRevealRoutine = null;
    }

    private void StopRevealCoroutines()
    {
        if (bubbleRevealRoutine != null)
        {
            StopCoroutine(bubbleRevealRoutine);
            bubbleRevealRoutine = null;
        }

        if (mainBubblePostClickRoutine != null)
        {
            StopCoroutine(mainBubblePostClickRoutine);
            mainBubblePostClickRoutine = null;
        }

        if (feifeiRevealRoutine != null)
        {
            StopCoroutine(feifeiRevealRoutine);
            feifeiRevealRoutine = null;
        }

        if (gachaRevealRoutine != null)
        {
            StopCoroutine(gachaRevealRoutine);
            gachaRevealRoutine = null;
        }
    }

    private void OnMainBubbleClicked()
    {
        if (currentStep != GuideStep.Bubble)
            return;

        if (mainBubbleButton != null)
            mainBubbleButton.interactable = false;

        PlayerGuideBubbleRevealAnimator.SetAnimatorEnabled(mainBubbleRoot, false);

        if (mainBubblePostClickRoutine != null)
            StopCoroutine(mainBubblePostClickRoutine);

        mainBubblePostClickRoutine = StartCoroutine(MainBubblePostClickRoutine());
    }

    private IEnumerator MainBubblePostClickRoutine()
    {
        HideDecorativeBubbles();

        RectTransform scaleTarget = GetMainBubbleScaleTarget();
        RectTransform rootRect = mainBubbleRoot != null ? mainBubbleRoot.transform as RectTransform : null;
        float currentScale = scaleTarget != null ? scaleTarget.localScale.x : 1f;

        yield return PlayerGuideBubbleRevealAnimator.RevealScale(
            scaleTarget,
            currentScale,
            mainBubbleExpandScale,
            mainBubbleExpandDuration,
            rootRect,
            keepRootScaleAtOne: true);

        TMP_Text text = GetMainBubbleText();
        if (text != null)
        {
            yield return PlayerGuideBubbleRevealAnimator.RevealGraphicColorAlpha(
                text,
                text.color.a,
                1f,
                mainBubbleTextRevealDuration);
        }

        while (!IsMousePressedThisFrame())
            yield return null;

        mainBubblePostClickRoutine = null;
        EnterStep(GuideStep.FeifeiDialogue);
    }

    private static bool IsMousePressedThisFrame()
    {
        if (Mouse.current != null)
            return Mouse.current.leftButton.wasPressedThisFrame;

        return Input.GetMouseButtonDown(0);
    }

    private void OnDialogueSequenceCompleted()
    {
        if (currentStep == GuideStep.FeifeiDialogue)
            EnterStep(GuideStep.Input);
    }

    private void OnInputSubmitted(string value)
    {
        if (currentStep != GuideStep.Input)
            return;

        if (string.IsNullOrWhiteSpace(value))
            return;

        PlayerPrefs.SetString(PlayerNamePrefsKey, value.Trim());
        PlayerPrefs.Save();
        EnterStep(GuideStep.Gacha);
    }

    private void BindGachaCardButtons()
    {
        UnbindGachaCardButtons();

        if (gachaNode == null)
            return;

        Button[] buttons = gachaNode.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            PlayerGuideBubbleRevealAnimator.ConfigureFadeForInteraction(button.gameObject);
            PlayerGuideBubbleRevealAnimator.EnableButtonAnimator(button.gameObject);
            button.interactable = true;
            button.onClick.AddListener(() => OnGachaCardClicked(button));
            gachaCardButtons.Add(button);
        }
    }

    private void UnbindGachaCardButtons()
    {
        foreach (Button button in gachaCardButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        gachaCardButtons.Clear();
    }

    private void OnGachaCardClicked(Button clickedButton)
    {
        if (currentStep != GuideStep.Gacha || isTransitioningToGameplay)
            return;

        isTransitioningToGameplay = true;

        foreach (Button button in gachaCardButtons)
        {
            if (button != null)
                button.interactable = false;
        }

        Animator animator = clickedButton != null ? clickedButton.GetComponent<Animator>() : null;
        if (animator != null)
            animator.SetTrigger("Pressed");

        if (gachaTransitionRoutine != null)
            StopCoroutine(gachaTransitionRoutine);

        gachaTransitionRoutine = StartCoroutine(TransitionToGameplayRoutine());
    }

    private IEnumerator TransitionToGameplayRoutine()
    {
        if (gachaTransitionDelay > 0f)
            yield return new WaitForSeconds(gachaTransitionDelay);

        if (GameLoop.Instance == null)
        {
            Debug.LogError("PlayerGuideFlowController: GameLoop is unavailable.");
            isTransitioningToGameplay = false;
            yield break;
        }

        GameLoop.Instance.ContinueToGameplay();
        gachaTransitionRoutine = null;
    }

    private static void SetNodeActive(GameObject node, bool active)
    {
        if (node != null)
            node.SetActive(active);
    }
}
