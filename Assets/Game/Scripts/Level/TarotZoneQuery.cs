using UnityEngine;

public static class TarotZoneQuery
{
    public static int CurrentZone { get; private set; } = -1;

    public static void EnterZone(int zoneIndex)
    {
        TarotResultData run = GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null;
        if (CurrentZone >= 0 && CurrentZone != zoneIndex)
            run?.SnapshotZone(CurrentZone);

        CurrentZone = zoneIndex;
        EventCenter.Publish(GameEvents.ZoneEntered, zoneIndex);
    }

    public static void ResetCurrent()
    {
        CurrentZone = -1;
    }

    public static bool HasEffect(int zoneIndex, TarotEffectId id)
    {
        TarotResultData run = GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null;
        if (run == null || id == TarotEffectId.E00)
            return false;

        if (zoneIndex < 0)
            return false;

        if (run.IsZoneResolved(zoneIndex))
            return run.ZoneHasEffect(zoneIndex, id);

        return run.HasActiveEffect(id);
    }
}
