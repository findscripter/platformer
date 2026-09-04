using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Frame 9 塔罗抽牌控制器 - 6 状态流程（A-D、F、G）
/// 状态 A: 腓腓引导对话（4 句）
/// 状态 B: 腓腓与对话淡出
/// 状态 C: 第一次抽牌（任意点击，固定翻出应龙/战车，上移并强调）
/// 状态 D: 继续选择第 2-4 张（剩余牌原位可点击，出牌顺序随机）
/// 状态 F: 4 张牌完整展示（剩余 18 张淡出）
/// 状态 G: Fade 到教学关
/// 注：设计稿的状态 E（第二至第四张）已与状态 D 合并——两者共用同一套
/// "原位点击 → 普通翻牌 → 上移填槽"流程，拆成两个协程会造成状态重复。
/// </summary>
public class TarotDrawController : MonoBehaviour
{
    private enum TarotState
    {
        A_GuideDialogue,
        B_FadeOutDialogue,
        C_FirstCardDraw,
        D_ContinuePrompt,
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
    [SerializeField] private GameObject continueIndicator;

    [Header("卡牌 UI")]
    [SerializeField] private GameObject cardArrayContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardParent;
    [SerializeField] private GameObject mainCardSlot;
    [SerializeField] private Image mainCardImage;

    [Header("牌阵布局（扇形）")]
    [SerializeField] private int arrayCardCount = 22;
    [SerializeField] private float arrayFanAngle = 30f;
    [SerializeField] private float arrayRadius = 700f;
    [SerializeField] private float arrayVerticalOffset = -980f;
    [SerializeField] private Vector2 arrayCardSize = new Vector2(120f, 200f);

    [Header("情绪卡槽位（3 个）")]
    [SerializeField] private Transform[] emotionCardSlots = new Transform[3];

    [Header("时长配置")]
    [SerializeField] private float stateADialogueDuration = 1.5f;
    [SerializeField] private float stateBFadeDuration = 0.4f;
    [SerializeField] private float cardFlipDuration = 0.3f;
    [SerializeField] private float cardMoveDuration = 0.5f;
    [Tooltip("主牌放大后的停顿时长（设计稿：约 0.6s）")]
    [SerializeField] private float mainCardEmphasisHold = 0.3f;
    [Tooltip("四张展示保持时长（设计稿：1.2-1.5s）")]
    [SerializeField] private float stateFDisplayDuration = 1.35f;
    [Tooltip("剩余 18 张牌淡出时长（设计稿：0.3s）")]
    [SerializeField] private float remainingFadeDuration = 0.3f;
    [SerializeField] private float stateGFadeDuration = 0.5f;
    [Tooltip("省略号呼吸速度")]
    [SerializeField] private float indicatorPulseSpeed = 1.5f;
    [Tooltip("省略号最低透明度")]
    [SerializeField] private float indicatorMinAlpha = 0.3f;

    #endregion

    #region Private Fields

    private TarotState currentState;
    private GameContext gameContext;

    private List<GameObject> instantiatedCards = new();
    private TarotCardData selectedMainCard;
    private List<TarotCardData> selectedEmotionCards = new();

    private bool isWaitingForCardClick;
    private int clickedCardIndex = -1;

    /// <summary>已被抽走的牌阵下标，用于防止重复点击同一张牌。</summary>
    private readonly List<int> drawnCardIndices = new();

    /// <summary>输入缓冲队列：动画期间的点击会缓存在这里，下次等待时优先消费。</summary>
    private readonly Queue<int> pendingClicks = new();

    private Coroutine indicatorPulseRoutine;

