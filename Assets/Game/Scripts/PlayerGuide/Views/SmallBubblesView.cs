using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.PlayerGuide.Views
{
    /// <summary>
    /// Manages spawning and animation of small decorative bubbles in the player guide UI.
    /// Bubbles float upward with wave motion for visual enhancement.
    /// </summary>
    public class SmallBubblesView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform bubblesContainer;
        [SerializeField] private GameObject bubblePrefab;

        [Header("Configuration")]
        [SerializeField] private int count = 10;
        [SerializeField] private Vector2 sizeRange = new Vector2(0.3f, 0.8f);
        [SerializeField] private float floatSpeed = 50f;
        [SerializeField] private float floatAmplitude = 20f;

        private readonly List<GameObject> activeBubbles = new List<GameObject>();
        private readonly List<Coroutine> activeCoroutines = new List<Coroutine>();

        /// <summary>
        /// Spawns the configured number of bubbles with random properties and starts their float animation.
        /// </summary>
        public void SpawnBubbles()
        {
            if (bubblesContainer == null || bubblePrefab == null)
            {
                Debug.LogWarning($"[SmallBubblesView] Missing references: container={bubblesContainer}, prefab={bubblePrefab}");
                return;
            }

            ClearBubbles();

            RectTransform containerRect = bubblesContainer as RectTransform;
            if (containerRect == null)
            {
                Debug.LogWarning("[SmallBubblesView] bubblesContainer is not a RectTransform");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                GameObject bubble = Instantiate(bubblePrefab, bubblesContainer);
                RectTransform bubbleRect = bubble.GetComponent<RectTransform>();

                if (bubbleRect == null)
                {
                    Debug.LogWarning($"[SmallBubblesView] Bubble prefab missing RectTransform");
                    Destroy(bubble);
                    continue;
                }

                // Random size
                float size = Random.Range(sizeRange.x, sizeRange.y);
                bubbleRect.sizeDelta = new Vector2(size, size);

                // Random starting position within container bounds
                float startX = Random.Range(-containerRect.rect.width * 0.5f, containerRect.rect.width * 0.5f);
                float startY = Random.Range(-containerRect.rect.height * 0.5f, containerRect.rect.height * 0.5f);
                bubbleRect.anchoredPosition = new Vector2(startX, startY);

                // Random initial delay for staggered animation
                float delay = Random.Range(0f, 2f);

                activeBubbles.Add(bubble);
                Coroutine routine = StartCoroutine(FloatBubble(bubbleRect, startX, delay));
                activeCoroutines.Add(routine);
            }
        }

        /// <summary>
        /// Animates a bubble floating upward with horizontal wave motion.
        /// Loops continuously, resetting position when reaching the top.
        /// </summary>
        /// <param name="bubbleRect">The RectTransform of the bubble to animate</param>
        /// <param name="startX">The initial X position for the wave pattern</param>
        /// <param name="delay">Initial delay before starting animation</param>
        private IEnumerator FloatBubble(RectTransform bubbleRect, float startX, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            RectTransform containerRect = bubblesContainer as RectTransform;
            if (containerRect == null) yield break;

            float containerHeight = containerRect.rect.height;
            float time = Random.Range(0f, Mathf.PI * 2f); // Random wave offset

            while (true)
            {
                if (bubbleRect == null) yield break;

                // Move upward
                Vector2 pos = bubbleRect.anchoredPosition;
                pos.y += floatSpeed * Time.deltaTime;

                // Apply horizontal wave
                time += Time.deltaTime;
                pos.x = startX + Mathf.Sin(time * 2f) * floatAmplitude;

                bubbleRect.anchoredPosition = pos;

                // Reset to bottom when reaching top
                if (pos.y > containerHeight * 0.5f)
                {
                    pos.y = -containerHeight * 0.5f;
                    bubbleRect.anchoredPosition = pos;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Stops all animations and destroys all active bubbles.
        /// </summary>
        public void ClearBubbles()
        {
            // Stop all coroutines
            foreach (Coroutine routine in activeCoroutines)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }
            activeCoroutines.Clear();

            // Destroy all bubble GameObjects
            foreach (GameObject bubble in activeBubbles)
            {
                if (bubble != null)
                {
                    Destroy(bubble);
                }
            }
            activeBubbles.Clear();
        }

        private void OnDestroy()
        {
            ClearBubbles();
        }
    }
}
