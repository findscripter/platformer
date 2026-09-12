using System.Collections;
using UnityEngine;

/// <summary>
/// Frame 6 对话控制器：第一次相遇对白（6-1 至 6-8）。
/// 忠实复刻 PlayerGuideFlowControllerV2.Frame6_DialogueRoutine / PlayDialogueSegment。
/// </summary>
public class Frame6DialogueController
{
    private readonly PlayerGuideView view;
    private readonly DialogueView dialogueView;
    private readonly FeifeiCharacterView feifeiView;
    private readonly DreamCoreView dreamCoreView;
    private readonly GameContext gameContext;
    private readonly Frame6DialogueTrackingState dialogueState;

    public Frame6DialogueController(
        PlayerGuideView view,
        DialogueView dialogueView,
        FeifeiCharacterView feifeiView,
        DreamCoreView dreamCoreView,
        GameContext gameContext,
        Frame6DialogueTrackingState dialogueState)
    {
        this.view = view;
        this.dialogueView = dialogueView;
        this.feifeiView = feifeiView;
        this.dreamCoreView = dreamCoreView;
        this.gameContext = gameContext;
        this.dialogueState = dialogueState;
    }

    public IEnumerator RunDialogue()
    {
        var config = view.Frame6Config;
        if (config == null || config.SegmentCount == 0)
        {
            Debug.LogWarning("[PlayerGuide] Frame6DialogueConfig is missing!");
            yield break;
        }

        dialogueView.SetPanelActive(true);
        dialogueView.ShowPlayerStandIn(true);
        if (view.DialoguePanel != null)
            view.DialoguePanel.transform.SetAsLastSibling();

        if (gameContext != null && gameContext.InputManager != null)
        {
            gameContext.InputManager.EnableUIInput();
        }

        feifeiView.SetAnimatorSpeed(0.6f);

        for (int i = 0; i < config.SegmentCount; i++)
        {
            if (i == 1)
                feifeiView.SetMeetingLayout();
            yield return PlayDialogueSegment(config, i);
        }

        if (view.DialogueCanvasGroup != null)
        {
            yield return dialogueView.FadeOut(0.3f);
        }
        dialogueView.SetPanelActive(false);
        dialogueView.ShowPlayerStandIn(false);

        if (gameContext != null && gameContext.InputManager != null)
        {
            gameContext.InputManager.EnableGameplayInput();
        }

        feifeiView.SetAnimatorSpeed(1.0f);
    }

    private IEnumerator PlayDialogueSegment(Frame6DialogueConfig config, int index)
    {
        var segment = config.GetSegment(index);
        if (segment == null)
            yield break;

        string beat = segment.feifeiAnimation;
        feifeiView.PlayMeetingBeat(beat);

        if (string.IsNullOrWhiteSpace(segment.text))
        {
            yield return dreamCoreView.ExpandHalo(Mathf.Max(0.7f, segment.suggestedDuration));
            yield return dialogueView.FadeOut(0.35f);
            yield break;
        }

        if (beat == "CatchCore")
            feifeiView.StartCoroutine(feifeiView.PlayCatchBob(0.4f));

        if (segment.playCoreRipple)
            dreamCoreView.StartCoroutine(dreamCoreView.PlayPurpleRipple());
        else
            dreamCoreView.StartCoroutine(dreamCoreView.LerpGlow(segment.coreGlowIntensity, 0.28f));

        if (beat == "QuietHold")
            feifeiView.StartCoroutine(feifeiView.PlayQuietTail(segment.suggestedDuration));

        dialogueView.SetSpeaker("腓腓");
        yield return dialogueView.PlayTypewriter(segment.text, 0.05f);

        dialogueState.IsWaitingForDialogueAdvance = true;
        float elapsed = 0f;
        float hold = Mathf.Max(0.4f, segment.suggestedDuration);
        while (dialogueState.IsWaitingForDialogueAdvance && elapsed < hold)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        dialogueState.IsWaitingForDialogueAdvance = false;
    }
}
