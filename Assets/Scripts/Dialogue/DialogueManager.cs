using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace DreamGame.Dialogue
{
    /// <summary>
    /// 对话管理器 - 控制整个对话系统的执行流程
    /// 管理对话节点的执行、UI显示、用户输入
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueContentText;
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private Transform choiceContainer;

        [Header("对话配置")]
        [SerializeField] private DialogueNode startNode;
        [SerializeField] private bool autoStartOnEnable = false;
        [SerializeField] private KeyCode advanceKey = KeyCode.Space;

        [Header("音效")]
        [SerializeField] private AudioSource audioSource;

        // 运行时状态
        private DialogueNode currentNode;
        private bool isTyping = false;
        private bool isWaitingForInput = false;
        private Coroutine typewriterCoroutine;
        private List<GameObject> currentChoiceButtons = new List<GameObject>();

        private void OnEnable()
        {
            if (autoStartOnEnable && startNode != null)
            {
                StartDialogue(startNode);
            }
        }

        private void Update()
        {
            // 处理玩家输入推进对话
            if (isWaitingForInput && Input.GetKeyDown(advanceKey))
            {
                if (isTyping)
                {
                    // 跳过打字机效果
                    SkipTypewriter();
                }
                else
                {
                    // 推进到下一个节点
                    AdvanceDialogue();
                }
            }
        }

        /// <summary>
        /// 开始对话
        /// </summary>
        public void StartDialogue(DialogueNode node)
        {
            if (node == null)
            {
                Debug.LogError("[DialogueManager] Start node is null!");
                return;
            }

            currentNode = node;
            ExecuteCurrentNode();
        }

        /// <summary>
        /// 执行当前节点
        /// </summary>
        private void ExecuteCurrentNode()
        {
            if (currentNode == null)
            {
                EndDialogue();
                return;
            }

            Debug.Log($"[DialogueManager] Executing node: {currentNode.nodeId}");
            currentNode.Execute(this);

            // 如果节点设置为自动推进且已完成，立即进入下一个
            if (currentNode.autoAdvance && currentNode.IsComplete())
            {
                StartCoroutine(AutoAdvanceDelay());
            }
        }

        private IEnumerator AutoAdvanceDelay()
        {
            yield return new WaitForSeconds(0.1f);
            AdvanceDialogue();
        }

        /// <summary>
        /// 推进到下一个节点
        /// </summary>
        public void AdvanceDialogue()
        {
            if (currentNode == null) return;

            var nextNode = currentNode.GetNextNode();
            if (nextNode != null)
            {
                currentNode = nextNode;
                ExecuteCurrentNode();
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        public void EndDialogue()
        {
            Debug.Log("[DialogueManager] Dialogue ended");
            HideUI();
            currentNode = null;
        }

        /// <summary>
        /// 显示文本节点（由TextNode调用）
        /// </summary>
        public void ShowText(TextNode textNode)
        {
            dialoguePanel.SetActive(true);
            choicePanel.SetActive(false);

            speakerNameText.text = textNode.speakerName;

            if (textNode.useTypewriter)
            {
                if (typewriterCoroutine != null)
                {
                    StopCoroutine(typewriterCoroutine);
                }
                typewriterCoroutine = StartCoroutine(TypewriterEffect(textNode));
            }
            else
            {
                dialogueContentText.text = textNode.dialogueText;
                textNode.MarkComplete();
                isWaitingForInput = true;
            }
        }

        /// <summary>
        /// 打字机效果协程
        /// </summary>
        private IEnumerator TypewriterEffect(TextNode textNode)
        {
            isTyping = true;
            isWaitingForInput = false;
            dialogueContentText.text = "";

            float delay = 1f / textNode.typewriterSpeed;
            string fullText = textNode.dialogueText;

            for (int i = 0; i < fullText.Length; i++)
            {
                dialogueContentText.text += fullText[i];

                // 播放文字音效
                if (textNode.textSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(textNode.textSound);
                }

                yield return new WaitForSeconds(delay);
            }

            isTyping = false;
            textNode.MarkComplete();
            isWaitingForInput = true;

            // 播放完成音效
            if (textNode.completeSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(textNode.completeSound);
            }
        }

        /// <summary>
        /// 跳过打字机效果
        /// </summary>
        private void SkipTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            if (currentNode is TextNode textNode)
            {
                dialogueContentText.text = textNode.dialogueText;
                textNode.MarkComplete();
            }

            isTyping = false;
            isWaitingForInput = true;
        }

        /// <summary>
        /// 显示选择节点（由ChoiceNode调用）
        /// </summary>
        public void ShowChoices(ChoiceNode choiceNode)
        {
            dialoguePanel.SetActive(false);
            choicePanel.SetActive(true);

            // 清除旧按钮
            foreach (var btn in currentChoiceButtons)
            {
                Destroy(btn);
            }
            currentChoiceButtons.Clear();

            // 创建选项按钮
            for (int i = 0; i < choiceNode.choices.Count; i++)
            {
                var choice = choiceNode.choices[i];
                var btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                var btn = btnObj.GetComponent<Button>();

                if (btnText != null)
                {
                    btnText.text = choice.text;
                }

                int index = i; // 闭包捕获
                btn.onClick.AddListener(() => OnChoiceSelected(choiceNode, index));
                btn.interactable = choice.isEnabled;

                currentChoiceButtons.Add(btnObj);
            }

            isWaitingForInput = false;
        }

        /// <summary>
        /// 选项被选中
        /// </summary>
        private void OnChoiceSelected(ChoiceNode choiceNode, int index)
        {
            choiceNode.MakeChoice(index);
            AdvanceDialogue();
        }

        /// <summary>
        /// 隐藏所有UI
        /// </summary>
        private void HideUI()
        {
            dialoguePanel.SetActive(false);
            choicePanel.SetActive(false);
        }
    }
}
