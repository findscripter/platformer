using UnityEngine;

/// <summary>
/// 同场景区域传送（TRANS-A-B）。塔罗状态保留在 GameContext。
/// </summary>
public sealed class RegionGate : InteractableBase
{
    [SerializeField] private Transform destination;

    public override string InteractPrompt
    {
        get
        {
            CollectibleTracker tracker = CollectibleTracker.Active;
            if (tracker != null && !tracker.AreaAComplete)
                return "收集全部梦核碎片后进入  " + tracker.AreaACollected + "/" + tracker.AreaASpawned;
            return "进入下一区域";
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        TryAutoEnter(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryAutoEnter(other);
    }

    private void TryAutoEnter(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        GameContext context = GameLoop.Instance != null ? GameLoop.Instance.Context : null;
        if (context == null)
            return;

        if (CollectibleTracker.Active != null && !CollectibleTracker.Active.AreaAComplete)
            return;

        Interact(context);
    }

    public override void Interact(GameContext context)
    {
        if (context?.Player == null)
            return;

        if (CollectibleTracker.Active != null && !CollectibleTracker.Active.AreaAComplete)
            return;

        LevelRegionInfo? regionB = LevelRegionRegistry.Get(false);
        float minX = regionB.HasValue ? regionB.Value.MinX : destination != null ? destination.position.x - 15f : 0f;
        float maxX = regionB.HasValue ? regionB.Value.MaxX : destination != null ? destination.position.x + 15f : 0f;
        int zone = regionB.HasValue ? regionB.Value.ZoneBase : 4;
        bool moved = MovePreservingRun(context, destination, zone, minX, maxX, false);

        if (moved)
            context.InteractionManager?.Unregister(this);
    }

    /// <summary>普通跨区与必要回流共用；不复活、不重新应用牌效、不重建本局数据。</summary>
    internal static bool MovePreservingRun(GameContext context, Transform target, int zone, float minX, float maxX, bool returning)
    {
        PlayerController player = context?.Player;
        if (player == null || player.IsDead || target == null)
            return false;

        // 只清空移动意图、跳跃缓冲与离开区域的 airwalk 锁，保留生命、空跳额度及动画状态。
        bool inputWasEnabled = player.IsInputEnabled;
        player.SetInputEnabled(false);
        player.ClearAirWalkLocks();
        player.transform.position = target.position;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = target.position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        player.SetInputEnabled(inputWasEnabled);

        context.PlayerSpawnPoint = target;
        context.LevelManager?.SetSpawnPoint(target);

        CameraTargetFollow follow = player.GetComponentInChildren<CameraTargetFollow>();
        if (follow == null)
            follow = Object.FindAnyObjectByType<CameraTargetFollow>();
        follow?.SetRoom(minX, maxX, 0f, 16f);

        // 回流不是重新结算区域：仅切换查询指针，避免 EnterZone 冻结尚未结算的离开区。
        // 正常 A→B 保留原有离区快照语义；已存在的 zone 快照两种路径都不修改。
        if (returning)
            TarotZoneQuery.ResetCurrent();
        TarotZoneQuery.EnterZone(zone);
        return true;
    }

    public void ConfigureDestination(Transform dest)
    {
        destination = dest;
        Configure("TRANS-A-B", InteractPrompt, 2.4f);
    }
}
