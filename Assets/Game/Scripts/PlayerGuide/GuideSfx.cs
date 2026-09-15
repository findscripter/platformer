using UnityEngine;

/// <summary>
/// 引导音效入口。素材映射参考《梦墟》Demo 音效清单。
/// </summary>
public static class GuideSfx
{
    public static void PlayWind(AudioSource preferred)
    {
        if (preferred == null) { GameAudioDirector.Instance.Play("echo_wind", .6f); return; }
        preferred.playOnAwake = false;
        preferred.loop = false;
        preferred.spatialBlend = 0f;
        preferred.clip = GameAudioDirector.Instance.Clip("echo_wind");
        preferred.volume = .6f;
        preferred.Play();
    }

    public static void StopWind(AudioSource preferred)
    {
        if (preferred != null && preferred.isPlaying)
            preferred.Stop();
    }

    public static void PlayClick()
    {
        GameAudioDirector.Instance.Play("click", .45f);
    }

    public static void PlayCardFlip()
    {
        GameAudioDirector.Instance.Play("card_flip", .6f);
    }

    public static void PlayPickup()
    {
        GameAudioDirector.Instance.Play("hint", .5f);
    }

    public static void PlayHint()
    {
        GameAudioDirector.Instance.Play("hint", .25f, .5f);
    }

    public static void PlayWhoosh()
    {
        GameAudioDirector.Instance.Play("whoosh", .5f);
    }

    public static void PlayCoreResponse(float volume = .55f) => GameAudioDirector.Instance.Play("core_response", volume);
    public static void PlayDissolve() => GameAudioDirector.Instance.Play("dissolve", .5f);
    public static void PlayContact() => GameAudioDirector.Instance.Play("contact", .4f);
    public static void PlaySoftTransition() => GameAudioDirector.Instance.Play("whoosh", .22f);
    public static void SetCoreBreathing(bool active)
    {
        if (Application.isPlaying) GameAudioDirector.Instance.SetCoreBreathing(active);
    }
    public static void SetStoryQuiet(bool quiet)
    {
        if (Application.isPlaying) GameAudioDirector.Instance.SetStoryQuiet(quiet);
    }
}
