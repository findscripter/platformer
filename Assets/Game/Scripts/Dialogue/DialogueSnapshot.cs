public readonly struct DialogueSnapshot
{
    public DialogueSnapshot(string dialogueId, string nodeId, string waitKind)
    {
        DialogueId = dialogueId ?? string.Empty;
        NodeId = nodeId ?? string.Empty;
        WaitKind = waitKind ?? string.Empty;
    }

    public string DialogueId { get; }
    public string NodeId { get; }
    public string WaitKind { get; }
    public bool HasActiveDialogue => !string.IsNullOrWhiteSpace(DialogueId) && !string.IsNullOrWhiteSpace(NodeId);
}

public static class DialogueWaitKinds
{
    public const string Text = "Text";
    public const string Choice = "Choice";
    public const string Input = "Input";
}
