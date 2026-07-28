using UnityEngine;
using UnityEngine.UI;

public class DialogueUIController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text hintText;

    private DialogueManager dialogueManager;

    public void Configure(GameObject panelRoot, Text speaker, Text body, Text hint)
    {
        root = panelRoot;
        speakerText = speaker;
        bodyText = body;
        hintText = hint;
    }

    public void Bind(DialogueManager manager)
    {
        if (dialogueManager == manager)
            return;

        Unbind();
        dialogueManager = manager;

        if (dialogueManager == null)
            return;

        dialogueManager.TextPresented += OnTextPresented;
        dialogueManager.ChoicesPresented += OnChoicesPresented;
        dialogueManager.InputPresented += OnInputPresented;
        dialogueManager.DialogueUiCleared += HideAll;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (dialogueManager == null)
            return;

        dialogueManager.TextPresented -= OnTextPresented;
        dialogueManager.ChoicesPresented -= OnChoicesPresented;
        dialogueManager.InputPresented -= OnInputPresented;
        dialogueManager.DialogueUiCleared -= HideAll;
        dialogueManager = null;
    }

    private void OnTextPresented(string speaker, string text)
    {
        if (root != null)
            root.SetActive(true);

        if (speakerText != null)
            speakerText.text = speaker ?? string.Empty;

        if (bodyText != null)
            bodyText.text = text ?? string.Empty;

        if (hintText != null)
            hintText.text = "按 Enter / Space 继续";
    }

    private void OnChoicesPresented(ChoiceOption[] options)
    {
        if (root != null)
            root.SetActive(true);

        if (bodyText != null && string.IsNullOrWhiteSpace(bodyText.text))
            bodyText.text = "请选择：";

        if (hintText != null)
            hintText.text = "请使用下方选项按钮";
    }

    private void OnInputPresented(string question)
    {
        if (root != null)
            root.SetActive(true);

        if (bodyText != null)
            bodyText.text = question ?? string.Empty;

        if (hintText != null)
            hintText.text = "请在输入框中作答";
    }

    private void HideAll()
    {
        if (root != null)
            root.SetActive(false);
    }
}
