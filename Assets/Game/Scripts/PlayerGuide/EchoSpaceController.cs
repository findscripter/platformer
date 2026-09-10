using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Figma Frame 10：关卡完成后 Fade 至黑，再进入独立梦境回响空间。
/// 使用独立深色背景、角色美术、四句对白与可滚动阅读的梦笺。
/// </summary>
public class EchoSpaceController : MonoBehaviour
{
    private static Sprite softHaloSprite;
    public bool IsFinished { get; private set; }

    private static readonly string[] DialogueLines =
    {
        "你看，梦核已经把这场旅程收好了。",
        "这一路上的光，都轻轻收进它里面了。",
        "现在，它想让你自己打开看看。",
        "打开看看吧。"
    };

    private GameContext context;
    private CanvasGroup rootGroup;
    private TMP_Text dialogueText;
    private Image coreImage;
    private Image haloImage;
    private CanvasGroup noteGroup;
    private TMP_Text noteText;
    private Button coreButton;
    private Button noteContinueButton;
    private ScrollRect noteScroll;
    private bool acceptingCoreClick;
    private bool waitingForCoreClick;
    private int coreWaitStartedFrame;
    private int noteWaitStartedFrame;
    private int lastAdvanceFrame = -1;
    private bool waitingForNoteAdvance;
    private bool coreClicked;
    private bool noteClicked;
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
        lastAdvanceFrame = -1;

