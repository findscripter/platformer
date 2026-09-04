using UnityEngine;

/// <summary>
/// 引导用到的现有项目贴图（按 GUID 取，避免再造占位圆）。
/// </summary>
public static class GuideArt
{
    public const string FeifeiFolder = "Assets/Game/Art/Characters/feifei_idle";
    public const string PlayerIdleGuid = "d2c7d160e9a4caf4aaaf9662037ea5f3";
    public const string MonsterWalkGuid = "17a32b05af2d6204ebd23731ee098375";
    public const string DreamCoreGuid = "d506798f8970fb645bec879362217bd6";
    public const string DialogueBannerGuid = "2033fe5a018fd9b45a37ec8c64c15755";
    public const string HintDotGuid = "41e0bc7119da43045b92e5f75de4d944";
    public const string CircleGuid = "e79b4d6c863b4124e9f6b264f6d371cb";
    public const string BubbleGuid = "cb83933569bc45344b1141bc6af262eb";

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
        if (string.IsNullOrEmpty(guid))
            return null;
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        if (!string.IsNullOrEmpty(path))
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
        }
#endif
        return null;
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
    public static Sprite DreamCore => Load(DreamCoreGuid);
    public static Sprite DialogueBanner => Load(DialogueBannerGuid);
    public static Sprite HintDot => Load(HintDotGuid);
    public static Sprite Circle => Load(CircleGuid);
}
