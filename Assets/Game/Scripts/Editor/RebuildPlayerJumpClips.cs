using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 重建玩家跳跃动画 clips（player_up / player_down / PlayerLanding），修复卡顿和姿势不连续：
/// - player_up: 帧 1-4 @ 8fps（覆盖 0.505s 上升段），不循环
/// - player_down: 帧 5-7 @ 6fps（覆盖 0.510s 下落段），不循环
/// - PlayerLanding: 帧 8 + idle/1 @ 12fps（0.167s 触地→站姿过渡），不循环
/// 原理：jump/1-8.png 是一条连贯弧线（1蓄力 2-3起跳展裙 4-5顶点收腿 6-7下落伸腿 8触地），
/// 旧 clip 切分错误（up=1-2循环抖动，down=3单帧静止，landing=4-8空中姿势当落地）。
/// 物理测算：jumpForce=5 m/s，gravity=-9.81 m/s²，上升 0.505s，下落 0.510s。
/// </summary>
public static class RebuildPlayerJumpClips
{
    private const string JumpFolder = "Assets/Game/Art/Characters/player/jump";
    private const string IdleFolder = "Assets/Game/Art/Characters/player/idle";

    // 调查 workflow 返回的 jump 帧 guid（顺序：1-8.png）
    private static readonly string[] JumpGuids = {
        "7fa4b936f9d8fed4d9227d3e57cb8dac", // 1.png
        "f2d7efc1615b7e84bb7b100d663f07c7", // 2.png
        "495afcfd3d630ee478c681f277ff2918", // 3.png
        "05c93c87f8f46574d829d4596e5c4897", // 4.png
        "1948e466b98141e448fb96b350a8b4c6", // 5.png
        "18852e1c281d26641a37f325b5971cde", // 6.png
        "06b849bf09f87be4d880ab6f923336dc", // 7.png
        "e394268ab48c4824fa58bdcf799f5ea9", // 8.png
    };

    [MenuItem("Tools/GameJam/12. Rebuild Player Jump Clips")]
    public static void Run()
    {
        // 取 idle/1.png 的 guid（落地最后一帧回到站姿过渡）
        var idleGuid = GetTextureGuid($"{IdleFolder}/1.png");
        if (string.IsNullOrEmpty(idleGuid))
        {
            Debug.LogError("idle/1.png guid 未找到");
            return;
        }

        // player_up: 帧 1-4 @ 8fps（0.5秒覆盖 0.505s 上升），不循环
        WriteClip("Assets/Game/Resources/Player/player_up.anim",
                  new[] { JumpGuids[0], JumpGuids[1], JumpGuids[2], JumpGuids[3] },
                  8f, false, "player_up");

        // player_down: 帧 5-7 @ 6fps（0.5秒覆盖 0.510s 下落），不循环
        WriteClip("Assets/Game/Resources/Player/player_down.anim",
                  new[] { JumpGuids[4], JumpGuids[5], JumpGuids[6] },
                  6f, false, "player_down");

        // PlayerLanding: 帧 8 + idle/1 @ 12fps（0.167s 触地→站姿），不循环
        WriteClip("Assets/Game/Resources/Player/PlayerLanding.anim",
                  new[] { JumpGuids[7], idleGuid },
                  12f, false, "PlayerLanding");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✓ player_up / player_down / PlayerLanding 已重建（新帧序列 + 精确帧率）");
    }

