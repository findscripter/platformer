using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 导入「元件分割综合」素材包（S01/S02 场景元件 + Dialogues 对话 UI 元件）
/// 复制到 Assets/Game/Art 下并配置 Sprite 导入参数。
/// 已在 Assets 其它位置存在的同内容文件（场景参考、S01_BG=BG01、Dialogue.png、FX）不重复导入。
/// </summary>
public static class ScenePackArtImporter
{
    private const string SourceRoot = "E:/腾讯gamejam/platformer/美术资源/extracted/元件";

    // 源子目录 → 目标目录
    private static readonly (string source, string target)[] Packs =
    {
        ("S01", "Assets/Game/Art/Scenes/S01"),
        ("S02", "Assets/Game/Art/Scenes/S02"),
        ("Dialogues", "Assets/Game/Art/UI/Dialogues"),
    };

    // 与 Assets 中已有文件内容重复（md5 相同），跳过
    private static readonly HashSet<string> SkipFiles = new HashSet<string>
    {
        "场景参考.png",   // = Assets/Art/Environment/SceneReference.png（旧导入）
        "S01_BG.jpg",     // = Assets/Game/Art/source/BG01.jpg
        "Dialogue.png",   // = Assets/Game/Art/source/Dialogue.png
    };

    [MenuItem("Tools/GameJam/9. Import Scene Packs (S01 S02 Dialogues)")]
    public static void ImportAndConfigure()
    {
        Debug.Log("=== 导入 S01/S02/Dialogues 素材包 ===");

        var copiedTotal = 0;
        foreach (var (source, target) in Packs)
        {
            copiedTotal += CopyPack(Path.Combine(SourceRoot, source), target);
        }

        AssetDatabase.Refresh();

        foreach (var (_, target) in Packs)
        {
            ConfigureSprites(target);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"=== 素材包导入完成：共复制 {copiedTotal} 个文件并配置 Sprite ===");
    }

    private static int CopyPack(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            Debug.LogWarning($"源目录不存在: {sourceDir}");
            return 0;
        }

        var targetFullPath = Path.GetFullPath(targetDir);
        Directory.CreateDirectory(targetFullPath);

        var copied = 0;
        // 仅顶层文件；子目录（如清点时产生的 _preview）不复制
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            var ext = Path.GetExtension(name).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg")
                continue;
            if (SkipFiles.Contains(name))
                continue;

            File.Copy(file, Path.Combine(targetFullPath, name), true);
            copied++;
        }

        Debug.Log($"  复制了 {copied} 个文件到 {targetDir}");
        return copied;
    }

    private static void ConfigureSprites(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        var configured = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            // 手绘水彩风元件：Single Sprite、双线性过滤、不压缩，与项目现有场景/UI 素材一致
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

        Debug.Log($"  配置了 {configured} 个Sprite: {folderPath}");
    }
}
