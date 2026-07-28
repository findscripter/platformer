using UnityEngine;
using UnityEngine.UI;

public class InputUIController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text questionText;
    [SerializeField] private InputField answerInput;
    [SerializeField] private Button submitButton;

    private DialogueManager dialogueManager;

    public void Configure(GameObject panelRoot, Text question, InputField input, Button submit)
    {
        root = panelRoot;
        questionText = question;
        answerInput = input;
        submitButton = submit;

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitCurrentInput);
            submitButton.onClick.AddListener(SubmitCurrentInput);
        }
    }

    private void Awake()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitCurrentInput);
    }

    public void Bind(DialogueManager manager)
    {
        if (dialogueManager == manager)
            return;

        Unbind();
        dialogueManager = manager;

        if (dialogueManager != null)
        {
            dialogueManager.InputPresented += OnInputPresented;
            dialogueManager.TextPresented += OnTextPresented;
            dialogueManager.ChoicesPresented += OnChoicesPresented;
        }
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (dialogueManager == null)
            return;

        dialogueManager.InputPresented -= OnInputPresented;
        dialogueManager.TextPresented -= OnTextPresented;
        dialogueManager.ChoicesPresented -= OnChoicesPresented;
        dialogueManager = null;
    }

    private void OnTextPresented(string speaker, string text)
    {
        if (root != null)
            root.SetActive(false);
    }

    private void OnChoicesPresented(ChoiceOption[] options)
    {
        if (root != null)
            root.SetActive(false);
    }

    private void OnInputPresented(string question)
    {
        if (root != null)
            root.SetActive(true);

        if (questionText != null)
            questionText.text = question ?? string.Empty;

        if (answerInput != null)
        {
            answerInput.text = string.Empty;
            answerInput.ActivateInputField();
        }
    }

    public void SubmitCurrentInput()
    {
        if (dialogueManager == null)
            return;

        string value = answerInput != null ? answerInput.text : string.Empty;
        dialogueManager.OnInputSubmitted(value);

        if (root != null)
            root.SetActive(false);
    }
}
