using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 回响结束后的出口。视觉跟写梦/回响同一套水墨底与输入框，不覆盖引导 MVC。
/// </summary>
public sealed class DreamReplayPanel : MonoBehaviour
{
    private static DreamReplayPanel instance;

    private GameContext context;
    private bool chosen;

    public static void Show(GameContext ctx)
    {
        if (instance != null)
            Destroy(instance.gameObject);

        var go = new GameObject("DreamReplayChoices");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DreamReplayPanel>();
        instance.Build();
        instance.context = ctx;
        instance.chosen = false;
        instance.gameObject.SetActive(true);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public static void Hide()
    {
        if (instance != null)
            instance.gameObject.SetActive(false);
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        GuideUiLayout.ConfigureCanvas(this);

        var root = CreateStretch(transform, "Root");

        var bg = CreateStretch(root, "Background").gameObject.AddComponent<Image>();
        Sprite dreamBg = GuideArt.DreamBackground != null ? GuideArt.DreamBackground : GuideArt.EchoBackground;
        GuideUiLayout.Cover(bg, dreamBg);
        if (dreamBg == null)
            bg.color = new Color(0.03f, 0.03f, 0.045f, 1f);
        bg.raycastTarget = true;

        var veil = CreateStretch(root, "Veil").gameObject.AddComponent<Image>();
        veil.color = new Color(0.02f, 0.02f, 0.035f, 0.42f);
        veil.raycastTarget = true;

        var panel = CreatePanel(root, "InkPanel", new Vector2(720f, 620f), Vector2.zero);
        var panelImage = panel.GetComponent<Image>();
        Sprite frame = GuideArt.InputPanelFrame;
        if (frame != null)
        {
            panelImage.sprite = frame;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = false;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.color = new Color(0.12f, 0.12f, 0.14f, 0.94f);
        }
        panelImage.raycastTarget = true;

        var title = CreateLabel(panel, "Title", "梦已留下回声。", new Vector2(0f, 214f), new Vector2(600f, 64f), 40f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        var subtitle = CreateLabel(panel, "Subtitle", "你想再次走入其中，还是写下另一场梦？",
            new Vector2(0f, 150f), new Vector2(600f, 56f), 26f, 0.86f);
        subtitle.alignment = TextAlignmentOptions.Center;

        var replay = CreateChoice(panel, "重返此梦", "保留这场梦，重新抽牌", 48f, 0);
        var rewrite = CreateChoice(panel, "写下新梦", "写下另一场梦", -56f, 1);
        var menu = CreateChoice(panel, "回到主界面", "暂别这场梦", -160f, 2);
        SetNav(replay, null, rewrite);
        SetNav(rewrite, replay, menu);
        SetNav(menu, rewrite, null);
    }

    private Button CreateChoice(Transform parent, string title, string hint, float y, int action)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        Center(rt, new Vector2(0f, y), new Vector2(520f, 86f));

        var image = go.GetComponent<Image>();
        Sprite frame = GuideArt.InputPanelFrame;
        if (frame != null)
        {
            image.sprite = frame;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = new Color(0.92f, 0.9f, 0.86f, 1f);
        }
        else
        {
            image.color = new Color(0.22f, 0.22f, 0.28f, 0.92f);
        }

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(0.92f, 0.9f, 0.86f, 1f);
        colors.highlightedColor = new Color(1f, 0.96f, 0.78f, 1f);
        colors.selectedColor = new Color(1f, 0.96f, 0.78f, 1f);
        colors.pressedColor = new Color(0.78f, 0.74f, 0.66f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        image.color = Color.white;
        go.AddComponent<ChoiceHover>();

        var titleText = CreateLabel(go.transform, "Title", title, new Vector2(0f, 10f), new Vector2(480f, 40f), 30f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;
        var hintText = CreateLabel(go.transform, "Hint", hint, new Vector2(0f, -18f), new Vector2(480f, 28f), 18f, 0.72f);
        hintText.alignment = TextAlignmentOptions.Center;

        button.onClick.AddListener(() =>
        {
            if (chosen || context?.GameLoop == null)
                return;
            chosen = true;
            if (action == 2)
                context.GameLoop.ReturnToMainMenu();
            else
                context.GameLoop.ReplayDream(action == 0);
        });
        return button;
    }

    private static void SetNav(Button button, Button up, Button down)
    {
        var navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = up,
            selectOnDown = down
        };
        button.navigation = navigation;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string words, Vector2 position, Vector2 size, float fontSize, float alpha)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        Center(rt, position, size);
        var text = go.GetComponent<TextMeshProUGUI>();
        GuideUiLayout.ReadableText(text, fontSize, GuideUiLayout.DialogueInk);
        text.color = new Color(GuideUiLayout.DialogueInk.r, GuideUiLayout.DialogueInk.g, GuideUiLayout.DialogueInk.b, alpha);
        text.alignment = TextAlignmentOptions.Center;
        text.text = words;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateStretch(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        GuideUiLayout.Stretch(rt, Vector2.zero, Vector2.one);
        return rt;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        Center(rt, position, size);
        return rt;
    }

    private static void Center(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private sealed class ChoiceHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Vector3 restScale = Vector3.one;

        private void Awake()
        {
            restScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = restScale * 1.06f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = restScale;
        }
    }
}