    // 状态 A 的 4 行对话
    private readonly string[] stateADialogues = new[]
    {
        "嗯……我听见一些了。",
        "不过，它说得很轻。",
        "来，先选一张。",
        "看看哪一张，会先回应你。"
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
        yield return RunState(TarotState.F_FullDisplay);
        yield return RunState(TarotState.G_FadeToLevel);
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        // 重置流程状态，支持重复进入
        drawnCardIndices.Clear();
        selectedEmotionCards.Clear();
        selectedMainCard = null;
        isWaitingForCardClick = false;
        clickedCardIndex = -1;

        SetPanelActive(dialoguePanel, false);
        SetPanelActive(cardArrayContainer, false);
        SetPanelActive(mainCardSlot, false);

        if (mainCardSlot != null)
            mainCardSlot.transform.localScale = Vector3.one;

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
        // Figma 状态 A：22 张牌背与引导对白同时在场
        SetPanelActive(cardArrayContainer, true);
        SpawnCardArray(false);

        SetPanelActive(dialoguePanel, true);
        if (dialoguePanel != null)
            dialoguePanel.transform.SetAsLastSibling();

        if (dialogueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, 0.3f);
        }

        if (dialogueSpeakerText != null)
            dialogueSpeakerText.text = "腓腓";

        // 启动省略号呼吸动画（表示还有后续台词）
        indicatorPulseRoutine = StartCoroutine(PulseContinueIndicator());

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
        // 停止省略号呼吸
        if (indicatorPulseRoutine != null)
        {
            StopCoroutine(indicatorPulseRoutine);
            indicatorPulseRoutine = null;
        }
        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        // 腓腓和对话淡出
        if (dialogueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, stateBFadeDuration);
        }

        SetPanelActive(dialoguePanel, false);