    private static string GetTextureGuid(string path)
    {
        var meta = File.ReadAllText(path + ".meta");
        var m = Regex.Match(meta, @"guid:\s*(\w+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static void WriteClip(string clipPath, string[] guids, float fps, bool loop, string clipName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("%YAML 1.1");
        sb.AppendLine("%TAG !u! tag:unity3d.com,2011:");
        sb.AppendLine("--- !u!74 &7400000");
        sb.AppendLine("AnimationClip:");
        sb.AppendLine("  m_ObjectHideFlags: 0");
        sb.AppendLine("  m_CorrespondingSourceObject: {fileID: 0}");
        sb.AppendLine("  m_PrefabInstance: {fileID: 0}");
        sb.AppendLine("  m_PrefabAsset: {fileID: 0}");
        sb.AppendLine($"  m_Name: {clipName}");
        sb.AppendLine("  serializedVersion: 8");
        sb.AppendLine("  m_Legacy: 0");
        sb.AppendLine("  m_Compressed: 0");
        sb.AppendLine("  m_UseHighQualityCurve: 1");
        sb.AppendLine("  m_RotationCurves: []");
        sb.AppendLine("  m_CompressedRotationCurves: []");
        sb.AppendLine("  m_EulerCurves: []");
        sb.AppendLine("  m_PositionCurves: []");
        sb.AppendLine("  m_ScaleCurves: []");
        sb.AppendLine("  m_FloatCurves: []");
        sb.AppendLine("  m_PPtrCurves:");
        sb.AppendLine("  - serializedVersion: 2");
        sb.AppendLine("    curve:");

        for (var i = 0; i < guids.Length; i++)
        {
            var time = i / fps;
            sb.AppendLine($"    - time: {time}");
            sb.AppendLine($"      value: {{fileID: 21300000, guid: {guids[i]}, type: 3}}");
        }

        sb.AppendLine("    attribute: m_Sprite");
        sb.AppendLine("    path: ");
        sb.AppendLine("    classID: 212");
        sb.AppendLine("    script: {fileID: 0}");
        sb.AppendLine("    flags: 2");
        sb.AppendLine($"  m_SampleRate: {fps}");
        sb.AppendLine("  m_WrapMode: 0");
        sb.AppendLine("  m_Bounds:");
        sb.AppendLine("    m_Center: {x: 0, y: 0, z: 0}");
        sb.AppendLine("    m_Extent: {x: 0, y: 0, z: 0}");
        sb.AppendLine("  m_ClipBindingConstant:");
        sb.AppendLine("    genericBindings:");
        sb.AppendLine("    - serializedVersion: 2");
        sb.AppendLine("      path: 0");
        sb.AppendLine("      attribute: 0");
        sb.AppendLine("      script: {fileID: 0}");
        sb.AppendLine("      typeID: 212");
        sb.AppendLine("      customType: 23");
        sb.AppendLine("      isPPtrCurve: 1");
        sb.AppendLine("      isIntCurve: 0");
        sb.AppendLine("      isSerializeReferenceCurve: 0");
        sb.AppendLine("      metaData: 0");
        sb.AppendLine("    pptrCurveMapping:");

        foreach (var guid in guids)
        {
            sb.AppendLine($"    - {{fileID: 21300000, guid: {guid}, type: 3}}");
        }

        sb.AppendLine("  m_AnimationClipSettings:");
        sb.AppendLine("    serializedVersion: 2");
        sb.AppendLine("    m_AdditiveReferencePoseClip: {fileID: 0}");
        sb.AppendLine("    m_AdditiveReferencePoseTime: 0");
        sb.AppendLine("    m_StartTime: 0");
        var stopTime = (guids.Length - 1) / fps;
        sb.AppendLine($"    m_StopTime: {stopTime}");
        sb.AppendLine("    m_OrientationOffsetY: 0");
        sb.AppendLine("    m_Level: 0");
        sb.AppendLine("    m_CycleOffset: 0");
        sb.AppendLine("    m_HasAdditiveReferencePose: 0");
        sb.AppendLine($"    m_LoopTime: {(loop ? 1 : 0)}");
        sb.AppendLine("    m_LoopBlend: 0");
        sb.AppendLine("    m_LoopBlendOrientation: 0");
        sb.AppendLine("    m_LoopBlendPositionY: 0");
        sb.AppendLine("    m_LoopBlendPositionXZ: 0");
        sb.AppendLine("    m_KeepOriginalOrientation: 0");
        sb.AppendLine("    m_KeepOriginalPositionY: 1");
        sb.AppendLine("    m_KeepOriginalPositionXZ: 0");
        sb.AppendLine("    m_HeightFromFeet: 0");
        sb.AppendLine("    m_Mirror: 0");
        sb.AppendLine("  m_EditorCurves: []");
        sb.AppendLine("  m_EulerEditorCurves: []");
        sb.AppendLine("  m_HasGenericRootTransform: 0");
        sb.AppendLine("  m_HasMotionFloatCurves: 0");
        sb.AppendLine("  m_Events: []");

        File.WriteAllText(clipPath, sb.ToString(), new UTF8Encoding(false));
        Debug.Log($"  wrote {clipPath}: {guids.Length} frames @ {fps}fps, loop={loop}");
    }
}
