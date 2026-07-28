using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text promptText;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        if (promptText == null)
            promptText = GetComponentInChildren<Text>(true);

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

        if (promptText != null)
            promptText.text = $"[E] {focus.InteractPrompt}";

        if (root != null)
            root.SetActive(true);
    }

    private void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}
