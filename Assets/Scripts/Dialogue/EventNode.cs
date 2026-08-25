using UnityEngine;
using UnityEngine.Events;

namespace DreamGame.Dialogue
{
    /// <summary>
    /// 事件节点 - 触发游戏事件，不显示UI
    /// 用于Frame之间的状态切换、音效触发等
    /// </summary>
    [CreateAssetMenu(fileName = "EventNode", menuName = "Dialogue/Event Node")]
    public class EventNode : DialogueNode
    {
        [Header("事件配置")]
        [Tooltip("事件名称（用于调试）")]
        public string eventName;

        [Tooltip("事件触发时调用")]
        public UnityEvent onEventTrigger;

        [Header("延迟设置")]
        [Tooltip("执行延迟（秒）")]
        public float delay = 0f;

        [Tooltip("是否等待延迟完成")]
        public bool waitForDelay = false;

        // 运行时状态
        private bool isComplete = false;

        public override void Execute(DialogueManager manager)
        {
            isComplete = false;

            if (delay <= 0f)
            {
                TriggerEvent();
                if (!waitForDelay)
                {
                    isComplete = true;
                }
            }
            else
            {
                // 延迟触发在Update中处理
                if (manager != null)
                {
                    manager.StartCoroutine(DelayedTrigger());
                }
            }
        }

        private System.Collections.IEnumerator DelayedTrigger()
        {
            yield return new WaitForSeconds(delay);
            TriggerEvent();
            if (waitForDelay)
            {
                isComplete = true;
            }
        }

        private void TriggerEvent()
        {
            Debug.Log($"[EventNode] Triggering event: {eventName}");
            onEventTrigger?.Invoke();
        }

        public override bool IsComplete()
        {
            return isComplete || (!waitForDelay && delay <= 0f);
        }

        public override void Reset()
        {
            base.Reset();
            isComplete = false;
        }
    }
}
