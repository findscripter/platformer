using System.Collections;
using UnityEngine;

/// <summary>
/// Frame 9-10 控制器：塔罗抽牌 → 关卡结束与梦核回应。
/// 忠实复刻 PlayerGuideFlowControllerV2.Frame9_TarotRoutine / Frame10_EndRoutine。
/// </summary>
public class Frame9And10Controller
{
    private readonly PlayerGuideView view;
    private readonly TarotView tarotView;
    private readonly DreamCoreView dreamCoreView;

    public Frame9And10Controller(PlayerGuideView view, TarotView tarotView, DreamCoreView dreamCoreView)
    {
        this.view = view;
        this.tarotView = tarotView;
        this.dreamCoreView = dreamCoreView;
    }

    public IEnumerator Frame9_TarotDrawing()
    {
        tarotView.SetPanelActive(true);

        if (tarotView.HasController)
        {
            yield return tarotView.StartTarotFlow();
        }
        else
        {
            Debug.LogWarning("[PlayerGuide] TarotDrawController is missing!");
            yield return new WaitForSeconds(2f);
        }

        // 塔罗系统完成后会自动调用 GameLoop.ContinueToGameplay()
        // 这里不需要手动跳转
    }

    public IEnumerator Frame10_CoreResponseAndEnd()
    {
        yield return dreamCoreView.PulseGlowToFull(view.CoreResponseDuration);

        // TODO: 播放音效 "core_response"

        yield return new WaitForSeconds(view.CoreResponseDuration);

        // Fade 到黑
        // TODO: 调用 SceneTransitionManager.FadeOut(fadeOutDuration)
        yield return new WaitForSeconds(view.FadeOutDuration);

        if (GameLoop.Instance != null)
        {
            GameLoop.Instance.ContinueToGameplay();
        }
    }
}
