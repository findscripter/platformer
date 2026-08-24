using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Frame 9 塔罗抽牌控制器 - 7 状态流程（A-G）
/// 状态 A: 腓腓引导对话
/// 状态 B: 腓腓与对话淡出
/// 状态 C: 第一张牌抽取（固定：应龙/战车）
/// 状态 D: 继续选择提示
/// 状态 E: 第二至第四张牌（从剩余 3 张中随机）
/// 状态 F: 4 张牌完整展示
/// 状态 G: Fade 到教学关
/// </summary>
public class TarotDrawController : MonoBehaviour
{
    private enum TarotState
    {
        A_GuideDialogue,
        B_FadeOutDialogue,
        C_FirstCardDraw,
        D_ContinuePrompt,
        E_RemainingCards,
        F_FullDisplay,
        G_FadeToLevel
    }

    #region Inspector Fields

    [Header("卡牌数据（4 张固定牌）")]
    [SerializeField] private TarotCardData[] fixedCards = new TarotCardData[4];

    [Header("UI 引用")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueSpeakerText;
    [SerializeField] private TMP_Text dialogueContentText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("卡牌 UI")]
    [SerializeField] private GameObject cardArrayContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardParent;
    [SerializeField] private GameObject mainCardSlot;
    [SerializeField] private Image mainCardImage;

    [Header("牌阵布局（扇形）")]
    [SerializeField] private int arrayCardCount = 22;
    [SerializeField] private float arrayFanAngle = 150f;
    [SerializeField] private float arrayRadius = 560f;
    [SerializeField] private float arrayVerticalOffset = -430f;
    [SerializeField] private Vector2 arrayCardSize = new Vector2(96f, 155f);

    [Header("情绪卡槽位（3 个）")]
    [SerializeField] private Transform[] emotionCardSlots = new Transform[3];

    [Header("时长配置")]
    [SerializeField] private float stateADialogueDuration = 1.5f;
    [SerializeField] private float stateBFadeDuration = 0.4f;
    [SerializeField] private float cardFlipDuration = 0.3f;
    [SerializeField] private float cardMoveDuration = 0.5f;
    [SerializeField] private float stateFDisplayDuration = 2f;
    [SerializeField] private float stateGFadeDuration = 1.5f;

    #endregion

    #region Private Fields

    private TarotState currentState;
    private GameContext gameContext;

    private List<GameObject> instantiatedCards = new();
    private TarotCardData selectedMainCard;
    private List<TarotCardData> selectedEmotionCards = new();

    private bool isWaitingForCardClick;
    private int clickedCardIndex = -1;

    // 状态 A 的 4 行对话
    private readonly string[] stateADialogues = new[]
    {
        "每一个梦核，都藏着主人的秘密。",
        "如果你想了解它……",
        "就得用塔罗，来问问看。",
        "来吧，选一张最吸引你的牌。"
    };

    #endregion

    #region Public API

    /// <summary>
    /// 启动塔罗流程（由 PlayerGuideFlowControllerV2 调用）
    /// </summary>
    public IEnumerator StartTarotFlow()
    {
        // 验证数据
        if (fixedCards == null || fixedCards.Length < 4)
        {
            Debug.LogError("[TarotDraw] fixedCards 必须包含 4 张牌！");
            yield break;
        }

        // 获取 GameContext
        if (GameLoop.Instance != null)
        {
            gameContext = GameLoop.Instance.Context;
        }

        // 初始化
        InitializeUI();

        // 执行状态机
        yield return RunState(TarotState.A_GuideDialogue);
        yield return RunState(TarotState.B_FadeOutDialogue);
        yield return RunState(TarotState.C_FirstCardDraw);
        yield return RunState(TarotState.D_ContinuePrompt);
        yield return RunState(TarotState.E_RemainingCards);
        yield return RunState(TarotState.F_FullDisplay);
        yield return RunState(TarotState.G_FadeToLevel);
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        SetPanelActive(dialoguePanel, false);
        SetPanelActive(cardArrayContainer, false);
        SetPanelActive(mainCardSlot, false);

        foreach (var slot in emotionCardSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }
    }

