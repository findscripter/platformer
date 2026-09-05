using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// 自动为导入的动画序列帧生成 AnimationClip
/// </summary>
public class CharacterAnimationGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Character Animations")]
    public static void GenerateAllAnimations()
    {
        // 主角动画
        GeneratePlayerAnimations();

        // 怪物动画
        GenerateMonsterAnimations();

        // 腓腓动画（如果还没生成）
        GenerateFeifeiAnimations();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All character animations generated!");
    }

    private static void GeneratePlayerAnimations()
    {
        string baseDir = "Assets/Game/Art/Characters/player";
        string outputDir = "Assets/Game/Animations/Player";

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        CreateAnimationFromFolder($"{baseDir}/idle", $"{outputDir}/player_idle.anim", 12);
        CreateAnimationFromFolder($"{baseDir}/walk", $"{outputDir}/player_walk.anim", 12);
        CreateAnimationFromFolder($"{baseDir}/run", $"{outputDir}/player_run.anim", 15);
        CreateAnimationFromFolder($"{baseDir}/jump", $"{outputDir}/player_jump.anim", 10);
        CreateAnimationFromFolder($"{baseDir}/attack", $"{outputDir}/player_attack.anim", 20);
    }

    private static void GenerateMonsterAnimations()
    {
        string baseDir = "Assets/Game/Art/Characters/monster";
        string outputDir = "Assets/Game/Animations/Monster";

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        CreateAnimationFromFolder($"{baseDir}/walk", $"{outputDir}/monster_walk.anim", 12);
        CreateAnimationFromFolder($"{baseDir}/hit", $"{outputDir}/monster_hit.anim", 24);
    }

    private static void GenerateFeifeiAnimations()
    {
        string baseDir = "Assets/Game/Art/Characters/feifei_idle";
        string outputDir = "Assets/Game/Animations/PlayerGuide";

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // 只在还没有的情况下生成
        string animPath = $"{outputDir}/Feifei_Idle.anim";
        if (!File.Exists(animPath))
        {
            CreateAnimationFromFolder(baseDir, animPath, 12);
        }
    }

    private static void CreateAnimationFromFolder(string folderPath, string outputPath, int fps)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Folder not found: {folderPath}");
            return;
        }

        // 获取所有 PNG 文件并按数字排序
        var sprites = Directory.GetFiles(folderPath, "*.png")
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
            .Where(s => s != null)
            .OrderBy(s => ExtractNumber(s.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"No sprites found in {folderPath}");
            return;
        }

        // 创建 AnimationClip
        AnimationClip clip = new AnimationClip();
        clip.frameRate = fps;

        // 创建关键帧
        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / (float)fps,
                value = sprites[i]
            };
        }

        // 绑定到 SpriteRenderer
        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
            "",
            typeof(SpriteRenderer),
            "m_Sprite"
        );

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        // 设置循环
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // 保存
        AssetDatabase.CreateAsset(clip, outputPath);
        Debug.Log($"Created animation: {outputPath}");
    }

    private static int ExtractNumber(string text)
    {
        // 从文件名中提取数字（例如 "1.png" -> 1, "10.png" -> 10）
        string numStr = new string(text.Where(char.IsDigit).ToArray());
        return int.TryParse(numStr, out int num) ? num : 0;
    }
}
