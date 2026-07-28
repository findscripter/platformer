using UnityEngine;

public readonly struct InteractRecord
{
    public readonly string InteractableId;
    public readonly string InteractableType;
    public readonly Vector3 PlayerPosition;

    public InteractRecord(string interactableId, string interactableType, Vector3 playerPosition)
    {
        InteractableId = interactableId;
        InteractableType = interactableType;
        PlayerPosition = playerPosition;
    }
}

public readonly struct DialogueChoiceResult
{
    public readonly string DialogueId;
    public readonly string NodeId;
    public readonly int ChoiceIndex;

    public DialogueChoiceResult(string dialogueId, string nodeId, int choiceIndex)
    {
        DialogueId = dialogueId;
        NodeId = nodeId;
        ChoiceIndex = choiceIndex;
    }
}

public readonly struct DialogueInputResult
{
    public readonly string DialogueId;
    public readonly string NodeId;
    public readonly bool Success;

    public DialogueInputResult(string dialogueId, string nodeId, bool success)
    {
        DialogueId = dialogueId;
        NodeId = nodeId;
        Success = success;
    }
}
