using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text promptText;

    private IInteractable currentFocus;
    private Image background;

    private void Awake()
    {
        EnsurePrompt();
        HideLabel();
    }

    private void OnEnable()
    {
        EventCenter.Subscribe(GameEvents.InteractionFocusChanged, OnFocusChanged);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe(GameEvents.InteractionFocusChanged, OnFocusChanged);
    }

    private void Update()
    {
        if (currentFocus == null || promptText == null || root == null || !root.activeSelf)
            return;

        promptText.text = "[E/F]  " + currentFocus.InteractPrompt;
    }

    private void OnFocusChanged(IInteractable focus)
    {
        currentFocus = focus;
        if (focus == null || !focus.CanInteract)
        {
            HideLabel();
            return;
        }

        EnsurePrompt();
        if (promptText != null)
            promptText.text = "[E/F]  " + focus.InteractPrompt;
        if (root != null)
            root.SetActive(true);
    }

    private void HideLabel()
    {
        currentFocus = null;
        if (root != null)
            root.SetActive(false);
    }

    private void EnsurePrompt()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GuideUiLayout.ConfigureCanvas(canvas);
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 40);
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
        }

        if (promptText != null && root != null)
        {
            ApplyStyle();
            return;
        }

        Transform host = canvas != null ? canvas.transform : transform;
        Transform existing = host.Find("InteractionPromptLabel");
        GameObject go = existing != null ? existing.gameObject : new GameObject("InteractionPromptLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null)
            go.transform.SetParent(host, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 96f);
        rect.sizeDelta = new Vector2(920f, 56f);

        background = go.GetComponent<Image>();
        if (background == null)
            background = go.AddComponent<Image>();

        Transform textTransform = go.transform.Find("Label");
        GameObject textGo = textTransform != null ? textTransform.gameObject : new GameObject("Label", typeof(RectTransform));
        if (textTransform == null)
            textGo.transform.SetParent(go.transform, false);

        promptText = textGo.GetComponent<TextMeshProUGUI>();
        if (promptText == null)
            promptText = textGo.AddComponent<TextMeshProUGUI>();
        GuideUiLayout.Stretch(promptText.rectTransform, Vector2.zero, Vector2.one);
        promptText.rectTransform.offsetMin = new Vector2(18f, 6f);
        promptText.rectTransform.offsetMax = new Vector2(-18f, -6f);

        root = go;
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        if (background == null && root != null)
            background = root.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.08f, 0.07f, 0.1f, 0.78f);
            background.raycastTarget = false;
        }

        if (promptText == null)
            return;

        GuideUiLayout.ReadableText(promptText, 26f, new Color(0.98f, 0.95f, 0.86f, 1f));
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.textWrappingMode = TextWrappingModes.NoWrap;
        promptText.raycastTarget = false;
        if (promptText.isActiveAndEnabled)
        {
            promptText.outlineWidth = 0.12f;
            promptText.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        }
    }
}
