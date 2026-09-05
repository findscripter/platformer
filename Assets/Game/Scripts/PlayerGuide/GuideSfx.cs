using UnityEngine;

/// <summary>
/// 引导音效。项目 SFX 目录为空，运行时用噪声/正弦合成短音，避免静音。
/// </summary>
public static class GuideSfx
{
    private static AudioSource source;
    private static AudioClip wind;
    private static AudioClip click;
    private static AudioClip flip;
    private static AudioClip pickup;
    private static AudioClip hint;
    private static AudioClip whoosh;

    public static void PlayWind(AudioSource preferred)
    {
        EnsureClips();
        var src = preferred != null ? preferred : SharedSource();
        src.clip = wind;
        src.loop = true;
        src.volume = 0.35f;
        if (!src.isPlaying)
            src.Play();
    }

    public static void StopWind(AudioSource preferred)
    {
        var src = preferred != null ? preferred : source;
        if (src == null)
            return;
        if (src.isPlaying)
            src.Stop();
        src.loop = false;
    }

    public static void PlayClick()
    {
        PlayOneShot(click, 0.55f);
    }

    public static void PlayCardFlip()
    {
        PlayOneShot(flip, 0.5f);
    }

    public static void PlayPickup()
    {
        PlayOneShot(pickup, 0.6f);
    }

    public static void PlayHint()
    {
        PlayOneShot(hint, 0.35f);
    }

    public static void PlayWhoosh()
    {
        PlayOneShot(whoosh, 0.45f);
    }

    private static void PlayOneShot(AudioClip clip, float volume)
    {
        EnsureClips();
        if (clip == null)
            return;
        SharedSource().PlayOneShot(clip, volume);
    }

    private static AudioSource SharedSource()
    {
        if (source != null)
            return source;

        var go = GameObject.Find("GuideSfxSource");
        if (go == null)
        {
            go = new GameObject("GuideSfxSource");
            Object.DontDestroyOnLoad(go);
        }

        source = go.GetComponent<AudioSource>();
        if (source == null)
            source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static void EnsureClips()
    {
        if (wind != null)
            return;

        wind = MakeNoise("guide_wind", 1.6f, 0.22f, true);
        click = MakeTone("guide_click", 0.09f, 880f, 0.5f);
        flip = MakeNoise("guide_flip", 0.12f, 0.45f, false);
        pickup = MakeTone("guide_pickup", 0.22f, 660f, 0.7f);
        hint = MakeTone("guide_hint", 0.16f, 520f, 0.4f);
        whoosh = MakeNoise("guide_whoosh", 0.28f, 0.3f, false);
    }

    private static AudioClip MakeTone(string name, float seconds, float freq, float decay)
    {
        int rate = 22050;
        int samples = Mathf.Max(64, Mathf.RoundToInt(seconds * rate));
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * (4f + decay * 8f));
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
        }

        var clip = AudioClip.Create(name, samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeNoise(string name, float seconds, float volume, bool loopable)
    {
        int rate = 22050;
        int samples = Mathf.Max(64, Mathf.RoundToInt(seconds * rate));
        var data = new float[samples];
        float prev = 0f;
        for (int i = 0; i < samples; i++)
        {
            float white = (Random.value * 2f - 1f);
            prev = prev * 0.86f + white * 0.14f;
            float env = loopable ? 1f : Mathf.Sin(Mathf.PI * i / samples);
            data[i] = prev * volume * env;
        }

        var clip = AudioClip.Create(name, samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
