/// <summary>
/// Frame 7-8 写梦输入状态。
/// 对应 PlayerGuideFlowControllerV2 中的 isInFollowUpState 及 GameContext.PlayerDreamInput。
/// </summary>
public class InputState
{
    public bool IsInFollowUpState { get; set; }

    public void Reset()
    {
        IsInFollowUpState = false;
    }
}
