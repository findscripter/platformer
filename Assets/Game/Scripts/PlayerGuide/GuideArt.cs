using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 引导与动态关卡的现有美术入口，发布包通过 Resources 目录读取序列化引用。
/// </summary>
public static class GuideArt
{
    public const string FeifeiFolder = "Assets/Game/Art/Characters/feifei_idle";
    public const string PlayerIdleGuid = "d2c7d160e9a4caf4aaaf9662037ea5f3";
    public const string MonsterWalkGuid = "17a32b05af2d6204ebd23731ee098375";
    public const string DreamCoreGuid = "d506798f8970fb645bec879362217bd6";
    public const string DialogueBannerGuid = "2033fe5a018fd9b45a37ec8c64c15755";
    public const string InputPanelFrameGuid = "a5aceca37d48c1b48bf56ed497043a7b";
    public const string HintDotGuid = "41e0bc7119da43045b92e5f75de4d944";
    public const string CircleGuid = "e79b4d6c863b4124e9f6b264f6d371cb";
    public const string TarotBackgroundGuid = "d2202ed4d2c36d8478ca29b1cbb5514c";
    public const string DreamBackgroundGuid = TarotBackgroundGuid;
    public const string EchoBackgroundGuid = TarotBackgroundGuid;
    // 主梦泡使用 S01/0.png 的深色玻璃切图；Dialogues/0.png 带白色颗粒纹理，适合其他 UI 场景。
    public const string BubbleGuid = "577b6198d75c3dc47af96ad3f63c5ede";
    public const string SmallBubbleGuid = "392a2361d8439944ab64d4e1a478c736";

    private static RuntimeArtCatalog catalog;
    private static bool catalogLoaded;
    private static readonly HashSet<string> MissingArt = new HashSet<string>();
    private static Sprite dreamCoreSource;
    private static Sprite dreamCoreDisplay;

    private static readonly string[] FeifeiGuids =
    {
        "cb83933569bc45344b1141bc6af262eb",
        "7159e0dbd05ae484c82ef5c050365f58",
        "4837efc162a57f34b9575c1f99e3463d",
        "679b83e10512cca43af6adb76659feb9",
        "daa38392fd0aa9740ba387147003770f",
        "e49d004dc4c121c4c9e2b6cef45a12ba",
        "09ffe58fe698438469bbd12b51441e75",
        "6fdd6b2f45a999241ab99778ebf6018c",
        "393314a13f3b73240b8eec3900f0a7fd",
        "e6862fdffe31bce40ab4d966d0f45290",
        "8556ebda28b526742927b5972d487868",
        "c88665428c99de444b5cbea30cb3b197",
        "d5506a57441c05c4abfa278972ba6779",
        "0ed0600f418553142a5afe7cc955eb05",
        "ef0e745a29ee1484a9eeed431fc0b2d4",
        "ab9f5a858db27b44ab7076dd769108f3",
        "b6991daf29c48db42b61d68c5abea6d4",
        "50c8ba51dc5b2a24997ecb34c93439e0",
        "bbe78d8eac7218d4198611b5a4a69b45",
        "4543aaf06a5af72459c7ed66ab522f80",
        "c75ac76fd6469c048b0ddb4c64a96472",
        "220fb868593214e4194cb0cc2c9041ce",
        "4962ad94590d7724cac90986a904c39f",
        "bbf6efe2d82927a4c83d0448c222a732",
        "0b0eb6a8341907349b88993fecc5e3b1",
        "b28d13aef07550947b48c52d182ff1e1",
        "f5a4bbdf0d3cb4e47be9eaa7a708b904"
    };

    public static Sprite Load(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
            return null;
        guid = guid.Trim();
        Sprite sprite = Catalog != null ? Catalog.LoadGuid(guid) : null;
        if (sprite != null)
            return sprite;
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        if (!string.IsNullOrEmpty(path))
        {
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
        }
#endif
        WarnMissing(guid);
        return null;
    }

    /// <summary>按 Assets/... 资产路径查找；支持 Windows 路径分隔符。</summary>
    public static Sprite LoadPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return null;
        string path = RuntimeArtCatalog.NormalizePath(assetPath);
        Sprite sprite = Catalog != null ? Catalog.LoadPath(path) : null;
        if (sprite != null)
            return sprite;
#if UNITY_EDITOR
        sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;
#endif
        WarnMissing(path);
        return null;
    }

    private static RuntimeArtCatalog Catalog
    {
        get
        {
            if (!catalogLoaded)
            {
                catalog = Resources.Load<RuntimeArtCatalog>(RuntimeArtCatalog.ResourcePath);
                catalogLoaded = true;
            }
            return catalog;
        }
    }

    private static void WarnMissing(string key)
    {
        if (MissingArt.Add(key))
            Debug.LogWarning($"[GuideArt] 美术未收录或引用丢失：{key}。请执行 Tools/梦境引导/生成运行时美术目录。");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ClearCache()
    {
        catalog = null;
        catalogLoaded = false;
        MissingArt.Clear();
        dreamCoreSource = null;
        if (dreamCoreDisplay != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(dreamCoreDisplay);
            else
#endif
                Object.Destroy(dreamCoreDisplay);
        }
        dreamCoreDisplay = null;
    }

    public static Sprite[] LoadFeifeiFrames()
    {
        var frames = new Sprite[FeifeiGuids.Length];
        for (int i = 0; i < FeifeiGuids.Length; i++)
            frames[i] = Load(FeifeiGuids[i]);
        return frames;
    }

    public static Sprite PlayerIdle => Load(PlayerIdleGuid);
    public static Sprite MonsterWalk => Load(MonsterWalkGuid);
    public static Sprite DreamCore
    {
        get
        {
            Sprite source = Load(DreamCoreGuid);
            if (source == null)
                return null;
            if (dreamCoreDisplay != null && dreamCoreSource == source)
                return dreamCoreDisplay;

            Rect trim = Catalog != null ? Catalog.DreamCoreContentRect : RuntimeArtCatalog.DefaultDreamCoreContentRect;
            Rect sourceRect = source.rect;
            Rect visibleRect = new Rect(sourceRect.x + trim.x * sourceRect.width,
                sourceRect.y + trim.y * sourceRect.height, trim.width * sourceRect.width, trim.height * sourceRect.height);
            // 只建立 Sprite 的取景矩形，不改原始 PNG、Alpha 或贴图导入设置。
            dreamCoreDisplay = Sprite.Create(source.texture, visibleRect, new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            dreamCoreDisplay.name = "DreamCore_Display";
            dreamCoreSource = source;
            return dreamCoreDisplay;
        }
    }
    public static Sprite DialogueBanner => Load(DialogueBannerGuid);
    public static Sprite InputPanelFrame => Load(InputPanelFrameGuid);
    public static Sprite HintDot => Load(HintDotGuid);
    public static Sprite Circle => Load(CircleGuid);
    public static Sprite TarotBackground => Load(TarotBackgroundGuid);
    public static Sprite EchoBackground => Load(EchoBackgroundGuid);
    public static Sprite DreamBackground => Load(DreamBackgroundGuid);
    public static Sprite Bubble => Load(BubbleGuid);
    public static Sprite SmallBubble => Load(SmallBubbleGuid);
}
