using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared, non-spatial demo SFX and a quiet story ambience, separate from BGM.</summary>
public sealed class GameAudioDirector : MonoBehaviour
{
    private static GameAudioDirector instance;
    private AudioSource effects;
    private AudioSource ambience;
    private bool coreBreathing;
    private bool storyQuiet;
    private readonly Dictionary<string, AudioClip> clips = new();
    private readonly Dictionary<string, float> lastPlayed = new();

    public static GameAudioDirector Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("GameAudioDirector");
                instance = go.AddComponent<GameAudioDirector>();
                if (Application.isPlaying) DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        effects = gameObject.AddComponent<AudioSource>();
        ambience = gameObject.AddComponent<AudioSource>();
        foreach (var source in new[] { effects, ambience })
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.ignoreListenerPause = false;
        }
        ambience.loop = true;
        ambience.clip = Clip("ambience");
        ambience.volume = 0f;
    }

    private void Update()
    {
        var context = GameLoop.Instance?.Context;
        var state = context?.StateMachine?.CurrentStateType ?? GameStateType.MainMenu;
        bool story = state == GameStateType.PlayerGuide || state == GameStateType.EchoSpace;
        if (state != GameStateType.EchoSpace) coreBreathing = false;
        if (state != GameStateType.PlayerGuide) storyQuiet = false;
        float target = story ? (state == GameStateType.EchoSpace ? .11f : .18f) : 0f;
        if (storyQuiet) target *= .65f;
        // The demo sound list reuses air ambience instead of a separate core loop.
        if (coreBreathing) target *= .92f + .08f * Mathf.Sin(Time.unscaledTime * Mathf.PI / 3f);
        if (target > 0f && !ambience.isPlaying) ambience.Play();
        ambience.volume = Mathf.MoveTowards(ambience.volume, target, Time.unscaledDeltaTime * .14f);
        if (target == 0f && ambience.volume <= .001f) ambience.Stop();
    }

    public void SetCoreBreathing(bool active) => coreBreathing = active;
    public void SetStoryQuiet(bool quiet) => storyQuiet = quiet;

    public AudioClip Clip(string id)
    {
        if (!clips.TryGetValue(id, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/Sfx/" + id);
            clips[id] = clip;
            if (clip == null) Debug.LogWarning("[Audio] Missing SFX: " + id);
        }
        return clip;
    }

    public void Play(string id, float volume = .55f, float minInterval = .045f)
    {
        if (!Application.isPlaying) return;
        if (lastPlayed.TryGetValue(id, out float last) && Time.unscaledTime - last < minInterval) return;
        var clip = Clip(id);
        if (clip == null) return;
        lastPlayed[id] = Time.unscaledTime;
        effects.PlayOneShot(clip, volume);
    }
}
