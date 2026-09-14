using UnityEngine;

/// <summary>
/// 全局循环 BGM。挂在 PersistentRoot/AudioManager 上，跨场景不停。
/// </summary>
public class BgmPlayer : MonoBehaviour
{
    public const string ResourcePath = "Audio/game_bgm";
    public const string AssetPath = "Assets/Game/Audio/BGM/game_bgm.mp3";

    [SerializeField] private AudioClip clip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.55f;

    private AudioSource source;

    public static BgmPlayer EnsureOn(Transform managersRoot)
    {
        if (managersRoot == null)
            return null;

        var existing = managersRoot.GetComponentInChildren<BgmPlayer>(true);
        if (existing != null)
        {
            existing.Play();
            return existing;
        }

        Transform audioRoot = managersRoot.Find("AudioManager");
        if (audioRoot == null)
        {
            var go = new GameObject("AudioManager");
            go.transform.SetParent(managersRoot, false);
            audioRoot = go.transform;
        }

        var player = audioRoot.GetComponent<BgmPlayer>();
        if (player == null)
            player = audioRoot.gameObject.AddComponent<BgmPlayer>();
        player.Play();
        return player;
    }

    private void Awake()
    {
        EnsureSource();
        Play();
    }

    public void Play()
    {
        EnsureSource();
        AudioClip resolved = ResolveClip();
        if (resolved == null)
        {
            Debug.LogWarning("BgmPlayer: game_bgm clip is missing.");
            return;
        }

        if (source.clip != resolved)
            source.clip = resolved;

        source.loop = true;
        source.volume = volume;
        if (!source.isPlaying)
            source.Play();
    }

    private void EnsureSource()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.priority = 32;
        source.volume = volume;
        source.ignoreListenerPause = true;
    }

    private AudioClip ResolveClip()
    {
        if (clip != null)
            return clip;

        clip = Resources.Load<AudioClip>(ResourcePath);
        if (clip != null)
            return clip;

#if UNITY_EDITOR
        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(AssetPath);
#endif
        return clip;
    }
}
