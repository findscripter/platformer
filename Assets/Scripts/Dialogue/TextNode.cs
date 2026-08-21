using UnityEngine;
using UnityEngine.Events;

namespace DreamGame.Dialogue
{
    /// <summary>
    /// 文本节点 - 显示对话文本，支持打字机效果
    /// 用于Frame 3, 6, 7, 10的对话显示
    /// </summary>
    [CreateAssetMenu(fileName = "TextNode", menuName = "Dialogue/Text Node")]
    public class TextNode : DialogueNode
    {
        [Header("文本内容")]
        [Tooltip("说话角色名称")]
        public string speakerName = "腓腓";

        [Tooltip("对话文本内容")]
        [TextArea(3, 10)]
        public string dialogueText;

        [Header("显示设置")]
        [Tooltip("打字机效果速度（字符/秒）")]
        public float typewriterSpeed = 20f;

        [Tooltip("是否启用打字机效果")]
        public bool useTypewriter = true;

        [Tooltip("显示完成后自动等待时间（秒）")]
        public float autoWaitTime = 0f;

        [Header("音效")]
        [Tooltip("文字显示音效")]
        public AudioClip textSound;

        [Tooltip("对话完成音效")]
        public AudioClip completeSound;

        [Header("事件")]
        [Tooltip("文本开始显示时触发")]
        public UnityEvent onTextStart;

        [Tooltip("文本显示完成时触发")]
        public UnityEvent onTextComplete;

        // 运行时状态
        private bool isComplete = false;

        public override void Execute(DialogueManager manager)
        {
            isComplete = false;
            onTextStart?.Invoke();

            if (manager != null)
            {
                manager.ShowText(this);
            }
        }

        public override bool IsComplete()
        {
            return isComplete;
        }

        public void MarkComplete()
        {
            isComplete = true;
            onTextComplete?.Invoke();
        }

        public override void Reset()
        {
            base.Reset();
            isComplete = false;
        }
    }
}
