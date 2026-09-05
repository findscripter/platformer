/// <summary>
/// 引导流程的 10 个主要帧状态。
/// 与 PlayerGuideFlowControllerV2.GuideFrame 保持一致。
/// </summary>
public enum GuideFrame
{
    Frame1_DreamSpaceGeneration,    // 梦境空间生成
    Frame2_MainBubbleAppear,        // 主梦泡可点击
    Frame3_DreamEchoPlay,           // 梦境残响播放
    Frame4_BubbleDissolve,          // 梦泡消散留下梦核
    Frame5_FeifeiEnterWithCore,     // 腓腓入场捧梦核
    Frame6_FirstMeetingDialogue,    // 第一次相遇对白（6-1 至 6-8）
    Frame7_DreamInput,              // 输入梦
    Frame8_WriteDreamAndFollowUp,   // 写梦与必要补问（双状态）
    Frame9_TarotDrawing,            // 塔罗抽牌（状态 A-G）
    Frame10_CoreResponseAndEnd      // 关卡结束与梦核回应
}

/// <summary>
/// PlayerGuide 帧状态机运行时数据（与 GameStateMachine 的 PlayerGuideState/IGameState 是两回事，
/// 后者是"进入引导场景"这个游戏级状态，本类是引导内部 10 帧子状态机的当前帧记录）。
/// </summary>
public class PlayerGuideFrameState
{
    public GuideFrame CurrentFrame { get; set; } = GuideFrame.Frame1_DreamSpaceGeneration;
}
