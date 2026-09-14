using UnityEngine;

/// <summary>
/// 把已解锁牌效接到角色、门和敌人。平台/尖刺/空中行走走 Zone 快照载体。
/// </summary>
public static class TarotEffectApplier
{
    public static void Apply(GameContext context)
    {
        if (context == null)
            return;

        TarotResultData run = context.TarotResult;
        PlayerController player = context.Player;
        if (run == null || !run.IsComplete() || player == null)
            return;

        player.SetMoveLeftAllowed(!run.HasActiveEffect(TarotEffectId.E13));
        player.SetAttackRangeMultiplier(run.HasActiveEffect(TarotEffectId.E04) ? 5f : 1f);
        player.SetExtraAirJumps(run.HasActiveEffect(TarotEffectId.E03) ? 2 : 1);

        if (run.HasActiveEffect(TarotEffectId.E07))
            player.SetMaxHealth(30, fill: player.MaxHealth < 30);

        TarotEffectId aerial = run.GetResolvedEffect(TarotEffectChannel.AerialCombat);
        if (aerial == TarotEffectId.E05)
            player.SetAerialAttackMode(PlayerController.AerialAttackMode.Execute);
        else if (aerial == TarotEffectId.E17)
            player.SetAerialAttackMode(PlayerController.AerialAttackMode.Nullify);
        else
            player.SetAerialAttackMode(PlayerController.AerialAttackMode.Normal);

        if (run.HasActiveEffect(TarotEffectId.E08))
            EventCenter.Publish(GameEvents.OpenDoor, "D1");
        if (run.HasActiveEffect(TarotEffectId.E10))
            EventCenter.Publish(GameEvents.OpenDoor, "D2");
        if (run.HasActiveEffect(TarotEffectId.E11))
            EventCenter.Publish(GameEvents.OpenDoor, "D3");

        PatrolEnemy[] enemies = Object.FindObjectsByType<PatrolEnemy>(FindObjectsInactive.Exclude);
        bool bulk = run.HasActiveEffect(TarotEffectId.E16);
        for (int i = 0; i < enemies.Length; i++)
            enemies[i].SetHealthMultiplier(bulk ? 10 : 1);
    }
}
