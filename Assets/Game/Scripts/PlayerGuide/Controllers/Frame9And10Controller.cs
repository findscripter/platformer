using System.Collections;
using UnityEngine;
using Game.PlayerGuide.Views;

namespace Game.PlayerGuide.Controllers
{
    /// <summary>
    /// Controls the final two frames of the PlayerGuide sequence:
    /// Frame 9: Tarot card drawing experience
    /// Frame 10: Dream Core response animation and transition to gameplay
    /// </summary>
    public class Frame9And10Controller : MonoBehaviour
    {
        [Header("View References")]
        [SerializeField] private TarotView tarotView;
        [SerializeField] private DreamCoreView dreamCoreView;
        [SerializeField] private BackgroundView backgroundView;

        [Header("GameContext")]
        [SerializeField] private GameContext context;

        [Header("Frame 9 - Tarot Drawing")]
        [SerializeField] private float tarotIntroDelay = 0.5f;
        [SerializeField] private float postTarotDelay = 1f;

        [Header("Frame 10 - Core Response")]
        [SerializeField] private float coreGlowDuration = 2f;
        [SerializeField] private float coreGlowCycles = 3f;
        [SerializeField] private float fadeToBlackDuration = 2f;
        [SerializeField] private float blackScreenHoldDuration = 1f;

        [Header("Scene Transition")]
        [SerializeField] private string gameplaySceneName = "Gameplay";

        private void Awake()
        {
            ValidateReferences();
        }

        /// <summary>
        /// Frame 9: Display tarot panel and run the full tarot drawing flow.
        /// Waits for the tarot flow to complete, then stores results in GameContext.
        /// </summary>
        public IEnumerator Frame9_TarotDrawing()
        {
            Debug.Log("[Frame9And10] Starting Frame 9 - Tarot Drawing");

            yield return new WaitForSeconds(tarotIntroDelay);

            if (tarotView != null)
            {
                tarotView.Show();
                yield return new WaitForSeconds(0.2f);

                tarotView.StartTarotFlow();

                yield return new WaitForSeconds(postTarotDelay);

                tarotView.Hide();
            }
            else
            {
                Debug.LogError("[Frame9And10] TarotView reference is null. Cannot run Frame 9.");
            }

            Debug.Log("[Frame9And10] Frame 9 complete.");
        }

        /// <summary>
        /// Frame 10: Dream Core glow animation, fade to black, and load gameplay scene.
        /// </summary>
        public IEnumerator Frame10_CoreResponseAndEnd()
        {
            Debug.Log("[Frame9And10] Starting Frame 10 - Core Response and End");

            if (dreamCoreView != null)
            {
                dreamCoreView.Show(instant: true);

                yield return PlayCoreGlowAnimation();

                dreamCoreView.Hide();
            }
            else
            {
                Debug.LogWarning("[Frame9And10] DreamCoreView reference is null. Skipping glow animation.");
            }

            yield return FadeToBlackAndLoadGameplay();

            Debug.Log("[Frame9And10] Frame 10 complete. Transitioning to gameplay.");
        }

        /// <summary>
        /// Plays a pulsing glow animation on the Dream Core.
        /// </summary>
        private IEnumerator PlayCoreGlowAnimation()
        {
            float elapsed = 0f;
            float cycleDuration = coreGlowDuration / coreGlowCycles;

            while (elapsed < coreGlowDuration)
            {
                float cycleTime = (elapsed % cycleDuration) / cycleDuration;
                float glowIntensity = Mathf.Sin(cycleTime * Mathf.PI);

                dreamCoreView.SetGlowIntensity(glowIntensity);

                elapsed += Time.deltaTime;
                yield return null;
            }

            dreamCoreView.SetGlowIntensity(0f);
        }

        /// <summary>
        /// Fades the screen to black, holds, then loads the gameplay scene.
        /// </summary>
        private IEnumerator FadeToBlackAndLoadGameplay()
        {
            if (backgroundView != null)
            {
                backgroundView.FadeOut(fadeToBlackDuration);
                yield return new WaitForSeconds(fadeToBlackDuration);
            }
            else
            {
                Debug.LogWarning("[Frame9And10] BackgroundView reference is null. Skipping fade to black.");
                yield return new WaitForSeconds(fadeToBlackDuration);
            }

            yield return new WaitForSeconds(blackScreenHoldDuration);

            LoadGameplayScene();
        }

        /// <summary>
        /// Loads the gameplay scene using GameLoop.
        /// </summary>
        private void LoadGameplayScene()
        {
            if (context != null && context.GameLoop != null)
            {
                context.GameLoop.LoadScene(gameplaySceneName);
            }
            else
            {
                Debug.LogError("[Frame9And10] GameContext or GameLoop is null. Cannot load gameplay scene.");
            }
        }

        /// <summary>
        /// Validates that all required references are assigned.
        /// </summary>
        private void ValidateReferences()
        {
            if (tarotView == null)
            {
                Debug.LogWarning("[Frame9And10] TarotView reference is not assigned.", this);
            }

            if (dreamCoreView == null)
            {
                Debug.LogWarning("[Frame9And10] DreamCoreView reference is not assigned.", this);
            }

            if (backgroundView == null)
            {
                Debug.LogWarning("[Frame9And10] BackgroundView reference is not assigned.", this);
            }

            if (context == null)
            {
                Debug.LogWarning("[Frame9And10] GameContext reference is not assigned.", this);
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor utility to test Frame 9 in isolation.
        /// </summary>
        [ContextMenu("Test Frame 9 - Tarot Drawing")]
        private void TestFrame9()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(Frame9_TarotDrawing());
            }
            else
            {
                Debug.LogWarning("Test Frame 9 can only run in Play Mode.");
            }
        }

        /// <summary>
        /// Editor utility to test Frame 10 in isolation.
        /// </summary>
        [ContextMenu("Test Frame 10 - Core Response")]
        private void TestFrame10()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(Frame10_CoreResponseAndEnd());
            }
            else
            {
                Debug.LogWarning("Test Frame 10 can only run in Play Mode.");
            }
        }
        #endif
    }
}
