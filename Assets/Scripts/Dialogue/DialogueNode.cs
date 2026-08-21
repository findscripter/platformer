using UnityEngine;
using System.Collections.Generic;

namespace DreamGame.Dialogue
{
    /// <summary>
    /// 对话节点基类 - 所有对话节点的抽象基类
    /// 支持节点链接和条件跳转
    /// </summary>
    public abstract class DialogueNode : ScriptableObject
    {
        [Header("节点基础信息")]
        [Tooltip("节点唯一ID，用于调试")]
        public string nodeId;

        [Tooltip("节点描述，便于在Inspector中识别")]
        [TextArea(2, 4)]
        public string description;

        [Header("节点流程")]
        [Tooltip("下一个要执行的节点")]
        public DialogueNode nextNode;

        [Tooltip("是否在执行后自动跳转到下一个节点")]
        public bool autoAdvance = true;

        /// <summary>
        /// 执行节点逻辑 - 由子类实现具体行为
        /// </summary>
        /// <param name="manager">对话管理器引用</param>
        public abstract void Execute(DialogueManager manager);

        /// <summary>
        /// 节点是否已完成执行
        /// </summary>
        public abstract bool IsComplete();

        /// <summary>
        /// 重置节点状态，用于对话重放
        /// </summary>
        public virtual void Reset()
        {
            // 子类可以重写此方法来重置特定状态
        }

        /// <summary>
        /// 获取下一个要执行的节点
        /// </summary>
        public virtual DialogueNode GetNextNode()
        {
            return nextNode;
        }

        protected virtual void OnEnable()
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                nodeId = name;
            }
        }
    }
}
