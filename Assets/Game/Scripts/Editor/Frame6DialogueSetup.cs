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

        // 配置 8 段对话（来自 design/frame_06_dialogues.md）
        var segments = new Frame6DialogueConfig.DialogueSegment[8];

        // 6-1
        segments[0] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "……你也听见了吗？",
            suggestedDuration = 1.5f,
            feifeiAnimation = "HoldCore",
            coreGlowIntensity = 0.6f,
            playCoreRipple = false
        };

        // 6-2
        segments[1] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "已经很久，没有人停下来听它们说话了。",
            suggestedDuration = 1.8f,
            feifeiAnimation = "LookDown",
            coreGlowIntensity = 0.5f,
            playCoreRipple = false
        };

        // 6-3
        segments[2] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "它刚刚……还在很多话没说完。",
            suggestedDuration = 1.9f,
            feifeiAnimation = "HoldCore",
            coreGlowIntensity = 0.7f,
            playCoreRipple = true
        };

        // 6-4
        segments[3] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "每一个梦，好像都想告诉我主人一些事情。",
            suggestedDuration = 1.8f,
            feifeiAnimation = "ThinkPose",
            coreGlowIntensity = 0.6f,
            playCoreRipple = false
        };

        // 6-5
        segments[4] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "只是很多人嫌来以后，就忘记回头听了。",
            suggestedDuration = 2.0f,
            feifeiAnimation = "Sad",
            coreGlowIntensity = 0.4f,
            playCoreRipple = false
        };

        // 6-6
        segments[5] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "如果你还记得那场梦。",
            suggestedDuration = 1.3f,
            feifeiAnimation = "LookAtPlayer",
            coreGlowIntensity = 0.5f,
            playCoreRipple = false
        };

        // 6-7
        segments[6] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "我们就从那里开始吧。",
            suggestedDuration = 2.1f,
            feifeiAnimation = "Smile",
            coreGlowIntensity = 0.8f,
            playCoreRipple = true
        };

        // 6-8 (Fade 过渡，无文本)
        segments[7] = new Frame6DialogueConfig.DialogueSegment
        {
            text = "",
            suggestedDuration = 0.9f,
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
