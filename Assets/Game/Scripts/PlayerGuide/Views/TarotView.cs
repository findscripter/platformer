using System.Collections;
using UnityEngine;

namespace Game.PlayerGuide.Views
{
    /// <summary>
    /// View wrapper for the tarot panel UI.
    /// Manages visibility and flow control for the tarot drawing experience.
    /// </summary>
    public class TarotView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject tarotPanel;
        [SerializeField] private TarotDrawController tarotDrawController;

        private void Awake()
        {
            ValidateReferences();
        }

        /// <summary>
        /// Shows the tarot panel.
        /// </summary>
        public void Show()
        {
            if (tarotPanel != null)
            {
                tarotPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the tarot panel.
        /// </summary>
        public void Hide()
        {
            if (tarotPanel != null)
            {
                tarotPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Starts the tarot drawing flow.
        /// Wrapper for TarotDrawController.StartTarotFlow().
        /// </summary>
        public void StartTarotFlow()
        {
            if (tarotDrawController != null)
            {
                StartCoroutine(StartTarotFlowCoroutine());
            }
            else
            {
                Debug.LogError("[TarotView] TarotDrawController reference is null. Cannot start tarot flow.");
            }
        }

        private IEnumerator StartTarotFlowCoroutine()
        {
            yield return tarotDrawController.StartTarotFlow();
        }

        private void ValidateReferences()
        {
            if (tarotPanel == null)
            {
                Debug.LogWarning("[TarotView] Tarot panel reference is not assigned.");
            }

            if (tarotDrawController == null)
            {
                Debug.LogWarning("[TarotView] TarotDrawController reference is not assigned.");
            }
        }

        /// <summary>
        /// Gets whether the tarot panel is currently visible.
        /// </summary>
        public bool IsVisible => tarotPanel != null && tarotPanel.activeSelf;

        /// <summary>
        /// Gets the TarotDrawController reference.
        /// </summary>
        public TarotDrawController DrawController => tarotDrawController;
    }
}
