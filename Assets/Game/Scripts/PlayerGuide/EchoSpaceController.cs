using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Figma Frame 10 细化：关卡完成后 Fade 至黑，再进入独立梦境回响空间。
/// 状态 A 关卡完成 → B–E 四句对白 → F 光晕扩散 → G 纸面呼吸等待 AI → H 连续正文阅读。
/// </summary>
public class EchoSpaceController : MonoBehaviour
{
    private static Sprite softHaloSprite;
    private static Sprite roundedRectSprite;
    private static Sprite inputFrameBorder;

    public bool IsFinished { get; private set; }

    private static readonly string[] DialogueLines =
    {
        "「你看，梦核已经把这场旅程收好了。」",
        "「每当有人愿意重新走进自己的梦，它都会留下一份回音。」",
        "「这一次，它也为你准备了一份礼物。」",
        "「打开看看吧。」"
    };

    private GameContext context;
    private CanvasGroup rootGroup;
    private CanvasGroup stageGroup;
    private CanvasGroup dialogueGroup;
    private CanvasGroup coreGroup;
    private CanvasGroup paperGroup;
    private TMP_Text dialogueText;
    private TMP_Text noteText;
    private TMP_Text clickHint;
    private TMP_Text waitingCaption;
    private TMP_Text readHint;
    private Image coreImage;
    private Image haloImage;
    private Image expandHalo;
    private Image waitingCore;
    private Button coreButton;
    private Button noteContinueButton;
    private ScrollRect noteScroll;
    private RectTransform continuePanel;
    private bool acceptingCoreClick;
    private bool waitingForCoreClick;
    private bool waitingForNoteAdvance;
    private bool breathingWait;
    private int coreWaitStartedFrame;
    private int noteWaitStartedFrame;
    private int lastAdvanceFrame = -1;
    private bool coreClicked;
    private bool noteClicked;
    private float breathTime;
    private Coroutine flowRoutine;

    public static EchoSpaceController Ensure()
    {
        var existing = Object.FindAnyObjectByType<EchoSpaceController>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        var go = new GameObject("EchoSpaceRoot");
        Object.DontDestroyOnLoad(go);
        return go.AddComponent<EchoSpaceController>();
    }

    public void Begin(GameContext gameContext)
    {
        StopFlow();
        context = gameContext;
        IsFinished = false;
        waitingForNoteAdvance = false;
        coreClicked = false;
        noteClicked = false;
        acceptingCoreClick = false;
        breathingWait = false;
        lastAdvanceFrame = -1;

        EnsureEventSystem();
        BuildUiIfNeeded();
        if (GameLoop.Instance != null && context != null)
            GameLoop.Instance.StartCoroutine(DreamLetterClient.ComposeLetter(context));

        gameObject.SetActive(true);
        ResetVisuals();
        flowRoutine = StartCoroutine(RunFlow());
    }

    public void Hide()
    {
        StopFlow();
        if (rootGroup != null)
            rootGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    /// <summary>跳过通关流程，直接显示「打开看看吧」那句，方便对框。</summary>
    public void PreviewLastDialogue()
    {
        StopFlow();
        EnsureEventSystem();
        if (rootGroup != null)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
            rootGroup = null;
        }

        BuildUiIfNeeded();
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
            canvas.sortingOrder = 2000;

        gameObject.SetActive(true);
        ResetVisuals();
        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }
        if (stageGroup != null)
            stageGroup.alpha = 1f;
        if (coreGroup != null)
            coreGroup.alpha = 1f;
        SetDialogueVisible(true);
        if (dialogueText != null)
        {
            dialogueText.text = DialogueLines[3];
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }
        SetCoreVisual(0.4f, 1.08f);
        SetClickHintVisible(true);
        acceptingCoreClick = false;
        if (coreButton != null)
            coreButton.interactable = false;
    }

