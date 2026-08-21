using UnityEngine;
using UnityEngine.UI;
using DreamGame.Core;
using DreamGame.Dialogue;

namespace DreamGame.UI
{
    /// <summary>
    /// 梦泡控制器 - 处理Frame1和Frame2的梦泡交互
    /// 包含漂浮动画和点击检测
    /// </summary>
    public class DreamBubbleController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private FrameStateMachine frameStateMachine;
        [SerializeField] private Image bubbleImage;
        [SerializeField] private AudioSource audioSource;

        [Header("漂浮动画")]
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float floatAmplitude = 20f;
        [SerializeField] private AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("点击效果")]
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private float clickScaleMultiplier = 1.2f;
        [SerializeField] private float clickAnimationDuration = 0.3f;

        [Header("状态")]
        [SerializeField] private bool isClickable = false;

        private Vector3 initialPosition;
        private float timeOffset;
        private bool isAnimating = false;

        private void Start()
        {
            initialPosition = transform.position;
            timeOffset = Random.Range(0f, 2f * Mathf.PI);

            // 监听Frame切换事件
            if (frameStateMachine != null)
            {
                frameStateMachine.OnFrameChanged += OnFrameChanged;
            }
        }

        private void OnDestroy()
        {
            if (frameStateMachine != null)
            {
                frameStateMachine.OnFrameChanged -= OnFrameChanged;
            }
        }

        private void Update()
        {
            // Frame1: 持续漂浮
            if (frameStateMachine != null &&
                frameStateMachine.GetCurrentFrame() == FrameState.Frame1_DreamBubble)
            {
                AnimateFloat();
            }
        }

        /// <summary>
        /// 漂浮动画
        /// </summary>
        private void AnimateFloat()
        {
            float time = Time.time * floatSpeed + timeOffset;
            float curveValue = floatCurve.Evaluate((Mathf.Sin(time) + 1f) / 2f);
            float yOffset = curveValue * floatAmplitude;

            transform.position = initialPosition + new Vector3(0, yOffset, 0);
        }

        /// <summary>
        /// Frame切换回调
        /// </summary>
        private void OnFrameChanged(FrameState from, FrameState to)
        {
            switch (to)
            {
                case FrameState.Frame1_DreamBubble:
                    ShowBubble();
                    break;
                case FrameState.Frame2_BubbleClick:
                    EnableClick();
                    break;
                case FrameState.Frame3_PhiPhiIntro:
                    HideBubble();
                    break;
            }
        }

        /// <summary>
        /// 显示梦泡
        /// </summary>
        private void ShowBubble()
        {
            gameObject.SetActive(true);
            isClickable = false;
            transform.position = initialPosition;
        }

        /// <summary>
        /// 启用点击
        /// </summary>
        private void EnableClick()
        {
            isClickable = true;
        }

        /// <summary>
        /// 隐藏梦泡
        /// </summary>
        private void HideBubble()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 点击检测（由Button或EventTrigger调用）
        /// </summary>
        public void OnBubbleClicked()
        {
            if (!isClickable || isAnimating) return;

            Debug.Log("[DreamBubble] Bubble clicked!");

            // 播放点击音效
            if (clickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }

            // 播放点击动画
            StartCoroutine(ClickAnimation());
        }

        /// <summary>
        /// 点击动画协程
        /// </summary>
        private System.Collections.IEnumerator ClickAnimation()
        {
            isAnimating = true;
            isClickable = false;

            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * clickScaleMultiplier;

            // 放大
            float elapsed = 0f;
            while (elapsed < clickAnimationDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (clickAnimationDuration / 2f);
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            // 缩小并推进到Frame3
            elapsed = 0f;
            while (elapsed < clickAnimationDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (clickAnimationDuration / 2f);
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
            isAnimating = false;

            // 推进到Frame3
            if (frameStateMachine != null)
            {
                frameStateMachine.AdvanceToNextFrame();
            }
        }
    }
}
