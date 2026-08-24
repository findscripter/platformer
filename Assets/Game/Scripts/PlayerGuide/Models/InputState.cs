using System;

namespace Game.PlayerGuide.Models
{
    /// <summary>
    /// Tracks user input state for Frame 7-8 dream input collection.
    /// </summary>
    [Serializable]
    public class InputState
    {
        /// <summary>
        /// User's dream text input.
        /// </summary>
        public string DreamInputText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user is in the follow-up input state (Frame 8).
        /// </summary>
        public bool IsInFollowUpState { get; set; }

        /// <summary>
        /// Checks if the dream input is valid (non-empty after trimming).
        /// </summary>
        public bool IsInputValid()
        {
            return !string.IsNullOrWhiteSpace(DreamInputText);
        }

        /// <summary>
        /// Checks if the input meets minimum length requirement.
        /// </summary>
        /// <param name="minLength">Minimum required character count.</param>
        /// <returns>True if input meets or exceeds minimum length.</returns>
        public bool MeetsMinimumLength(int minLength)
        {
            if (string.IsNullOrWhiteSpace(DreamInputText))
                return false;

            return DreamInputText.Trim().Length >= minLength;
        }

        /// <summary>
        /// Checks if the input exceeds maximum length limit.
        /// </summary>
        /// <param name="maxLength">Maximum allowed character count.</param>
        /// <returns>True if input is within the limit.</returns>
        public bool IsWithinMaximumLength(int maxLength)
        {
            if (string.IsNullOrWhiteSpace(DreamInputText))
                return true;

            return DreamInputText.Length <= maxLength;
        }

        /// <summary>
        /// Gets the trimmed dream input text.
        /// </summary>
        public string GetTrimmedInput()
        {
            return DreamInputText?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Gets the current character count of the dream input.
        /// </summary>
        public int GetCharacterCount()
        {
            return DreamInputText?.Length ?? 0;
        }

        /// <summary>
        /// Clears the dream input and resets the follow-up state.
        /// </summary>
        public void Clear()
        {
            DreamInputText = string.Empty;
            IsInFollowUpState = false;
        }

        /// <summary>
        /// Validates input against both minimum and maximum length constraints.
        /// </summary>
        /// <param name="minLength">Minimum required character count.</param>
        /// <param name="maxLength">Maximum allowed character count.</param>
        /// <returns>True if input meets all constraints.</returns>
        public bool ValidateLength(int minLength, int maxLength)
        {
            return IsInputValid() &&
                   MeetsMinimumLength(minLength) &&
                   IsWithinMaximumLength(maxLength);
        }
    }
}
