using UnityEngine;
using UnityEngine.UI;

public class BootPersistentRoot : MonoBehaviour
{
    public static BootPersistentRoot Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        FixZeroScaleCanvases();
        EnsureInteractionPrompt();
    }

    private void EnsureInteractionPrompt()
    {
        Transform prompt = transform.Find("UI/Canvas_Prompt/InteractionPrompt");
        if (prompt == null || prompt.GetComponent<InteractionPromptController>() != null)
            return;

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(prompt, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var promptText = labelObject.GetComponent<Text>();
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        promptText.fontSize = 22;
        promptText.text = "[E] 交互";

        prompt.gameObject.AddComponent<InteractionPromptController>();
        prompt.gameObject.SetActive(false);
    }

    private void FixZeroScaleCanvases()
    {
        var uiRoot = transform.Find("UI");
        if (uiRoot == null)
        {
            return;
        }

        foreach (Transform child in uiRoot)
        {
            if (child is not RectTransform rectTransform)
            {
                continue;
            }

            if (rectTransform.localScale == Vector3.zero)
            {
                rectTransform.localScale = Vector3.one;
            }
        }
    }
}
