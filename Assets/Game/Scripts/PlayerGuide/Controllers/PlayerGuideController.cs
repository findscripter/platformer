using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerGuide 主状态机控制器 - 完整 10 帧流程。
/// 忠实复刻 PlayerGuideFlowControllerV2，拆分为 MVC：
/// - PlayerGuideView 持有 Inspector 引用
/// - 各 FrameXView 负责单一视觉职责的显示/动画
/// - FrameXController（普通类）负责编排单帧内的协程逻辑
/// 本类负责帧切换、清理与跨帧共享状态（Update 中的对话推进检测）。
/// </summary>
public class PlayerGuideController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private PlayerGuideView view;
    [SerializeField] private DialogueView dialogueView;
    [SerializeField] private DreamInputView dreamInputView;
    [SerializeField] private TarotView tarotView;
    [SerializeField] private FeifeiCharacterView feifeiView;
    [SerializeField] private DreamCoreView dreamCoreView;
    [SerializeField] private SmallBubblesView smallBubblesView;
    [SerializeField] private MainBubbleView mainBubbleView;
    [SerializeField] private BackgroundView backgroundView;

    private GameContext gameContext;
    private PlayerGuideFrameState guideState;
    private Frame6DialogueTrackingState dialogueState;
    private InputState inputState;

    private Frame1To5Controller frame1To5Controller;
    private Frame6DialogueController frame6Controller;
    private Frame7And8Controller frame7And8Controller;
    private Frame9And10Controller frame9And10Controller;

    private Coroutine currentFrameCoroutine;
    private Coroutine hintLoopCoroutine;

    private void Start()
    {
        if (GameLoop.Instance != null)
        {
            gameContext = GameLoop.Instance.Context;
        }

        guideState = new PlayerGuideFrameState();
        dialogueState = new Frame6DialogueTrackingState();
        inputState = new InputState();

        frame1To5Controller = new Frame1To5Controller(view, smallBubblesView, mainBubbleView, backgroundView, dreamCoreView, feifeiView, this);
        frame6Controller = new Frame6DialogueController(view, dialogueView, feifeiView, dreamCoreView, gameContext, dialogueState);
        frame7And8Controller = new Frame7And8Controller(view, dreamInputView, dialogueView, gameContext, inputState);
        frame9And10Controller = new Frame9And10Controller(view, tarotView, dreamCoreView);

        InitializePanels();

        if (gameContext != null && (gameContext.ReplaySameDream || gameContext.ReplayNewDream))
        {
            bool sameDream = gameContext.ReplaySameDream;
            gameContext.ReplaySameDream = gameContext.ReplayNewDream = false;
            if (backgroundView != null)
                backgroundView.SetAlpha(1f);
            TransitionToFrame(sameDream ? GuideFrame.Frame9_TarotDrawing : GuideFrame.Frame7_DreamInput);
        }
        else
        {
            TransitionToFrame(GuideFrame.Frame1_DreamSpaceGeneration);
        }
    }

    private void Update()
    {
        if (guideState == null)
            return;

        if (guideState.CurrentFrame == GuideFrame.Frame6_FirstMeetingDialogue && dialogueState.IsWaitingForDialogueAdvance)
        {
            bool advance = false;
            if (gameContext != null && gameContext.InputManager != null)
            {
                advance = gameContext.InputManager.ConfirmPressed
                    || gameContext.InputManager.InteractPressed;
            }

            if (!advance && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                advance = true;
            }

            if (advance)
            {
                dialogueState.IsWaitingForDialogueAdvance = false;
            }
        }
    }

    private void InitializePanels()
    {
        dreamCoreView.SetActive(false);
        feifeiView.SetActive(false);
        dialogueView.SetPanelActive(false);
        dreamInputView.SetPanelActive(false);
        tarotView.SetPanelActive(false);

        mainBubbleView.SetActive(false);
        backgroundView.SetAlpha(0f);
    }

    private void TransitionToFrame(GuideFrame frame)
    {
        if (currentFrameCoroutine != null)
        {
            StopCoroutine(currentFrameCoroutine);
            currentFrameCoroutine = null;
        }

        if (hintLoopCoroutine != null)
        {
            StopCoroutine(hintLoopCoroutine);
            hintLoopCoroutine = null;
        }

        CleanupPreviousFrame(guideState.CurrentFrame);

        guideState.CurrentFrame = frame;
        PrepareStage(frame);
        Debug.Log($"[PlayerGuide] Entering {frame}");

        switch (frame)
        {
            case GuideFrame.Frame1_DreamSpaceGeneration:
                currentFrameCoroutine = StartCoroutine(Frame1Routine());
                break;

            case GuideFrame.Frame2_MainBubbleAppear:
                currentFrameCoroutine = StartCoroutine(Frame2Routine());
                break;

            case GuideFrame.Frame3_DreamEchoPlay:
                currentFrameCoroutine = StartCoroutine(Frame3Routine());
                break;

            case GuideFrame.Frame4_BubbleDissolve:
                currentFrameCoroutine = StartCoroutine(Frame4Routine());
                break;

            case GuideFrame.Frame5_FeifeiEnterWithCore:
                currentFrameCoroutine = StartCoroutine(Frame5Routine());
                break;

            case GuideFrame.Frame6_FirstMeetingDialogue:
                currentFrameCoroutine = StartCoroutine(Frame6Routine());
                break;

            case GuideFrame.Frame7_DreamInput:
                currentFrameCoroutine = StartCoroutine(Frame7Routine());
                break;

            case GuideFrame.Frame8_WriteDreamAndFollowUp:
                currentFrameCoroutine = StartCoroutine(Frame8Routine());
                break;

            case GuideFrame.Frame9_TarotDrawing:
                currentFrameCoroutine = StartCoroutine(Frame9Routine());
                break;

            case GuideFrame.Frame10_CoreResponseAndEnd:
                currentFrameCoroutine = StartCoroutine(Frame10Routine());
                break;
        }
    }

    private IEnumerator Frame1Routine()
    {
        yield return frame1To5Controller.Frame1_DreamSpaceGeneration();
        Debug.Log("[PlayerGuide] Frame 1 完成，进入 Frame 2");
        TransitionToFrame(GuideFrame.Frame2_MainBubbleAppear);
    }

    private IEnumerator Frame2Routine()
    {
        yield return frame1To5Controller.Frame2_MainBubbleAppear();
        mainBubbleView.OnClicked += OnMainBubbleClicked;
        hintLoopCoroutine = StartCoroutine(frame1To5Controller.Frame2_HintLoop(() => guideState.CurrentFrame == GuideFrame.Frame2_MainBubbleAppear));
    }

    private void OnMainBubbleClicked()
    {
        mainBubbleView.OnClicked -= OnMainBubbleClicked;
        mainBubbleView.DisableClick();
        GuideSfx.PlayClick();
        TransitionToFrame(GuideFrame.Frame3_DreamEchoPlay);
    }

    private IEnumerator Frame3Routine()
    {
        yield return frame1To5Controller.Frame3_DreamEchoPlay();
        TransitionToFrame(GuideFrame.Frame4_BubbleDissolve);
    }

    private IEnumerator Frame4Routine()
    {
        yield return frame1To5Controller.Frame4_BubbleDissolve();
        TransitionToFrame(GuideFrame.Frame5_FeifeiEnterWithCore);
    }

    private IEnumerator Frame5Routine()
    {
        yield return frame1To5Controller.Frame5_FeifeiEnter();
        TransitionToFrame(GuideFrame.Frame6_FirstMeetingDialogue);
    }

    private IEnumerator Frame6Routine()
    {
        yield return frame6Controller.RunDialogue();
        TransitionToFrame(GuideFrame.Frame7_DreamInput);
    }

    private IEnumerator Frame7Routine()
    {
        yield return frame7And8Controller.Frame7_DreamInput();
        TransitionToFrame(GuideFrame.Frame8_WriteDreamAndFollowUp);
    }

    private IEnumerator Frame8Routine()
    {
        yield return frame7And8Controller.Frame8_WriteDreamAndFollowUp();
        TransitionToFrame(GuideFrame.Frame9_TarotDrawing);
    }

    private IEnumerator Frame9Routine()
    {
        yield return frame9And10Controller.Frame9_TarotDrawing();
        // 塔罗系统完成后会自动调用 GameLoop.ContinueToGameplay()，这里不需要手动跳转
    }

    private IEnumerator Frame10Routine()
    {
        yield return frame9And10Controller.Frame10_CoreResponseAndEnd();
    }

    /// <summary>
    /// 每帧只保留当前该出现的层，避免写梦/塔罗/对白叠在一起。
    /// </summary>
    private void PrepareStage(GuideFrame frame)
    {
        switch (frame)
        {
            case GuideFrame.Frame6_FirstMeetingDialogue:
                dreamInputView.SetPanelActive(false);
                tarotView.SetPanelActive(false);
                backgroundView.SetBackgroundSprite(
                    GuideArt.DreamBackground != null ? GuideArt.DreamBackground : view.DreamBackgroundSpriteLate);
                break;

            case GuideFrame.Frame7_DreamInput:
            case GuideFrame.Frame8_WriteDreamAndFollowUp:
                dialogueView.SetPanelActive(false);
                dialogueView.ShowPlayerStandIn(false);
                tarotView.SetPanelActive(false);
                dreamCoreView.SetActive(false);
                feifeiView.SetCompanionLayout();
                if (view.InputPanel != null)
                    view.InputPanel.transform.SetAsLastSibling();
                break;

            case GuideFrame.Frame9_TarotDrawing:
                dialogueView.SetPanelActive(false);
                dialogueView.ShowPlayerStandIn(false);
                dreamInputView.SetPanelActive(false);
                dreamCoreView.SetActive(false);
                feifeiView.SetActive(false);
                if (view.TarotPanel != null)
                    view.TarotPanel.transform.SetAsLastSibling();
                break;
        }
    }

    private void CleanupPreviousFrame(GuideFrame previousFrame)
    {
        switch (previousFrame)
        {
            case GuideFrame.Frame1_DreamSpaceGeneration:
                // 保留小梦泡作为背景装饰（设计稿：Frame 1-3 期间小梦泡持续漂浮）
                // 粒子系统停止
                if (view.DreamSpaceParticles != null)
                {
                    view.DreamSpaceParticles.Stop();
                }
                break;

            case GuideFrame.Frame2_MainBubbleAppear:
                // 只解绑交互，不隐藏梦泡：Frame 3 要震动它、Frame 4 才让它消散。
                // 此处 SetActive(false) 会导致主梦泡在 Frame 2→3 之间闪一下。
                mainBubbleView.OnClicked -= OnMainBubbleClicked;
                mainBubbleView.DisableClick();
                mainBubbleView.ShowClickHint(false);
                break;

            case GuideFrame.Frame3_DreamEchoPlay:
                backgroundView.StopDreamEcho();
                break;

            case GuideFrame.Frame4_BubbleDissolve:
                // Frame 4 主梦泡消散时，同时清理背景小梦泡
                if (view.SmallBubblesContainer != null)
                {
                    foreach (Transform child in view.SmallBubblesContainer)
                    {
                        if (child.gameObject != null)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }
                break;

            case GuideFrame.Frame5_FeifeiEnterWithCore:
                // 腓腓和梦核在后续帧继续使用，不隐藏
                break;

            case GuideFrame.Frame6_FirstMeetingDialogue:
                dialogueView.SetPanelActive(false);
                break;

            case GuideFrame.Frame7_DreamInput:
                // 只解绑提交，不隐藏面板：Frame 8 的追问分支还要让玩家重新输入。
                // 此处 SetPanelActive(false) 会让 Frame 8 的 WaitUntil 永远等不到提交（死锁）。
                // 面板由 Frame 8 自己在结尾淡出并关闭。
                dreamInputView.UnbindSubmit();
                break;

            case GuideFrame.Frame8_WriteDreamAndFollowUp:
                dreamInputView.SetPanelActive(false);
                dreamInputView.UnbindSubmit();
                break;

            case GuideFrame.Frame9_TarotDrawing:
                tarotView.SetPanelActive(false);
                break;

            case GuideFrame.Frame10_CoreResponseAndEnd:
                // 最终帧，无需清理
                break;
        }
    }
}
