using UnityEngine;
using UnityEngine.UI;

public readonly struct DialogueUIBuildResult
{
    public DialogueUIBuildResult(
        GameObject panelRoot,
        DialogueUIController dialogueUI,
        ChoiceUIController choiceUI,
        InputUIController inputUI)
    {
        PanelRoot = panelRoot;
        DialogueUI = dialogueUI;
        ChoiceUI = choiceUI;
        InputUI = inputUI;
    }

    public GameObject PanelRoot { get; }
    public DialogueUIController DialogueUI { get; }
    public ChoiceUIController ChoiceUI { get; }
    public InputUIController InputUI { get; }
}

public static class DialogueUIBuilder
{
    public const string CanvasName = "Canvas_Dialogue";
    public const string PanelName = "DialoguePanel";
    public const int CanvasSortOrder = 120;

    public static readonly Vector2 PanelAnchorMin = new(0.06f, 0f);
    public static readonly Vector2 PanelAnchorMax = new(0.94f, 0f);
    public static readonly Vector2 PanelPivot = new(0.5f, 0f);
    public static readonly Vector2 PanelAnchoredPosition = new(0f, 24f);
    public static readonly Vector2 PanelSizeDelta = new(0f, 240f);

    public static DialogueUIBuildResult Build(Transform uiRoot, Transform dialogueCanvasOverride = null)
    {
        Transform dialogueCanvas = dialogueCanvasOverride != null
            ? dialogueCanvasOverride
            : EnsureChild(uiRoot, CanvasName);

        ConfigureCanvas(dialogueCanvas.gameObject);

        Transform panel = EnsureChild(dialogueCanvas, PanelName);
        ConfigurePanel(panel);

        Text speakerText = EnsureLabel(panel, "SpeakerText", new Vector2(20f, -12f), 20, FontStyle.Bold);
        Text bodyText = EnsureLabel(panel, "BodyText", new Vector2(20f, -48f), 18, FontStyle.Normal);
        bodyText.rectTransform.sizeDelta = new Vector2(-40f, 80f);
        Text hintText = EnsureLabel(panel, "HintText", new Vector2(20f, -130f), 16, FontStyle.Italic);

        Transform choiceRoot = EnsureChild(panel, "ChoicePanel");
        ConfigureChoicePanel(choiceRoot);
        Button choiceButtonPrefab = CreateChoiceButtonPrefab(panel);

        Transform inputRoot = EnsureChild(panel, "InputPanel");
        ConfigureInputPanel(inputRoot);
        Text inputQuestion = EnsureLabel(inputRoot, "QuestionText", new Vector2(0f, -8f), 16, FontStyle.Normal);
        InputField inputField = CreateInputField(inputRoot, "AnswerInput");
        Button submitButton = CreateButton(inputRoot, "SubmitButton", "确认");

        var dialogueUI = EnsureComponent<DialogueUIController>(panel.gameObject);
        dialogueUI.Configure(panel.gameObject, speakerText, bodyText, hintText);

        var choiceUI = EnsureComponent<ChoiceUIController>(choiceRoot.gameObject);
        choiceUI.Configure(choiceRoot.gameObject, choiceRoot, choiceButtonPrefab);

        var inputUI = EnsureComponent<InputUIController>(inputRoot.gameObject);
        inputUI.Configure(inputRoot.gameObject, inputQuestion, inputField, submitButton);

        panel.gameObject.SetActive(false);
        choiceRoot.gameObject.SetActive(false);
        inputRoot.gameObject.SetActive(false);

        return new DialogueUIBuildResult(panel.gameObject, dialogueUI, choiceUI, inputUI);
    }

    public static void ConfigureCanvas(GameObject canvasObject)
    {
        var canvas = EnsureComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;

        var scaler = EnsureComponent<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        EnsureComponent<GraphicRaycaster>(canvasObject);

        RectTransform canvasRect = EnsureComponent<RectTransform>(canvasObject);
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;
    }

    private static void ConfigurePanel(Transform panel)
    {
        RectTransform panelRect = EnsureComponent<RectTransform>(panel.gameObject);
        panelRect.anchorMin = PanelAnchorMin;
        panelRect.anchorMax = PanelAnchorMax;
        panelRect.pivot = PanelPivot;
        panelRect.anchoredPosition = PanelAnchoredPosition;
        panelRect.sizeDelta = PanelSizeDelta;
        panelRect.localScale = Vector3.one;

        Image panelImage = EnsureComponent<Image>(panel.gameObject);
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);
    }

    private static void ConfigureChoicePanel(Transform choiceRoot)
    {
        RectTransform choiceRect = EnsureComponent<RectTransform>(choiceRoot.gameObject);
        choiceRect.anchorMin = new Vector2(0f, 0f);
        choiceRect.anchorMax = new Vector2(1f, 0f);
        choiceRect.pivot = new Vector2(0.5f, 0f);
        choiceRect.anchoredPosition = new Vector2(0f, 12f);
        choiceRect.sizeDelta = new Vector2(-40f, 120f);

        var choiceLayout = EnsureComponent<VerticalLayoutGroup>(choiceRoot.gameObject);
        choiceLayout.spacing = 8f;
        choiceLayout.childAlignment = TextAnchor.LowerCenter;
        choiceLayout.childControlWidth = true;
        choiceLayout.childControlHeight = true;
        choiceLayout.childForceExpandWidth = true;
        choiceLayout.childForceExpandHeight = false;
    }

    private static void ConfigureInputPanel(Transform inputRoot)
    {
        RectTransform inputRect = EnsureComponent<RectTransform>(inputRoot.gameObject);
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(1f, 0f);
        inputRect.pivot = new Vector2(0.5f, 0f);
        inputRect.anchoredPosition = new Vector2(0f, 12f);
        inputRect.sizeDelta = new Vector2(-40f, 80f);
    }

    private static Text EnsureLabel(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        int fontSize,
        FontStyle style)
    {
        Transform existing = parent.Find(name);
        GameObject labelObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Text));

        if (existing == null)
            labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(-40f, 28f);

        Text text = labelObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        return text;
    }

    private static Button CreateChoiceButtonPrefab(Transform parent)
    {
        Transform misplaced = parent.Find("ChoicePanel/ChoiceButtonPrefab");
        if (misplaced != null)
            misplaced.SetParent(parent, false);

        Transform existing = parent.Find("ChoiceButtonPrefab");
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
            Button existingButton = existing.GetComponent<Button>();
            UIButtonAnimation.Apply(existingButton);
            return existingButton;
        }

        var buttonObject = new GameObject("ChoiceButtonPrefab", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.SetActive(false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 36f);
        buttonObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        Text labelText = label.GetComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        UIButtonAnimation.Apply(button);
        return button;
    }

    private static InputField CreateInputField(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.GetComponent<InputField>();

        var inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0.7f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(0f, 8f);
        rect.sizeDelta = new Vector2(0f, 32f);
        inputObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);

        var placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderObject.transform.SetParent(inputObject.transform, false);
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(inputObject.transform, false);

        InputField inputField = inputObject.GetComponent<InputField>();
        inputField.textComponent = textObject.GetComponent<Text>();
        inputField.placeholder = placeholderObject.GetComponent<Text>();
        return inputField;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            UIButtonAnimation.Apply(existingButton);
            return existingButton;
        }

        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.75f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(0f, 8f);
        rect.sizeDelta = new Vector2(0f, 32f);
        buttonObject.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 0.85f);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        Text labelText = labelObject.GetComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.text = label;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        UIButtonAnimation.Apply(button);
        return button;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            return child;

        var childObject = new GameObject(name, typeof(RectTransform));
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
