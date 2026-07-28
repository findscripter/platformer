using UnityEngine;
using UnityEngine.UI;

public class DialogueUIBootstrap : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueUIController dialogueUI;
    [SerializeField] private ChoiceUIController choiceUI;
    [SerializeField] private InputUIController inputUI;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = GetComponent<DialogueManager>();

        dialogueUI?.Bind(dialogueManager);
        choiceUI?.Bind(dialogueManager);
        inputUI?.Bind(dialogueManager);
    }

    public void Configure(
        DialogueManager manager,
        DialogueUIController dialogueUiController,
        ChoiceUIController choiceUiController,
        InputUIController inputUiController)
    {
        dialogueManager = manager;
        dialogueUI = dialogueUiController;
        choiceUI = choiceUiController;
        inputUI = inputUiController;

        dialogueUI?.Bind(dialogueManager);
        choiceUI?.Bind(dialogueManager);
        inputUI?.Bind(dialogueManager);
    }
}