        var feifei = Object.FindFirstObjectByType<FeifeiCharacterView>();
        if (feifei != null)
        {
            yield return feifei.FadeAlpha(1f, 0f, stateBFadeDuration);
            feifei.SetActive(false);
        }
    }

    #endregion

    #region State C: First Card Draw

    private IEnumerator State_C_FirstCardDraw()
    {
        if (instantiatedCards.Count == 0)
        {
            SetPanelActive(cardArrayContainer, true);
            SpawnCardArray(true);
        }
        else
        {
            BindCardButtons();
        }

        yield return new WaitForSeconds(0.25f);

        // 等待玩家点击任意一张牌
        isWaitingForCardClick = true;
        clickedCardIndex = -1;

        yield return new WaitUntil(() => !isWaitingForCardClick);

        // 无论点击哪张，翻出的都是第 0 张（应龙/战车）
        selectedMainCard = fixedCards[0];
        int mainCardIndex = clickedCardIndex;
        drawnCardIndices.Add(mainCardIndex);

        // 原位翻牌
        yield return FlipCard(mainCardIndex, selectedMainCard);

        // TODO: 播放音效 "main_card_flip"（共鸣感）

        // 上移到中上方主卡槽（设计稿状态 C：所选牌原位翻开 → 上移到中上方第 1 位）
        yield return MoveCardToMainSlot(mainCardIndex);

        // 主牌强调：轻微放大 105% + 淡光 + 停顿 0.6s
        yield return EmphasizeMainCard();

        // 设计稿状态 D：剩余牌仍在中下方原位可点击，牌阵不销毁
    }

    /// <summary>
    /// 主牌强调效果：轻微放大到 105% 后回落，并停顿约 0.6s（设计稿状态 C）。
    /// </summary>
    private IEnumerator EmphasizeMainCard()
    {
        if (mainCardSlot == null)
            yield break;

        Transform slot = mainCardSlot.transform;
        Vector3 baseScale = Vector3.one;
        Vector3 upScale = baseScale * 1.05f;

        const float scaleDuration = 0.18f;
        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            float t = EaseOutCubic(elapsed / scaleDuration);
            slot.localScale = Vector3.Lerp(baseScale, upScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        slot.localScale = upScale;

        Image glowTarget = mainCardImage != null ? mainCardImage : slot.GetComponent<Image>();
        Color glowStart = glowTarget != null ? glowTarget.color : Color.white;
        if (glowTarget != null)
            glowTarget.color = Color.Lerp(glowStart, Color.white, 0.55f);

        yield return new WaitForSeconds(mainCardEmphasisHold > 0.5f ? mainCardEmphasisHold : 0.6f);

        if (glowTarget != null)
            glowTarget.color = glowStart;

        elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            float t = EaseOutCubic(elapsed / scaleDuration);
            slot.localScale = Vector3.Lerp(upScale, baseScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        slot.localScale = baseScale;
    }

    private void SpawnCardArray(bool enableClick = true)
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

        int cardCount = arrayCardCount;
        float overlap = 28f;
        float startX = -((cardCount - 1) * overlap) * 0.5f;
        float rowY = 80f;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardParent);
            instantiatedCards.Add(cardObj);

            var cardRect = cardObj.transform as RectTransform;
            if (cardRect != null)
                cardRect.sizeDelta = arrayCardSize.x > 0f ? arrayCardSize : new Vector2(120f, 200f);

            float t = cardCount <= 1 ? 0.5f : i / (float)(cardCount - 1);
            float tilt = Mathf.Lerp(-8f, 8f, t);
            cardObj.transform.localPosition = new Vector3(startX + i * overlap, rowY + Mathf.Abs(t - 0.5f) * 18f, 0f);
            cardObj.transform.localRotation = Quaternion.Euler(0f, 0f, -tilt);
            cardObj.transform.SetAsLastSibling();
        }

        if (enableClick)
            BindCardButtons();
    }

    /// <summary>
    /// 点哪张牌就抽哪张（设计稿：任意点击第一张固定翻应龙，后续点中的牌上移）。
    /// </summary>
    private void BindCardButtons()
    {
        if (cardArrayContainer != null)
        {
            Transform existingArea = cardArrayContainer.transform.Find("FullscreenClickArea");
            if (existingArea != null)
                Destroy(existingArea.gameObject);
        }

        for (int i = 0; i < instantiatedCards.Count; i++)
        {
            GameObject card = instantiatedCards[i];
            if (card == null)
                continue;

            int captured = i;
            Button btn = card.GetComponent<Button>();
            if (btn == null)
                btn = card.AddComponent<Button>();

            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnCardClicked(captured));

            Image img = card.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = true;
        }
    }

    private void OnCardClicked(int index)
    {
        // 已被抽走的位置不可再点（设计稿程序需求：防止重复点击）
        if (drawnCardIndices.Contains(index))
            return;

        // 如果当前不在等待点击状态（动画播放中），缓冲这次点击
        if (!isWaitingForCardClick)
        {
            // 避免重复缓冲同一张牌，且最多缓冲 3 张（后三张情绪牌）
            if (!pendingClicks.Contains(index) && pendingClicks.Count < 3)
            {
                pendingClicks.Enqueue(index);
                // 给缓冲的点击一个轻微的视觉反馈
                if (index >= 0 && index < instantiatedCards.Count)
                {
                    var card = instantiatedCards[index];
                    if (card != null)
                        StartCoroutine(QuickPunchScale(card.transform, 0.15f));
                }
            }
            return;
        }

        // 即时视觉反馈：缩放动画
        if (index >= 0 && index < instantiatedCards.Count)
        {
            var card = instantiatedCards[index];
            if (card != null)
                StartCoroutine(QuickPunchScale(card.transform, 0.1f));
        }

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

        GuideSfx.PlayCardFlip();
        Transform cardTransform = cardObj.transform;
        Image cardImage = cardObj.GetComponent<Image>();

        // Y 轴旋转 180 度
        float elapsed = 0f;
        Quaternion startRot = cardTransform.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 180f, 0f);

        while (elapsed < cardFlipDuration)
        {
            float t = elapsed / cardFlipDuration;
            t = EaseOutCubic(t);  // 添加缓动，避免线性插值的生硬感
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

        // 将卡牌图片复制到主卡槽（场景搭建时 CardFace 默认禁用，此处必须显式启用）
        if (mainCardImage != null && selectedMainCard != null && selectedMainCard.CardFace != null)
        {
            mainCardImage.sprite = selectedMainCard.CardFace;
            mainCardImage.color = Color.white;
            mainCardImage.enabled = true;
        }

        // 销毁临时卡牌对象，并在列表中置空，避免后续淡出访问已销毁对象
        instantiatedCards[index] = null;
        Destroy(cardObj);
    }

    /// <summary>
    /// 淡出并销毁牌阵中剩余的未抽牌（设计稿状态 F：剩余 18 张牌淡出 0.3s）。
    /// 已抽走的牌在上移到槽位时已被销毁并在列表中置 null，此处自然跳过。
    /// </summary>
    private IEnumerator FadeOutRemainingCards()
    {
        List<CanvasGroup> groups = new();

        for (int i = 0; i < instantiatedCards.Count; i++)
        {
            if (instantiatedCards[i] == null)
                continue;

            CanvasGroup group = instantiatedCards[i].GetComponent<CanvasGroup>();
            if (group == null)
                group = instantiatedCards[i].AddComponent<CanvasGroup>();

            groups.Add(group);
        }

        float elapsed = 0f;
        while (elapsed < remainingFadeDuration)
        {
            float t = elapsed / remainingFadeDuration;
            foreach (var group in groups)
            {
                if (group != null)
                    group.alpha = 1f - t;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < instantiatedCards.Count; i++)
        {
            if (instantiatedCards[i] != null)
            {
                Destroy(instantiatedCards[i]);
                instantiatedCards[i] = null;
            }
        }

        SetPanelActive(cardArrayContainer, false);
    }

    #endregion

    #region State D: Continue Prompt

    /// <summary>
    /// 设计稿状态 D + E：第 1 张保持在中上方，剩余牌仍在中下方原位可点击；
    /// 玩家依次点第 2、3、4 张，每张用同一普通翻牌 + 上移动效填入情绪卡槽。
    /// 后三张出牌顺序随机（设计稿：后三张顺序随机）。
    /// 设计稿明确不加"情绪牌"标签、也不增加引导对白。
    /// </summary>
    private IEnumerator State_D_ContinuePrompt()
    {
        // 后三张的出牌顺序随机，与玩家点击的具体位置无关
        List<TarotCardData> remainingCards = new()
        {
            fixedCards[1],
            fixedCards[2],
            fixedCards[3]
        };

        for (int i = 0; i < remainingCards.Count; i++)
        {
            int randomIndex = Random.Range(i, remainingCards.Count);
            (remainingCards[i], remainingCards[randomIndex]) = (remainingCards[randomIndex], remainingCards[i]);
        }

        // 依次等待玩家点击三张未抽过的牌
        for (int slot = 0; slot < 3; slot++)
        {
            // 优先从缓冲队列取点击
            if (pendingClicks.Count > 0)
            {
                clickedCardIndex = pendingClicks.Dequeue();
            }
            else
            {
                // 队列空时才等待新点击
                isWaitingForCardClick = true;
                clickedCardIndex = -1;
                yield return new WaitUntil(() => !isWaitingForCardClick);
            }

            int index = clickedCardIndex;
            drawnCardIndices.Add(index);

            TarotCardData card = remainingCards[slot];
            selectedEmotionCards.Add(card);

            // 原位普通翻牌
            yield return FlipCard(index, card);

            // TODO: 播放音效 "normal_card_flip"（轻量普通翻牌声）

            // 上移填入中上方对应情绪卡槽
            yield return MoveCardToEmotionSlot(index, slot, card);
        }
    }

    /// <summary>
    /// 把抽中的牌从牌阵原位上移到指定情绪卡槽，到位后销毁临时牌、由槽位承接牌面。
    /// </summary>
    private IEnumerator MoveCardToEmotionSlot(int index, int slotIndex, TarotCardData cardData)
    {
        if (index < 0 || index >= instantiatedCards.Count)
            yield break;
        if (slotIndex < 0 || slotIndex >= emotionCardSlots.Length)
            yield break;

        GameObject cardObj = instantiatedCards[index];
        Transform slot = emotionCardSlots[slotIndex];
        if (cardObj == null || slot == null)
            yield break;

        slot.gameObject.SetActive(true);

        Vector3 startPos = cardObj.transform.position;
        Vector3 endPos = slot.position;
        Quaternion startRot = cardObj.transform.rotation;
        Quaternion endRot = Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < cardMoveDuration)
        {
            float t = EaseOutCubic(elapsed / cardMoveDuration);
            cardObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            cardObj.transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 槽位承接牌面，避免临时牌残留在牌阵父节点下影响后续淡出。
        // 不能用 GetComponentInChildren：会先命中槽根自己的深色底板 Image，
        // 牌面会被底板颜色染暗，必须写到独立的子 CardFace 上。
        var slotImage = GetOrCreateSlotCardFace(slot);
        if (slotImage != null && cardData != null && cardData.CardFace != null)
        {
            slotImage.sprite = cardData.CardFace;
            slotImage.color = Color.white;
            slotImage.enabled = true;
        }

        instantiatedCards[index] = null;
        Destroy(cardObj);
    }

    /// <summary>
    /// 取槽位下名为 CardFace 的子 Image（跳过槽根底板）；场景里没有就补建一个。
    /// </summary>
    private Image GetOrCreateSlotCardFace(Transform slot)
    {
        Transform face = slot.Find("CardFace");
        if (face == null)
        {
            var faceObj = new GameObject("CardFace", typeof(RectTransform));
            face = faceObj.transform;
            face.SetParent(slot, false);
            var faceRect = (RectTransform)face;
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.sizeDelta = Vector2.zero;
            var img = faceObj.AddComponent<Image>();
            img.preserveAspect = true;
            return img;
        }

        return face.GetComponent<Image>();
    }

    #endregion


    #region State F: Full Display

    private IEnumerator State_F_FullDisplay()
    {
        // 设计稿状态 F：剩余 18 张牌淡出 0.3s，四张结果保持在中上方完整展示 1.2-1.5s
        yield return FadeOutRemainingCards();

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
        // 渐入黑屏后再切关；黑屏由 ContinueToGameplay → Loading 的入场淡出接管
        var transitionManager = gameContext != null ? gameContext.SceneTransitionManager : null;
        if (transitionManager != null)
        {
            yield return transitionManager.FadeToBlack(stateGFadeDuration);
        }
        else
        {
            yield return new WaitForSeconds(stateGFadeDuration);
        }

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

    /// <summary>
    /// 即时反馈：快速缩放动画（punch scale），用于点击反馈。
    /// </summary>
    private IEnumerator QuickPunchScale(Transform target, float duration)
    {
        if (target == null)
            yield break;

        Vector3 originalScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0.9f, 1f, EaseOutCubic(t));
            target.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = originalScale;
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

        // 自动同步 blocksRaycasts 和 interactable，避免隐形遮罩层
        group.blocksRaycasts = (to > 0f);
        group.interactable = (to > 0f);
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    /// <summary>
    /// 省略号提示点的呼吸循环，表示腓腓还有后续台词。
    /// 由调用方 StopCoroutine 终止，终止后需自行隐藏 continueIndicator。
    /// </summary>
    private IEnumerator PulseContinueIndicator()
    {
        if (continueIndicator == null)
            yield break;

        var image = continueIndicator.GetComponent<Image>();
        if (image == null)
            yield break;

        continueIndicator.SetActive(true);

        while (true)
        {
            float t = Mathf.PingPong(Time.time * indicatorPulseSpeed, 1f);
            Color color = image.color;
            color.a = Mathf.Lerp(indicatorMinAlpha, 1f, t);
            image.color = color;
            yield return null;
        }
    }

    #endregion
}