        EnsureEventSystem();
        BuildUiIfNeeded();
        gameObject.SetActive(true);
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = true;
        }
        if (noteGroup != null)
        {
            noteGroup.gameObject.SetActive(false);
            noteGroup.alpha = 0f;
            noteGroup.interactable = false;
            noteGroup.blocksRaycasts = false;
        }
        if (noteText != null)
            noteText.maxVisibleCharacters = int.MaxValue;
        if (noteContinueButton != null)
            noteContinueButton.interactable = false;
        if (dialogueText != null)
            dialogueText.text = string.Empty;
        if (noteScroll != null)
        {
            noteScroll.StopMovement();
            noteScroll.content.anchoredPosition = Vector2.zero;
        }
        if (coreButton != null)
            coreButton.interactable = false;
        if (coreImage != null)
            coreImage.color = Color.white;
        if (haloImage != null)
        {
            haloImage.transform.localScale = Vector3.one;
            haloImage.color = new Color(0.55f, 0.42f, 0.88f, 0.18f);
        }

        flowRoutine = StartCoroutine(RunFlow());
    }

    public void Hide()
    {
        StopFlow();
        if (rootGroup != null)
            rootGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

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

    private IEnumerator RunFlow()
    {
        var transition = context != null ? context.SceneTransitionManager : null;
        if (rootGroup != null)
            yield return FadeGroup(rootGroup, 0f, 1f, 0.45f);

        if (transition != null)
            yield return transition.FadeFromBlack(0.5f);

        for (int i = 0; i < DialogueLines.Length; i++)
        {
            yield return PlayLine(DialogueLines[i], i == DialogueLines.Length - 1 ? 1.6f : 2.0f);
            if (i == 2)
                SetCoreResponseState();
        }

        waitingForCoreClick = true;
        coreWaitStartedFrame = Time.frameCount;
        while (!coreClicked)
            yield return null;
        waitingForCoreClick = false;
        acceptingCoreClick = false;
        coreButton.interactable = false;

        GuideSfx.PlayWhoosh();
        yield return ExpandHalo();
        yield return ShowNote();

        waitingForNoteAdvance = true;
        noteWaitStartedFrame = Time.frameCount;
        noteClicked = false;
        noteContinueButton.interactable = true;
        while (!noteClicked)
            yield return null;
        waitingForNoteAdvance = false;
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
        float elapsed = 0f;
        while (elapsed < hold)
        {
            if (WasAdvancePressed())
                break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetCoreResponseState()
    {
        acceptingCoreClick = true;
        coreButton.interactable = true;
        if (coreImage != null)
            coreImage.color = Color.white;
        if (haloImage != null)
        {
            var color = haloImage.color;
            color.a = 0.35f;
            haloImage.color = color;
        }
    }

    private IEnumerator ExpandHalo()
    {
        if (haloImage == null)
            yield break;

        Transform halo = haloImage.transform;
        Vector3 from = Vector3.one;
        Vector3 to = Vector3.one * 3.2f;
        Color start = haloImage.color;
        start.a = 0.55f;
        Color end = start;
        end.a = 0.05f;
        halo.localScale = from;
        haloImage.color = start;

        float elapsed = 0f;
        const float duration = 1.1f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            halo.localScale = Vector3.Lerp(from, to, t);
            haloImage.color = Color.Lerp(start, end, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator ShowNote()
    {
        if (noteGroup == null)
            yield break;

        if (noteText != null)
            noteText.text = DreamNoteWriter.WaitingLine();

        noteGroup.gameObject.SetActive(true);
        yield return FadeGroup(noteGroup, 0f, 1f, 0.4f);
        yield return new WaitForSecondsRealtime(1.35f);

        // 梦笺只展示牌名，日志用的 T1 / 效果编号不作为玩家文案。
        string cards = "尚未翻开的牌";
        if (context?.TarotResult != null && context.TarotResult.IsComplete())
        {
            var names = new string[TarotResultData.SlotCount];
            for (int i = 0; i < names.Length; i++)
                names[i] = context.TarotResult.GetCard(i).DisplayName;
            cards = string.Join("、", names);
        }
        string body = DreamNoteWriter.Compose(context != null ? context.PlayerDreamInput : null, cards);

        if (noteText != null)
        {
            yield return RevealText(noteText, body, 0.03f);
        }

        yield return new WaitForSecondsRealtime(0.35f);
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
        // 最后一句尚在播放时，确认键只用于对白；开放梦核后需要一次新的确认。
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
            if (WasAdvancePressed())
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
        GuideUiLayout.Cover(bg, GuideArt.EchoBackground);
        bg.raycastTarget = true;

        var label = CreateText(root, "SpaceLabel", "梦的回响", 28, TextAlignmentOptions.MidlineLeft);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.08f, 0.88f);
        labelRect.anchorMax = new Vector2(0.55f, 0.96f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.color = new Color(1f, 1f, 1f, 0.7f);

        var box = CreatePanel(root, "DialogueBox", new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.88f), Color.white);
        var banner = GuideArt.DialogueBanner;
        if (banner != null)
        {
            var boxImage = box.GetComponent<Image>();
            boxImage.sprite = banner;
            boxImage.color = Color.white;
            boxImage.type = Image.Type.Sliced;
        }

        dialogueText = CreateText(box, "DialogueText", string.Empty, 34, TextAlignmentOptions.Center);
        Stretch(dialogueText.rectTransform, new Vector2(0.13f, 0.12f), new Vector2(0.87f, 0.88f));
        dialogueText.color = new Color(0.12f, 0.12f, 0.16f, 1f);
        dialogueText.outlineWidth = 0f;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;

        var feifeiRect = CreateCharacter(root, "Feifei", GuideArt.Load("cb83933569bc45344b1141bc6af262eb"), new Vector2(-300f, -80f), new Vector2(280f, 280f), false);
        var feifeiFrames = GuideArt.LoadFeifeiFrames();
        if (feifeiFrames != null && feifeiFrames.Length > 10 && feifeiFrames[10] != null)
        {
            var feifeiImg = root.Find("Feifei").GetComponent<Image>();
            if (feifeiImg != null)
                feifeiImg.sprite = feifeiFrames[10];
        }

        // Reuse the meeting pose's hand location, keeping the same core with Feifei.
        var feifeiImage = feifeiRect.GetComponent<Image>();
        Vector2 drawnSize = feifeiRect.rect.size;
        if (feifeiImage.sprite != null)
        {
            Vector2 nativeSize = feifeiImage.sprite.rect.size;
            drawnSize = nativeSize * Mathf.Min(drawnSize.x / nativeSize.x, drawnSize.y / nativeSize.y);
        }
        Vector2 handOffset = Vector2.Scale(new Vector2(0.14f, 0.36f) - new Vector2(0.5f, 0.5f), drawnSize);
        var haloGo = CreateNamedCircle(feifeiRect, "CoreHalo", handOffset, 110f, new Color(0.55f, 0.42f, 0.88f, 0.18f), null);
        haloImage = haloGo.GetComponent<Image>();
        haloImage.sprite = SoftHaloSprite();

        var coreGo = CreateCharacter(feifeiRect, "DreamCore", GuideArt.DreamCore, handOffset, new Vector2(66f, 78f), true);
        coreImage = coreGo.GetComponent<Image>();
        coreButton = coreGo.gameObject.AddComponent<Button>();
        coreButton.transition = Selectable.Transition.None;
        coreButton.onClick.AddListener(AcceptCoreClick);

        CreateCharacter(root, "Player", GuideArt.PlayerIdle, new Vector2(340f, -80f), new Vector2(200f, 240f), false);

        var note = CreatePanel(root, "DreamNote", new Vector2(0.14f, 0.08f), new Vector2(0.86f, 0.6f), new Color(0.96f, 0.93f, 0.86f, 1f));
        noteGroup = note.gameObject.AddComponent<CanvasGroup>();
        noteGroup.alpha = 0f;
        note.gameObject.SetActive(false);
        noteScroll = note.gameObject.AddComponent<ScrollRect>();
        noteScroll.horizontal = false;
        noteScroll.movementType = ScrollRect.MovementType.Clamped;
        noteScroll.scrollSensitivity = 32f;
        var viewport = CreateStretch(note, "Viewport");
        Stretch(viewport, new Vector2(0.07f, 0.22f), new Vector2(0.93f, 0.92f));
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
        var continuePanel = CreatePanel(note, "Continue", new Vector2(0.78f, 0.045f), new Vector2(0.94f, 0.17f), new Color(0.2f, 0.18f, 0.16f, 1f));
        noteContinueButton = continuePanel.gameObject.AddComponent<Button>();
        noteContinueButton.onClick.AddListener(AcceptNoteContinue);
        var continueText = CreateText(continuePanel, "Label", "继续", 28f, TextAlignmentOptions.Center);
        Stretch(continueText.rectTransform, Vector2.zero, Vector2.one);
        var readHint = CreateText(note, "ReadHint", "滚动阅读", 24f, TextAlignmentOptions.MidlineLeft);
        Stretch(readHint.rectTransform, new Vector2(0.07f, 0.045f), new Vector2(0.68f, 0.17f));
        readHint.color = new Color(0.3f, 0.28f, 0.25f, 1f);
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
        if (softHaloSprite != null) return softHaloSprite;
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
        image.raycastTarget = label == "梦核";
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
