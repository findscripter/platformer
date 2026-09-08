using UnityEditor;
using UnityEngine;

/// <summary>
/// 创建 Frame 6 对话配置资源（8 段对话）
/// </summary>
public static class Frame6DialogueSetup
{
    private const string OutputPath = "Assets/Game/Resources/PlayerGuide/Frame6DialogueConfig.asset";

    [MenuItem("Tools/GameJam/6. Generate Frame 6 Dialogue Config")]
    public static void Generate()
    {
        Debug.Log("=== 生成 Frame 6 对话配置 ===");

        // 确保目录存在
        string directory = System.IO.Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // 加载或创建
        var config = AssetDatabase.LoadAssetAtPath<Frame6DialogueConfig>(OutputPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<Frame6DialogueConfig>();
            AssetDatabase.CreateAsset(config, OutputPath);
        }

        var segments = new Frame6DialogueConfig.DialogueSegment[3];
        segments[0] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "如果你还记得那场梦。",
            suggestedDuration = 1.6f,
            feifeiAnimation = "LookAtPlayer",
            coreGlowIntensity = 0.5f,
            playCoreRipple = false
        };
        segments[1] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "我们就从那里开始吧。",
            suggestedDuration = 2f,
            feifeiAnimation = "Smile",
            coreGlowIntensity = 0.8f,
            playCoreRipple = true
        };
        segments[2] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "",
            suggestedDuration = 0.8f,
            feifeiAnimation = "",
            coreGlowIntensity = 0f,
            playCoreRipple = false
        };

        // 使用反射设置私有字段（因为 Segments 是只读属性）
        var field = typeof(Frame6DialogueConfig).GetField("segments",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(config, segments);
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ Frame 6 对话配置已生成: {OutputPath}");
    }
}
