using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 批量导入并配置美术资源
/// 自动设置Sprite导入参数、创建目录结构
/// </summary>
public static class ArtAssetImporter
{
    private const string SourceRoot = "E:/腾讯gamejam/美术资源/extracted";
    private const string TargetRoot = "Assets/Art";

    [MenuItem("Tools/GameJam/1. Import Art Assets")]
    public static void ImportAll()
    {
        var startTime = System.DateTime.Now;
        Debug.Log("=== 开始导入美术资源 ===");

        CreateDirectoryStructure();

        ImportCharacterAnimations();
        ImportUIElements();
        ImportVFX();
        ImportEnvironment();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var duration = (System.DateTime.Now - startTime).TotalSeconds;
        Debug.Log($"=== 资源导入完成！耗时 {duration:F1} 秒 ===");
    }

    private static void CreateDirectoryStructure()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Characters"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Characters/Player"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Characters/Monster"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Characters/Feifei"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/UI"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/VFX"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Environment"));

        Debug.Log("✓ 目录结构创建完成");
    }

    private static void ImportCharacterAnimations()
    {
        Debug.Log("导入角色动画序列帧...");

        // 玩家动画
        CopyAnimationFrames(
            Path.Combine(SourceRoot, "关键帧/关键帧/角色/待机"),
            "Assets/Art/Characters/Player/Idle"
        );
        CopyAnimationFrames(
            Path.Combine(SourceRoot, "关键帧/关键帧/角色/走路"),
            "Assets/Art/Characters/Player/Walk"
        );
        CopyAnimationFrames(
            Path.Combine(SourceRoot, "关键帧/关键帧/角色/跑步"),
            "Assets/Art/Characters/Player/Run"
        );
        CopyAnimationFrames(
            Path.Combine(SourceRoot, "关键帧/关键帧/角色/跳跃"),
            "Assets/Art/Characters/Player/Jump"
        );
        CopyAnimationFrames(
            Path.Combine(SourceRoot, "关键帧/关键帧/角色/攻击"),
            "Assets/Art/Characters/Player/Attack"
        );

        // 怪物动画
        CopyAnimationFrames(
            Path.Combine(SourceRoot, "关键帧/关键帧/怪物/走路"),
            "Assets/Art/Characters/Monster/Walk"
        );
        CopyAnimationFrames(
            Path.Combine(SourceRoot, "关键帧/关键帧/怪物/受击"),
            "Assets/Art/Characters/Monster/Hit"
        );

        // 腓腓动画
        CopyAnimationFrames(
            Path.Combine(SourceRoot, "关键帧/关键帧/腓腓/待机"),
            "Assets/Art/Characters/Feifei/Idle"
        );

        Debug.Log("✓ 角色动画序列帧导入完成");
    }

    private static void ImportUIElements()
    {
        Debug.Log("导入UI元件...");

        CopySingleFile(
            Path.Combine(SourceRoot, "元件/dialogues.png"),
            "Assets/Art/UI/dialogues.png"
        );
        CopySingleFile(
            Path.Combine(SourceRoot, "元件/DR 元件.png"),
            "Assets/Art/UI/DR_Elements.png"
        );

        Debug.Log("✓ UI元件导入完成");
    }

    private static void ImportVFX()
    {
        Debug.Log("导入特效资源...");

        CopySingleFile(
            Path.Combine(SourceRoot, "元件/Mist FX.png"),
            "Assets/Art/VFX/MistFX.png"
        );
        CopySingleFile(
            Path.Combine(SourceRoot, "元件/Mist FX2.png"),
            "Assets/Art/VFX/MistFX2.png"
        );
        CopySingleFile(
            Path.Combine(SourceRoot, "元件/Wave FX.png"),
            "Assets/Art/VFX/WaveFX.png"
        );

        Debug.Log("✓ 特效资源导入完成");
    }

    private static void ImportEnvironment()
    {
        Debug.Log("导入场景参考...");

        CopySingleFile(
            Path.Combine(SourceRoot, "元件/场景参考.png"),
            "Assets/Art/Environment/SceneReference.png"
        );

        Debug.Log("✓ 场景参考导入完成");
    }

    private static void CopyAnimationFrames(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogWarning($"源目录不存在: {sourcePath}");
            return;
        }

        var targetFullPath = Path.Combine(Application.dataPath, targetPath.Replace("Assets/", ""));
        Directory.CreateDirectory(targetFullPath);

        var files = Directory.GetFiles(sourcePath, "*.png");
        var copiedCount = 0;

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var dest = Path.Combine(targetFullPath, fileName);
            File.Copy(file, dest, true);
            copiedCount++;
        }

        Debug.Log($"  复制了 {copiedCount} 个文件到 {targetPath}");
    }

    private static void CopySingleFile(string sourcePath, string targetPath)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"源文件不存在: {sourcePath}");
            return;
        }

        var targetFullPath = Path.Combine(Application.dataPath, targetPath.Replace("Assets/", ""));
        var targetDir = Path.GetDirectoryName(targetFullPath);
        Directory.CreateDirectory(targetDir);

        File.Copy(sourcePath, targetFullPath, true);
    }

    [MenuItem("Tools/GameJam/2. Configure Sprite Import Settings")]
    public static void ConfigureSpriteSettings()
    {
        Debug.Log("=== 配置Sprite导入设置 ===");

        ConfigureAnimationSprites();
        ConfigureUISprites();
        ConfigureVFXSprites();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== Sprite配置完成 ===");
    }

    private static void ConfigureAnimationSprites()
    {
        Debug.Log("配置动画Sprite...");

        // 角色动画 - 单张模式，无过滤，像素对齐
        ConfigureSpritesInFolder("Assets/Game/Art/Characters/player", TextureImporterType.Sprite,
            FilterMode.Point, TextureImporterCompression.Uncompressed, 100);
        ConfigureSpritesInFolder("Assets/Game/Art/Characters/monster", TextureImporterType.Sprite,
            FilterMode.Point, TextureImporterCompression.Uncompressed, 100);
        ConfigureSpritesInFolder("Assets/Art/Characters/Feifei", TextureImporterType.Sprite,
            FilterMode.Point, TextureImporterCompression.Uncompressed, 100);

        Debug.Log("✓ 动画Sprite配置完成");
    }

    private static void ConfigureUISprites()
    {
        Debug.Log("配置UI Sprite...");

        // UI元件 - Multiple模式，允许切分
        var uiFiles = new[]
        {
            "Assets/Art/UI/dialogues.png",
            "Assets/Art/UI/DR_Elements.png"
        };

        foreach (var path in uiFiles)
        {
            if (!File.Exists(path))
                continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100;
            importer.maxTextureSize = 2048;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Debug.Log("✓ UI Sprite配置完成（需手动使用Sprite Editor切分）");
    }

    private static void ConfigureVFXSprites()
    {
        Debug.Log("配置VFX Sprite...");

        ConfigureSpritesInFolder("Assets/Art/VFX", TextureImporterType.Sprite,
            FilterMode.Bilinear, TextureImporterCompression.Uncompressed, 100);

        Debug.Log("✓ VFX Sprite配置完成");
    }

    private static void ConfigureSpritesInFolder(
        string folderPath,
        TextureImporterType type,
        FilterMode filterMode,
        TextureImporterCompression compression,
        float pixelsPerUnit)
    {
        if (!Directory.Exists(folderPath))
            return;

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        var configuredCount = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                continue;

            importer.textureType = type;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = filterMode;
            importer.textureCompression = compression;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.maxTextureSize = 2048;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            configuredCount++;
        }

        Debug.Log($"  配置了 {configuredCount} 个Sprite: {folderPath}");
    }
}
