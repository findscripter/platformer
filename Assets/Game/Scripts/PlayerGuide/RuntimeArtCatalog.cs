using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Resources 中的美术依赖目录。序列化 Sprite 和字体引用让构建收集动态 UI 依赖，
/// GUID 和工程资产路径仅作索引，Player 不依赖 AssetDatabase。
/// </summary>
public sealed class RuntimeArtCatalog : ScriptableObject
{
    public const string ResourcePath = "PlayerGuide/RuntimeArtCatalog";
    public const string AssetPath = "Assets/Game/Resources/PlayerGuide/RuntimeArtCatalog.asset";

    [Serializable]
    public struct Entry
    {
        public string guid;
        public string assetPath;
        public Sprite sprite;

        public Entry(string guid, string assetPath, Sprite sprite)
        {
            this.guid = guid;
            this.assetPath = NormalizePath(assetPath);
            this.sprite = sprite;
        }
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    [SerializeField] private TMP_FontAsset uiFont;
    // 原始 26.png 的透明留白不进入显示范围；菜单根据原图 alpha 重新计算。
    [SerializeField] private Rect dreamCoreContentRect = DefaultDreamCoreContentRect;

    private Dictionary<string, Sprite> byGuid;
    private Dictionary<string, Sprite> byPath;

    public static Rect DefaultDreamCoreContentRect => new Rect(29f / 120f, 55f / 264f, 59f / 120f, 99f / 264f);
    public Rect DreamCoreContentRect => dreamCoreContentRect;
    public TMP_FontAsset UiFont => uiFont;
    public int Count => entries.Length;

    public Sprite LoadGuid(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
            return null;
        EnsureIndex();
        return byGuid.TryGetValue(guid.Trim(), out Sprite sprite) ? sprite : null;
    }

    public Sprite LoadPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return null;
        EnsureIndex();
        return byPath.TryGetValue(NormalizePath(assetPath), out Sprite sprite) ? sprite : null;
    }

    public static string NormalizePath(string assetPath)
    {
        string path = (assetPath ?? string.Empty).Trim().Replace('\\', '/');
        while (path.StartsWith("./", StringComparison.Ordinal))
            path = path.Substring(2);
        return path;
    }

    private void EnsureIndex()
    {
        if (byGuid != null && byPath != null)
            return;

        byGuid = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        byPath = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (Entry entry in entries)
        {
            if (entry.sprite == null)
                continue;
            if (!string.IsNullOrEmpty(entry.guid))
                byGuid[entry.guid] = entry.sprite;
            if (!string.IsNullOrEmpty(entry.assetPath))
                byPath[NormalizePath(entry.assetPath)] = entry.sprite;
        }
    }

    private void OnEnable()
    {
        byGuid = null;
        byPath = null;
    }

#if UNITY_EDITOR
    public bool SetUiFont(TMP_FontAsset font)
    {
        if (uiFont == font)
            return false;
        uiFont = font;
        return true;
    }

    /// <summary>只在内容变化时标脏，重复生成保持资产稳定。</summary>
    public bool ReplaceEntries(Entry[] updatedEntries, Rect contentRect)
    {
        bool changed = entries.Length != updatedEntries.Length || dreamCoreContentRect != contentRect;
        for (int i = 0; !changed && i < entries.Length; i++)
        {
            changed = entries[i].guid != updatedEntries[i].guid ||
                      entries[i].assetPath != updatedEntries[i].assetPath ||
                      entries[i].sprite != updatedEntries[i].sprite;
        }
        if (!changed)
            return false;

        entries = updatedEntries;
        dreamCoreContentRect = contentRect;
        OnEnable();
        return true;
    }
#endif
}
