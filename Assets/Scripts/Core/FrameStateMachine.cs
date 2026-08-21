using UnityEngine;

namespace DreamGame.Core
{
    /// <summary>
    /// Frame状态枚举 - 对应10个Frame的完整流程
    /// </summary>
    public enum FrameState
    {
        None = 0,           // 初始状态
        Frame1_DreamBubble = 1,     // 梦泡漂浮
        Frame2_BubbleClick = 2,     // 点击梦泡
        Frame3_PhiPhiIntro = 3,     // 腓腓自我介绍
        Frame4_EnterWorld = 4,      // 进入梦境世界
        Frame5_MainScene = 5,       // 主场景探索
        Frame6_FirstDialogue = 6,   // 第一次对话
        Frame7_TarotPrompt = 7,     // 塔罗牌提示
        Frame8_TarotChoice = 8,     // 选择塔罗牌
        Frame9_LevelTransition = 9, // 关卡过渡
        Frame10_LevelEcho = 10      // 关卡回响
    }

    /// <summary>
    /// Frame状态机 - 管理游戏整体流程
    /// 扩展自原有状态机，增加Frame相关逻辑
    /// </summary>
    public class FrameStateMachine : MonoBehaviour
    {
        [Header("当前状态")]
        [SerializeField] private FrameState currentFrame = FrameState.None;

        [Header("调试")]
        [SerializeField] private bool enableDebugLog = true;

        // 事件：Frame切换时触发
        public System.Action<FrameState, FrameState> OnFrameChanged;

        private void Start()
        {
            // 游戏开始时自动进入Frame1
            TransitionToFrame(FrameState.Frame1_DreamBubble);
        }

        /// <summary>
        /// 切换到指定Frame
        /// </summary>
        public void TransitionToFrame(FrameState targetFrame)
        {
            if (currentFrame == targetFrame)
            {
                LogDebug($"Already in {targetFrame}, skipping transition");
                return;
            }

            FrameState previousFrame = currentFrame;
            currentFrame = targetFrame;

            LogDebug($"Frame transition: {previousFrame} -> {targetFrame}");

            // 触发切换事件
            OnFrameChanged?.Invoke(previousFrame, targetFrame);

            // 执行Frame进入逻辑
            OnEnterFrame(targetFrame);
        }

        /// <summary>
        /// Frame进入时的逻辑
        /// </summary>
        private void OnEnterFrame(FrameState frame)
        {
            switch (frame)
            {
                case FrameState.Frame1_DreamBubble:
                    OnEnterFrame1();
                    break;
                case FrameState.Frame2_BubbleClick:
                    OnEnterFrame2();
                    break;
                case FrameState.Frame3_PhiPhiIntro:
                    OnEnterFrame3();
                    break;
                case FrameState.Frame4_EnterWorld:
                    OnEnterFrame4();
                    break;
                case FrameState.Frame5_MainScene:
                    OnEnterFrame5();
                    break;
                case FrameState.Frame6_FirstDialogue:
                    OnEnterFrame6();
                    break;
                case FrameState.Frame7_TarotPrompt:
                    OnEnterFrame7();
                    break;
                case FrameState.Frame8_TarotChoice:
                    OnEnterFrame8();
                    break;
                case FrameState.Frame9_LevelTransition:
                    OnEnterFrame9();
                    break;
                case FrameState.Frame10_LevelEcho:
                    OnEnterFrame10();
                    break;
            }
        }

        #region Frame进入逻辑

        private void OnEnterFrame1()
        {
            LogDebug("Frame1: 显示梦泡漂浮动画");
            // TODO: 播放梦泡漂浮动画
        }

        private void OnEnterFrame2()
        {
            LogDebug("Frame2: 等待玩家点击梦泡");
            // TODO: 激活梦泡点击检测
        }

        private void OnEnterFrame3()
        {
            LogDebug("Frame3: 触发腓腓自我介绍对话");
            // TODO: 启动对话系统，播放intro对话
        }

        private void OnEnterFrame4()
        {
            LogDebug("Frame4: 进入梦境世界过渡");
            // TODO: 播放场景过渡动画
        }

        private void OnEnterFrame5()
        {
            LogDebug("Frame5: 加载主场景，启用玩家控制");
            // TODO: 加载主场景，激活角色控制器
        }

        private void OnEnterFrame6()
        {
            LogDebug("Frame6: 触发第一次对话");
            // TODO: 启动对话系统
        }

        private void OnEnterFrame7()
        {
            LogDebug("Frame7: 显示塔罗牌提示");
            // TODO: 显示塔罗牌UI
        }

        private void OnEnterFrame8()
        {
            LogDebug("Frame8: 激活塔罗牌选择");
            // TODO: 显示三张塔罗牌，等待玩家选择
        }

        private void OnEnterFrame9()
        {
            LogDebug("Frame9: 关卡过渡");
            // TODO: 根据塔罗牌选择，过渡到对应关卡
        }

        private void OnEnterFrame10()
        {
            LogDebug("Frame10: 关卡回响");
            // TODO: 显示关卡回响UI
        }

        #endregion

        /// <summary>
        /// 推进到下一个Frame
        /// </summary>
        public void AdvanceToNextFrame()
        {
            int nextFrameInt = (int)currentFrame + 1;
            if (nextFrameInt <= (int)FrameState.Frame10_LevelEcho)
            {
                TransitionToFrame((FrameState)nextFrameInt);
            }
            else
            {
                LogDebug("已完成所有Frame，游戏流程结束");
            }
        }

        /// <summary>
        /// 获取当前Frame
        /// </summary>
        public FrameState GetCurrentFrame()
        {
            return currentFrame;
        }

        /// <summary>
        /// 跳转到指定Frame（调试用）
        /// </summary>
        public void JumpToFrame(int frameNumber)
        {
            if (frameNumber >= 1 && frameNumber <= 10)
            {
                TransitionToFrame((FrameState)frameNumber);
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[FrameStateMachine] {message}");
            }
        }

        #region Inspector调试按钮

        [ContextMenu("下一个Frame")]
        private void DebugNextFrame()
        {
            AdvanceToNextFrame();
        }

        [ContextMenu("跳转到Frame 3")]
        private void DebugJumpToFrame3()
        {
            TransitionToFrame(FrameState.Frame3_PhiPhiIntro);
        }

        [ContextMenu("跳转到Frame 8")]
        private void DebugJumpToFrame8()
        {
            TransitionToFrame(FrameState.Frame8_TarotChoice);
        }

        #endregion
    }
}
