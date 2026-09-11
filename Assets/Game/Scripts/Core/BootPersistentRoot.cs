using UnityEngine;

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
        if (prompt == null)
            return;

        // 主机必须一直开着才能订阅交互焦点；只隐藏文案，不要把整个物体关掉。
        prompt.gameObject.SetActive(true);
        if (prompt.GetComponent<InteractionPromptController>() == null)
            prompt.gameObject.AddComponent<InteractionPromptController>();
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
                rectTransform.localScale = Vector3.one;

            Canvas canvas = child.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
        }
    }
}