#if UNITY_EDITOR
    public const string PreviewFlag = "Dreamremains.PreviewEchoDialogue";
    public const string PreviewShotPath = "Temp/echo_dialogue_preview.png";
    public const string PreviewDonePath = "Temp/echo_dialogue_preview.done";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoPreviewIfRequested()
    {
        if (!UnityEditor.EditorPrefs.GetBool(PreviewFlag))
            return;
        UnityEditor.EditorPrefs.DeleteKey(PreviewFlag);
        var echo = Ensure();
        echo.StartCoroutine(echo.DelayedPreviewAndCapture());
    }

    private IEnumerator DelayedPreviewAndCapture()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        PreviewLastDialogue();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSecondsRealtime(0.25f);
        yield return new WaitForEndOfFrame();
        string dir = Path.GetDirectoryName(PreviewShotPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        if (File.Exists(PreviewShotPath))
            File.Delete(PreviewShotPath);
        if (File.Exists(PreviewDonePath))
            File.Delete(PreviewDonePath);
        ScreenCapture.CaptureScreenshot(PreviewShotPath);
        yield return new WaitForSecondsRealtime(0.6f);
        File.WriteAllText(PreviewDonePath, PreviewShotPath);
        Debug.Log("[EchoSpace] 预览截图 " + Path.GetFullPath(PreviewShotPath));
    }
