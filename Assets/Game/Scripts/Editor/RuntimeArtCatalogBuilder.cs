using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 仅生成资源目录，不重建场景、不刷新整个 AssetDatabase、不改源图或导入参数。
/// 构建预处理同样执行，避免只在编辑器依靠 AssetDatabase 正常显示。
/// </summary>
public sealed class RuntimeArtCatalogBuilder : IPreprocessBuildWithReport
{
    private static readonly string[] SourceFolders =
    {
        "Assets/Game/Art/Scenes",
        "Assets/Game/Art/UI",
        GuideArt.FeifeiFolder
    };

    private static readonly string[] RequiredGuids =
    {
        GuideArt.PlayerIdleGuid, GuideArt.MonsterWalkGuid, GuideArt.DreamCoreGuid,
        GuideArt.DialogueBannerGuid, GuideArt.HintDotGuid, GuideArt.CircleGuid,
        GuideArt.TarotBackgroundGuid, GuideArt.DreamBackgroundGuid, GuideArt.EchoBackgroundGuid,
        GuideArt.BubbleGuid, GuideArt.SmallBubbleGuid
    };

    public int callbackOrder => -10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        Generate();
    }

    [MenuItem("Tools/梦境引导/生成运行时美术目录")]
    public static void Generate()
    {
        var paths = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", SourceFolders))
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));

        foreach (string guid in RequiredGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                throw new BuildFailedException($"[RuntimeArtCatalog] 必需素材 GUID 无法解析：{guid}");
            paths.Add(path);
        }

        // 显式校验所有待机帧；缺图时阻止构建，而不是发布静止/空白角色。
        for (int i = 1; i <= 27; i++)
            paths.Add($"{GuideArt.FeifeiFolder}/{i}.png");

        var entries = new List<RuntimeArtCatalog.Entry>(paths.Count);
        foreach (string path in paths)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new BuildFailedException($"[RuntimeArtCatalog] 素材不是可加载的 Sprite：{path}");
            entries.Add(new RuntimeArtCatalog.Entry(AssetDatabase.AssetPathToGUID(path), path, sprite));
        }

        string fontPath = AssetDatabase.GUIDToAssetPath(GuideUiFont.FontAssetGuid);
        TMP_FontAsset uiFont = string.IsNullOrEmpty(fontPath) ? null : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (uiFont == null)
            throw new BuildFailedException($"[RuntimeArtCatalog] 必需中文字体无法加载：{GuideUiFont.FontAssetGuid}");
        if (uiFont.atlasPopulationMode == AtlasPopulationMode.Dynamic && uiFont.sourceFontFile == null)
            throw new BuildFailedException($"[RuntimeArtCatalog] 动态中文字体缺少源字体，发布包无法生成汉字：{fontPath}");

        Rect coreContentRect = ReadCoreContentRect();
        EnsureFolder("Assets/Game/Resources/PlayerGuide");
        RuntimeArtCatalog catalog = AssetDatabase.LoadAssetAtPath<RuntimeArtCatalog>(RuntimeArtCatalog.AssetPath);
        bool created = catalog == null;
        if (created)
        {
            catalog = ScriptableObject.CreateInstance<RuntimeArtCatalog>();
            AssetDatabase.CreateAsset(catalog, RuntimeArtCatalog.AssetPath);
        }

        bool changed = catalog.ReplaceEntries(entries.ToArray(), coreContentRect);
        changed |= catalog.SetUiFont(uiFont);
        if (created || changed)
        {
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
        GuideArt.ClearCache();
        GuideUiFont.ClearCache();
        Debug.Log($"[RuntimeArtCatalog] {(created || changed ? "已更新" : "已验证")} {catalog.Count} 个 Sprite 和中文字体 {uiFont.name}，" +
                  $"支持 GUID/资产路径加载：{RuntimeArtCatalog.AssetPath}", catalog);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;
        int separator = folder.LastIndexOf('/');
        string parent = folder.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder.Substring(separator + 1));
    }

    private static Rect ReadCoreContentRect()
    {
        string path = AssetDatabase.GUIDToAssetPath(GuideArt.DreamCoreGuid);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            // 读取源文件到临时内存，完全不触碰原图和 TextureImporter 的可读设置。
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
                throw new BuildFailedException($"[RuntimeArtCatalog] 无法读取梦核透明度：{path}");

            Color32[] pixels = texture.GetPixels32();
            int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[y * texture.width + x].a == 0)
                        continue;
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
            if (maxX < minX || maxY < minY)
                throw new BuildFailedException("[RuntimeArtCatalog] 梦核素材完全透明。");

            // 留两像素透明边，避免双线性采样切掉透明过渡；矩形以左下角为原点。
            minX = Math.Max(0, minX - 2);
            minY = Math.Max(0, minY - 2);
            maxX = Math.Min(texture.width, maxX + 3);
            maxY = Math.Min(texture.height, maxY + 3);
            return new Rect((float)minX / texture.width, (float)minY / texture.height,
                (float)(maxX - minX) / texture.width, (float)(maxY - minY) / texture.height);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
