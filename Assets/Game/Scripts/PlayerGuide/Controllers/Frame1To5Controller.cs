using System.Collections;
using UnityEngine;

/// <summary>
/// Frame 1-5 流程控制器：梦境空间生成 → 主梦泡 → 梦境残响 → 梦泡消散 → 腓腓入场。
/// 忠实复刻 PlayerGuideFlowControllerV2 的 Frame1_DreamSpaceGenerationRoutine 至 Frame5_FeifeiEnterRoutine。
/// </summary>
public class Frame1To5Controller
{
    private readonly PlayerGuideView view;
    private readonly SmallBubblesView smallBubblesView;
    private readonly MainBubbleView mainBubbleView;
    private readonly BackgroundView backgroundView;
    private readonly DreamCoreView dreamCoreView;
    private readonly FeifeiCharacterView feifeiView;
    private readonly MonoBehaviour coroutineHost;

    public Frame1To5Controller(
        PlayerGuideView view,
        SmallBubblesView smallBubblesView,
        MainBubbleView mainBubbleView,
        BackgroundView backgroundView,
        DreamCoreView dreamCoreView,
        FeifeiCharacterView feifeiView,
        MonoBehaviour coroutineHost)
    {
        this.view = view;
        this.smallBubblesView = smallBubblesView;
        this.mainBubbleView = mainBubbleView;
        this.backgroundView = backgroundView;
        this.dreamCoreView = dreamCoreView;
        this.feifeiView = feifeiView;
        this.coroutineHost = coroutineHost;
    }

    public IEnumerator Frame1_DreamSpaceGeneration()
    {
        Debug.Log("[PlayerGuide] Frame 1: 梦境空间生成开始");

        backgroundView.SetBackgroundSprite(view.DreamBackgroundSpriteEarly);

        if (view.DreamSpaceParticles != null)
        {
            view.DreamSpaceParticles.Play();
        }

        if (view.SmallBubblesContainer != null && view.SmallBubblePrefab != null)
        {
            smallBubblesView.SpawnSmallBubbles(
                view.SmallBubblesContainer,
                view.SmallBubblePrefab,
                view.SmallBubbleCount,
                view.SmallBubbleSizeRange,
                view.SmallBubbleFloatSpeed,
                view.SmallBubbleFloatAmplitude);
        }

        if (view.BackgroundCanvasGroup != null)
        {
            yield return backgroundView.FadeCanvasGroup(0f, 1f, view.Frame1Duration);
        }
        else
        {
            yield return new WaitForSeconds(view.Frame1Duration);
        }

        // TODO: 播放音效 "dream_space_generate"

        Debug.Log("[PlayerGuide] Frame 1 完成，进入 Frame 2");
    }

    public IEnumerator Frame2_MainBubbleAppear()
    {
        yield return mainBubbleView.PlayAppearAnimation(0.5f);
        mainBubbleView.EnableClick();
    }

    public IEnumerator Frame2_HintLoop(System.Func<bool> isStillInFrame2)
    {
        float elapsedSinceLastHint = 0f;
        float nextHintTime = view.Frame2HintDelayFirst;
        bool hintActive = false;

        while (isStillInFrame2())
        {
            elapsedSinceLastHint += Time.deltaTime;

            if (elapsedSinceLastHint >= nextHintTime && !hintActive)
            {
                coroutineHost.StartCoroutine(mainBubbleView.PlayHintGlow(view.Frame2HintGlowIntensity));
                hintActive = true;

                // TODO: 播放提示音效

                elapsedSinceLastHint = 0f;
                nextHintTime = view.Frame2HintDelayRepeat;
                hintActive = false;
            }

            yield return null;
        }
    }

    public IEnumerator Frame3_DreamEchoPlay()
    {
        backgroundView.PlayDreamEcho();

        if (view.MainBubbleVisual != null)
        {
            yield return mainBubbleView.PlayVibration(view.Frame3Duration);
        }
        else
        {
            yield return new WaitForSeconds(view.Frame3Duration);
        }
    }

    public IEnumerator Frame4_BubbleDissolve()
    {
        if (view.MainBubbleVisual != null)
        {
            yield return mainBubbleView.PlayDissolve(view.Frame4DissolveDuration);
        }

        // TODO: 播放音效 "bubble_dissolve"

        backgroundView.SetBackgroundSprite(view.DreamBackgroundSpriteLate);

        if (view.DreamCoreObject != null)
        {
            dreamCoreView.SetActive(true);
            yield return dreamCoreView.FadeIn(1.5f);
        }

        yield return new WaitForSeconds(view.Frame4CoreStabilizeDuration);
    }

    public IEnumerator Frame5_FeifeiEnter()
    {
        if (view.FeifeiCharacter == null)
            yield break;

        feifeiView.SetActive(true);

        Vector3 startPos = new Vector3(800f, 0f, 0f);
        Vector3 endPos = Vector3.zero;
        feifeiView.SetLocalPosition(startPos);

        // TODO: 播放脚步声音效（循环）

        feifeiView.SafeSetBool("IsWalking", true);

        yield return feifeiView.MoveLocalPosition(startPos, endPos, view.Frame5WalkDuration);

        feifeiView.SafeSetBool("IsWalking", false);

        // TODO: 停止脚步声

        yield return new WaitForSeconds(0.5f);

        feifeiView.SafeSetTrigger("PickUpCore");

        // TODO: 播放拾取音效 "core_pickup"

        yield return new WaitForSeconds(view.Frame5PickupDuration);

        yield return new WaitForSeconds(1f);
    }
}
