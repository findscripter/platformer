using System.Collections;
using UnityEngine;
using Game.PlayerGuide.Models;
using Game.PlayerGuide.Views;
using PlayerGuide.Views;

namespace Game.PlayerGuide.Controllers
{
    /// <summary>
    /// Frame 6 对话控制器 - 管理 8 段对话流程。
    /// 协调 DialogueView、FeifeiCharacterView 和 DreamCoreView 的显示。
    /// </summary>
    public class Frame6DialogueController : MonoBehaviour
    {
        [Header("View References")]
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private FeifeiCharacterView feifeiCharacterView;
        [SerializeField] private DreamCoreView dreamCoreView;

        [Header("Model & Config")]
        [SerializeField] private DialogueState dialogueState;
        [SerializeField] private Frame6DialogueConfig dialogueConfig;

        [Header("Context")]
        [SerializeField] private GameContext gameContext;

        private Coroutine dialogueCoroutine;

        private void Awake()
        {
            ValidateReferences();
        }

        /// <summary>
        /// 启动对话流程（主入口）。
        /// </summary>
        public void StartDialogue()
        {
            if (dialogueCoroutine != null)
            {
                Debug.LogWarning("Frame6DialogueController: Dialogue already running.");
                return;
            }

            dialogueCoroutine = StartCoroutine(RunDialogue());
        }

        /// <summary>
        /// 停止对话流程。
        /// </summary>
        public void StopDialogue()
        {
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
                dialogueCoroutine = null;
            }

            dialogueState?.Reset();
        }

        /// <summary>
        /// 主对话流程协程。
        /// </summary>
        private IEnumerator RunDialogue()
        {
            // 初始化状态
            if (dialogueState == null)
            {
                dialogueState = new DialogueState(dialogueConfig);
            }
            else
            {
                dialogueState.DialogueConfig = dialogueConfig;
                dialogueState.Reset();
            }

            // 启用 UI 输入模式
            EnableUIInput();

            // 设置动画速度为 0.6
            SetAnimatorSpeed(0.6f);

            // 播放 8 段对话
            for (int i = 0; i < 8; i++)
            {
                Frame6DialogueConfig.DialogueSegment segment = dialogueConfig.GetSegment(i);
                if (segment == null)
                {
                    Debug.LogWarning($"Frame6DialogueController: Segment {i} is null, skipping.");
                    continue;
                }

                yield return PlayDialogueSegment(segment);

                // 推进状态
                dialogueState.Advance();
            }

            // 恢复游戏输入和动画速度
            RestoreGameplayInput();
            SetAnimatorSpeed(1f);

            // 隐藏对话框
            if (dialogueView != null)
            {
                dialogueView.FadeOut();
            }

            dialogueCoroutine = null;
        }

        /// <summary>
        /// 播放单段对话的协程。
        /// </summary>
        /// <param name="segment">对话段数据</param>
        private IEnumerator PlayDialogueSegment(Frame6DialogueConfig.DialogueSegment segment)
        {
            // 更新对话文本
            if (dialogueView != null)
            {
                dialogueView.SetSpeaker("腓腓");
                dialogueView.SetContent(segment.text);

                // 如果对话框尚未显示，淡入显示
                if (!dialogueView.gameObject.activeSelf)
                {
                    dialogueView.FadeIn();
                    yield return new WaitForSeconds(0.3f); // 等待淡入完成
                }
            }

            // 播放腓腓动画（如果指定）
            if (feifeiCharacterView != null && !string.IsNullOrEmpty(segment.feifeiAnimation))
            {
                feifeiCharacterView.PlayAnimation(segment.feifeiAnimation);
            }

            // 更新梦核光效强度（如果指定）
            if (dreamCoreView != null && segment.coreGlowIntensity > 0f)
            {
                dreamCoreView.SetGlowIntensity(segment.coreGlowIntensity);
            }

            // 播放梦核涟漪效果（如果启用）
            if (dreamCoreView != null && segment.playCoreRipple)
            {
                dreamCoreView.PlayRippleEffect();
            }

            // 等待建议时长
            float elapsed = 0f;
            float duration = segment.suggestedDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 检测确认输入以跳过等待
                if (gameContext?.InputManager != null && gameContext.InputManager.ConfirmPressed)
                {
                    break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 启用 UI 输入模式。
        /// </summary>
        private void EnableUIInput()
        {
            if (gameContext?.InputManager != null)
            {
                gameContext.InputManager.EnableUIInput();
            }
        }

        /// <summary>
        /// 恢复游戏输入模式。
        /// </summary>
        private void RestoreGameplayInput()
        {
            if (gameContext?.InputManager != null)
            {
                gameContext.InputManager.EnableGameplayInput();
            }
        }

        /// <summary>
        /// 设置腓腓动画速度。
        /// </summary>
        /// <param name="speed">动画速度倍率</param>
        private void SetAnimatorSpeed(float speed)
        {
            if (feifeiCharacterView != null)
            {
                feifeiCharacterView.SetAnimatorSpeed(speed);
            }
        }

        /// <summary>
        /// 验证必要的引用。
        /// </summary>
        private void ValidateReferences()
        {
            if (dialogueView == null)
            {
                Debug.LogWarning("Frame6DialogueController: dialogueView is not assigned.");
            }

            if (feifeiCharacterView == null)
            {
                Debug.LogWarning("Frame6DialogueController: feifeiCharacterView is not assigned.");
            }

            if (dreamCoreView == null)
            {
                Debug.LogWarning("Frame6DialogueController: dreamCoreView is not assigned.");
            }

            if (dialogueConfig == null)
            {
                Debug.LogWarning("Frame6DialogueController: dialogueConfig is not assigned.");
            }

            if (gameContext == null)
            {
                Debug.LogWarning("Frame6DialogueController: gameContext is not assigned.");
            }
        }
    }
}
