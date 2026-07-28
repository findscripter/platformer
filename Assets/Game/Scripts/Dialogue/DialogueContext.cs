public enum DialogueNodeStep
{
    Waiting,
    AutoAdvance,
    End
}

public class DialogueContext
{
    public GameContext Game { get; }
    public DialogueManager Manager { get; }
    public DialogueSO Dialogue { get; }

    public DialogueContext(GameContext game, DialogueManager manager, DialogueSO dialogue)
    {
        Game = game;
        Manager = manager;
        Dialogue = dialogue;
    }
}
