public class EventContext
{
    public GameContext Game;
    public DialogueContext Dialogue;
    public string SourceNodeId;
    public DialogueNode PendingNextNode;
}

public interface IEventAction
{
    void Execute(EventContext context);
}
