using UnityEngine;
using System.Collections.Generic;

namespace DreamGame.Dialogue
{
    /// <summary>
    /// 选择节点 - 显示多个选项供玩家选择
    /// 用于Frame 8的塔罗牌三选一
    /// </summary>
    [CreateAssetMenu(fileName = "ChoiceNode", menuName = "Dialogue/Choice Node")]
    public class ChoiceNode : DialogueNode
    {
        [System.Serializable]
        public class Choice
        {
            [Tooltip("选项文本")]
            public string text;

            [Tooltip("选择此项后跳转的节点")]
            public DialogueNode targetNode;

            [Tooltip("选项图标（可选）")]
            public Sprite icon;

            [Tooltip("选项是否可用")]
            public bool isEnabled = true;

            [Tooltip("选项的条件ID（用于保存选择结果）")]
            public string conditionId;
        }

        [Header("选择内容")]
        [Tooltip("提示文本")]
        [TextArea(2, 4)]
        public string promptText;

        [Tooltip("所有可选项")]
        public List<Choice> choices = new List<Choice>();

        [Header("显示设置")]
        [Tooltip("选择超时时间（秒，0表示无限等待）")]
        public float timeoutSeconds = 0f;

        [Tooltip("超时后的默认选项索引")]
        public int defaultChoiceIndex = 0;

        // 运行时状态
        private bool isComplete = false;
        private int selectedIndex = -1;

        public override void Execute(DialogueManager manager)
        {
            isComplete = false;
            selectedIndex = -1;

            if (manager != null)
            {
                manager.ShowChoices(this);
            }
        }

        public override bool IsComplete()
        {
            return isComplete;
        }

        /// <summary>
        /// 玩家做出选择
        /// </summary>
        public void MakeChoice(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                Debug.LogError($"Invalid choice index: {choiceIndex}");
                return;
            }

            selectedIndex = choiceIndex;
            isComplete = true;

            // 保存选择结果（如果有conditionId）
            var choice = choices[choiceIndex];
            if (!string.IsNullOrEmpty(choice.conditionId))
            {
                // 这里可以对接到游戏状态管理系统
                PlayerPrefs.SetString(choice.conditionId, "true");
            }
        }

        public override DialogueNode GetNextNode()
        {
            if (selectedIndex >= 0 && selectedIndex < choices.Count)
            {
                return choices[selectedIndex].targetNode;
            }
            return nextNode;
        }

        public override void Reset()
        {
            base.Reset();
            isComplete = false;
            selectedIndex = -1;
        }
    }
}
