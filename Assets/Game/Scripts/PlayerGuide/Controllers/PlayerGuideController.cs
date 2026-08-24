using System.Collections;
using UnityEngine;
using PlatformerGame.PlayerGuide.Models;
using Game.PlayerGuide.Models;
using Game.PlayerGuide.Controllers;
using Platformer.PlayerGuide;

namespace PlatformerGame.PlayerGuide.Controllers
{
    /// <summary>
    /// Main PlayerGuide state machine controller.
    /// Manages the 10-frame tutorial flow, coordinates all sub-controllers,
    /// and handles frame transitions.
    /// Replaces PlayerGuideFlowControllerV2.
    /// </summary>
    public class PlayerGuideController : MonoBehaviour
    {
        #region Inspector References

        [Header("View")]
        [SerializeField] private PlayerGuideView view;

        [Header("Context")]
        [SerializeField] private GameContext gameContext;

        [Header("Specialized Controllers")]
        [SerializeField] private Frame6DialogueController frame6DialogueController;
        [SerializeField] private Frame7And8Controller frame7InputController;
        [SerializeField] private TarotDrawController frame9TarotController;

        #endregion

        #region State Models

        private PlayerGuideState guideState;
        private DialogueState dialogueState;
        private InputState inputState;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            ValidateReferences();
            InitializeModels();
        }

        private void Start()
        {
            InitializePanels();
            StartCoroutine(RunTutorialFlow());
        }

