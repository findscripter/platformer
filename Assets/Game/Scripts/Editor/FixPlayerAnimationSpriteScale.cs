using UnityEditor;
using UnityEngine;
using System.IO;

public class FixPlayerAnimationSpriteScale : EditorWindow
{
    [MenuItem("Tools/GameJam/13. Fix Player Animation Sprite Scale")]
    static void Fix()
    {
        Debug.Log("=== 修正玩家动画精灵缩放 ===");

        // idle 精灵：834x1112 -> pixelsPerUnit = 100
        // run 精灵：1668x2388 (是 idle 的 2 倍) -> pixelsPerUnit = 200
        // jump 精灵：检测后设置
        // walk 精灵：检测后设置

        FixSpritesInFolder("Assets/Game/Art/Characters/player/idle", 100);
        FixSpritesInFolder("Assets/Game/Art/Characters/player/run", 200);
        FixSpritesInFolder("Assets/Game/Art/Characters/player/walk", 200);  // 可能也是大尺寸
        FixSpritesInFolder("Assets/Game/Art/Characters/player/jump", 150);  // 中等尺寸假设

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== 精灵缩放修正完成 ===");
    }

    static void FixSpritesInFolder(string folderPath, float pixelsPerUnit)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"目录不存在: {folderPath}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        int count = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                continue;

            if (Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
                continue;  // 已经是正确值，跳过

            importer.spritePixelsPerUnit = pixelsPerUnit;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            count++;
        }

        Debug.Log($"✓ {folderPath}: 修正了 {count} 个精灵 (pixelsPerUnit={pixelsPerUnit})");
    }
}
