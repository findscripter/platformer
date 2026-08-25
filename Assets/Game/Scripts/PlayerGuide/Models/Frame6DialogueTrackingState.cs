/// <summary>
/// Frame 6 对话跟踪状态（与 GameStateMachine 的 DialogueState/IGameState 是两回事，
/// 后者是"进入独立对话场景"这个游戏级状态，本类是 PlayerGuide 内部 Frame 6 的对话推进记录）。
/// 对应 PlayerGuideFlowControllerV2 中的 currentDialogueIndex / isWaitingForDialogueAdvance。
/// </summary>
public class Frame6DialogueTrackingState
{
    public int CurrentDialogueIndex { get; set; }
    public bool IsWaitingForDialogueAdvance { get; set; }

    public void Reset()
    {
        CurrentDialogueIndex = 0;
        IsWaitingForDialogueAdvance = false;
    }
}
