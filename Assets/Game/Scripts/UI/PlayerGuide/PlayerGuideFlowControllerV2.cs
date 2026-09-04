using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新手引导流程控制器 V2 - 完整 10 帧流程
/// Frame 1-10 覆盖从梦境生成到塔罗抽牌的完整新手引导
/// </summary>
public class PlayerGuideFlowControllerV2 : MonoBehaviour
{
    /// <summary>
    /// 引导流程的 10 个主要帧状态
    /// </summary>
    private enum GuideFrame
    {
        Frame1_DreamSpaceGeneration,    // 梦境空间生成
        Frame2_MainBubbleAppear,        // 主梦泡可点击
        Frame3_DreamEchoPlay,           // 梦境残响播放
        Frame4_BubbleDissolve,          // 梦泡消散留下梦核
        Frame5_FeifeiEnterWithCore,     // 腓腓入场捧梦核
        Frame6_FirstMeetingDialogue,    // 第一次相遇对白（6-1 至 6-8）
        Frame7_DreamInput,              // 输入梦
        Frame8_WriteDreamAndFollowUp,   // 写梦与必要补问（双状态）
        Frame9_TarotDrawing,            // 塔罗抽牌（状态 A-G）
        Frame10_CoreResponseAndEnd      // 关卡结束与梦核回应
    }

    #region Inspector Fields

    [Header("Frame 1: 梦境空间生成")]
    [SerializeField] private Transform smallBubblesContainer;     // 小梦泡的父容器
    [SerializeField] private GameObject smallBubblePrefab;         // 小梦泡预制体
    [SerializeField] private int smallBubbleCount = 6;             // 小梦泡数量 (5-8)
    [SerializeField] private Vector2 smallBubbleSizeRange = new Vector2(94f, 128f); // 小梦泡尺寸范围
    [SerializeField] private float smallBubbleFloatSpeed = 20f;    // 漂浮速度 (px/s)
    [SerializeField] private float smallBubbleFloatAmplitude = 15f; // 上下波动幅度

    [Header("Frame 2: 弱引导")]
    [SerializeField] private float frame2HintDelayFirst = 3f;     // 首次提示延迟 3s
    [SerializeField] private float frame2HintDelayRepeat = 5f;    // 重复提示间隔 5s
    [SerializeField] private float frame2HintGlowIntensity = 1.3f; // 提示发光强度
    [SerializeField] private ParticleSystem dreamSpaceParticles;
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [SerializeField] private PlayerGuideMainBubbleVisual mainBubbleVisual;
    [SerializeField] private Button mainBubbleButton;
    [SerializeField] private AudioSource dreamEchoAudioSource;

    [Header("Frame 4-5: 梦核与腓腓")]
    [SerializeField] private GameObject dreamCoreObject;
    [SerializeField] private Image dreamCoreGlow;
    [SerializeField] private GameObject feifeiCharacter;
    [SerializeField] private Animator feifeiAnimator;

    [Header("Frame 6: 对话系统")]
    [SerializeField] private Frame6DialogueConfig frame6Config;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueSpeakerText;
    [SerializeField] private TMP_Text dialogueContentText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Frame 7-8: 输入系统")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField dreamInputField;
    [SerializeField] private TMP_Text inputPlaceholderText;
    [SerializeField] private Button inputSubmitButton;
    [SerializeField] private CanvasGroup inputCanvasGroup;

    [Header("Frame 9: 塔罗系统")]
    [SerializeField] private GameObject tarotPanel;
    [SerializeField] private TarotDrawController tarotController;

