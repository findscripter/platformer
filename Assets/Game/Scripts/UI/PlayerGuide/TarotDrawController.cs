using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Frame 9 塔罗抽牌。关卡实现规则：22 张随机抽 4 张、不重复、正逆位独立随机，
/// 不用 fixedCards。玩家点 4 张牌背，按点击顺序对应 T1–T4。
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

    [Header("卡牌数据（留空则从 Resources 加载全部 22 张）")]
    [SerializeField] private TarotCardData[] deckCards;

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

    [Header("牌阵布局（对白上方的手牌扇）")]
    [SerializeField] private Vector2 arrayCardSize = new Vector2(260f, 458f);
    [Tooltip("相邻牌中心距。小于牌宽才会重叠，露出每张的边。")]
    [SerializeField] private float arraySpacing = 72f;
    [Tooltip("左右最大倾角（度），左负右正。")]
    [SerializeField] private float arrayFanTilt = 4f;
    [Tooltip("两端相对中心下沉的弧度，模拟握在手里。")]
    [SerializeField] private float arrayArcDrop = 18f;

    [Header("其余卡槽位（3 个）")]
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

    private GameContext gameContext;

    private List<GameObject> instantiatedCards = new();
    private readonly TarotCardData[] drawnCards = new TarotCardData[TarotResultData.SlotCount];
    private readonly bool[] drawnReversed = new bool[TarotResultData.SlotCount];

    private int clickedCardIndex = -1;
    private bool acceptingCardClicks;
    private bool flowRunning;
    private int cardBeingAnimated = -1;
    private Vector2 lastLayoutSize;
    private TMP_Text selectionHint;
    private float resultDisplayProgress;

    /// <summary>点击时立即预留的位置，含正在播放和排队的牌。</summary>
    private readonly List<int> drawnCardIndices = new();

    /// <summary>输入缓冲队列：动画期间的点击会缓存在这里，下次等待时优先消费。</summary>
    private readonly Queue<int> pendingClicks = new();

    private Coroutine indicatorPulseRoutine;
    private Coroutine flowRoutine;
    private int flowVersion;

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
    /// 启动塔罗流程（由 TarotView 或旧引导控制器调用）。
    /// </summary>
    public IEnumerator StartTarotFlow()
    {
        if (flowRunning || !isActiveAndEnabled)
            yield break;
        if (!PrepareDrawnHand())
            yield break;

        if (cardPrefab == null || cardParent == null || mainCardSlot == null
            || emotionCardSlots == null || emotionCardSlots.Length < 3
            || System.Array.Exists(emotionCardSlots, slot => slot == null))
        {
            Debug.LogError("[TarotDraw] 缺少牌阵或结果槽引用，无法播放抽牌。");
            yield break;
        }

        flowRunning = true;
        int version = ++flowVersion;
        // 演出由当前面板持有；隐藏面板时可以停止嵌套动画与等待。
        flowRoutine = StartCoroutine(RunFlow(version));
        while (flowRunning && version == flowVersion)
            yield return null;
    }

    private IEnumerator RunFlow(int version)
    {
        try
        {
            gameContext = GameLoop.Instance != null ? GameLoop.Instance.Context : null;
            InitializeUI();
            yield return RunState(TarotState.A_GuideDialogue);
            yield return RunState(TarotState.B_FadeOutDialogue);
            yield return RunState(TarotState.C_FirstCardDraw);
            yield return RunState(TarotState.D_ContinuePrompt);
            yield return RunState(TarotState.F_FullDisplay);
            yield return RunState(TarotState.G_FadeToLevel);
        }
        finally
        {
            if (version == flowVersion)
            {
                acceptingCardClicks = false;
                flowRunning = false;
                flowRoutine = null;
                StopIndicator();
            }
        }
    }

    private void OnDisable()
    {
        // 外部 PlayerGuideController 仍可能在等待本 IEnumerator，显式释放它。
        ++flowVersion;
        acceptingCardClicks = false;
        flowRunning = false;
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);
        flowRoutine = null;
        pendingClicks.Clear();
        StopIndicator();
    }

    private void StopIndicator()
    {
        if (indicatorPulseRoutine != null)
            StopCoroutine(indicatorPulseRoutine);
        indicatorPulseRoutine = null;
        SetPanelActive(continueIndicator, false);
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        // 重置流程状态，支持重复进入
        drawnCardIndices.Clear();
        pendingClicks.Clear();
        acceptingCardClicks = false;
        clickedCardIndex = -1;
        cardBeingAnimated = -1;
        resultDisplayProgress = 0f;

        GuideUiLayout.ConfigureCanvas(this);
        GuideUiLayout.Stretch(transform as RectTransform, Vector2.zero, Vector2.one);
        GuideUiLayout.Stretch(cardParent as RectTransform, Vector2.zero, Vector2.one);
        ApplyTarotBackground();

        SetPanelActive(dialoguePanel, false);
        SetPanelActive(cardArrayContainer, false);
        SetPanelActive(mainCardSlot, false);

        for (int i = 0; i < TarotResultData.SlotCount; i++)
        {
            Transform slot = GetSlot(i);
            slot.localScale = Vector3.one;
            slot.localRotation = Quaternion.identity;
            var backplate = slot.GetComponent<Image>();
            if (backplate != null)
                backplate.enabled = false;
            var face = GetOrCreateSlotCardFace(slot);
            face.sprite = null;
            face.enabled = false;
            if (i == 0)
                mainCardImage = face;
            slot.gameObject.SetActive(false);
        }
        LayoutCards();
    }

    #endregion

    #region State Machine

    private IEnumerator RunState(TarotState state)
    {
        switch (state)
        {
            case TarotState.A_GuideDialogue:
                SetPanelActive(cardArrayContainer, true);
                SpawnCardArray();
                acceptingCardClicks = true;
                UpdateSelectionHint();
                SetPanelActive(dialoguePanel, true);
                if (dialoguePanel != null)
                {
                    GuideUiLayout.LayoutDialogueBar(dialoguePanel.transform as RectTransform,
                        dialogueSpeakerText, dialogueContentText);
                    dialoguePanel.transform.SetAsLastSibling();
                }
                if (dialogueSpeakerText != null)
                    dialogueSpeakerText.text = "腓腓";
                if (dialogueContentText != null)
                    dialogueContentText.text = stateADialogues[0];
                yield return FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, 0.3f);
                indicatorPulseRoutine = StartCoroutine(PulseContinueIndicator());
                foreach (string line in stateADialogues)
                {
                    if (dialogueContentText != null)
                        dialogueContentText.text = line;
                    yield return new WaitForSecondsRealtime(stateADialogueDuration);
                }
                break;

            case TarotState.B_FadeOutDialogue:
                if (indicatorPulseRoutine != null)
                {
                    StopCoroutine(indicatorPulseRoutine);
                    indicatorPulseRoutine = null;
                }
                SetPanelActive(continueIndicator, false);
                yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, stateBFadeDuration);
                SetPanelActive(dialoguePanel, false);
                var feifei = Object.FindAnyObjectByType<FeifeiCharacterView>();
                if (feifei != null)
                {
                    yield return feifei.FadeAlpha(1f, 0f, stateBFadeDuration);
                    feifei.SetActive(false);
                }
                break;

            case TarotState.C_FirstCardDraw:
                yield return WaitForNextCard();
                yield return FlipCard(clickedCardIndex, drawnCards[0], drawnReversed[0]);
                yield return MoveCardToSlot(clickedCardIndex, 0, drawnCards[0], drawnReversed[0]);
                yield return EmphasizeMainCard();
                break;

            case TarotState.D_ContinuePrompt:
                for (int slot = 1; slot < TarotResultData.SlotCount; slot++)
                {
                    yield return WaitForNextCard();
                    yield return FlipCard(clickedCardIndex, drawnCards[slot], drawnReversed[slot]);
                    yield return MoveCardToSlot(clickedCardIndex, slot, drawnCards[slot], drawnReversed[slot]);
                }
                break;

            case TarotState.F_FullDisplay:
                acceptingCardClicks = false;
                if (selectionHint != null)
                    selectionHint.gameObject.SetActive(false);
                yield return FadeOutRemainingCards();
                yield return ExpandResultsForReading();
                yield return new WaitForSecondsRealtime(stateFDisplayDuration);
                if (gameContext != null)
                {
                    var result = new TarotResultData();
                    result.BeginRun(drawnCards, drawnReversed);
                    gameContext.TarotResult = result;
                    Debug.Log("[TarotDraw] " + result.SummarizeDrawn());
                }
                break;

            case TarotState.G_FadeToLevel:
                var transition = gameContext != null ? gameContext.SceneTransitionManager : null;
                if (transition != null)
                    yield return transition.FadeToBlack(stateGFadeDuration);
                else
                    yield return new WaitForSecondsRealtime(stateGFadeDuration);
                if (GameLoop.Instance != null)
                    GameLoop.Instance.ContinueToGameplay();
                break;
        }
    }

    private bool PrepareDrawnHand()
    {
        TarotCardData[] deck = deckCards != null && deckCards.Length == TarotCatalog.All.Length
            ? deckCards : TarotCatalog.LoadDeck();
        var pool = new List<TarotCardData>();
        var ids = new HashSet<string>();
        if (deck != null)
        {
            foreach (var card in deck)
                if (card != null && ids.Add(card.CardId))
                    pool.Add(card);
        }
        if (pool.Count != TarotCatalog.All.Length)
        {
            Debug.LogError("[TarotDraw] 需要完整的 22 张不重复牌组，无法开局。");
            return false;
        }
        // 保留局内四张不放回随机，正逆位分别随机，点击顺序对应 T1–T4。
        for (int i = 0; i < TarotResultData.SlotCount; i++)
        {
            int pick = Random.Range(i, pool.Count);
            TarotCardData swap = pool[i];
            pool[i] = pool[pick];
            pool[pick] = swap;
            drawnCards[i] = pool[i];
            drawnReversed[i] = Random.value < 0.5f;
        }
        return true;
    }

    private void SpawnCardArray()
    {
        foreach (var card in instantiatedCards)
            if (card != null)
            {
                card.SetActive(false);
                Destroy(card);
            }
        instantiatedCards.Clear();
        int cardCount = TarotCatalog.All.Length;
        for (int i = 0; i < cardCount; i++)
        {
            var card = Instantiate(cardPrefab, cardParent, false);
            card.SetActive(true);
            instantiatedCards.Add(card);
            var image = card.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.white;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
                image.raycastTarget = true;
            }
            int captured = i;
            var button = card.GetComponent<Button>();
            if (button == null)
                button = card.AddComponent<Button>();
            button.interactable = true;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnCardClicked(captured));
        }
        // 兼容旧场景遗留的整屏点击层。
        var clickArea = cardArrayContainer != null
            ? cardArrayContainer.transform.Find("FullscreenClickArea") : null;
        if (clickArea != null)
            clickArea.gameObject.SetActive(false);
        LayoutCards();
    }

    private void OnCardClicked(int index)
    {
        if (!acceptingCardClicks || index < 0 || index >= instantiatedCards.Count
            || instantiatedCards[index] == null || drawnCardIndices.Contains(index)
            || drawnCardIndices.Count >= TarotResultData.SlotCount)
            return;
        // 回调内即预留位置，包含正在播放和排队的牌，同帧连点也不会重复入队。
        drawnCardIndices.Add(index);
        pendingClicks.Enqueue(index);
        var card = instantiatedCards[index];
        var button = card.GetComponent<Button>();
        if (button != null)
            button.interactable = false;
        var image = card.GetComponent<Image>();
        if (image != null)
            image.color = new Color(1f, 0.85f, 0.6f, 1f);
        // 颜色反馈不会与翻牌协程争抢缩放；禁用按钮仍挡住下层牌，避免点击穿透。
        if (drawnCardIndices.Count == TarotResultData.SlotCount)
            acceptingCardClicks = false;
        UpdateSelectionHint();
    }

    private IEnumerator WaitForNextCard()
    {
        // 首张与后续三张采用同一队列，开场和动画期间的选择不会被重置。
        while (pendingClicks.Count == 0)
            yield return null;
        clickedCardIndex = pendingClicks.Dequeue();
        cardBeingAnimated = clickedCardIndex;
    }

    private IEnumerator FlipCard(int index, TarotCardData cardData, bool reversed)
    {
        var card = instantiatedCards[index];
        if (card == null)
            yield break;
        GuideSfx.PlayCardFlip();
        var image = card.GetComponent<Image>();
        card.transform.SetParent(transform, true);
        card.transform.SetAsLastSibling();
        bool faceShown = false;
        float elapsed = 0f;
        while (elapsed < cardFlipDuration)
        {
            float t = elapsed / cardFlipDuration;
            // 收窄再展开，侧向不可见时换图。逆位不再把立绘倒过来，改用角标。
            card.transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * Mathf.PI)), 1f, 1f);
            if (t >= 0.5f && !faceShown)
            {
                faceShown = true;
                if (image != null)
                {
                    image.sprite = cardData.CardFace;
                    image.color = Color.white;
                    image.preserveAspect = true;
                }
                card.transform.localRotation = Quaternion.identity;
                SetOrientationMark(card.transform, reversed);
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (image != null)
        {
            image.sprite = cardData.CardFace;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        card.transform.localScale = Vector3.one;
        card.transform.localRotation = Quaternion.identity;
        SetOrientationMark(card.transform, reversed);
    }

    private IEnumerator MoveCardToSlot(int index, int slotIndex, TarotCardData cardData, bool reversed)
    {
        var card = instantiatedCards[index];
        var rect = card != null ? card.transform as RectTransform : null;
        var slot = GetSlot(slotIndex) as RectTransform;
        if (rect == null || slot == null)
            yield break;
        Vector3 startPos = rect.position;
        Vector2 startSize = rect.rect.size;
        float elapsed = 0f;
        while (elapsed < cardMoveDuration)
        {
            float t = EaseOutCubic(elapsed / cardMoveDuration);
            rect.position = Vector3.Lerp(startPos, slot.position, t);
            rect.sizeDelta = Vector2.Lerp(startSize, slot.rect.size, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rect.position = slot.position;
        rect.sizeDelta = slot.rect.size;
        var face = GetOrCreateSlotCardFace(slot);
        face.sprite = cardData.CardFace;
        face.color = Color.white;
        face.enabled = true;
        face.preserveAspect = true;
        face.transform.localRotation = Quaternion.identity;
        SetOrientationMark(slot, reversed);
        slot.gameObject.SetActive(true);
        card.SetActive(false);
        instantiatedCards[index] = null;
        cardBeingAnimated = -1;
        Destroy(card);
    }

    private Image GetOrCreateSlotCardFace(Transform slot)
    {
        Transform face = slot.Find("CardFace");
        if (face == null)
        {
            face = new GameObject("CardFace", typeof(RectTransform), typeof(Image)).transform;
            face.SetParent(slot, false);
        }
        var image = face.GetComponent<Image>();
        if (image == null)
            image = face.gameObject.AddComponent<Image>();
        var fitter = face.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            fitter.enabled = false;
        GuideUiLayout.Stretch(face as RectTransform, Vector2.zero, Vector2.one);
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        face.gameObject.SetActive(true);
        return image;
    }

    private static void SetOrientationMark(Transform parent, bool reversed)
    {
        Transform existing = parent.Find("OrientationMark");
        if (!reversed)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        GameObject go = existing != null ? existing.gameObject : new GameObject("OrientationMark", typeof(RectTransform));
        if (existing == null)
            go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(72f, 32f);
        rect.anchoredPosition = new Vector2(-8f, -8f);

        var tmp = go.GetComponent<TMP_Text>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshProUGUI>();
        GuideUiLayout.ReadableText(tmp, 22f, new Color(0.95f, 0.86f, 0.55f, 1f));
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.text = "逆位";
        tmp.raycastTarget = false;
        go.SetActive(true);
        go.transform.SetAsLastSibling();
    }

    private IEnumerator EmphasizeMainCard()
    {
        Transform slot = mainCardSlot.transform;
        const float duration = 0.18f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            slot.localScale = Vector3.one * Mathf.Lerp(1f, 1.05f, EaseOutCubic(elapsed / duration));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        slot.localScale = Vector3.one * 1.05f;
        // 后续牌已经排队时直接回落；单次选择仍保留最多 0.6 秒的呼吸停顿。
        float hold = Mathf.Clamp(mainCardEmphasisHold, 0f, 0.6f);
        elapsed = 0f;
        while (elapsed < hold && pendingClicks.Count == 0)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            slot.localScale = Vector3.one * Mathf.Lerp(1.05f, 1f, EaseOutCubic(elapsed / duration));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        slot.localScale = Vector3.one;
    }

    private IEnumerator FadeOutRemainingCards()
    {
        // 剩余 Image 直接淡出，避免在这一帧给 18 张牌新增 CanvasGroup 和重建画布。
        var images = new List<Image>();
        foreach (var card in instantiatedCards)
            if (card != null && card.TryGetComponent<Image>(out var image))
            {
                image.raycastTarget = false;
                images.Add(image);
            }
        float elapsed = 0f;
        while (elapsed < remainingFadeDuration)
        {
            float alpha = 1f - elapsed / remainingFadeDuration;
            foreach (var image in images)
                image.color = new Color(1f, 1f, 1f, alpha);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        foreach (var card in instantiatedCards)
            if (card != null)
            {
                card.SetActive(false);
                Destroy(card);
            }
        instantiatedCards.Clear();
        SetPanelActive(cardArrayContainer, false);
    }

    private IEnumerator ExpandResultsForReading()
    {
        float elapsed = 0f;
        const float duration = 0.3f;
        while (elapsed < duration)
        {
            resultDisplayProgress = EaseOutCubic(elapsed / duration);
            LayoutCards();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        resultDisplayProgress = 1f;
        LayoutCards();
    }

    private void ApplyTarotBackground()
    {
        // 面板本身保持全屏；单独的背景子节点负责等比覆盖，避免带着卡槽一起放大。
        var panelImage = GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.sprite = null;
            panelImage.color = new Color(0.025f, 0.025f, 0.035f, 1f);
            panelImage.raycastTarget = false;
        }
        var background = transform.Find("TarotBackground");
        if (background == null)
        {
            background = new GameObject("TarotBackground", typeof(RectTransform), typeof(Image)).transform;
            background.SetParent(transform, false);
        }
        background.SetAsFirstSibling();
        GuideUiLayout.Cover(background.GetComponent<Image>(), GuideArt.TarotBackground);
        if (dialoguePanel != null)
        {
            GuideUiLayout.ApplyDialogueBanner(dialoguePanel.GetComponent<Image>());
        }
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private Transform GetSlot(int index)
    {
        return index == 0 ? mainCardSlot.transform : emotionCardSlots[index - 1];
    }

    private void LateUpdate()
    {
        if (!flowRunning || transform is not RectTransform rect)
            return;
        if (rect.rect.size != lastLayoutSize)
            LayoutCards();
    }

    private void LayoutCards()
    {
        if (transform is not RectTransform panel)
            return;
        Vector2 viewport = panel.rect.size;
        if (viewport.x <= 0f || viewport.y <= 0f)
            return;
        lastLayoutSize = viewport;
        float aspect = arrayCardSize.x > 1f && arrayCardSize.y > 1f
            ? arrayCardSize.x / arrayCardSize.y : 260f / 458f;
        float height = Mathf.Min(458f, viewport.y * 0.36f, viewport.x * 0.2f / aspect);
        float readingHeight = Mathf.Min(640f, viewport.y * 0.68f, viewport.x * 0.205f / aspect);
        height = Mathf.Lerp(height, readingHeight, resultDisplayProgress);
        Vector2 size = new Vector2(height * aspect, height);
        float gap = Mathf.Min(36f, viewport.x * 0.025f);
        for (int i = 0; i < TarotResultData.SlotCount; i++)
        {
            var slot = GetSlot(i) as RectTransform;
            if (slot == null)
                continue;
            var fitter = slot.GetComponent<AspectRatioFitter>();
            if (fitter != null)
                fitter.enabled = false;
            slot.anchorMin = slot.anchorMax = new Vector2(0.5f, Mathf.Lerp(0.79f, 0.54f, resultDisplayProgress));
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.sizeDelta = size;
            slot.anchoredPosition = new Vector2((i - 1.5f) * (size.x + gap), 0f);
        }
        float fanHeight = Mathf.Min(arrayCardSize.y > 1f ? arrayCardSize.y : 458f, viewport.y * 0.3f);
        Vector2 fanSize = new Vector2(fanHeight * aspect, fanHeight);
        int count = instantiatedCards.Count;
        float spacing = Mathf.Min(arraySpacing > 1f ? arraySpacing : 72f,
            Mathf.Max(1f, (viewport.x * 0.92f - fanSize.x) / Mathf.Max(1, count - 1)));
        for (int i = 0; i < count; i++)
        {
            if (instantiatedCards[i] == null || i == cardBeingAnimated)
                continue;
            var card = instantiatedCards[i].transform as RectTransform;
            if (card == null)
                continue;
            float t = count <= 1 ? 0.5f : i / (float)(count - 1);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.46f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = fanSize;
            card.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * spacing,
                -Mathf.Abs(t - 0.5f) * 2f * Mathf.Min(arrayArcDrop, viewport.y * 0.015f));
            card.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(-arrayFanTilt, arrayFanTilt, t));
            card.localScale = Vector3.one;
        }
    }

    private void UpdateSelectionHint()
    {
        if (selectionHint == null)
        {
            var go = new GameObject("SelectionHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            selectionHint = go.GetComponent<TMP_Text>();
            GuideUiLayout.Stretch(selectionHint.rectTransform, new Vector2(0.1f, 0.56f), new Vector2(0.9f, 0.6f));
            GuideUiLayout.ReadableText(selectionHint, 26f, new Color(0.96f, 0.92f, 0.82f, 1f));
            selectionHint.alignment = TextAlignmentOptions.Center;
        }
        selectionHint.gameObject.SetActive(true);
        selectionHint.text = drawnCardIndices.Count < TarotResultData.SlotCount
            ? $"选出四张回应你的牌 · {drawnCardIndices.Count}/4"
            : "四张牌正在回应你……";
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
            elapsed += Time.unscaledDeltaTime;
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
            float t = Mathf.PingPong(Time.unscaledTime * indicatorPulseSpeed, 1f);
            Color color = image.color;
            color.a = Mathf.Lerp(indicatorMinAlpha, 1f, t);
            image.color = color;
            yield return null;
        }
    }

    #endregion
}
