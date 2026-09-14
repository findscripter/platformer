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

        backgroundView.SetBackgroundSprite(GuideArt.DreamBackground != null ? GuideArt.DreamBackground : view.DreamBackgroundSpriteEarly);
        backgroundView.EnsureSceneMist();

        if (view.DreamSpaceParticles != null)
        {
            view.DreamSpaceParticles.Play();
        }

        if (view.SmallBubblesContainer != null && view.SmallBubblePrefab != null)
        {
            smallBubblesView.SpawnFigmaLayout(
                view.SmallBubblesContainer,
                view.SmallBubblePrefab,
                view.SmallBubbleFloatSpeed,
                view.SmallBubbleFloatAmplitude);
            coroutineHost.StartCoroutine(smallBubblesView.FadeInContainer(view.SmallBubblesContainer, view.Frame1Duration));
        }

        // 主梦泡是 Frame 1 舞台的一部分（设计稿：中偏左 39%,45% 192px 蓝描边），
        // 与小梦泡一同随背景淡入，不是 Frame 2 才凭空出现。
        // 注意：MainBubble 是 BackgroundCanvasGroup 的兄弟节点而非子节点，
        // 不会被背景的 CanvasGroup 带着淡入，必须自己跑一条同时长的淡入。
        mainBubbleView.ShowAsStageElement();
        mainBubbleView.DisableClick();
        coroutineHost.StartCoroutine(mainBubbleView.FadeIn(view.Frame1Duration));

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
        // 设计稿 Frame 2「延续 Frame 1 布局，主梦泡放大强调，其余小梦泡淡化」
        if (view.SmallBubblesContainer != null)
            yield return smallBubblesView.DimAll(view.SmallBubblesContainer, 0.35f, 0.4f);

        yield return mainBubbleView.PlayEmphasis(0.5f);
        mainBubbleView.ShowClickHint(true);
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
                GuideSfx.PlayHint();
                hintActive = true;

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
        GuideSfx.PlayWhoosh();

        if (view.DreamCoreObject != null)
        {
            dreamCoreView.SetActive(true);
            dreamCoreView.ApplyFigmaSize();
            if (view.MainBubbleVisual != null)
                dreamCoreView.PlaceAt(view.MainBubbleVisual.transform);
            dreamCoreView.SetAlpha(0f);
        }

        float dissolve = view.Frame4DissolveDuration;
        coroutineHost.StartCoroutine(backgroundView.DissipateSceneMist(dissolve));
        if (view.DreamCoreObject != null)
            coroutineHost.StartCoroutine(RevealCoreAfterMist(dissolve * 0.55f, Mathf.Max(0.35f, dissolve * 0.5f)));

        if (view.MainBubbleVisual != null)
            yield return mainBubbleView.PlayDissolve(dissolve);
        else
            yield return new WaitForSeconds(dissolve);

        backgroundView.SetBackgroundSprite(GuideArt.DreamBackground != null ? GuideArt.DreamBackground : view.DreamBackgroundSpriteLate);

        yield return new WaitForSeconds(view.Frame4CoreStabilizeDuration);
    }

    private IEnumerator RevealCoreAfterMist(float delay, float fade)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        yield return dreamCoreView.FadeIn(fade);
    }

    public IEnumerator Frame5_FeifeiEnter()
    {
        if (view.FeifeiCharacter == null)
            yield break;

        feifeiView.SetActive(true);

        Vector3 startPos = new Vector3(800f, 0f, 0f);
        feifeiView.SetLocalPosition(startPos);
        Vector3 endPos = feifeiView.GetPickupPosition(view.DreamCoreObject != null ? view.DreamCoreObject.transform : null);

        // TODO: 播放脚步声音效（循环）

        feifeiView.SafeSetBool("IsWalking", true);

        yield return feifeiView.MoveLocalPosition(startPos, endPos, view.Frame5WalkDuration);

        feifeiView.SafeSetBool("IsWalking", false);

        feifeiView.SafeSetTrigger("PickUpCore");

        // 拾取动作开始即挂到爪前，后续走位、扫尾和会面布局都会共同移动。
        dreamCoreView.AttachTo(feifeiView.CoreHoldAnchor, Vector2.zero);

        GuideSfx.PlayPickup();
        feifeiView.SafePlayState("HoldCore");
        coroutineHost.StartCoroutine(HoldFlourish());
    }

    private IEnumerator HoldFlourish()
    {
        yield return new WaitForSeconds(view.Frame5PickupDuration);
        yield return feifeiView.PlayTailSweep(view.Frame5TailDuration);
        yield return dreamCoreView.CalmFromAgitated(0.4f);
    }
}