        private void Update()
        {
            // Monitor frame-specific conditions
            if (guideState == null || guideState.IsFrameTransitioning)
                return;

            // Check for frame advancement conditions
            if (guideState.IsFrameConditionMet() && !guideState.IsFrameTransitioning)
            {
                // Specific frames handle their own transitions
                switch (guideState.CurrentFrame)
                {
                    case GuideFrame.Frame6_DialogueIntro:
                    case GuideFrame.Frame7_ContinueDialogue:
                    case GuideFrame.Frame8_FollowUp:
                    case GuideFrame.Frame9_ReachGoal:
                        // These frames are managed by their controllers
                        break;

                    default:
                        // Auto-advance for simple frames
                        StartCoroutine(TransitionToNextFrame());
                        break;
                }
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Validate all required references.
        /// </summary>
        private void ValidateReferences()
        {
            if (view == null)
            {
                Debug.LogError("PlayerGuideController: PlayerGuideView is not assigned!");
            }

            if (gameContext == null)
            {
                Debug.LogWarning("PlayerGuideController: GameContext is not assigned. Attempting to find it.");
                // GameContext is typically injected by GameLoop, but fallback to Find
                gameContext = Object.FindFirstObjectByType<GameLoop>()?.GetComponent<GameLoop>()?.GameContext;
            }

            if (frame6DialogueController == null)
            {
                Debug.LogWarning("PlayerGuideController: Frame6DialogueController is not assigned.");
            }

            if (frame7InputController == null)
            {
                Debug.LogWarning("PlayerGuideController: Frame7And8Controller is not assigned.");
            }

            if (frame9TarotController == null)
            {
                Debug.LogWarning("PlayerGuideController: TarotDrawController is not assigned.");
            }
        }

        /// <summary>
        /// Initialize all state models.
        /// </summary>
        private void InitializeModels()
        {
            guideState = new PlayerGuideState();
            dialogueState = new DialogueState(view?.Frame6Config);
            inputState = new InputState();
        }

        /// <summary>
        /// Initialize all UI panels to their default states.
        /// </summary>
        private void InitializePanels()
        {
            if (view == null)
                return;

            // Hide all panels initially
            if (view.DialoguePanel != null)
                view.DialoguePanel.SetActive(false);

            if (view.InputPanel != null)
                view.InputPanel.SetActive(false);

            if (view.TarotPanel != null)
                view.TarotPanel.SetActive(false);

            // Set initial canvas group alphas
            if (view.DialogueCanvasGroup != null)
                view.DialogueCanvasGroup.alpha = 0f;

            if (view.InputCanvasGroup != null)
                view.InputCanvasGroup.alpha = 0f;

            if (view.BackgroundCanvasGroup != null)
                view.BackgroundCanvasGroup.alpha = 1f;
        }

        #endregion

        #region Main Tutorial Flow

        /// <summary>
        /// Run the complete 10-frame tutorial flow.
        /// </summary>
        private IEnumerator RunTutorialFlow()
        {
            Debug.Log("PlayerGuideController: Starting tutorial flow");

            for (int frame = 1; frame <= 10; frame++)
            {
                guideState.CurrentFrame = (GuideFrame)frame;

                yield return TransitionToFrame(guideState.CurrentFrame);

                // Wait for frame completion
                yield return new WaitUntil(() => guideState.IsFrameConditionMet());
            }

            guideState.IsTutorialComplete = true;
            Debug.Log("PlayerGuideController: Tutorial complete");

            // Transition to main game
            yield return TransitionToMainGame();
        }

        #endregion

        #region Frame Transition

        /// <summary>
        /// Transition to the next sequential frame.
        /// </summary>
        private IEnumerator TransitionToNextFrame()
        {
            guideState.IsFrameTransitioning = true;

            GuideFrame nextFrame = (GuideFrame)((int)guideState.CurrentFrame + 1);
            if (nextFrame <= GuideFrame.Frame10_Complete)
            {
                yield return TransitionToFrame(nextFrame);
                guideState.CurrentFrame = nextFrame;
            }

            guideState.IsFrameTransitioning = false;
        }

        /// <summary>
        /// Main frame routing and execution logic.
        /// </summary>
        /// <param name="targetFrame">The frame to transition to</param>
        private IEnumerator TransitionToFrame(GuideFrame targetFrame)
        {
            Debug.Log($"PlayerGuideController: Transitioning to {targetFrame}");

            // Cleanup previous frame
            CleanupPreviousFrame(guideState.CurrentFrame);

            // Route to frame-specific logic
            switch (targetFrame)
            {
                case GuideFrame.Frame1_Welcome:
                    yield return Frame1_Welcome();
                    break;

                case GuideFrame.Frame2_Movement:
                    yield return Frame2_Movement();
                    break;

                case GuideFrame.Frame3_Jump:
                    yield return Frame3_Jump();
                    break;

                case GuideFrame.Frame4_Interaction:
                    yield return Frame4_Interaction();
                    break;

                case GuideFrame.Frame5_CollectCoin:
                    yield return Frame5_CollectCoin();
                    break;

                case GuideFrame.Frame6_DialogueIntro:
                    yield return Frame6_DialogueIntro();
                    break;

                case GuideFrame.Frame7_ContinueDialogue:
                    yield return Frame7_ContinueDialogue();
                    break;

                case GuideFrame.Frame8_FollowUp:
                    yield return Frame8_FollowUp();
                    break;

                case GuideFrame.Frame9_ReachGoal:
                    yield return Frame9_ReachGoal();
                    break;

                case GuideFrame.Frame10_Complete:
                    yield return Frame10_Complete();
                    break;

                default:
                    Debug.LogWarning($"PlayerGuideController: Unhandled frame {targetFrame}");
                    break;
            }
        }

        /// <summary>
        /// Cleanup resources and state from the previous frame.
        /// </summary>
        /// <param name="previousFrame">The frame being exited</param>
        private void CleanupPreviousFrame(GuideFrame previousFrame)
        {
            switch (previousFrame)
            {
                case GuideFrame.Frame6_DialogueIntro:
                    // Stop any running dialogue
                    if (frame6DialogueController != null)
                    {
                        frame6DialogueController.StopDialogue();
                    }
                    break;

                case GuideFrame.Frame7_ContinueDialogue:
                case GuideFrame.Frame8_FollowUp:
                    // Hide input panel
                    if (view?.InputPanel != null)
                    {
                        view.InputPanel.SetActive(false);
                    }
                    break;

                case GuideFrame.Frame9_ReachGoal:
                    // Hide tarot panel
                    if (view?.TarotPanel != null)
                    {
                        view.TarotPanel.SetActive(false);
                    }
                    break;
            }
        }

        #endregion

        #region Frame-Specific Logic

        private IEnumerator Frame1_Welcome()
        {
            // Display welcome bubble
            if (view?.MainBubbleVisual != null)
            {
                // Show main bubble with welcome message
                view.MainBubbleVisual.gameObject.SetActive(true);
            }

            // Wait for configured duration or button press
            float elapsed = 0f;
            while (elapsed < view.Frame1Duration)
            {
                if (gameContext?.InputManager != null && gameContext.InputManager.ConfirmPressed)
                {
                    break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            guideState.AdvanceFrame();
        }

        private IEnumerator Frame2_Movement()
        {
            // Wait for player to move left and right
            while (!guideState.HasMovedLeft || !guideState.HasMovedRight)
            {
                // Monitor input (handled by external input system)
                yield return null;
            }

            guideState.AdvanceFrame();
        }

        private IEnumerator Frame3_Jump()
        {
            // Wait for player to jump
            while (!guideState.HasJumped)
            {
                yield return null;
            }

            yield return new WaitForSeconds(view.Frame3Duration);
            guideState.AdvanceFrame();
        }

        private IEnumerator Frame4_Interaction()
        {
            // Wait for player to interact
            while (!guideState.HasInteracted)
            {
                yield return null;
            }

            guideState.AdvanceFrame();
        }

        private IEnumerator Frame5_CollectCoin()
        {
            // Wait for player to collect coin
            while (!guideState.HasCollectedCoin)
            {
                yield return null;
            }

            guideState.AdvanceFrame();
        }

        private IEnumerator Frame6_DialogueIntro()
        {
            if (frame6DialogueController != null)
            {
                frame6DialogueController.StartDialogue();

                // Wait for dialogue to complete
                while (!guideState.DialogueWasTriggered)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("PlayerGuideController: Frame6DialogueController is missing!");
                guideState.DialogueWasTriggered = true;
            }

            guideState.AdvanceFrame();
        }

        private IEnumerator Frame7_ContinueDialogue()
        {
            if (frame7InputController != null)
            {
                yield return frame7InputController.Frame7_ShowInput();

                // Store input in GameContext
                if (gameContext != null && inputState != null)
                {
                    gameContext.PlayerDreamInput = inputState.DreamInputText;
                }
            }
            else
            {
                Debug.LogWarning("PlayerGuideController: Frame7And8Controller is missing!");
            }

            guideState.HasCompletedDialogue = true;
            guideState.AdvanceFrame();
        }

        private IEnumerator Frame8_FollowUp()
        {
            if (frame7InputController != null)
            {
                yield return frame7InputController.Frame8_HandleFollowUp();
            }
            else
            {
                Debug.LogWarning("PlayerGuideController: Frame7And8Controller is missing!");
            }

            guideState.FollowUpDialogueCompleted = true;
            guideState.AdvanceFrame();
        }

        private IEnumerator Frame9_ReachGoal()
        {
            if (frame9TarotController != null)
            {
                // Tarot controller has its own internal state machine
                // Wait for it to complete (monitored via HasReachedGoal flag)
                while (!guideState.HasReachedGoal)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("PlayerGuideController: TarotDrawController is missing!");
                guideState.HasReachedGoal = true;
            }

            guideState.AdvanceFrame();
        }

        private IEnumerator Frame10_Complete()
        {
            // Final completion state
            guideState.IsTutorialComplete = true;
            yield return null;
        }

        #endregion

        #region Game Transition

        /// <summary>
        /// Transition from PlayerGuide to the main gameplay.
        /// </summary>
        private IEnumerator TransitionToMainGame()
        {
            // Fade out
            if (view?.BackgroundCanvasGroup != null)
            {
                float elapsed = 0f;
                float duration = view.FadeOutDuration;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    view.BackgroundCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    yield return null;
                }
            }

            // Trigger scene transition via GameContext
            if (gameContext?.StateMachine != null)
            {
                // Transition to Loading state, which will load the first gameplay level
                gameContext.StateMachine.ChangeState(new LoadingState());
            }
            else
            {
                Debug.LogError("PlayerGuideController: Cannot transition - GameContext or StateMachine is null!");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Mark player movement as completed (called by external input system).
        /// </summary>
        public void OnPlayerMovedLeft()
        {
            if (guideState != null)
            {
                guideState.HasMovedLeft = true;
            }
        }

        /// <summary>
        /// Mark player movement as completed (called by external input system).
        /// </summary>
        public void OnPlayerMovedRight()
        {
            if (guideState != null)
            {
                guideState.HasMovedRight = true;
            }
        }

        /// <summary>
        /// Mark player jump as completed (called by external input system).
        /// </summary>
        public void OnPlayerJumped()
        {
            if (guideState != null)
            {
                guideState.HasJumped = true;
            }
        }

        /// <summary>
        /// Mark player interaction as completed (called by external input system).
        /// </summary>
        public void OnPlayerInteracted()
        {
            if (guideState != null)
            {
                guideState.HasInteracted = true;
            }
        }

        /// <summary>
        /// Mark coin collection as completed (called by external collectible system).
        /// </summary>
        public void OnCoinCollected()
        {
            if (guideState != null)
            {
                guideState.HasCollectedCoin = true;
            }
        }

        /// <summary>
        /// Mark dialogue as started (called by Frame6DialogueController).
        /// </summary>
        public void OnDialogueStarted()
        {
            if (guideState != null)
            {
                guideState.HasStartedDialogue = true;
                guideState.DialogueWasTriggered = true;
            }
        }

        /// <summary>
        /// Mark goal as reached (called by Frame9TarotController).
        /// </summary>
        public void OnGoalReached()
        {
            if (guideState != null)
            {
                guideState.HasReachedGoal = true;
            }
        }

        /// <summary>
        /// Get the current tutorial state for external systems.
        /// </summary>
        public PlayerGuideState GetState()
        {
            return guideState;
        }

        #endregion
    }
}
