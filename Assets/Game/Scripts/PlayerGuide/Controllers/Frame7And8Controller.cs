using System.Collections;
using UnityEngine;

/// <summary>
/// Frame 7-8 控制器：输入梦 → 写梦与必要补问。
/// 忠实复刻 PlayerGuideFlowControllerV2.Frame7_DreamInputRoutine / Frame8_WriteDreamRoutine，
/// 并复用已有的 DreamInputValidator 判定逻辑（不重新发明校验规则）。
/// </summary>
public class Frame7And8Controller
{
    private readonly PlayerGuideView view;
    private readonly DreamInputView dreamInputView;
    private readonly DialogueView dialogueView;
    private readonly GameContext gameContext;
    private readonly InputState inputState;

    private bool isWaitingForSubmit;
    private bool isWaitingForReInput;

    public Frame7And8Controller(
        PlayerGuideView view,
        DreamInputView dreamInputView,
        DialogueView dialogueView,
        GameContext gameContext,
        InputState inputState)
    {
        this.view = view;
        this.dreamInputView = dreamInputView;
        this.dialogueView = dialogueView;
        this.gameContext = gameContext;
        this.inputState = inputState;
    }

    public IEnumerator Frame7_DreamInput()
    {
        dreamInputView.SetPanelActive(true);

        if (view.InputCanvasGroup != null)
        {
            yield return dreamInputView.FadeIn(0.3f);
        }

        dreamInputView.SetPlaceholder("写下你的梦境...");
        dreamInputView.ClearAndActivate();

        isWaitingForSubmit = true;
        dreamInputView.OnSubmit += HandleSubmit;
        dreamInputView.BindSubmit();

        yield return new WaitUntil(() => !isWaitingForSubmit);

        dreamInputView.OnSubmit -= HandleSubmit;
        dreamInputView.UnbindSubmit();
    }

    private void HandleSubmit()
    {
        string userInput = dreamInputView.GetInputText();

        if (gameContext != null)
        {
            gameContext.PlayerDreamInput = userInput;
        }

        isWaitingForSubmit = false;
    }

    public IEnumerator Frame8_WriteDreamAndFollowUp()
    {
        string userInput = gameContext?.PlayerDreamInput ?? "";

        if (DreamInputValidator.NeedsFollowUp(userInput))
        {
            inputState.IsInFollowUpState = true;

            dialogueView.SetPanelActive(true);
            if (view.DialogueCanvasGroup != null)
            {
                yield return dialogueView.FadeIn(0.3f);
            }

            dialogueView.SetSpeaker("腓腓");
            dialogueView.SetContent("能再多说一点吗？");

            yield return new WaitForSeconds(2f);

            if (view.DialogueCanvasGroup != null)
            {
                yield return dialogueView.FadeOut(0.3f);
            }
            dialogueView.SetPanelActive(false);

            // 追问后重新征询输入：面板必须先确保可见可交互，
            // 否则 ActivateInputField 和提交按钮都作用在已关闭的对象上，下面的等待会永久挂起。
            dreamInputView.EnsureVisible();
            dreamInputView.ClearAndActivate();

            isWaitingForReInput = true;
            dreamInputView.UnbindSubmit();
            dreamInputView.OnSubmit += HandleReInputSubmit;
            dreamInputView.BindSubmit();

            yield return new WaitUntil(() => !isWaitingForReInput);

            dreamInputView.OnSubmit -= HandleReInputSubmit;
        }

        if (view.InputCanvasGroup != null)
        {
            yield return dreamInputView.FadeOut(0.3f);
        }
        dreamInputView.SetPanelActive(false);
    }

    private void HandleReInputSubmit()
    {
        string newInput = dreamInputView.GetInputText();
        if (gameContext != null)
        {
            gameContext.PlayerDreamInput = newInput;
        }
        isWaitingForReInput = false;
    }
}
