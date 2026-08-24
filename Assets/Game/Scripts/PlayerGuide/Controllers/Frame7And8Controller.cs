using System.Collections;
using UnityEngine;
using TMPro;

namespace Platformer.PlayerGuide
{
    /// <summary>
    /// Handles Frame 7-8: Dream input collection and follow-up dialogue system.
    /// Frame 7: Show input panel and wait for user submission
    /// Frame 8: Validate input, show follow-up dialogue if needed, re-prompt
    /// </summary>
    public class Frame7And8Controller : MonoBehaviour
    {
        [Header("View References")]
        [SerializeField] private DreamInputView dreamInputView;
        [SerializeField] private DialogueView dialogueView;

        [Header("Input Validation")]
        [SerializeField] private int minCharacterCount = 5;
        [SerializeField] private string[] requiredKeywords = { "梦", "想", "愿望", "希望" };
        [SerializeField] private float followUpDisplayDuration = 3f;

        private GameContext context;
        private InputState inputState;
        private bool isWaitingForInput;
        private bool needsFollowUp;

        public void Initialize(GameContext gameContext, InputState state)
        {
            context = gameContext;
            inputState = state;

            if (dreamInputView != null)
            {
                dreamInputView.OnInputSubmitted += HandleInputSubmitted;
            }

            if (dialogueView != null)
            {
                dialogueView.OnDialogueComplete += HandleDialogueComplete;
            }
        }

        private void OnDestroy()
        {
            if (dreamInputView != null)
            {
                dreamInputView.OnInputSubmitted -= HandleInputSubmitted;
            }

            if (dialogueView != null)
            {
                dialogueView.OnDialogueComplete -= HandleDialogueComplete;
            }
        }

        /// <summary>
        /// Frame 7: Show input panel and wait for user to submit their dream
        /// </summary>
        public IEnumerator Frame7_ShowInput()
        {
            Debug.Log("[Frame7] Showing dream input panel");

            // Reset state
            isWaitingForInput = true;
            needsFollowUp = false;
            inputState.ClearInput();

            // Show input view
            if (dreamInputView != null)
            {
                dreamInputView.Show();
                dreamInputView.ClearInput();
            }
            else
            {
                Debug.LogError("[Frame7] DreamInputView is null!");
                yield break;
            }

            // Wait for user to submit input
            while (isWaitingForInput)
            {
                yield return null;
            }

            // Hide input view
            if (dreamInputView != null)
            {
                dreamInputView.Hide();
            }

            Debug.Log($"[Frame7] Input received: {inputState.UserInput}");
        }

        /// <summary>
        /// Frame 8: Check if follow-up is needed, show dialogue, and re-prompt if necessary
        /// </summary>
        public IEnumerator Frame8_HandleFollowUp()
        {
            Debug.Log("[Frame8] Checking if follow-up is needed");

            // Validate input
            needsFollowUp = RequiresFollowUp(inputState.UserInput);

            if (needsFollowUp)
            {
                Debug.Log("[Frame8] Follow-up required, showing dialogue");

                // Show follow-up dialogue
                if (dialogueView != null)
                {
                    string followUpMessage = GenerateFollowUpMessage(inputState.UserInput);
                    dialogueView.Show();
                    dialogueView.SetDialogueText(followUpMessage);

                    // Wait for follow-up display duration
                    yield return new WaitForSeconds(followUpDisplayDuration);

                    dialogueView.Hide();
                }

                // Re-prompt for input
                Debug.Log("[Frame8] Re-prompting for better input");
                yield return Frame7_ShowInput();

                // Recursively check again after re-prompt
                yield return Frame8_HandleFollowUp();
            }
            else
            {
                Debug.Log("[Frame8] Input is valid, proceeding");
                inputState.MarkAsSubmitted();
            }
        }

        /// <summary>
        /// Handle input submission from DreamInputView
        /// </summary>
        private void HandleInputSubmitted(string userInput)
        {
            Debug.Log($"[Frame7And8Controller] Input submitted: {userInput}");

            // Store input
            inputState.SetInput(userInput);

            // Stop waiting
            isWaitingForInput = false;
        }

        /// <summary>
        /// Handle dialogue completion (currently unused, reserved for future)
        /// </summary>
        private void HandleDialogueComplete()
        {
            Debug.Log("[Frame7And8Controller] Dialogue complete");
        }

        /// <summary>
        /// Determine if the input requires a follow-up prompt
        /// </summary>
        private bool RequiresFollowUp(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                Debug.Log("[Validation] Input is empty");
                return true;
            }

            // Check minimum character count
            if (input.Length < minCharacterCount)
            {
                Debug.Log($"[Validation] Input too short: {input.Length} < {minCharacterCount}");
                return true;
            }

            // Check for required keywords
            bool hasKeyword = false;
            foreach (string keyword in requiredKeywords)
            {
                if (input.Contains(keyword))
                {
                    hasKeyword = true;
                    break;
                }
            }

            if (!hasKeyword)
            {
                Debug.Log("[Validation] Missing required keywords");
                return true;
            }

            // Input is valid
            return false;
        }

        /// <summary>
        /// Generate context-specific follow-up message based on validation failure
        /// </summary>
        private string GenerateFollowUpMessage(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "请输入你的梦想...";
            }

            if (input.Length < minCharacterCount)
            {
                return "能再详细一点吗？告诉我更多关于你的梦想...";
            }

            // Missing keywords
            return "你的梦想是什么？试着用"梦想"、"希望"这样的词语来描述...";
        }

        #region Inspector Utilities
#if UNITY_EDITOR
        [ContextMenu("Test Frame 7")]
        private void TestFrame7()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(Frame7_ShowInput());
            }
            else
            {
                Debug.LogWarning("Test Frame 7 requires Play Mode");
            }
        }

        [ContextMenu("Test Frame 8 (Valid Input)")]
        private void TestFrame8Valid()
        {
            if (Application.isPlaying)
            {
                inputState = new InputState();
                inputState.SetInput("我的梦想是成为一名游戏开发者，创造有趣的游戏");
                StartCoroutine(Frame8_HandleFollowUp());
            }
            else
            {
                Debug.LogWarning("Test Frame 8 requires Play Mode");
            }
        }

        [ContextMenu("Test Frame 8 (Invalid Input)")]
        private void TestFrame8Invalid()
        {
            if (Application.isPlaying)
            {
                inputState = new InputState();
                inputState.SetInput("好的");
                StartCoroutine(Frame8_HandleFollowUp());
            }
            else
            {
                Debug.LogWarning("Test Frame 8 requires Play Mode");
            }
        }
#endif
        #endregion

        /// <summary>
        /// Zenject 依赖注入完成后的回调
        /// </summary>
        public void InjectionComplete()
        {
            _logger.Info($"Frame7And8Controller dependencies injected: FlowController={_flowController != null}, DialogueSystem={_dialogueSystem != null}");
        }
    }
}
