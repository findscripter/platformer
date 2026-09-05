using UnityEditor;
using UnityEngine;

/// <summary>
/// 修复 PlayerAnimator.controller 的转移设置，消除跳跃卡顿：
/// 1. 所有 AnyState 转移的 m_TransitionDuration 从 0.05 改成 0（sprite-swap 动画不需要 crossfade，会产生重影）
/// 2. 所有 AnyState 转移的 m_CanTransitionToSelf 从 false 改成 true（允许自转移，保证每次跳跃从第一帧开始）
/// </summary>
public static class FixPlayerAnimatorTransitions
{
    [MenuItem("Tools/GameJam/11. Fix PlayerAnimator Transitions")]
    public static void Run()
    {
        var path = "Assets/Game/Resources/Player/PlayerAnimator.controller";
        var lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.UTF8);
        var modified = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 查找 AnyState 转移块（以 m_Name: 开头且值为空字符串的段落是 AnyState 转移）
            if (line.Trim() == "m_Name:" && i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("- m_ConditionMode:"))
            {
                // 向下扫描这个转移块，找到 m_TransitionDuration 和 m_CanTransitionToSelf
                for (var j = i; j < lines.Length && j < i + 30; j++)
                {
                    if (lines[j].Contains("m_TransitionDuration:"))
                    {
                        var old = lines[j];
                        lines[j] = lines[j].Replace("m_TransitionDuration: 0.05", "m_TransitionDuration: 0");
                        if (lines[j] != old)
                        {
                            modified = true;
                            Debug.Log($"Line {j + 1}: {old.Trim()} → {lines[j].Trim()}");
                        }
                    }

                    if (lines[j].Contains("m_CanTransitionToSelf:"))
                    {
                        var old = lines[j];
                        lines[j] = lines[j].Replace("m_CanTransitionToSelf: 0", "m_CanTransitionToSelf: 1");
                        if (lines[j] != old)
                        {
                            modified = true;
                            Debug.Log($"Line {j + 1}: {old.Trim()} → {lines[j].Trim()}");
                        }
                    }

                    // 转移块结束标志：遇到下一个顶级字段或新的转移
                    if (j > i && lines[j].StartsWith("  m_") && !lines[j].StartsWith("    "))
                        break;
                }
            }
        }

        if (modified)
        {
            System.IO.File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log($"✓ {path}: AnyState 转移已修复（duration=0, canTransitionToSelf=1）");
        }
        else
        {
            Debug.Log($"{path}: 已是最新，无需修改");
        }
    }
}
