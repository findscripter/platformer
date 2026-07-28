public static class GameEvents
{
    public static readonly GameEvent<IInteractable> InteractionFocusChanged = new("InteractionFocusChanged");
    public static readonly GameEvent<InteractRecord> InteractStarted = new("InteractStarted");

    public static readonly GameEvent<DialogueSO> DialogueStarted = new("DialogueStarted");
    public static readonly GameEvent<string> DialogueNodeEntered = new("DialogueNodeEntered");
    public static readonly GameEvent<DialogueChoiceResult> DialogueChoice = new("DialogueChoice");
    public static readonly GameEvent<DialogueInputResult> DialogueInputResult = new("DialogueInputResult");
    public static readonly GameEvent<DialogueSO> DialogueEnded = new("DialogueEnded");

    public static readonly GameEvent<string> OpenDoor = new("OpenDoor");
    public static readonly GameEvent<string> MechanismActivated = new("MechanismActivated");
}