    [Header("Frame 10: 结束")]
    [SerializeField] private float coreResponseDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("时长配置")]
    [SerializeField] private float frame1Duration = 3.5f;
    [SerializeField] private float frame3Duration = 2.5f;
    [SerializeField] private float frame4DissolveDuration = 2f;
    [SerializeField] private float frame4CoreStabilizeDuration = 0.5f;
    [SerializeField] private float frame5WalkDuration = 3f;
    [SerializeField] private float frame5PickupDuration = 1.5f;

    #endregion

    #region Private Fields

    private GuideFrame currentFrame;
    private Coroutine currentFrameCoroutine;
    private GameContext gameContext;

    // Frame 6 state
    private int currentDialogueIndex;
    private bool isWaitingForDialogueAdvance;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // 获取 GameContext
        if (GameLoop.Instance != null)
        {
            gameContext = GameLoop.Instance.Context;
        }

        // 初始化所有面板为隐藏
        InitializePanels();

        // 开始 Frame 1
        TransitionToFrame(GuideFrame.Frame1_DreamSpaceGeneration);
    }

    private void Update()
    {
        // Frame 6 的点击继续逻辑
        if (currentFrame == GuideFrame.Frame6_FirstMeetingDialogue && isWaitingForDialogueAdvance)
        {
            // 使用 UI 输入模式的 ConfirmPressed (支持鼠标左键)
            bool advance = false;
            if (gameContext != null && gameContext.InputManager != null)
            {
                advance = gameContext.InputManager.ConfirmPressed;
            }

            if (advance)
            {
                isWaitingForDialogueAdvance = false;
            }
        }
    }

    #endregion

    #region Initialization

    private void InitializePanels()
    {
        SetPanelActive(dreamCoreObject, false);
        SetPanelActive(feifeiCharacter, false);
        SetPanelActive(dialoguePanel, false);
        SetPanelActive(inputPanel, false);
        SetPanelActive(tarotPanel, false);

        if (mainBubbleVisual != null)
            mainBubbleVisual.gameObject.SetActive(false);

        if (backgroundCanvasGroup != null)
            backgroundCanvasGroup.alpha = 0f;
    }

    #endregion

    #region Frame Transition

    private void TransitionToFrame(GuideFrame frame)
    {
        // 停止当前帧的协程
        if (currentFrameCoroutine != null)
        {
            StopCoroutine(currentFrameCoroutine);
            currentFrameCoroutine = null;
        }

        // 清理上一帧的 UI 元素（防止累积显示）
        CleanupPreviousFrame(currentFrame);

        currentFrame = frame;
        Debug.Log($"[PlayerGuide] Entering {frame}");

        // 启动对应帧的协程
        switch (frame)
        {
            case GuideFrame.Frame1_DreamSpaceGeneration:
                currentFrameCoroutine = StartCoroutine(Frame1_DreamSpaceGenerationRoutine());
                break;

            case GuideFrame.Frame2_MainBubbleAppear:
                currentFrameCoroutine = StartCoroutine(Frame2_MainBubbleAppearRoutine());
                break;

            case GuideFrame.Frame3_DreamEchoPlay:
                currentFrameCoroutine = StartCoroutine(Frame3_DreamEchoPlayRoutine());
                break;

            case GuideFrame.Frame4_BubbleDissolve:
                currentFrameCoroutine = StartCoroutine(Frame4_BubbleDissolveRoutine());
                break;

            case GuideFrame.Frame5_FeifeiEnterWithCore:
                currentFrameCoroutine = StartCoroutine(Frame5_FeifeiEnterRoutine());
                break;

            case GuideFrame.Frame6_FirstMeetingDialogue:
                currentFrameCoroutine = StartCoroutine(Frame6_DialogueRoutine());
                break;

            case GuideFrame.Frame7_DreamInput:
                currentFrameCoroutine = StartCoroutine(Frame7_DreamInputRoutine());
                break;

            case GuideFrame.Frame8_WriteDreamAndFollowUp:
                currentFrameCoroutine = StartCoroutine(Frame8_WriteDreamRoutine());
                break;

            case GuideFrame.Frame9_TarotDrawing:
                currentFrameCoroutine = StartCoroutine(Frame9_TarotRoutine());
                break;

            case GuideFrame.Frame10_CoreResponseAndEnd:
                currentFrameCoroutine = StartCoroutine(Frame10_EndRoutine());
                break;
        }
    }

    #endregion

    #region Frame 1: Dream Space Generation

    private IEnumerator Frame1_DreamSpaceGenerationRoutine()
    {
        Debug.Log("[PlayerGuide] Frame 1: 梦境空间生成开始");

        // 播放粒子效果
        if (dreamSpaceParticles != null)
        {
            dreamSpaceParticles.Play();
        }

        // 生成小梦泡（5-8 个，尺寸 94-128px）
        if (smallBubblesContainer != null && smallBubblePrefab != null)
        {
            RectTransform containerRect = smallBubblesContainer as RectTransform;
            if (containerRect == null)
            {
                Debug.LogWarning("[PlayerGuide] smallBubblesContainer 不是 RectTransform，跳过小梦泡生成");
            }
            else
            {
                SpawnSmallBubbles(containerRect);
            }
        }

        // 背景淡入
        if (backgroundCanvasGroup != null)
        {
            yield return FadeCanvasGroup(backgroundCanvasGroup, 0f, 1f, frame1Duration);
        }
        else
        {
            yield return new WaitForSeconds(frame1Duration);
        }

        // TODO: 播放音效 "dream_space_generate"

        Debug.Log("[PlayerGuide] Frame 1 完成，进入 Frame 2");
        TransitionToFrame(GuideFrame.Frame2_MainBubbleAppear);
    }

    private void SpawnSmallBubbles(RectTransform containerRect)
    {
        float width = containerRect.rect.width;
        float height = containerRect.rect.height;

        for (int i = 0; i < smallBubbleCount; i++)
        {
            GameObject bubble = Instantiate(smallBubblePrefab, containerRect);
            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            if (bubbleRect == null) continue;

            // 随机尺寸
            float size = Random.Range(smallBubbleSizeRange.x, smallBubbleSizeRange.y);
            bubbleRect.sizeDelta = new Vector2(size, size);

            // 随机位置（避免屏幕边缘）
            float margin = size * 0.5f + 50f;
            float x = Random.Range(-width * 0.5f + margin, width * 0.5f - margin);
            float y = Random.Range(-height * 0.5f + margin, height * 0.5f - margin);
            bubbleRect.anchoredPosition = new Vector2(x, y);

            // 启动漂浮协程
            StartCoroutine(FloatSmallBubble(bubbleRect, x, y));
        }
    }

    private IEnumerator FloatSmallBubble(RectTransform bubbleRect, float startX, float startY)
    {
        float time = 0f;
        float randomPhase = Random.Range(0f, Mathf.PI * 2f);

        while (bubbleRect != null && gameObject.activeInHierarchy)
        {
            time += Time.deltaTime;

            // 上下波动 (sin wave)
            float offsetY = Mathf.Sin(time + randomPhase) * smallBubbleFloatAmplitude;

            // 水平缓慢漂移
            float offsetX = (time * smallBubbleFloatSpeed) % 100f - 50f;

            bubbleRect.anchoredPosition = new Vector2(startX + offsetX, startY + offsetY);

            yield return null;
        }
    }

    #endregion

    #region Frame 2: Main Bubble Appear

    private IEnumerator Frame2_MainBubbleAppearRoutine()
    {
        // 显示主梦泡
        if (mainBubbleVisual != null)
        {
            mainBubbleVisual.gameObject.SetActive(true);

            // 确保父级 Image 的 alpha 为 1（修复透明问题）
            var parentImage = mainBubbleVisual.GetComponent<Image>();
            if (parentImage != null)
            {
                var color = parentImage.color;
                color.a = 1f;
                parentImage.color = color;
            }

            // 播放出现动画（缩放从 0 到 1）
            yield return ScaleTransform(
                mainBubbleVisual.transform,
                Vector3.zero,
                Vector3.one,
                0.5f
            );
        }

        // 启用点击
        if (mainBubbleButton != null)
        {
            mainBubbleButton.interactable = true;
            mainBubbleButton.onClick.AddListener(OnMainBubbleClicked);
        }

        // 弱引导：3s 无操作后提示，每 5s 重复一次
        float elapsedSinceLastHint = 0f;
        float nextHintTime = frame2HintDelayFirst;
        bool hintActive = false;

        while (currentFrame == GuideFrame.Frame2_MainBubbleAppear)
        {
            elapsedSinceLastHint += Time.deltaTime;

            if (elapsedSinceLastHint >= nextHintTime && !hintActive)
            {
                // 触发提示：梦泡微微变亮
                StartCoroutine(ApplyBubbleHintGlow());
                hintActive = true;

                // TODO: 播放提示音效
                // AudioManager.PlaySFX("bubble_hint");

                // 下次提示间隔
                elapsedSinceLastHint = 0f;
                nextHintTime = frame2HintDelayRepeat;
                hintActive = false;
            }

            yield return null;
        }
    }

    private IEnumerator ApplyBubbleHintGlow()
    {
        if (mainBubbleVisual == null) yield break;

        Transform bubbleTransform = mainBubbleVisual.transform;
        Vector3 originalScale = bubbleTransform.localScale;
        Vector3 targetScale = originalScale * frame2HintGlowIntensity;

        // 放大 0.3s
        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            bubbleTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 恢复 0.3s
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            bubbleTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        bubbleTransform.localScale = originalScale;
    }

    private void OnMainBubbleClicked()
    {
        if (mainBubbleButton != null)
        {
            mainBubbleButton.interactable = false;
            mainBubbleButton.onClick.RemoveListener(OnMainBubbleClicked);
        }

        TransitionToFrame(GuideFrame.Frame3_DreamEchoPlay);
    }

    #endregion

    #region Frame 3: Dream Echo Play

    private IEnumerator Frame3_DreamEchoPlayRoutine()
    {
        // 播放音频残响
        if (dreamEchoAudioSource != null && dreamEchoAudioSource.clip != null)
        {
            dreamEchoAudioSource.volume = 0.7f;
            dreamEchoAudioSource.Play();
        }

        // 梦泡轻微震动效果
        if (mainBubbleVisual != null)
        {
            Transform bubbleTransform = mainBubbleVisual.transform;
            Vector3 originalScale = bubbleTransform.localScale;

            float elapsed = 0f;
            while (elapsed < frame3Duration)
            {
                float vibration = Mathf.Sin(elapsed * 10f) * 0.05f;
                bubbleTransform.localScale = originalScale * (1f + vibration);

                elapsed += Time.deltaTime;
                yield return null;
            }

            bubbleTransform.localScale = originalScale;
        }
        else
        {
            yield return new WaitForSeconds(frame3Duration);
        }

        TransitionToFrame(GuideFrame.Frame4_BubbleDissolve);
    }

    #endregion

    #region Frame 4: Bubble Dissolve

    private IEnumerator Frame4_BubbleDissolveRoutine()
    {
        // 梦泡消散动画（alpha 渐变 + 缩放），持续 2s
        if (mainBubbleVisual != null)
        {
            CanvasGroup bubbleGroup = mainBubbleVisual.GetComponent<CanvasGroup>();
            if (bubbleGroup == null)
            {
                bubbleGroup = mainBubbleVisual.gameObject.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            Vector3 startScale = mainBubbleVisual.transform.localScale;

            while (elapsed < frame4DissolveDuration)
            {
                float t = elapsed / frame4DissolveDuration;
                bubbleGroup.alpha = 1f - t;
                mainBubbleVisual.transform.localScale = startScale * (1f - t * 0.5f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainBubbleVisual.gameObject.SetActive(false);
        }

        // TODO: 播放音效 "bubble_dissolve"

        // 梦核显现（同时进行，不等待梦泡消散结束）
        if (dreamCoreObject != null)
        {
            dreamCoreObject.SetActive(true);

            // 梦核淡入 + 发光
            CanvasGroup coreGroup = dreamCoreObject.GetComponent<CanvasGroup>();
            if (coreGroup == null)
            {
                coreGroup = dreamCoreObject.AddComponent<CanvasGroup>();
            }

            yield return FadeCanvasGroup(coreGroup, 0f, 1f, 1.5f);
        }

        // 梦核稳定 0.5s
        yield return new WaitForSeconds(frame4CoreStabilizeDuration);

        TransitionToFrame(GuideFrame.Frame5_FeifeiEnterWithCore);
    }

    #endregion

    #region Frame 5: Feifei Enter

    private IEnumerator Frame5_FeifeiEnterRoutine()
    {
        // 腓腓从右侧进入
        if (feifeiCharacter != null)
        {
            feifeiCharacter.SetActive(true);

            Vector3 startPos = new Vector3(800f, 0f, 0f);
            Vector3 endPos = Vector3.zero;
            feifeiCharacter.transform.localPosition = startPos;

            // TODO: 播放脚步声音效（循环）

            // 行走动画
            SafeSetBool(feifeiAnimator, "IsWalking", true);

            // 移动到中央
            float elapsed = 0f;
            while (elapsed < frame5WalkDuration)
            {
                float t = elapsed / frame5WalkDuration;
                t = EaseOutCubic(t);
                feifeiCharacter.transform.localPosition = Vector3.Lerp(startPos, endPos, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            feifeiCharacter.transform.localPosition = endPos;

            // 停止行走
            SafeSetBool(feifeiAnimator, "IsWalking", false);

            // TODO: 停止脚步声

            yield return new WaitForSeconds(0.5f);

            // 捧起梦核动作
            SafeSetTrigger(feifeiAnimator, "PickUpCore");

            // TODO: 播放拾取音效 "core_pickup"

            yield return new WaitForSeconds(frame5PickupDuration);
        }

        yield return new WaitForSeconds(1f);

        TransitionToFrame(GuideFrame.Frame6_FirstMeetingDialogue);
    }

    #endregion

    #region Frame 6: First Meeting Dialogue

    private IEnumerator Frame6_DialogueRoutine()
    {
        if (frame6Config == null || frame6Config.SegmentCount == 0)
        {
            Debug.LogWarning("[PlayerGuide] Frame6DialogueConfig is missing!");
            TransitionToFrame(GuideFrame.Frame7_DreamInput);
            yield break;
        }

        // 显示对话面板
        SetPanelActive(dialoguePanel, true);

        // 启用 UI 输入模式以支持鼠标点击
        if (gameContext != null && gameContext.InputManager != null)
        {
            gameContext.InputManager.EnableUIInput();
        }

        // 降低腓腓动画速度
        if (feifeiAnimator != null)
        {
            feifeiAnimator.speed = 0.6f;
        }

        // 播放 8 段对话
        for (int i = 0; i < frame6Config.SegmentCount; i++)
        {
            yield return PlayDialogueSegment(i);
        }

        // 隐藏对话面板
        if (dialogueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, 0.3f);
        }
        SetPanelActive(dialoguePanel, false);

        // 恢复游戏输入模式
        if (gameContext != null && gameContext.InputManager != null)
        {
            gameContext.InputManager.EnableGameplayInput();
        }

        // 恢复腓腓动画速度
        if (feifeiAnimator != null)
        {
            feifeiAnimator.speed = 1.0f;
        }

        TransitionToFrame(GuideFrame.Frame7_DreamInput);
    }

    private IEnumerator PlayDialogueSegment(int index)
    {
        var segment = frame6Config.GetSegment(index);
        if (segment == null)
            yield break;

        // 最后一段（6-8）是 Fade，无文本
        if (string.IsNullOrWhiteSpace(segment.text))
        {
            // TODO: 播放 Fade 动画
            yield return new WaitForSeconds(segment.suggestedDuration);
            yield break;
        }

        // 显示对话文本
        if (dialogueSpeakerText != null)
        {
            dialogueSpeakerText.text = "腓腓";
        }

        if (dialogueContentText != null)
        {
            dialogueContentText.text = segment.text;
        }

        // 播放角色动画
        SafePlayState(feifeiAnimator, segment.feifeiAnimation);

        // 梦核光效
        if (segment.coreGlowIntensity > 0f && dreamCoreGlow != null)
        {
            Color glowColor = dreamCoreGlow.color;
            glowColor.a = segment.coreGlowIntensity;
            dreamCoreGlow.color = glowColor;
        }

        // TODO: 梦核涟漪效果
        if (segment.playCoreRipple)
        {
            // PlayCoreRippleEffect();
        }

        // 等待玩家点击或自动播放
        isWaitingForDialogueAdvance = true;
        float elapsed = 0f;

        while (isWaitingForDialogueAdvance && elapsed < segment.suggestedDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isWaitingForDialogueAdvance = false;
    }

    #endregion

    #region Frame 7: Dream Input

    private IEnumerator Frame7_DreamInputRoutine()
    {
        // 显示输入面板
        SetPanelActive(inputPanel, true);

        // 输入框弹出动画
        if (inputCanvasGroup != null)
        {
            yield return FadeCanvasGroup(inputCanvasGroup, 0f, 1f, 0.3f);
        }

        // 设置占位符
        if (inputPlaceholderText != null)
        {
            inputPlaceholderText.text = "写下你的梦境...";
        }

        // 清空输入框
        if (dreamInputField != null)
        {
            dreamInputField.text = "";
            dreamInputField.ActivateInputField();
        }

        // 绑定提交按钮
        if (inputSubmitButton != null)
        {
            inputSubmitButton.onClick.AddListener(OnDreamInputSubmitted);
        }

        // 等待用户提交（不自动跳转）
    }

    private void OnDreamInputSubmitted()
    {
        if (dreamInputField == null)
            return;

        string userInput = dreamInputField.text.Trim();

        // 存储到 GameContext
        if (gameContext != null)
        {
            gameContext.PlayerDreamInput = userInput;
        }

        // 解绑按钮
        if (inputSubmitButton != null)
        {
            inputSubmitButton.onClick.RemoveListener(OnDreamInputSubmitted);
        }

        // 进入 Frame 8
        TransitionToFrame(GuideFrame.Frame8_WriteDreamAndFollowUp);
    }

    #endregion

    #region Frame 8: Write Dream and Follow Up

    private IEnumerator Frame8_WriteDreamRoutine()
    {
        string userInput = gameContext?.PlayerDreamInput ?? "";

        // 判断是否需要补问
        if (DreamInputValidator.NeedsFollowUp(userInput))
        {
            // 状态 B：腓腓补问

            // 显示对话面板
            SetPanelActive(dialoguePanel, true);
            if (dialogueCanvasGroup != null)
            {
                yield return FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, 0.3f);
            }

            if (dialogueSpeakerText != null)
                dialogueSpeakerText.text = "腓腓";

            if (dialogueContentText != null)
                dialogueContentText.text = "能再多说一点吗？";

            yield return new WaitForSeconds(2f);

            // 隐藏对话，重新显示输入框
            if (dialogueCanvasGroup != null)
            {
                yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, 0.3f);
            }
            SetPanelActive(dialoguePanel, false);

            // 清空输入框，等待重新输入
            if (dreamInputField != null)
            {
                dreamInputField.text = "";
                dreamInputField.ActivateInputField();
            }

            bool isWaitingForReInput = true;

            if (inputSubmitButton != null)
            {
                inputSubmitButton.onClick.RemoveAllListeners();
                inputSubmitButton.onClick.AddListener(() =>
                {
                    string newInput = dreamInputField.text.Trim();
                    if (gameContext != null)
                    {
                        gameContext.PlayerDreamInput = newInput;
                    }
                    isWaitingForReInput = false;
                });
            }

            yield return new WaitUntil(() => !isWaitingForReInput);
        }

        // 隐藏输入面板
        if (inputCanvasGroup != null)
        {
            yield return FadeCanvasGroup(inputCanvasGroup, 1f, 0f, 0.3f);
        }
        SetPanelActive(inputPanel, false);

        // 进入 Frame 9
        TransitionToFrame(GuideFrame.Frame9_TarotDrawing);
    }

    #endregion

    #region Frame 9: Tarot Drawing

    private IEnumerator Frame9_TarotRoutine()
    {
        // 显示塔罗面板
        SetPanelActive(tarotPanel, true);

        // 启动塔罗控制器
        if (tarotController != null)
        {
            yield return tarotController.StartTarotFlow();
        }
        else
        {
            Debug.LogWarning("[PlayerGuide] TarotDrawController is missing!");
            yield return new WaitForSeconds(2f);
        }

        // 塔罗系统完成后会自动调用 GameLoop.ContinueToGameplay()
        // 这里不需要手动跳转
    }

    #endregion

    #region Frame 10: Core Response and End

    private IEnumerator Frame10_EndRoutine()
    {
        // 梦核发光增强
        if (dreamCoreGlow != null)
        {
            Color startColor = dreamCoreGlow.color;
            Color endColor = startColor;
            endColor.a = 1f;

            float elapsed = 0f;
            while (elapsed < coreResponseDuration)
            {
                float t = elapsed / coreResponseDuration;
                dreamCoreGlow.color = Color.Lerp(startColor, endColor, t);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // TODO: 播放音效 "core_response"

        yield return new WaitForSeconds(coreResponseDuration);

        // Fade 到黑
        // TODO: 调用 SceneTransitionManager.FadeOut(fadeOutDuration)
        yield return new WaitForSeconds(fadeOutDuration);

        // 加载教学关
        if (GameLoop.Instance != null)
        {
            GameLoop.Instance.ContinueToGameplay();
        }
    }

    #endregion

    #region Frame Cleanup

    /// <summary>
    /// 清理上一帧的 UI 元素，防止累积显示
    /// </summary>
    private void CleanupPreviousFrame(GuideFrame previousFrame)
    {
        switch (previousFrame)
        {
            case GuideFrame.Frame1_DreamSpaceGeneration:
                // 隐藏小气泡容器（停止所有漂浮协程）
                if (smallBubblesContainer != null)
                {
                    // 销毁所有生成的小气泡
                    foreach (Transform child in smallBubblesContainer)
                    {
                        if (child.gameObject != null)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }
                // 停止粒子效果
                if (dreamSpaceParticles != null)
                {
                    dreamSpaceParticles.Stop();
                }
                break;

            case GuideFrame.Frame2_MainBubbleAppear:
                // 隐藏主梦泡（如果还在显示）
                if (mainBubbleVisual != null)
                {
                    mainBubbleVisual.gameObject.SetActive(false);
                }
                // 确保按钮监听器被移除
                if (mainBubbleButton != null)
                {
                    mainBubbleButton.onClick.RemoveListener(OnMainBubbleClicked);
                    mainBubbleButton.interactable = false;
                }
                break;

            case GuideFrame.Frame3_DreamEchoPlay:
                // 停止音频
                if (dreamEchoAudioSource != null && dreamEchoAudioSource.isPlaying)
                {
                    dreamEchoAudioSource.Stop();
                }
                break;

            case GuideFrame.Frame4_BubbleDissolve:
                // Frame 4 自己会隐藏主梦泡，这里确保梦核保持显示
                break;

            case GuideFrame.Frame5_FeifeiEnterWithCore:
                // 腓腓和梦核在后续帧继续使用，不隐藏
                break;

            case GuideFrame.Frame6_FirstMeetingDialogue:
                // 隐藏对话面板
                SetPanelActive(dialoguePanel, false);
                break;

            case GuideFrame.Frame7_DreamInput:
            case GuideFrame.Frame8_WriteDreamAndFollowUp:
                // 隐藏输入面板
                SetPanelActive(inputPanel, false);
                if (inputSubmitButton != null)
                {
                    inputSubmitButton.onClick.RemoveAllListeners();
                }
                break;

            case GuideFrame.Frame9_TarotDrawing:
                // 隐藏塔罗面板
                SetPanelActive(tarotPanel, false);
                break;

            case GuideFrame.Frame10_CoreResponseAndEnd:
                // 最终帧，无需清理
                break;
        }
    }

    #endregion

    #region Animator Helper Methods

    /// <summary>
    /// 安全地设置 Animator Bool 参数（仅当参数存在时）
    /// </summary>
    private void SafeSetBool(Animator animator, string paramName, bool value)
    {
        if (animator == null) return;

        foreach (var param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(paramName, value);
                return;
            }
        }

        Debug.LogWarning($"[PlayerGuide] Animator parameter '{paramName}' (Bool) not found");
    }

    /// <summary>
    /// 安全地触发 Animator Trigger（仅当参数存在时）
    /// </summary>
    private void SafeSetTrigger(Animator animator, string paramName)
    {
        if (animator == null) return;

        foreach (var param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(paramName);
                return;
            }
        }

        Debug.LogWarning($"[PlayerGuide] Animator parameter '{paramName}' (Trigger) not found");
    }

    /// <summary>
    /// 安全地播放 Animator 状态（仅当状态存在时）
    /// </summary>
    private void SafePlayState(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;

        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, Animator.StringToHash(stateName)))
            {
                animator.Play(stateName);
                return;
            }
        }

        Debug.LogWarning($"[PlayerGuide] Animator state '{stateName}' not found");
    }

    #endregion

    #region Utility Methods

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
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

    private IEnumerator ScaleTransform(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = EaseOutCubic(t);
            target.localScale = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = to;
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    #endregion
}
