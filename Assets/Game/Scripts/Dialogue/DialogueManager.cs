using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public event Action<string, string> TextPresented;
    public event Action<ChoiceOption[]> ChoicesPresented;
    public event Action<string> InputPresented;
    public event Action DialogueUiCleared;

    private DialogueSO currentDialogue;
    private DialogueContext dialogueContext;
    private DialogueNode currentNode;
    private bool isRunning;
    private bool isSuspended;

    private DialogueNodeStep currentStep;
    private ChoiceNode currentChoiceNode;
    private InputNode currentInputNode;

    public bool IsRunning => isRunning;
    public bool IsSuspended => isSuspended;
    public DialogueSO CurrentDialogue => currentDialogue;
    public DialogueNode CurrentNode => currentNode;
    public bool IsWaitingForConfirm => isRunning && currentNode is TextNode && currentStep == DialogueNodeStep.Waiting;
    public bool IsWaitingForChoice => isRunning && currentNode is ChoiceNode && currentStep == DialogueNodeStep.Waiting;
    public bool IsWaitingForInput => isRunning && currentNode is InputNode && currentStep == DialogueNodeStep.Waiting;

    public void BeginDialogue(DialogueSO dialogue, GameContext gameContext)
    {
        if (dialogue == null || gameContext == null)
            return;

        currentDialogue = dialogue;
        dialogueContext = new DialogueContext(gameContext, this, dialogue);
        isRunning = true;
        isSuspended = false;

        gameContext.StateMachine.ChangeState(GameStateType.Dialogue);
        EventCenter.Publish(GameEvents.DialogueStarted, dialogue);
        AdvanceTo(dialogue.startNode);
    }

    public void ResumeFromSave(DialogueSO dialogue, string nodeId, GameContext gameContext)
    {
        if (dialogue == null || gameContext == null || string.IsNullOrWhiteSpace(nodeId))
            return;

        DialogueNode node = dialogue.FindNodeById(nodeId);
        if (node == null)
        {
            Debug.LogWarning($"DialogueManager: node '{nodeId}' was not found in dialogue '{dialogue.dialogueId}'.");
            return;
        }

        currentDialogue = dialogue;
        dialogueContext = new DialogueContext(gameContext, this, dialogue);
        isRunning = true;
        isSuspended = false;

        gameContext.StateMachine.ChangeState(GameStateType.Dialogue);
        EventCenter.Publish(GameEvents.DialogueStarted, dialogue);
        EnterNode(node, publishNodeEntered: false);
    }

    public void Tick()
    {
    }

    public void OnConfirmPressed()
    {
        if (!IsWaitingForConfirm || currentNode is not TextNode textNode)
            return;

        AdvanceTo(textNode.nextNode);
    }

    public void OnChoiceSelected(int choiceIndex)
    {
        if (!IsWaitingForChoice || currentChoiceNode?.options == null)
            return;

        if (choiceIndex < 0 || choiceIndex >= currentChoiceNode.options.Length)
            return;

        string dialogueId = currentDialogue != null ? currentDialogue.dialogueId : string.Empty;
        string nodeId = currentChoiceNode.nodeId ?? string.Empty;
        EventCenter.Publish(
            GameEvents.DialogueChoice,
            new DialogueChoiceResult(dialogueId, nodeId, choiceIndex));

        AdvanceTo(currentChoiceNode.options[choiceIndex].nextNode);
    }

    public void OnInputSubmitted(string value)
    {
        if (!IsWaitingForInput || currentInputNode == null)
            return;

        bool success = string.Equals(
            value?.Trim(),
            currentInputNode.expectedAnswer?.Trim(),
            StringComparison.OrdinalIgnoreCase);

        string dialogueId = currentDialogue != null ? currentDialogue.dialogueId : string.Empty;
        string nodeId = currentInputNode.nodeId ?? string.Empty;
        EventCenter.Publish(
            GameEvents.DialogueInputResult,
            new DialogueInputResult(dialogueId, nodeId, success));

        AdvanceTo(success ? currentInputNode.successNode : currentInputNode.failNode);
    }

    public void PresentText(string speaker, string text)
    {
        TextPresented?.Invoke(speaker, text);
    }

    public void PresentChoices(ChoiceOption[] options)
    {
        ChoicesPresented?.Invoke(options);
    }

    public void PresentInput(string question)
    {
        InputPresented?.Invoke(question);
    }

    public void SuspendForSceneTransition()
    {
        if (!isRunning)
            return;

        isSuspended = true;
        DialogueUiCleared?.Invoke();
    }

    public void ResumeAfterSceneTransition()
    {
        if (!isRunning || !isSuspended)
            return;

        isSuspended = false;
        RepresentCurrentNode();
    }

    public DialogueSnapshot GetSnapshot()
    {
        if (!isRunning || currentDialogue == null || currentNode == null)
            return default;

        string waitKind = string.Empty;
        if (IsWaitingForConfirm)
            waitKind = DialogueWaitKinds.Text;
        else if (IsWaitingForChoice)
            waitKind = DialogueWaitKinds.Choice;
        else if (IsWaitingForInput)
            waitKind = DialogueWaitKinds.Input;

        return new DialogueSnapshot(currentDialogue.dialogueId, currentNode.nodeId, waitKind);
    }

    public void AdvanceTo(DialogueNode node)
    {
        if (!isRunning || node == null)
        {
            EndDialogue();
            return;
        }

        EnterNode(node, publishNodeEntered: true);
        if (currentStep == DialogueNodeStep.End)
            EndDialogue();
    }

    public void EndDialogue()
    {
        if (!isRunning)
            return;

        isRunning = false;
        isSuspended = false;
        currentNode = null;
        currentChoiceNode = null;
        currentInputNode = null;
        currentStep = DialogueNodeStep.End;

        DialogueUiCleared?.Invoke();

        if (currentDialogue != null)
            EventCenter.Publish(GameEvents.DialogueEnded, currentDialogue);

        GameContext game = dialogueContext?.Game;
        currentDialogue = null;
        dialogueContext = null;

        if (game?.StateMachine != null &&
            game.StateMachine.CurrentStateType == GameStateType.Dialogue)
        {
            game.StateMachine.ChangeState(GameStateType.Playing);
        }
    }

    private void EnterNode(DialogueNode node, bool publishNodeEntered)
    {
        currentNode = node;
        currentChoiceNode = node as ChoiceNode;
        currentInputNode = node as InputNode;

        if (publishNodeEntered && !string.IsNullOrWhiteSpace(node.nodeId))
            EventCenter.Publish(GameEvents.DialogueNodeEntered, node.nodeId);

        currentStep = node.Enter(dialogueContext);
    }

    private void RepresentCurrentNode()
    {
        if (!isRunning || currentNode == null)
            return;

        switch (currentNode)
        {
            case TextNode textNode:
                PresentText(textNode.speaker, textNode.text);
                break;
            case ChoiceNode choiceNode:
                PresentChoices(choiceNode.options);
                break;
            case InputNode inputNode:
                PresentInput(inputNode.question);
                break;
        }
    }
}
