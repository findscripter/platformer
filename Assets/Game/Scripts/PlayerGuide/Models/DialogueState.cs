using UnityEngine;

namespace Game.PlayerGuide.Models
{
    /// <summary>
    /// Frame 6 对话跟踪状态模型。
    /// 管理当前对话段索引、等待推进标志和配置引用。
    /// </summary>
    [System.Serializable]
    public class DialogueState
    {
        [Header("当前进度")]
        [SerializeField]
        [Tooltip("当前对话段索引 (0-7)")]
        private int currentSegmentIndex;

        [Header("控制标志")]
        [SerializeField]
        [Tooltip("是否等待玩家点击以推进对话")]
        private bool isWaitingForAdvance;

        [Header("配置引用")]
        [SerializeField]
        [Tooltip("Frame 6 对话配置资源")]
        private Frame6DialogueConfig dialogueConfig;

        /// <summary>
        /// 当前对话段索引 (0-7)。
        /// </summary>
        public int CurrentSegmentIndex
        {
            get => currentSegmentIndex;
            set => currentSegmentIndex = Mathf.Clamp(value, 0, MaxSegmentIndex);
        }

        /// <summary>
        /// 是否等待玩家推进对话（用于手动模式）。
        /// </summary>
        public bool IsWaitingForAdvance
        {
            get => isWaitingForAdvance;
            set => isWaitingForAdvance = value;
        }

        /// <summary>
        /// Frame 6 对话配置引用。
        /// </summary>
        public Frame6DialogueConfig DialogueConfig
        {
            get => dialogueConfig;
            set => dialogueConfig = value;
        }

        /// <summary>
        /// 当前段是否为最后一段。
        /// </summary>
        public bool IsLastSegment => currentSegmentIndex >= MaxSegmentIndex;

        /// <summary>
        /// 对话是否已完成（超出最大索引）。
        /// </summary>
        public bool IsCompleted => currentSegmentIndex > MaxSegmentIndex;

        /// <summary>
        /// 最大段索引（基于配置）。
        /// </summary>
        private int MaxSegmentIndex => dialogueConfig != null ? dialogueConfig.SegmentCount - 1 : 7;

        /// <summary>
        /// 构造函数 - 初始化状态。
        /// </summary>
        public DialogueState()
        {
            currentSegmentIndex = 0;
            isWaitingForAdvance = false;
            dialogueConfig = null;
        }

        /// <summary>
        /// 构造函数 - 带配置初始化。
        /// </summary>
        /// <param name="config">对话配置</param>
        public DialogueState(Frame6DialogueConfig config)
        {
            currentSegmentIndex = 0;
            isWaitingForAdvance = false;
            dialogueConfig = config;
        }

        /// <summary>
        /// 重置状态到初始值。
        /// </summary>
        public void Reset()
        {
            currentSegmentIndex = 0;
            isWaitingForAdvance = false;
        }

        /// <summary>
        /// 推进到下一段对话。
        /// </summary>
        /// <returns>推进是否成功（未达到末尾）</returns>
        public bool Advance()
        {
            if (IsCompleted)
                return false;

            currentSegmentIndex++;
            isWaitingForAdvance = false;
            return true;
        }

        /// <summary>
        /// 获取当前对话段数据。
        /// </summary>
        /// <returns>当前段数据，如果无效则返回 null</returns>
        public Frame6DialogueConfig.DialogueSegment GetCurrentSegment()
        {
            if (dialogueConfig == null)
                return null;

            return dialogueConfig.GetSegment(currentSegmentIndex);
        }

        /// <summary>
        /// 验证状态是否有效（配置已设置）。
        /// </summary>
        public bool IsValid()
        {
            return dialogueConfig != null;
        }
    }
}
