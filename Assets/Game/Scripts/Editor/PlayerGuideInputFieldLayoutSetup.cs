using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerGuideInputFieldLayoutSetup
{
    private const string ScenePath = "Assets/Game/Scenes/PlayerGuide.unity";
    private const string InputRowName = "guidepanel-input-row";
    private const string InputFieldName = "guidepanel-inputfield";
    private const string ConfirmButtonName = "guidepanel-confirm-button";
    private const string LegacyInputFieldName = "InputField (TMP)";
    private const string FontAssetPath = "Assets/Game/Art/source/dialogue_font SDF.asset";
    private const string ConfirmButtonLabel = "确定";

    private static readonly Color BackgroundColor = new(0.20f, 0.20f, 0.22f, 0.96f);
    private static readonly Color ButtonBackgroundColor = new(0.32f, 0.32f, 0.35f, 1f);
    private static readonly Color TextColor = new(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color PlaceholderColor = new(0.58f, 0.58f, 0.60f, 0.65f);
    private static readonly Color CaretColor = new(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color SelectionColor = new(0.42f, 0.42f, 0.45f, 0.55f);

    [MenuItem("Tools/UI/Optimize GuidePanel InputField")]
    public static void OptimizeFromMenu()
    {
        if (!TryOptimizeOpenScene(out string message) && !TryOptimizeSceneAsset(out message))
        {
            Debug.LogError(message);
            return;
        }

        Debug.Log(message);
    }

    public static void OptimizeBatch()
    {
        if (!TryOptimizeSceneAsset(out string message))
        {
            Debug.LogError(message);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log(message);
        EditorApplication.Exit(0);
    }

    private static bool TryOptimizeOpenScene(out string message)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded || scene.path != ScenePath)
                continue;

            if (!TryOptimizeScene(scene, markDirty: true, out message))
                return false;

            return true;
        }

        message = null;
        return false;
    }

    private static bool TryOptimizeSceneAsset(out string message)
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            message = $"Scene not found: {ScenePath}";
            return false;
        }

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        bool success = TryOptimizeScene(scene, markDirty: true, out message);

        if (success)
            EditorSceneManager.SaveScene(scene);

        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded && previousActiveScene.path != ScenePath)
            SceneManager.SetActiveScene(previousActiveScene);

        return success;
    }

    private static bool TryOptimizeScene(Scene scene, bool markDirty, out string message)
    {
        TMP_InputField inputField = FindGuidePanelInputField(scene);
        if (inputField == null)
        {
            message = $"Could not find '{InputFieldName}' or '{LegacyInputFieldName}' under GuidePanel.";
            return false;
        }

        Transform guidePanel = FindGuidePanel(scene);
        if (guidePanel == null)
        {
            message = "GuidePanel not found.";
            return false;
        }

        Transform inputRow = EnsureInputRow(guidePanel, inputField.transform);
        inputField.transform.SetParent(inputRow, false);
        inputField.gameObject.name = InputFieldName;

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        ApplyInputFieldLayout(inputField, fontAsset);
        Button confirmButton = EnsureConfirmButton(inputRow, fontAsset);
        EnsureController(inputRow.gameObject, inputField, confirmButton);

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(scene);

        message = $"GuidePanel input field layout optimized with confirm button: {inputField.name}";
        return true;
    }

    private static Transform FindGuidePanel(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform guidePanel = FindChildByName(root.transform, "GuidePanel");
            if (guidePanel != null)
                return guidePanel;
        }

        return null;
    }

    private static TMP_InputField FindGuidePanelInputField(Scene scene)
    {
        Transform guidePanel = FindGuidePanel(scene);
        if (guidePanel == null)
            return null;

        TMP_InputField named = guidePanel.Find(InputFieldName)?.GetComponent<TMP_InputField>();
        if (named != null)
            return named;

        TMP_InputField inRow = guidePanel.Find($"{InputRowName}/{InputFieldName}")?.GetComponent<TMP_InputField>();
        if (inRow != null)
            return inRow;

        TMP_InputField legacy = guidePanel.Find(LegacyInputFieldName)?.GetComponent<TMP_InputField>();
        if (legacy != null)
            return legacy;

        return guidePanel.GetComponentInChildren<TMP_InputField>(true);
    }

    private static Transform FindChildByName(Transform parent, string targetName)
    {
        if (parent.name == targetName)
            return parent;

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform found = FindChildByName(parent.GetChild(index), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform EnsureInputRow(Transform guidePanel, Transform inputField)
    {
        Transform existingRow = guidePanel.Find(InputRowName);
        if (existingRow != null)
        {
            ConfigureInputRowRect(existingRow as RectTransform);
            ConfigureHorizontalLayout(existingRow);
            return existingRow;
        }

        var rowObject = new GameObject(InputRowName, typeof(RectTransform));
        rowObject.layer = LayerMask.NameToLayer("UI");
        Transform row = rowObject.transform;
        row.SetParent(guidePanel, false);
        row.SetSiblingIndex(inputField.GetSiblingIndex());

        ConfigureInputRowRect(row as RectTransform);
        ConfigureHorizontalLayout(row);
        return row;
    }

    private static void ConfigureInputRowRect(RectTransform row)
    {
        if (row == null)
            return;

        row.anchorMin = new Vector2(0.14f, 0f);
        row.anchorMax = new Vector2(0.96f, 0f);
        row.pivot = new Vector2(0.5f, 0f);
        row.anchoredPosition = new Vector2(0f, 64f);
        row.sizeDelta = new Vector2(0f, 56f);
        row.localScale = Vector3.one;
    }

    private static void ConfigureHorizontalLayout(Transform row)
    {
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>() ??
                                       row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);
    }

    private static void ApplyInputFieldLayout(TMP_InputField inputField, TMP_FontAsset fontAsset)
    {
        RectTransform rect = inputField.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        LayoutElement layoutElement = inputField.GetComponent<LayoutElement>() ??
                                      inputField.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.minWidth = 120f;
        layoutElement.preferredHeight = 56f;
        layoutElement.minHeight = 56f;

        if (inputField.targetGraphic is Image background)
        {
            background.color = BackgroundColor;
            background.raycastTarget = true;
        }

        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.pointSize = 22f;
        inputField.caretColor = CaretColor;
        inputField.selectionColor = SelectionColor;

        if (fontAsset != null)
            inputField.fontAsset = fontAsset;

        Transform textArea = inputField.textViewport != null
            ? inputField.textViewport
            : inputField.transform.Find("Text Area");
        if (textArea is RectTransform textAreaRect)
        {
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.pivot = new Vector2(0.5f, 0.5f);
            textAreaRect.anchoredPosition = Vector2.zero;
            textAreaRect.sizeDelta = new Vector2(-28f, -16f);

            RectMask2D mask = textArea.GetComponent<RectMask2D>();
            if (mask != null)
                mask.padding = new Vector4(12f, 8f, 12f, 8f);
        }

        ApplyTextStyle(inputField.textComponent, fontAsset, TextColor, 22, FontStyles.Normal);
        ApplyTextStyle(inputField.placeholder as TMP_Text, fontAsset, PlaceholderColor, 20, FontStyles.Italic);

        EditorUtility.SetDirty(inputField);
    }

    private static Button EnsureConfirmButton(Transform inputRow, TMP_FontAsset fontAsset)
    {
        Transform existing = inputRow.Find(ConfirmButtonName);
        Button button = existing != null
            ? existing.GetComponent<Button>()
            : CreateConfirmButton(inputRow, fontAsset);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>() ??
                                      button.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 0f;
        layoutElement.preferredWidth = 104f;
        layoutElement.minWidth = 104f;
        layoutElement.preferredHeight = 56f;
        layoutElement.minHeight = 56f;

        if (button.targetGraphic is Image background)
            background.color = ButtonBackgroundColor;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        ApplyTextStyle(label, fontAsset, TextColor, 22, FontStyles.Normal);
        if (label != null)
        {
            label.alignment = TextAlignmentOptions.Center;
            label.text = ConfirmButtonLabel;
        }

        UIButtonAnimationUtility.Apply(button);
        EditorUtility.SetDirty(button);
        return button;
    }

    private static Button CreateConfirmButton(Transform inputRow, TMP_FontAsset fontAsset)
    {
        var buttonObject = new GameObject(ConfirmButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.layer = LayerMask.NameToLayer("UI");
        buttonObject.transform.SetParent(inputRow, false);

        Image background = buttonObject.GetComponent<Image>();
        background.color = ButtonBackgroundColor;

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.layer = LayerMask.NameToLayer("UI");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(label, fontAsset, TextColor, 22, FontStyles.Normal);
        label.alignment = TextAlignmentOptions.Center;
        label.text = ConfirmButtonLabel;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        return button;
    }

    private static void EnsureController(GameObject inputRow, TMP_InputField inputField, Button confirmButton)
    {
        GuidePanelInputFieldController controller = inputRow.GetComponent<GuidePanelInputFieldController>() ??
                                                  inputRow.AddComponent<GuidePanelInputFieldController>();

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("inputField").objectReferenceValue = inputField;
        serializedController.FindProperty("confirmButton").objectReferenceValue = confirmButton;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void ApplyTextStyle(
        TMP_Text text,
        TMP_FontAsset fontAsset,
        Color color,
        float fontSize,
        FontStyles fontStyle)
    {
        if (text == null)
            return;

        if (fontAsset != null)
            text.font = fontAsset;

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.margin = new Vector4(0f, 0f, 0f, 0f);

        EditorUtility.SetDirty(text);
    }
}
