/// <summary>
/// 关卡实现规则表 E00–E18。效果通道与覆写以该表为准，不从 HTML 推断。
/// </summary>
public enum TarotEffectId
{
    E00 = 0,
    E01 = 1,
    E02 = 2,
    E03 = 3,
    E04 = 4,
    E05 = 5,
    E06 = 6,
    E07 = 7,
    E08 = 8,
    E09 = 9,
    E10 = 10,
    E11 = 11,
    E12 = 12,
    E13 = 13,
    E14 = 14,
    E15 = 15,
    E16 = 16,
    E17 = 17,
    E18 = 18
}

public enum TarotEffectChannel
{
    None,
    Movement,
    HazardDamage,
    CombatRange,
    AerialCombat,
    RouteTopology,
    Survival,
    DoorState,
    Progression,
    PlatformState,
    MovementRestriction,
    HazardGeometry,
    PlatformGeometry,
    EnemyDurability
}

public static class TarotEffectChannels
{
    public static TarotEffectChannel Of(TarotEffectId id)
    {
        switch (id)
        {
            case TarotEffectId.E01:
            case TarotEffectId.E03:
                return TarotEffectChannel.Movement;
            case TarotEffectId.E02:
                return TarotEffectChannel.HazardDamage;
            case TarotEffectId.E04:
                return TarotEffectChannel.CombatRange;
            case TarotEffectId.E05:
            case TarotEffectId.E17:
                return TarotEffectChannel.AerialCombat;
            case TarotEffectId.E06:
                return TarotEffectChannel.RouteTopology;
            case TarotEffectId.E07:
                return TarotEffectChannel.Survival;
            case TarotEffectId.E08:
            case TarotEffectId.E10:
            case TarotEffectId.E11:
                return TarotEffectChannel.DoorState;
            case TarotEffectId.E09:
                return TarotEffectChannel.Progression;
            case TarotEffectId.E12:
            case TarotEffectId.E18:
                return TarotEffectChannel.PlatformState;
            case TarotEffectId.E13:
                return TarotEffectChannel.MovementRestriction;
            case TarotEffectId.E14:
                return TarotEffectChannel.HazardGeometry;
            case TarotEffectId.E15:
                return TarotEffectChannel.PlatformGeometry;
            case TarotEffectId.E16:
                return TarotEffectChannel.EnemyDurability;
            default:
                return TarotEffectChannel.None;
        }
    }

    public static bool Stacks(TarotEffectChannel channel)
    {
        return channel == TarotEffectChannel.Movement
            || channel == TarotEffectChannel.DoorState
            || channel == TarotEffectChannel.RouteTopology
            || channel == TarotEffectChannel.HazardDamage;
    }
}
