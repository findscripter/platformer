using UnityEngine;

/// <summary>
/// 引导音效入口。已改为静音：全游戏只保留全局 BGM。
/// </summary>
public static class GuideSfx
{
    public static void PlayWind(AudioSource preferred)
    {
        StopWind(preferred);
    }

    public static void StopWind(AudioSource preferred)
    {
        if (preferred != null && preferred.isPlaying)
            preferred.Stop();
    }

    public static void PlayClick()
    {
    }

    public static void PlayCardFlip()
    {
    }

    public static void PlayPickup()
    {
    }

    public static void PlayHint()
    {
    }

    public static void PlayWhoosh()
    {
    }
}
