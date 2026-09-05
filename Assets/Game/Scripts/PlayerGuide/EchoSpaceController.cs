using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Figma Frame 10：关卡完成后 Fade 至黑，再进入独立梦境回响空间。
/// 低保真几何：近黑蓝紫底、腓腓/梦核/玩家三圆、四句对白、点梦核扩光晕、梦笺阅读。
/// </summary>
public class EchoSpaceController : MonoBehaviour
{
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
    private bool waitingForNoteAdvance;
    private bool coreClicked;
    private bool noteClicked;
    private Coroutine flowRoutine;

    public static EchoSpaceController Ensure()
    {
        var existing = Object.FindFirstObjectByType<EchoSpaceController>();
        if (existing != null)
            return existing;

        var go = new GameObject("EchoSpaceRoot");
        Object.DontDestroyOnLoad(go);
        return go.AddComponent<EchoSpaceController>();
    }

    public void Begin(GameContext gameContext)
    {
        context = gameContext;
        IsFinished = false;
        waitingForNoteAdvance = false;
        coreClicked = false;
        noteClicked = false;

        BuildUiIfNeeded();
        gameObject.SetActive(true);
        if (rootGroup != null)
            rootGroup.alpha = 0f;

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);
        flowRoutine = StartCoroutine(RunFlow());
    }

    public void Hide()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        if (rootGroup != null)
            rootGroup.alpha = 0f;
        gameObject.SetActive(false);
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

        coreClicked = false;
        while (!coreClicked)
            yield return null;

        GuideSfx.PlayWhoosh();
        yield return ExpandHalo();
        yield return ShowNote();

        waitingForNoteAdvance = true;
        noteClicked = false;
        while (!noteClicked)
            yield return null;
        waitingForNoteAdvance = false;

        if (rootGroup != null)
            yield return FadeGroup(rootGroup, 1f, 0f, 0.35f);

        IsFinished = true;
    }

    private IEnumerator PlayLine(string line, float hold)
    {
        if (dialogueText == null)
            yield break;

        dialogueText.text = string.Empty;
        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text = line.Substring(0, i + 1);
            if (WasAdvancePressed())
            {
                dialogueText.text = line;
                break;
            }

            yield return new WaitForSeconds(0.045f);
        }

        dialogueText.text = line;
        float elapsed = 0f;
        while (elapsed < hold)
        {
            if (WasAdvancePressed())
                break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SetCoreResponseState()
    {
        if (coreImage != null)
            coreImage.color = new Color(0.62f, 0.48f, 0.92f, 1f);
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
            elapsed += Time.deltaTime;
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
        yield return new WaitForSeconds(1.35f);

        string body = DreamNoteWriter.Compose(
            context != null ? context.PlayerDreamInput : null,
            context != null ? context.TarotResult : null);

        if (noteText != null)
        {
            noteText.text = string.Empty;
            for (int i = 0; i < body.Length; i++)
            {
                noteText.text = body.Substring(0, i + 1);
                if (WasAdvancePressed())
                {
                    noteText.text = body;
                    break;
                }

                yield return new WaitForSeconds(0.03f);
            }

            noteText.text = body;
        }

        yield return new WaitForSeconds(0.35f);
    }

    private bool WasAdvancePressed()
    {
        if (context != null && context.InputManager != null)
        {
            if (context.InputManager.ConfirmPressed || context.InputManager.InteractPressed)
                return true;
        }

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private void Update()
    {
        if (waitingForNoteAdvance && (WasAdvancePressed() || noteClicked))
            noteClicked = true;
    }

    private void BuildUiIfNeeded()
    {
        if (rootGroup != null)
            return;

        EnsureEventSystem();

        var canvasGo = new GameObject("EchoSpaceCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var root = CreateStretch(canvasGo.transform, "Root");
        rootGroup = root.gameObject.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;

        var bg = CreateStretch(root, "Background").gameObject.AddComponent<Image>();
        var tarotBg = GuideArt.TarotBackground;
        if (tarotBg != null)
        {
            bg.sprite = tarotBg;
            bg.color = Color.white;
            bg.preserveAspect = false;
        }
        else
        {
            bg.color = new Color(0.17f, 0.16f, 0.27f, 1f);
        }
        bg.raycastTarget = true;

        var label = CreateText(root, "SpaceLabel", "独立梦境回响空间", 22, TextAlignmentOptions.MidlineLeft);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.08f, 0.88f);
        labelRect.anchorMax = new Vector2(0.55f, 0.96f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.color = new Color(1f, 1f, 1f, 0.7f);

        var box = CreatePanel(root, "DialogueBox", new Vector2(0.16f, 0.54f), new Vector2(0.84f, 0.8f), Color.white);
        var banner = GuideArt.DialogueBanner;
        if (banner != null)
        {
            var boxImage = box.GetComponent<Image>();
            boxImage.sprite = banner;
            boxImage.color = Color.white;
            boxImage.type = Image.Type.Sliced;
        }

        dialogueText = CreateText(box, "DialogueText", string.Empty, 30, TextAlignmentOptions.Center);
        Stretch(dialogueText.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f));
        dialogueText.color = new Color(0.16f, 0.16f, 0.2f, 1f);
        dialogueText.textWrappingMode = TextWrappingModes.Normal;

        CreateCharacter(root, "Feifei", GuideArt.Load("cb83933569bc45344b1141bc6af262eb"), new Vector2(-320f, -210f), new Vector2(220f, 220f), false);
        var feifeiFrames = GuideArt.LoadFeifeiFrames();
        if (feifeiFrames != null && feifeiFrames.Length > 10 && feifeiFrames[10] != null)
        {
            var feifeiImg = root.Find("Feifei").GetComponent<Image>();
            if (feifeiImg != null)
                feifeiImg.sprite = feifeiFrames[10];
        }

        var haloGo = CreateNamedCircle(root, "CoreHalo", new Vector2(0f, -210f), 160f, new Color(0.55f, 0.42f, 0.88f, 0.22f), null);
        haloImage = haloGo.GetComponent<Image>();

        var coreGo = CreateCharacter(root, "DreamCore", GuideArt.DreamCore, new Vector2(0f, -210f), new Vector2(88f, 120f), true);
        coreImage = coreGo.GetComponent<Image>();
        coreButton = coreGo.gameObject.AddComponent<Button>();
        coreButton.transition = Selectable.Transition.None;
        coreButton.onClick.AddListener(() => coreClicked = true);

        CreateCharacter(root, "Player", GuideArt.PlayerIdle, new Vector2(320f, -210f), new Vector2(180f, 220f), false);

        var note = CreatePanel(root, "DreamNote", new Vector2(0.22f, 0.12f), new Vector2(0.78f, 0.48f), new Color(0.96f, 0.93f, 0.86f, 1f));
        noteGroup = note.gameObject.AddComponent<CanvasGroup>();
        noteGroup.alpha = 0f;
        note.gameObject.SetActive(false);
        var noteButton = note.gameObject.AddComponent<Button>();
        noteButton.transition = Selectable.Transition.None;
        noteButton.onClick.AddListener(() => noteClicked = true);
        noteText = CreateText(note, "NoteText", string.Empty, 28, TextAlignmentOptions.Center);
        Stretch(noteText.rectTransform, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f));
        noteText.color = new Color(0.2f, 0.18f, 0.16f, 1f);
        noteText.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var go = new GameObject("EchoSpaceEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        Object.DontDestroyOnLoad(go);
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
        var circle = LoadCircleSprite();
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
        GuideUiFont.Apply(tmp);
        return tmp;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private const string CircleSpriteGuid = "e79b4d6c863b4124e9f6b264f6d371cb";

    private static Sprite LoadCircleSprite()
    {
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(CircleSpriteGuid);
        var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;
#endif
        return null;
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        group.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = to;
        group.blocksRaycasts = to > 0.01f;
        group.interactable = to > 0.01f;
    }
}
