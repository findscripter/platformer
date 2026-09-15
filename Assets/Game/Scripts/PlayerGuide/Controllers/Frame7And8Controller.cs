using System.Collections;
using UnityEngine;

/// <summary>
/// Frame 7-8：自由写梦后直接继续，不强制字数或补问。
/// </summary>
public class Frame7And8Controller
{
    private readonly PlayerGuideView view;
    private readonly DreamInputView dreamInputView;
    private readonly GameContext gameContext;
    private readonly InputState inputState;

    private bool isWaitingForSubmit;

    public Frame7And8Controller(
        PlayerGuideView view,
        DreamInputView dreamInputView,
        DialogueView dialogueView,
        GameContext gameContext,
        InputState inputState)
    {
        this.view = view;
        this.dreamInputView = dreamInputView;
        this.gameContext = gameContext;
        this.inputState = inputState;
    }

    public IEnumerator Frame7_DreamInput()
    {
        dreamInputView.SetPanelActive(true);
        dreamInputView.ShowWriteState();
        GuideSfx.PlaySoftTransition();

        if (view.InputCanvasGroup != null)
        {
            yield return dreamInputView.FadeIn(0.3f);
        }

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
        // Any amount of dream text is accepted. Sparse input must not trap the
        // player in a mandatory second question or replace their own words.
        inputState.IsInFollowUpState = false;

        if (view.InputCanvasGroup != null)
        {
            yield return dreamInputView.FadeOut(0.3f);
        }
        dreamInputView.SetPanelActive(false);

        if (GameLoop.Instance != null && gameContext != null)
            GameLoop.Instance.StartCoroutine(DreamLetterClient.Analyze(gameContext));
    }

}
