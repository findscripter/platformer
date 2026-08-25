using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 导入 Dreamremains 塔罗牌美术（22 张大牌 + 1 张统一牌背，945x1664）
/// 并生成/更新 TarotCardData 资产。
///
/// 设计稿（design/frame_09_tarot.md）要求：统一牌背 ×1 + 山海经塔罗牌面 ×4。
/// 抽牌流程只用到 4 张：应龙（战车，主牌）、日月轮转（命运之轮）、
/// 常羲·星月祈愿（星星）、蟾蜍精魄（月亮）。
/// 其余 18 张大牌一并导入备用（22 张牌背铺阵时无需牌面，翻开才需要）。
///
/// 牌面统一 Sprite / Single / Bilinear / 未压缩 / PPU 100。
/// 可重复执行：已存在的 TarotCardData 只更新 cardFace 引用，不覆写手工填写的文案。
/// </summary>
public static class TarotCardArtImporter
{
    private const string ArtTargetDir = "Assets/Game/Art/UI/Tarot";
    private const string DataTargetDir = "Assets/Game/Resources/PlayerGuide/TarotCards";
    private const string CardBackFile = "card-back";

    /// <summary>
    /// 设计稿指定的 4 张抽牌用牌，沿用已有山海经命名与既存资产名。
    /// (牌面文件, 资产名, 中文显示名, 塔罗对应, 是否主牌)
    /// </summary>
    private static readonly (string file, string assetName, string displayName, string tarotName, bool isMain)[] StoryCards =
    {
        ("07-chariot",          "Card_YingLong",       "应龙",           "战车",     true),
        ("10-wheel-of-fortune", "Card_RiYueLunZhuan",  "日月轮转",       "命运之轮", false),
        ("17-star",             "Card_ChangXi",        "常羲·星月祈愿",  "星星",     false),
        ("18-moon",             "Card_ChanChuJingPo",  "蟾蜍精魄",       "月亮",     false),
    };

    /// <summary>
    /// 其余 18 张标准大牌，导入备用（暂不参与抽牌流程）。
    /// (牌面文件, 资产名, 中文显示名)
    /// </summary>
    private static readonly (string file, string assetName, string displayName)[] ExtraCards =
    {
        ("00-fool",           "Card_Fool",          "愚者"),
        ("01-magician",       "Card_Magician",      "魔术师"),
        ("02-high-priestess", "Card_HighPriestess", "女祭司"),
        ("03-empress",        "Card_Empress",       "女皇"),
        ("04-emperor",        "Card_Emperor",       "皇帝"),
        ("05-hierophant",     "Card_Hierophant",    "教皇"),
        ("06-lovers",         "Card_Lovers",        "恋人"),
        ("08-strength",       "Card_Strength",      "力量"),
        ("09-hermit",         "Card_Hermit",        "隐者"),
        ("11-justice",        "Card_Justice",       "正义"),
        ("12-hanged-man",     "Card_HangedMan",     "倒吊人"),
        ("13-death",          "Card_Death",         "死神"),
        ("14-temperance",     "Card_Temperance",    "节制"),
        ("15-devil",          "Card_Devil",         "恶魔"),
        ("16-tower",          "Card_Tower",         "塔"),
        ("19-sun",            "Card_Sun",           "太阳"),
        ("20-judgement",      "Card_Judgement",     "审判"),
        ("21-world",          "Card_World",         "世界"),
    };

    [MenuItem("Tools/GameJam/13. Import Tarot Card Art")]
    public static void Run()
    {
        if (!Directory.Exists(ArtTargetDir))
        {
            Debug.LogError($"牌面美术目录不存在: {ArtTargetDir}");
            return;
        }

        ConfigureSprites();

        if (!Directory.Exists(DataTargetDir))
        {
            Directory.CreateDirectory(DataTargetDir);
        }

        int created = 0, updated = 0, missing = 0;

        foreach (var (file, assetName, displayName, tarotName, isMain) in StoryCards)
        {
            WriteCard(file, assetName, displayName, tarotName, isMain, ref created, ref updated, ref missing);
        }

        foreach (var (file, assetName, displayName) in ExtraCards)
        {
            WriteCard(file, assetName, displayName, displayName, false, ref created, ref updated, ref missing);
        }

        var back = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtTargetDir}/{CardBackFile}.png");
        Debug.Log(back != null
            ? $"✓ 统一牌背就绪: {ArtTargetDir}/{CardBackFile}.png（需在 TarotCardPrefab 上引用）"
            : $"✗ 缺少统一牌背: {ArtTargetDir}/{CardBackFile}.png");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✓ 塔罗牌导入完成：新建 {created}，更新 {updated}，缺失 {missing}");
    }

    private static void WriteCard(
        string file, string assetName, string displayName, string tarotName, bool isMain,
        ref int created, ref int updated, ref int missing)
    {
        string spritePath = $"{ArtTargetDir}/{file}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"缺少牌面: {spritePath}");
            missing++;
            return;
        }

        string assetPath = $"{DataTargetDir}/{assetName}.asset";
        var data = AssetDatabase.LoadAssetAtPath<TarotCardData>(assetPath);
        bool isNew = data == null;

        if (isNew)
        {
            data = ScriptableObject.CreateInstance<TarotCardData>();
        }

        var so = new SerializedObject(data);
        // cardFace 始终对齐美术，避免旧占位图残留
        so.FindProperty("cardFace").objectReferenceValue = sprite;
        // isMainCard 由设计稿决定，始终写入
        so.FindProperty("isMainCard").boolValue = isMain;

        if (isNew)
        {
            so.FindProperty("cardId").stringValue = assetName;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("tarotName").stringValue = tarotName;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        if (isNew)
        {
            AssetDatabase.CreateAsset(data, assetPath);
            created++;
        }
        else
        {
            EditorUtility.SetDirty(data);
            updated++;
        }
    }

    private static void ConfigureSprites()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtTargetDir });
        int configured = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100;
            importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            configured++;
        }

        Debug.Log($"  配置了 {configured} 张 Sprite: {ArtTargetDir}");
    }
}
