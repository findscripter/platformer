using UnityEngine;

namespace PlatformerGame.PlayerGuide.Models
{
    /// <summary>
    /// Defines the 10 sequential frames of the player guide tutorial.
    /// </summary>
    public enum GuideFrame
    {
        Frame1_Welcome = 1,
        Frame2_Movement = 2,
        Frame3_Jump = 3,
        Frame4_Interaction = 4,
        Frame5_CollectCoin = 5,
        Frame6_DialogueIntro = 6,
        Frame7_ContinueDialogue = 7,
        Frame8_FollowUp = 8,
        Frame9_ReachGoal = 9,
        Frame10_Complete = 10
    }

    /// <summary>
    /// Holds the runtime state for the PlayerGuide tutorial system.
    /// Tracks current frame, progress flags, and frame-specific state data.
    /// </summary>
    public class PlayerGuideState
    {
        // Current frame
        public GuideFrame CurrentFrame { get; set; } = GuideFrame.Frame1_Welcome;

        // Progress tracking flags
        public bool HasMovedLeft { get; set; }
        public bool HasMovedRight { get; set; }
        public bool HasJumped { get; set; }
        public bool HasInteracted { get; set; }
        public bool HasCollectedCoin { get; set; }
        public bool HasStartedDialogue { get; set; }
        public bool HasCompletedDialogue { get; set; }
        public bool HasReachedGoal { get; set; }

        // Frame 6: Dialogue waiting state
        public bool IsWaitingForDialogueStart { get; set; }
        public bool DialogueWasTriggered { get; set; }

        // Frame 8: Follow-up dialogue state
        public bool IsWaitingForFollowUpDialogue { get; set; }
        public bool FollowUpDialogueCompleted { get; set; }

        // General state flags
        public bool IsFrameTransitioning { get; set; }
        public bool IsTutorialComplete { get; set; }

        /// <summary>
        /// Advances to the next frame in sequence.
        /// </summary>
        public void AdvanceFrame()
        {
            if (CurrentFrame < GuideFrame.Frame10_Complete)
            {
                CurrentFrame = (GuideFrame)((int)CurrentFrame + 1);
            }
        }

        /// <summary>
        /// Resets all progress flags and state to initial values.
        /// </summary>
        public void Reset()
        {
            CurrentFrame = GuideFrame.Frame1_Welcome;

            HasMovedLeft = false;
            HasMovedRight = false;
            HasJumped = false;
            HasInteracted = false;
            HasCollectedCoin = false;
            HasStartedDialogue = false;
            HasCompletedDialogue = false;
            HasReachedGoal = false;

            IsWaitingForDialogueStart = false;
            DialogueWasTriggered = false;
            IsWaitingForFollowUpDialogue = false;
            FollowUpDialogueCompleted = false;

            IsFrameTransitioning = false;
            IsTutorialComplete = false;
        }

        /// <summary>
        /// Checks if the current frame's conditions are met for advancement.
        /// </summary>
        public bool IsFrameConditionMet()
        {
            return CurrentFrame switch
            {
                GuideFrame.Frame1_Welcome => true, // Auto-advance or button press
                GuideFrame.Frame2_Movement => HasMovedLeft && HasMovedRight,
                GuideFrame.Frame3_Jump => HasJumped,
                GuideFrame.Frame4_Interaction => HasInteracted,
                GuideFrame.Frame5_CollectCoin => HasCollectedCoin,
                GuideFrame.Frame6_DialogueIntro => DialogueWasTriggered,
                GuideFrame.Frame7_ContinueDialogue => HasCompletedDialogue,
                GuideFrame.Frame8_FollowUp => FollowUpDialogueCompleted,
                GuideFrame.Frame9_ReachGoal => HasReachedGoal,
                GuideFrame.Frame10_Complete => true,
                _ => false
            };
        }
    }
}
