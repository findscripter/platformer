using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text promptText;

    private const string FontAssetGuid = "7e794ed8003821b4dac6b110719e0135";

    private void Awake()
    {
        EnsurePrompt();
        Hide();
    }

    private void OnEnable()
    {
        EventCenter.Subscribe(GameEvents.InteractionFocusChanged, OnFocusChanged);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe(GameEvents.InteractionFocusChanged, OnFocusChanged);
    }

    private void OnFocusChanged(IInteractable focus)
    {
        if (focus == null || !focus.CanInteract)
        {
            Hide();
            return;
        }

        EnsurePrompt();
        if (promptText != null)
            promptText.text = "[E]  " + focus.InteractPrompt;

        if (root != null)
            root.SetActive(true);
    }

    private void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void EnsurePrompt()
    {
        if (promptText != null && root != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform host = canvas != null ? canvas.transform : transform;
        Transform existing = host.Find("InteractionPromptLabel");
        GameObject go = existing != null ? existing.gameObject : new GameObject("InteractionPromptLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        if (existing == null)
            go.transform.SetParent(host, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 48f);
        rect.sizeDelta = new Vector2(640f, 48f);

        promptText = go.GetComponent<TextMeshProUGUI>();
        promptText.fontSize = 28;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.raycastTarget = false;
        promptText.outlineWidth = 0.18f;
        promptText.outlineColor = new Color(0f, 0f, 0f, 0.85f);
#if UNITY_EDITOR
        TMP_FontAsset font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            UnityEditor.AssetDatabase.GUIDToAssetPath(FontAssetGuid));
        if (font != null)
            promptText.font = font;
#endif
        root = go;
    }
}