#endif

    private void OnDisable()
    {
        StopFlow();
    }

    private void StopFlow()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        acceptingCoreClick = false;
        waitingForCoreClick = false;
        waitingForNoteAdvance = false;
        breathingWait = false;
        if (coreButton != null)
            coreButton.interactable = false;
        if (noteContinueButton != null)
            noteContinueButton.interactable = false;
        if (rootGroup != null)
        {
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
    }

    private void ResetVisuals()
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
        }
        if (stageGroup != null)
            stageGroup.alpha = 1f;
        if (coreGroup != null)
            coreGroup.alpha = 1f;
        SetDialogueVisible(false);
        SetClickHintVisible(false);
        SetCoreVisual(0.18f, 1f);
        if (expandHalo != null)
        {
            expandHalo.gameObject.SetActive(false);
            expandHalo.rectTransform.localScale = Vector3.one;
        }
        if (paperGroup != null)
        {
            paperGroup.gameObject.SetActive(false);
            paperGroup.alpha = 0f;
            paperGroup.interactable = false;
            paperGroup.blocksRaycasts = false;
        }
        if (noteText != null)
        {
            noteText.text = string.Empty;
            noteText.maxVisibleCharacters = int.MaxValue;
            noteText.gameObject.SetActive(false);
        }
        if (waitingCore != null)
        {
            waitingCore.gameObject.SetActive(true);
            waitingCore.transform.localScale = Vector3.one;
            waitingCore.color = new Color(1f, 1f, 1f, 0.82f);
        }
        if (waitingCaption != null)
        {
            waitingCaption.gameObject.SetActive(true);
            waitingCaption.text = DreamNoteWriter.WaitingLine();
        }
        if (readHint != null)
            readHint.gameObject.SetActive(false);
        if (continuePanel != null)
            continuePanel.gameObject.SetActive(false);
        if (noteContinueButton != null)
            noteContinueButton.interactable = false;
        if (noteScroll != null)
        {
            noteScroll.enabled = false;
            noteScroll.StopMovement();
            if (noteScroll.content != null)
                noteScroll.content.anchoredPosition = Vector2.zero;
        }
        if (coreButton != null)
            coreButton.interactable = false;
        if (dialogueText != null)
            dialogueText.text = string.Empty;
    }

    private IEnumerator RunFlow()
    {
        var transition = context != null ? context.SceneTransitionManager : null;
        if (rootGroup != null)
            yield return FadeGroup(rootGroup, 0f, 1f, 0.45f);
        if (transition != null)
            yield return transition.FadeFromBlack(0.5f);

        // A｜关卡完成：独立回响空间，尚无对白。
        yield return HoldOrSkip(1.35f);

        // B–D｜四句里的前三句。D 进入梦核回应态。
        SetDialogueVisible(true);
        for (int i = 0; i < 3; i++)
        {
            if (i == 2)
                SetCoreVisual(0.4f, 1.08f);
            yield return PlayLine(DialogueLines[i], 2.15f);
        }

        // E｜打开看看吧，等待点击梦核。
        yield return PlayLine(DialogueLines[3], 0.35f);
        SetClickHintVisible(true);
        acceptingCoreClick = true;
        coreButton.interactable = true;
        waitingForCoreClick = true;
        coreWaitStartedFrame = Time.frameCount;
        while (!coreClicked)
            yield return null;
        waitingForCoreClick = false;
        acceptingCoreClick = false;
        coreButton.interactable = false;
        SetClickHintVisible(false);

        // F｜确认声 + 蓝紫光晕外扩 + 场景淡出。不飞、不转、不生成 CG。
        GuideSfx.PlayPickup();
        yield return ExpandHalo();

        // G｜纸面呼吸，等待 AI 正文；不显示电子 Loading。
        yield return ShowWaitingPaper();

        // H｜连续正文写入梦笺，滚动阅读。
        yield return ShowLetter();

        waitingForNoteAdvance = true;
        noteWaitStartedFrame = Time.frameCount;
        noteClicked = false;
        if (continuePanel != null)
            continuePanel.gameObject.SetActive(true);
        if (noteContinueButton != null)
            noteContinueButton.interactable = true;
        while (!noteClicked)
            yield return null;
        waitingForNoteAdvance = false;
        if (noteContinueButton != null)
            noteContinueButton.interactable = false;

        if (rootGroup != null)
            yield return FadeGroup(rootGroup, 1f, 0f, 0.35f);

        IsFinished = true;
        flowRoutine = null;
    }

    private IEnumerator PlayLine(string line, float hold)
    {
        if (dialogueText == null)
            yield break;

        yield return RevealText(dialogueText, line, 0.045f);
        yield return HoldOrSkip(hold);
    }

    private IEnumerator HoldOrSkip(float hold)
    {
        float elapsed = 0f;
        while (elapsed < hold)
        {
            if (WasAdvancePressed())
                break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetCoreVisual(float glowAlpha, float scale)
    {
        if (coreImage != null)
            coreImage.color = Color.white;
        if (coreImage != null)
            coreImage.transform.localScale = Vector3.one * scale;
        if (haloImage != null)
        {
            haloImage.transform.localScale = Vector3.one * (0.92f + scale * 0.18f);
            haloImage.color = new Color(0.55f, 0.42f, 0.88f, glowAlpha);
        }
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialogueGroup == null)
            return;
        dialogueGroup.alpha = visible ? 1f : 0f;
        dialogueGroup.blocksRaycasts = false;
        dialogueGroup.interactable = false;
    }

    private void SetClickHintVisible(bool visible)
    {
        if (clickHint == null)
            return;
        clickHint.gameObject.SetActive(visible);
        var color = clickHint.color;
        color.a = visible ? 0.92f : 0f;
        clickHint.color = color;
    }

    private IEnumerator ExpandHalo()
    {
        if (expandHalo == null)
            yield break;

        expandHalo.gameObject.SetActive(true);
        if (coreImage != null)
            expandHalo.rectTransform.position = coreImage.rectTransform.position;

        Vector3 from = Vector3.one;
        Vector3 to = Vector3.one * 18f;
        Color start = new Color(0.55f, 0.42f, 0.88f, 0.62f);
        Color end = new Color(0.55f, 0.42f, 0.88f, 0.02f);
        expandHalo.rectTransform.localScale = from;
        expandHalo.color = start;
        GuideSfx.PlayWhoosh();

        float elapsed = 0f;
        const float duration = 1.05f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = 1f - (1f - t) * (1f - t);
            expandHalo.rectTransform.localScale = Vector3.LerpUnclamped(from, to, eased);
            expandHalo.color = Color.Lerp(start, end, t);
            if (stageGroup != null && t > 0.45f)
                stageGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.45f) / 0.55f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (stageGroup != null)
            stageGroup.alpha = 0f;
        if (coreGroup != null)
            yield return FadeGroup(coreGroup, 1f, 0f, 0.28f);
        expandHalo.gameObject.SetActive(false);
    }

    private IEnumerator ShowWaitingPaper()
    {
        if (paperGroup == null)
            yield break;

        paperGroup.gameObject.SetActive(true);
        if (waitingCore != null)
            waitingCore.gameObject.SetActive(true);
        if (waitingCaption != null)
        {
            waitingCaption.gameObject.SetActive(true);
            waitingCaption.text = DreamNoteWriter.WaitingLine();
        }
        if (noteText != null)
            noteText.gameObject.SetActive(false);
        if (readHint != null)
            readHint.gameObject.SetActive(false);
        if (continuePanel != null)
            continuePanel.gameObject.SetActive(false);

        breathTime = 0f;
        breathingWait = true;
        yield return FadeGroup(paperGroup, 0f, 1f, 0.4f);

        float minHold = 1.05f;
        float elapsed = 0f;
        while (elapsed < minHold)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (context != null)
            yield return DreamLetterClient.ComposeLetter(context);

        breathingWait = false;
        if (waitingCore != null)
            waitingCore.transform.localScale = Vector3.one;
    }

    private IEnumerator ShowLetter()
    {
        string body = context?.DreamRun != null && !string.IsNullOrWhiteSpace(context.DreamRun.LetterBody)
            ? context.DreamRun.LetterBody
            : DreamNoteWriter.Compose(context != null ? context.PlayerDreamInput : null, context != null ? context.TarotResult : null);

        if (waitingCore != null)
            waitingCore.gameObject.SetActive(false);
        if (waitingCaption != null)
            waitingCaption.gameObject.SetActive(false);
        if (noteText != null)
        {
            noteText.gameObject.SetActive(true);
            if (noteScroll != null)
            {
                noteScroll.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(noteScroll.content);
                noteScroll.verticalNormalizedPosition = 1f;
            }
            yield return RevealText(noteText, body, 0.028f);
        }
        if (readHint != null)
            readHint.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(0.25f);
    }

    private bool WasAdvancePressed()
    {
        if (lastAdvanceFrame == Time.frameCount)
            return false;
        bool pressed = WasKeyboardAdvancePressed()
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (pressed)
            lastAdvanceFrame = Time.frameCount;
        return pressed;
    }

    private bool WasKeyboardAdvancePressed()
    {
        if (context != null && context.InputManager != null)
        {
            if (context.InputManager.ConfirmPressed || context.InputManager.InteractPressed)
                return true;
        }

        return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame
            || Keyboard.current.spaceKey.wasPressedThisFrame);
    }

    private void Update()
    {
        if (breathingWait && waitingCore != null)
        {
            breathTime += Time.unscaledDeltaTime;
            float wave = 0.5f + 0.5f * Mathf.Sin(breathTime * 1.65f);
            waitingCore.transform.localScale = Vector3.one * (1f + 0.07f * wave);
            waitingCore.color = new Color(1f, 1f, 1f, 0.55f + 0.3f * wave);
        }

        if (acceptingCoreClick && haloImage != null)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.1f);
            var color = haloImage.color;
            color.a = 0.28f + 0.22f * wave;
            haloImage.color = color;
        }

        if (waitingForCoreClick && Time.frameCount > coreWaitStartedFrame && ConsumeKeyboardAdvance())
            AcceptCoreClick();
        else if (waitingForNoteAdvance && Time.frameCount > noteWaitStartedFrame && ConsumeKeyboardAdvance())
            AcceptNoteContinue();
    }

    private bool ConsumeKeyboardAdvance()
    {
        if (lastAdvanceFrame == Time.frameCount || !WasKeyboardAdvancePressed())
            return false;
        lastAdvanceFrame = Time.frameCount;
        return true;
    }

    private void AcceptCoreClick()
    {
        if (!acceptingCoreClick || coreClicked)
            return;
        coreClicked = true;
        acceptingCoreClick = false;
        coreButton.interactable = false;
        lastAdvanceFrame = Time.frameCount;
    }

    private void AcceptNoteContinue()
    {
        if (!waitingForNoteAdvance || noteClicked)
            return;
        noteClicked = true;
        waitingForNoteAdvance = false;
        noteContinueButton.interactable = false;
        lastAdvanceFrame = Time.frameCount;
    }

    private IEnumerator RevealText(TMP_Text text, string content, float secondsPerCharacter)
    {
        text.text = content;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();
        int count = text.textInfo.characterCount;
        if (text == noteText && noteScroll != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(noteScroll.content);
            noteScroll.verticalNormalizedPosition = 1f;
        }
        float elapsed = 0f;
        while (text.maxVisibleCharacters < count)
        {
            bool skip = text == noteText ? ConsumeKeyboardAdvance() : WasAdvancePressed();
            if (skip)
                break;
            elapsed += Time.unscaledDeltaTime;
            text.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(elapsed / secondsPerCharacter));
            yield return null;
        }
        text.maxVisibleCharacters = int.MaxValue;
    }

    private void BuildUiIfNeeded()
    {
        if (rootGroup != null)
            return;

        var canvasGo = new GameObject("EchoSpaceCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var root = CreateStretch(canvasGo.transform, "Root");
        rootGroup = root.gameObject.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;

        var bg = CreateStretch(root, "Background").gameObject.AddComponent<Image>();
        Sprite dreamBg = GuideArt.DreamBackground != null ? GuideArt.DreamBackground : GuideArt.EchoBackground;
        GuideUiLayout.Cover(bg, dreamBg);
        if (dreamBg == null)
            bg.color = Color.black;
        bg.raycastTarget = true;

        var stage = CreateStretch(root, "Stage");
        stageGroup = stage.gameObject.AddComponent<CanvasGroup>();

        var box = CreatePanel(stage, "DialogueBox", new Vector2(0.16f, 0.54f), new Vector2(0.84f, 0.86f), Color.white);
        dialogueGroup = box.gameObject.AddComponent<CanvasGroup>();
        var boxImage = box.GetComponent<Image>();
        Sprite inputFrame = GuideArt.InputPanelFrame;
        if (inputFrame != null)
        {
            boxImage.sprite = inputFrame;
            boxImage.type = Image.Type.Simple;
            boxImage.preserveAspect = false;
            boxImage.color = Color.white;
        }
        boxImage.raycastTarget = false;
        dialogueText = CreateText(box, "DialogueText", string.Empty, 34, TextAlignmentOptions.Center);
        Stretch(dialogueText.rectTransform, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.86f));
        GuideUiLayout.ReadableText(dialogueText, 34f, GuideUiLayout.DialogueInk);
        dialogueText.alignment = TextAlignmentOptions.Center;
        dialogueText.outlineWidth = 0f;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        SetDialogueVisible(false);

        CreateCharacter(stage, "Feifei", ResolveFeifei(), new Vector2(-430f, -260f), new Vector2(280f, 280f), false);
        CreateCharacter(stage, "Player", GuideArt.PlayerIdle, new Vector2(470f, -260f), new Vector2(210f, 250f), false);

        var coreRoot = CreateStretch(root, "CoreGroup");
        coreGroup = coreRoot.gameObject.AddComponent<CanvasGroup>();
        var coreAnchor = new GameObject("CoreAnchor", typeof(RectTransform)).GetComponent<RectTransform>();
        coreAnchor.SetParent(coreRoot, false);
        coreAnchor.anchorMin = coreAnchor.anchorMax = new Vector2(0.38f, 0.22f);
        coreAnchor.pivot = new Vector2(0.5f, 0.5f);
        coreAnchor.sizeDelta = new Vector2(160f, 160f);
        coreAnchor.anchoredPosition = Vector2.zero;

        var haloGo = CreateNamedCircle(coreAnchor, "CoreHalo", Vector2.zero, 150f, new Color(0.55f, 0.42f, 0.88f, 0.18f), null);
        haloImage = haloGo.GetComponent<Image>();
        haloImage.sprite = SoftHaloSprite();
        haloImage.raycastTarget = false;

        var coreGo = CreateCharacter(coreAnchor, "DreamCore", GuideArt.DreamCore, Vector2.zero, new Vector2(86f, 102f), false);
        coreImage = coreGo.GetComponent<Image>();
        var hot = new GameObject("Hit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hot.transform.SetParent(coreAnchor, false);
        var hotRect = hot.GetComponent<RectTransform>();
        hotRect.anchorMin = hotRect.anchorMax = new Vector2(0.5f, 0.5f);
        hotRect.sizeDelta = new Vector2(140f, 140f);
        hotRect.anchoredPosition = Vector2.zero;
        var hotImage = hot.GetComponent<Image>();
        hotImage.color = new Color(1f, 1f, 1f, 0f);
        hotImage.raycastTarget = true;
        coreButton = hot.AddComponent<Button>();
        coreButton.transition = Selectable.Transition.None;
        coreButton.onClick.AddListener(AcceptCoreClick);

        clickHint = CreateText(coreAnchor, "ClickHint", "点击梦核", 24, TextAlignmentOptions.Center);
        clickHint.rectTransform.anchorMin = clickHint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        clickHint.rectTransform.pivot = new Vector2(0.5f, 1f);
        clickHint.rectTransform.sizeDelta = new Vector2(180f, 36f);
        clickHint.rectTransform.anchoredPosition = new Vector2(0f, -8f);
        clickHint.color = new Color(0.86f, 0.82f, 0.98f, 0f);
        clickHint.gameObject.SetActive(false);

        var expandGo = CreateNamedCircle(root, "ExpandHalo", Vector2.zero, 220f, new Color(0.55f, 0.42f, 0.88f, 0.5f), null);
        expandHalo = expandGo.GetComponent<Image>();
        expandHalo.sprite = SoftHaloSprite();
        expandHalo.raycastTarget = false;
        expandGo.gameObject.SetActive(false);

        var paper = CreatePanel(root, "DreamNote", new Vector2(0.18f, 0.1f), new Vector2(0.82f, 0.9f), new Color(0.97f, 0.95f, 0.91f, 1f));
        paperGroup = paper.gameObject.AddComponent<CanvasGroup>();
        paperGroup.alpha = 0f;
        paper.gameObject.SetActive(false);
        var paperImage = paper.GetComponent<Image>();
        paperImage.sprite = RoundedRectSprite();
        paperImage.type = Image.Type.Sliced;
        paperImage.pixelsPerUnitMultiplier = 10f;

        noteScroll = paper.gameObject.AddComponent<ScrollRect>();
        noteScroll.horizontal = false;
        noteScroll.movementType = ScrollRect.MovementType.Clamped;
        noteScroll.scrollSensitivity = 32f;
        var viewport = CreateStretch(paper, "Viewport");
        Stretch(viewport, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.9f));
        viewport.gameObject.AddComponent<RectMask2D>();
        noteText = CreateText(viewport, "NoteText", string.Empty, 30, TextAlignmentOptions.TopLeft);
        Stretch(noteText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        noteText.rectTransform.pivot = new Vector2(0.5f, 1f);
        noteText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        noteScroll.viewport = viewport;
        noteScroll.content = noteText.rectTransform;
        noteText.color = new Color(0.2f, 0.18f, 0.16f, 1f);
        noteText.textWrappingMode = TextWrappingModes.Normal;
        noteText.richText = false;
        noteText.gameObject.SetActive(false);

        waitingCore = CreateCharacter(paper, "WaitingCore", GuideArt.DreamCore, Vector2.zero, new Vector2(168f, 198f), false)
            .GetComponent<Image>();
        waitingCore.rectTransform.anchorMin = waitingCore.rectTransform.anchorMax = new Vector2(0.5f, 0.58f);
        waitingCore.color = new Color(1f, 1f, 1f, 0.82f);

        waitingCaption = CreateText(paper, "WaitingCaption", DreamNoteWriter.WaitingLine(), 28, TextAlignmentOptions.Center);
        Stretch(waitingCaption.rectTransform, new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.38f));
        waitingCaption.color = new Color(0.32f, 0.3f, 0.28f, 1f);
        waitingCaption.textWrappingMode = TextWrappingModes.Normal;

        continuePanel = CreatePanel(paper, "Continue", new Vector2(0.78f, 0.035f), new Vector2(0.94f, 0.13f), new Color(0.2f, 0.18f, 0.16f, 1f));
        var continueImage = continuePanel.GetComponent<Image>();
        continueImage.sprite = RoundedRectSprite();
        continueImage.type = Image.Type.Sliced;
        continueImage.pixelsPerUnitMultiplier = 16f;
        noteContinueButton = continuePanel.gameObject.AddComponent<Button>();
        noteContinueButton.onClick.AddListener(AcceptNoteContinue);
        var continueText = CreateText(continuePanel, "Label", "继续", 26f, TextAlignmentOptions.Center);
        Stretch(continueText.rectTransform, Vector2.zero, Vector2.one);
        continuePanel.gameObject.SetActive(false);

        readHint = CreateText(paper, "ReadHint", "滚动阅读", 24f, TextAlignmentOptions.MidlineLeft);
        Stretch(readHint.rectTransform, new Vector2(0.08f, 0.035f), new Vector2(0.7f, 0.13f));
        readHint.color = new Color(0.3f, 0.28f, 0.25f, 1f);
        readHint.gameObject.SetActive(false);
    }

    private static Sprite ResolveFeifei()
    {
        var frames = GuideArt.LoadFeifeiFrames();
        if (frames != null && frames.Length > 10 && frames[10] != null)
            return frames[10];
        return GuideArt.Load("cb83933569bc45344b1141bc6af262eb");
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var go = new GameObject("EchoSpaceEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        Object.DontDestroyOnLoad(go);
    }

    private static Sprite SoftHaloSprite()
    {
        if (softHaloSprite != null)
            return softHaloSprite;
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "DreamCoreSoftHalo";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f).magnitude * 2f;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - radius), 2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        softHaloSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        return softHaloSprite;
    }

    private static void ApplyInputFrameBorder(Image image)
    {
        if (image == null)
            return;

        image.raycastTarget = false;
        Sprite src = GuideArt.InputPanelFrame;
        if (src == null)
        {
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        if (inputFrameBorder == null)
        {
            float pad = Mathf.Clamp(src.rect.width * 0.018f, 10f, 28f);
            inputFrameBorder = Sprite.Create(
                src.texture,
                src.rect,
                new Vector2(0.5f, 0.5f),
                src.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                new Vector4(pad, pad, pad, pad));
            inputFrameBorder.name = "InputPanelFrameBorder";
        }

        image.sprite = inputFrameBorder;
        image.type = Image.Type.Sliced;
        image.fillCenter = false;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    private static Sprite RoundedRectSprite()
    {
        if (roundedRectSprite != null)
            return roundedRectSprite;
        const int size = 64;
        const int radius = 16;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "EchoRoundedRect";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Min(x + 0.5f, size - x - 0.5f);
                float dy = Mathf.Min(y + 0.5f, size - y - 0.5f);
                float alpha = 1f;
                if (dx < radius && dy < radius)
                {
                    float dist = new Vector2(radius - dx, radius - dy).magnitude;
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        roundedRectSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            64f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        return roundedRectSprite;
    }

    private static RectTransform CreateStretch(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        Stretch(rect, Vector2.zero, Vector2.one);
        return rect;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        Stretch(rect, min, max);
        var image = go.GetComponent<Image>();
        image.color = color;
        return rect;
    }

    private static RectTransform CreateCharacter(Transform parent, string name, Sprite sprite, Vector2 anchored, Vector2 size, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchored;
        var image = go.GetComponent<Image>();
        image.sprite = sprite != null ? sprite : GuideArt.Circle;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = raycast;
        return rect;
    }

    private static RectTransform CreateNamedCircle(Transform parent, string name, Vector2 anchored, float size, Color color, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = anchored;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        var circle = GuideArt.Circle;
        if (circle != null)
        {
            image.sprite = circle;
            image.preserveAspect = true;
        }

        if (!string.IsNullOrEmpty(label))
        {
            var text = CreateText(rect, "Label", label, 26, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            text.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            text.raycastTarget = false;
        }

        return rect;
    }

    private static RectTransform CreateDecorCircle(Transform parent, string name, Vector2 anchor, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        var circle = GuideArt.Circle;
        if (circle != null)
        {
            image.sprite = circle;
            image.preserveAspect = true;
        }
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = false;
        tmp.font = GuideUiLayout.LoadFont();
        return tmp;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        group.alpha = from;
        group.interactable = false;
        group.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        group.alpha = to;
        group.blocksRaycasts = to > 0.01f;
        group.interactable = to > 0.01f;
    }
}
