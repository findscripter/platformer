using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceUIController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button choiceButtonPrefab;

    private readonly List<Button> spawnedButtons = new();
    private DialogueManager dialogueManager;

    public void Configure(GameObject panelRoot, Transform container, Button buttonPrefab)
    {
        root = panelRoot;
        buttonContainer = container;
        choiceButtonPrefab = buttonPrefab;
    }

    public void Bind(DialogueManager manager)
    {
        if (dialogueManager == manager)
            return;

        Unbind();
        dialogueManager = manager;

        if (dialogueManager != null)
        {
            dialogueManager.ChoicesPresented += OnChoicesPresented;
            dialogueManager.TextPresented += OnTextPresented;
            dialogueManager.InputPresented += OnInputPresented;
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

        dialogueManager.ChoicesPresented -= OnChoicesPresented;
        dialogueManager.TextPresented -= OnTextPresented;
        dialogueManager.InputPresented -= OnInputPresented;
        dialogueManager = null;
    }

    private void OnTextPresented(string speaker, string text)
    {
        HideChoices();
    }

    private void OnInputPresented(string question)
    {
        HideChoices();
    }

    private void HideChoices()
    {
        ClearButtons();
        if (root != null)
            root.SetActive(false);
    }

    private void OnChoicesPresented(ChoiceOption[] options)
    {
        ClearButtons();

        if (options == null || options.Length == 0)
        {
            if (root != null)
                root.SetActive(false);
            return;
        }

        if (root != null)
            root.SetActive(true);

        for (int i = 0; i < options.Length; i++)
        {
            int choiceIndex = i;
            ChoiceOption option = options[i];
            if (option == null || choiceButtonPrefab == null || buttonContainer == null)
                continue;

            Button button = Instantiate(choiceButtonPrefab, buttonContainer);
            button.gameObject.SetActive(true);
            spawnedButtons.Add(button);

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = option.text;

            button.onClick.AddListener(() => dialogueManager?.OnChoiceSelected(choiceIndex));
        }
    }

    private void ClearButtons()
    {
        for (int i = spawnedButtons.Count - 1; i >= 0; i--)
        {
            Button button = spawnedButtons[i];
            if (button != null)
                Destroy(button.gameObject);
        }

        spawnedButtons.Clear();
        ClearLegacyContainerChildren();
    }

    private void ClearLegacyContainerChildren()
    {
        if (buttonContainer == null)
            return;

        for (int i = buttonContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = buttonContainer.GetChild(i);
            if (!IsAlive(child))
                continue;

            if (IsTemplateChild(child))
                continue;

            Destroy(child.gameObject);
        }
    }

    private bool IsTemplateChild(Transform child)
    {
        return choiceButtonPrefab != null &&
               IsAlive(choiceButtonPrefab) &&
               child.gameObject == choiceButtonPrefab.gameObject;
    }

    private static bool IsAlive(Object unityObject)
    {
        return unityObject != null;
    }
}
