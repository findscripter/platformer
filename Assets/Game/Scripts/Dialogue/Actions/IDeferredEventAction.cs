public interface IDeferredEventAction : IEventAction
{
    bool DeferDialogueAdvance { get; }
}
