using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 导入 Dreamremains 塔罗牌美术（945x1664 整张牌面）并生成/更新 TarotCardData 资产。
/// 牌面统一 Sprite / Single / Bilinear / 未压缩 / PPU 100，与项目其它 UI 素材一致。
/// 可重复执行：已存在的 TarotCardData 只更新 cardFace 引用，不覆写手工填写的中文名。
/// </summary>
public static class TarotCardArtImporter
{
    private const string ArtTargetDir = "Assets/Game/Art/UI/Tarot";
    private const string DataTargetDir = "Assets/Game/Resources/PlayerGuide/TarotCards";

    /// <summary>
    /// 文件名 → (资产名, 中文显示名, 塔罗牌名)。
    /// 中文名沿用山海经主题，与已有 4 张牌的命名风格保持一致。
    /// </summary>
    private static readonly (string file, string assetName, string displayName, string tarotName)[] CardMap =
    {
        ("09-hermit",           "Card_Hermit",       "隐者",     "隐者"),
        ("10-wheel-of-fortune", "Card_WheelFortune", "命运之轮", "命运之轮"),
        ("11-justice",          "Card_Justice",      "正义",     "正义"),
        ("12-hanged-man",       "Card_HangedMan",    "倒吊人",   "倒吊人"),
        ("13-death",            "Card_Death",        "死神",     "死神"),
        ("14-temperance",       "Card_Temperance",   "节制",     "节制"),
        ("16-tower",            "Card_Tower",        "塔",       "塔"),
        ("19-sun",              "Card_Sun",          "太阳",     "太阳"),
        ("20-judgement",        "Card_Judgement",    "审判",     "审判"),
        ("21-world",            "Card_World",        "世界",     "世界"),
    };

    [MenuItem("Tools/GameJam/13. Import Tarot Card Art")]
    public static void Run()
    {
        if (!Directory.Exists(ArtTargetDir))
        {
            Debug.LogError($"牌面美术目录不存在: {ArtTargetDir}（请先把 PNG 放进该目录）");
            return;
        }

        ConfigureSprites();

        int created = 0;
        int updated = 0;
        int missingArt = 0;

        if (!Directory.Exists(DataTargetDir))
        {
            Directory.CreateDirectory(DataTargetDir);
        }

        foreach (var (file, assetName, displayName, tarotName) in CardMap)
        {
            string spritePath = $"{ArtTargetDir}/{file}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"缺少牌面: {spritePath}");
                missingArt++;
                continue;
            }

            string assetPath = $"{DataTargetDir}/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<TarotCardData>(assetPath);
            bool isNew = data == null;

            if (isNew)
            {
                data = ScriptableObject.CreateInstance<TarotCardData>();
            }

            var so = new SerializedObject(data);
            so.FindProperty("cardFace").objectReferenceValue = sprite;

            // 仅新建时写入文案，避免覆盖后续手工调整
            if (isNew)
            {
                so.FindProperty("cardId").stringValue = assetName;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("tarotName").stringValue = tarotName;
                so.FindProperty("isMainCard").boolValue = false;
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

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✓ 塔罗牌导入完成：新建 {created} 张，更新 {updated} 张，缺失美术 {missingArt} 张");
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

        Debug.Log($"  配置了 {configured} 张牌面 Sprite: {ArtTargetDir}");
    }
}
