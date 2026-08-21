using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DreamGame.Core;

namespace DreamGame.UI
{
    /// <summary>
    /// 塔罗牌选择控制器 - 处理Frame7和Frame8的塔罗牌交互
    /// 显示三张塔罗牌供玩家选择
    /// </summary>
    public class TarotCardController : MonoBehaviour
    {
        [System.Serializable]
        public class TarotCard
        {
            public string cardName;
            public Sprite cardSprite;
            public string description;
            public int levelIndex; // 对应的关卡索引
        }

        [Header("引用")]
        [SerializeField] private FrameStateMachine frameStateMachine;
        [SerializeField] private GameObject tarotPanel;
        [SerializeField] private List<Button> cardButtons;
        [SerializeField] private AudioSource audioSource;

        [Header("塔罗牌数据")]
        [SerializeField] private List<TarotCard> availableCards;

        [Header("音效")]
        [SerializeField] private AudioClip cardSelectSound;
        [SerializeField] private AudioClip cardHoverSound;

        [Header("动画")]
        [SerializeField] private float cardRevealDelay = 0.2f;
        [SerializeField] private float cardScaleOnHover = 1.1f;

        private int selectedCardIndex = -1;

        private void Start()
        {
            // 监听Frame切换
            if (frameStateMachine != null)
            {
                frameStateMachine.OnFrameChanged += OnFrameChanged;
            }

            // 初始化按钮事件
            for (int i = 0; i < cardButtons.Count; i++)
            {
                int index = i; // 闭包捕获
                cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
            }

            // 初始隐藏
            tarotPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (frameStateMachine != null)
            {
                frameStateMachine.OnFrameChanged -= OnFrameChanged;
            }
        }

        /// <summary>
        /// Frame切换回调
        /// </summary>
        private void OnFrameChanged(FrameState from, FrameState to)
        {
            switch (to)
            {
                case FrameState.Frame7_TarotPrompt:
                    ShowTarotPrompt();
                    break;
                case FrameState.Frame8_TarotChoice:
                    EnableCardSelection();
                    break;
                case FrameState.Frame9_LevelTransition:
                    HideTarotPanel();
                    break;
            }
        }

        /// <summary>
        /// 显示塔罗牌提示（Frame7）
        /// </summary>
        private void ShowTarotPrompt()
        {
            tarotPanel.SetActive(true);

            // 禁用所有按钮，只显示卡牌背面
            foreach (var btn in cardButtons)
            {
                btn.interactable = false;
            }

            // 播放卡牌翻转动画
            StartCoroutine(RevealCardsAnimation());
        }

        /// <summary>
        /// 卡牌翻转动画
        /// </summary>
        private System.Collections.IEnumerator RevealCardsAnimation()
        {
            for (int i = 0; i < cardButtons.Count && i < availableCards.Count; i++)
            {
                yield return new WaitForSeconds(cardRevealDelay);

                // 设置卡牌图片
                var image = cardButtons[i].GetComponent<Image>();
                if (image != null && availableCards[i].cardSprite != null)
                {
                    image.sprite = availableCards[i].cardSprite;
                }

                // 播放翻转音效（如果有）
            }

            // 动画完成后推进到Frame8
            yield return new WaitForSeconds(0.5f);
            if (frameStateMachine != null)
            {
                frameStateMachine.AdvanceToNextFrame();
            }
        }

        /// <summary>
        /// 启用卡牌选择（Frame8）
        /// </summary>
        private void EnableCardSelection()
        {
            foreach (var btn in cardButtons)
            {
                btn.interactable = true;
            }
        }

        /// <summary>
        /// 卡牌被选中
        /// </summary>
        private void OnCardSelected(int index)
        {
            if (index < 0 || index >= availableCards.Count) return;

            selectedCardIndex = index;
            Debug.Log($"[TarotCard] Selected card: {availableCards[index].cardName}");

            // 播放选择音效
            if (cardSelectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(cardSelectSound);
            }

            // 保存选择结果
            PlayerPrefs.SetInt("SelectedTarotIndex", index);
            PlayerPrefs.SetString("SelectedTarotName", availableCards[index].cardName);
            PlayerPrefs.Save();

            // 播放选中动画后推进到Frame9
            StartCoroutine(CardSelectedAnimation(index));
        }

        /// <summary>
        /// 卡牌选中动画
        /// </summary>
        private System.Collections.IEnumerator CardSelectedAnimation(int index)
        {
            // 禁用所有按钮
            foreach (var btn in cardButtons)
            {
                btn.interactable = false;
            }

            // 放大选中的卡牌
            Transform selectedCard = cardButtons[index].transform;
            Vector3 originalScale = selectedCard.localScale;
            Vector3 targetScale = originalScale * 1.3f;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                selectedCard.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            // 推进到Frame9
            if (frameStateMachine != null)
            {
                frameStateMachine.AdvanceToNextFrame();
            }
        }

        /// <summary>
        /// 隐藏塔罗牌面板
        /// </summary>
        private void HideTarotPanel()
        {
            tarotPanel.SetActive(false);
        }

        /// <summary>
        /// 获取选中的塔罗牌关卡索引
        /// </summary>
        public int GetSelectedLevelIndex()
        {
            if (selectedCardIndex >= 0 && selectedCardIndex < availableCards.Count)
            {
                return availableCards[selectedCardIndex].levelIndex;
            }
            return 0; // 默认返回第一个关卡
        }
    }
}
