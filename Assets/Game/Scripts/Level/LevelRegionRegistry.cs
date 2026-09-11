using UnityEngine;

/// <summary>
/// A/B 区域边界的只读快照。ProceduralLevelGenerator.Generate() 后写入，
/// 供 RegionGate / TarotReturnInteractable 查询相机房间范围和 zone 归属，
/// 替代旧关卡里硬编码的 SceneBOrigin=34、RoomSpec 表。
/// </summary>
public readonly struct LevelRegionInfo
{
    public readonly float MinX;
    public readonly float MaxX;
    public readonly int ZoneBase;
    public readonly int ZoneCount;
    public readonly int PlatformsPerZone;

    public LevelRegionInfo(float minX, float maxX, int zoneBase, int zoneCount, int platformsPerZone)
    {
        MinX = minX;
        MaxX = maxX;
        ZoneBase = zoneBase;
        ZoneCount = zoneCount;
        PlatformsPerZone = platformsPerZone;
    }

    /// <summary>粗略按区域内相对位置换算 zone 编号；用于回流时的近似定位，
    /// 不需要逐平台精确匹配 —— 回流后角色会重新触发真正的 ZoneTrigger 校正。</summary>
    public int ZoneAt(float worldX)
    {
        if (ZoneCount <= 0)
            return -1;
        float span = Mathf.Max(0.001f, MaxX - MinX);
        float t = Mathf.Clamp01((worldX - MinX) / span);
        int offset = Mathf.Clamp(Mathf.FloorToInt(t * ZoneCount), 0, ZoneCount - 1);
        return ZoneBase + offset;
    }
}

public static class LevelRegionRegistry
{
    private static LevelRegionInfo? regionA;
    private static LevelRegionInfo? regionB;

    public static void Register(bool sceneA, LevelRegionInfo info)
    {
        if (sceneA)
            regionA = info;
        else
            regionB = info;
    }

    public static void Clear()
    {
        regionA = null;
        regionB = null;
    }

    public static LevelRegionInfo? Get(bool sceneA)
    {
        return sceneA ? regionA : regionB;
    }
}
