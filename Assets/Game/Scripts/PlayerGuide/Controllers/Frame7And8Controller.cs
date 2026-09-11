using System.Collections;
using UnityEngine;

/// <summary>
/// Frame 7-8：同一问话框写梦与必要补问。
/// Figma：状态 A 写下梦境；仅当意象/情绪不足时原位切到状态 B 补问一次，不销毁 UI。
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
        dreamInputView.ShowWriteState();

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
        string userInput = gameContext?.PlayerDreamInput ?? "";

        if (!dreamInputView.UsedSampleDream && DreamInputValidator.NeedsFollowUp(userInput))
        {
            inputState.IsInFollowUpState = true;

            dreamInputView.EnsureVisible();
            dreamInputView.ShowFollowUpState(userInput);
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

        if (GameLoop.Instance != null && gameContext != null)
            GameLoop.Instance.StartCoroutine(DreamLetterClient.Analyze(gameContext));
    }

    private void HandleReInputSubmit()
    {
        string newInput = dreamInputView.GetInputText();
        if (gameContext != null)
        {
            string original = gameContext.PlayerDreamInput ?? string.Empty;
            gameContext.PlayerDreamInput = string.IsNullOrWhiteSpace(original)
                ? newInput
                : original + " / " + newInput;
        }
        isWaitingForReInput = false;
    }
}