    #endregion

    #region State Machine

    private IEnumerator RunState(TarotState state)
    {
        currentState = state;
        Debug.Log($"[TarotDraw] Entering {state}");

        switch (state)
        {
            case TarotState.A_GuideDialogue:
                yield return State_A_GuideDialogue();
                break;

            case TarotState.B_FadeOutDialogue:
                yield return State_B_FadeOutDialogue();
                break;

            case TarotState.C_FirstCardDraw:
                yield return State_C_FirstCardDraw();
                break;

            case TarotState.D_ContinuePrompt:
                yield return State_D_ContinuePrompt();
                break;

            case TarotState.E_RemainingCards:
                yield return State_E_RemainingCards();
                break;

            case TarotState.F_FullDisplay:
                yield return State_F_FullDisplay();
                break;

            case TarotState.G_FadeToLevel:
                yield return State_G_FadeToLevel();
                break;
        }
    }

    #endregion

    #region State A: Guide Dialogue

    private IEnumerator State_A_GuideDialogue()
    {
        SetPanelActive(dialoguePanel, true);

        if (dialogueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, 0.3f);
        }

        if (dialogueSpeakerText != null)
            dialogueSpeakerText.text = "腓腓";

        // 播放 4 行对话
        foreach (string line in stateADialogues)
        {
            if (dialogueContentText != null)
                dialogueContentText.text = line;

            yield return new WaitForSeconds(stateADialogueDuration);
        }
    }

    #endregion

    #region State B: Fade Out Dialogue

    private IEnumerator State_B_FadeOutDialogue()
    {
        // 腓腓和对话淡出
        if (dialogueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, stateBFadeDuration);
        }

        SetPanelActive(dialoguePanel, false);

        // TODO: 腓腓角色淡出（需要角色 CanvasGroup 引用）
    }

    #endregion

    #region State C: First Card Draw

    private IEnumerator State_C_FirstCardDraw()
    {
        // 生成 22 张牌阵列（扇形排列）
        SetPanelActive(cardArrayContainer, true);
        SpawnCardArray();

        // TODO: 播放音效 "card_array_appear"

        yield return new WaitForSeconds(0.5f);

        // 等待玩家点击任意一张牌
        isWaitingForCardClick = true;
        clickedCardIndex = -1;

        yield return new WaitUntil(() => !isWaitingForCardClick);

        // 无论点击哪张，翻出的都是第 0 张（应龙/战车）
        selectedMainCard = fixedCards[0];

        // 翻牌动画
        yield return FlipCard(clickedCardIndex, selectedMainCard);

        // TODO: 播放音效 "main_card_flip"（共鸣感）

        // 将翻开的牌移动到主卡槽
        yield return MoveCardToMainSlot(clickedCardIndex);

        // 其余 21 张牌淡出
        yield return FadeOutRemainingCards(clickedCardIndex);
    }

    private void SpawnCardArray()
    {
        if (cardPrefab == null || cardParent == null)
        {
            Debug.LogWarning("[TarotDraw] cardPrefab 或 cardParent 未设置！");
            return;
        }

        // 清空之前的牌
        foreach (var card in instantiatedCards)
        {
            if (card != null)
                Destroy(card);
        }
        instantiatedCards.Clear();

        // 生成 22 张牌背（扇形排列）
        int cardCount = arrayCardCount;
        float startAngle = -arrayFanAngle / 2f;
        float radius = arrayRadius;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardParent);
            instantiatedCards.Add(cardObj);

            var cardRect = cardObj.transform as RectTransform;
            if (cardRect != null)
                cardRect.sizeDelta = arrayCardSize;

            // 扇形位置
            float angle = startAngle + (arrayFanAngle / (cardCount - 1)) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius + arrayVerticalOffset, 0f);
            cardObj.transform.localPosition = pos;

            // 旋转朝向中心
            cardObj.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);

            // 绑定点击事件
            int index = i;
            Button btn = cardObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnCardClicked(index));
            }
        }
    }

    private void OnCardClicked(int index)
    {
        if (!isWaitingForCardClick)
            return;

        clickedCardIndex = index;
        isWaitingForCardClick = false;

        // TODO: 播放音效 "card_click"
    }

    private IEnumerator FlipCard(int index, TarotCardData cardData)
    {
        if (index < 0 || index >= instantiatedCards.Count)
            yield break;

        GameObject cardObj = instantiatedCards[index];
        if (cardObj == null)
            yield break;

        Transform cardTransform = cardObj.transform;
        Image cardImage = cardObj.GetComponent<Image>();

        // Y 轴旋转 180 度
        float elapsed = 0f;
        Quaternion startRot = cardTransform.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 180f, 0f);

        while (elapsed < cardFlipDuration)
        {
            float t = elapsed / cardFlipDuration;
            cardTransform.localRotation = Quaternion.Lerp(startRot, endRot, t);

            // 中点切换图片
            if (t >= 0.5f && cardImage != null && cardData != null && cardData.CardFace != null)
            {
                cardImage.sprite = cardData.CardFace;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        cardTransform.localRotation = endRot;
    }

    private IEnumerator MoveCardToMainSlot(int index)
    {
        if (index < 0 || index >= instantiatedCards.Count || mainCardSlot == null)
            yield break;

        GameObject cardObj = instantiatedCards[index];
        if (cardObj == null)
            yield break;

        SetPanelActive(mainCardSlot, true);

        Vector3 startPos = cardObj.transform.position;
        Vector3 endPos = mainCardSlot.transform.position;
        Quaternion startRot = cardObj.transform.rotation;
        Quaternion endRot = Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < cardMoveDuration)
        {
            float t = elapsed / cardMoveDuration;
            t = EaseOutCubic(t);

            cardObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            cardObj.transform.rotation = Quaternion.Lerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 将卡牌图片复制到主卡槽
        if (mainCardImage != null && selectedMainCard != null && selectedMainCard.CardFace != null)
        {
            mainCardImage.sprite = selectedMainCard.CardFace;
        }

        // 销毁临时卡牌对象
        Destroy(cardObj);
    }

    private IEnumerator FadeOutRemainingCards(int excludeIndex)
    {
        List<CanvasGroup> groups = new();

        for (int i = 0; i < instantiatedCards.Count; i++)
        {
            if (i == excludeIndex || instantiatedCards[i] == null)
                continue;

            CanvasGroup group = instantiatedCards[i].GetComponent<CanvasGroup>();
            if (group == null)
                group = instantiatedCards[i].AddComponent<CanvasGroup>();

            groups.Add(group);
        }

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            float t = elapsed / 0.5f;
            foreach (var group in groups)
            {
                if (group != null)
                    group.alpha = 1f - t;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 销毁淡出的牌
        for (int i = 0; i < instantiatedCards.Count; i++)
        {
            if (i != excludeIndex && instantiatedCards[i] != null)
            {
                Destroy(instantiatedCards[i]);
            }
        }

        SetPanelActive(cardArrayContainer, false);
    }

    #endregion

    #region State D: Continue Prompt

    private IEnumerator State_D_ContinuePrompt()
    {
        // 显示"继续选择"提示
        SetPanelActive(dialoguePanel, true);

        if (dialogueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, 0.3f);
        }

        if (dialogueContentText != null)
            dialogueContentText.text = "继续选择...";

        yield return new WaitForSeconds(1.5f);

        if (dialogueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, 0.3f);
        }

        SetPanelActive(dialoguePanel, false);
    }

    #endregion

    #region State E: Remaining Cards

    private IEnumerator State_E_RemainingCards()
    {
        // 从剩余 3 张牌（索引 1, 2, 3）中随机抽取 3 张作为情绪牌
        List<TarotCardData> remainingCards = new()
        {
            fixedCards[1],
            fixedCards[2],
            fixedCards[3]
        };

        // 打乱顺序
        for (int i = 0; i < remainingCards.Count; i++)
        {
            int randomIndex = Random.Range(i, remainingCards.Count);
            (remainingCards[i], remainingCards[randomIndex]) = (remainingCards[randomIndex], remainingCards[i]);
        }

        selectedEmotionCards.AddRange(remainingCards);

        // 依次抽取并放置到 3 个情绪卡槽
        for (int i = 0; i < 3; i++)
        {
            yield return DrawEmotionCard(i, selectedEmotionCards[i]);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator DrawEmotionCard(int slotIndex, TarotCardData cardData)
    {
        if (slotIndex < 0 || slotIndex >= emotionCardSlots.Length)
            yield break;

        Transform slot = emotionCardSlots[slotIndex];
        if (slot == null)
            yield break;

        slot.gameObject.SetActive(true);

        // 生成临时卡牌
        GameObject cardObj = Instantiate(cardPrefab, slot);
        cardObj.transform.localPosition = Vector3.zero;
        cardObj.transform.localRotation = Quaternion.identity;

        // 翻牌动画
        Image cardImage = cardObj.GetComponent<Image>();
        if (cardImage != null && cardData != null && cardData.CardFace != null)
        {
            yield return FlipCardSimple(cardObj.transform, cardImage, cardData.CardFace);
        }

        // TODO: 播放音效 "normal_card_flip"
    }

    private IEnumerator FlipCardSimple(Transform cardTransform, Image cardImage, Sprite faceSprite)
    {
        float elapsed = 0f;
        Quaternion startRot = cardTransform.localRotation;
        Quaternion midRot = startRot * Quaternion.Euler(0f, 90f, 0f);
        Quaternion endRot = startRot * Quaternion.Euler(0f, 180f, 0f);

        // 前半段：0 → 90 度
        while (elapsed < cardFlipDuration / 2f)
        {
            float t = elapsed / (cardFlipDuration / 2f);
            cardTransform.localRotation = Quaternion.Lerp(startRot, midRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 切换图片
        if (cardImage != null && faceSprite != null)
        {
            cardImage.sprite = faceSprite;
        }

        // 后半段：90 → 180 度
        elapsed = 0f;
        while (elapsed < cardFlipDuration / 2f)
        {
            float t = elapsed / (cardFlipDuration / 2f);
            cardTransform.localRotation = Quaternion.Lerp(midRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cardTransform.localRotation = endRot;
    }

    #endregion

    #region State F: Full Display

    private IEnumerator State_F_FullDisplay()
    {
        // 展示所有 4 张牌（1 主牌 + 3 情绪牌）
        yield return new WaitForSeconds(stateFDisplayDuration);

        // 保存结果到 GameContext
        if (gameContext != null)
        {
            gameContext.TarotResult = new TarotResultData(selectedMainCard, selectedEmotionCards);
            Debug.Log($"[TarotDraw] 主牌: {selectedMainCard.DisplayName}, 情绪牌: {selectedEmotionCards.Count} 张");
        }
    }

    #endregion

    #region State G: Fade to Level

    private IEnumerator State_G_FadeToLevel()
    {
        // TODO: 调用 SceneTransitionManager.FadeOut(stateGFadeDuration)
        yield return new WaitForSeconds(stateGFadeDuration);

        // 加载教学关
        if (GameLoop.Instance != null)
        {
            GameLoop.Instance.ContinueToGameplay();
        }
    }

    #endregion

    #region Utility Methods

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            group.alpha = Mathf.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = to;
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    #endregion
}
