using UnityEngine;

/// <summary>
/// 同场景区域传送（TRANS-A-B）。塔罗状态保留在 GameContext。
/// </summary>
public sealed class RegionGate : InteractableBase
{
    [SerializeField] private Transform destination;

    public override string InteractPrompt => "进入下一区域";

    public override void Interact(GameContext context)
    {
        if (destination == null || context?.Player == null)
            return;

        context.Player.transform.position = destination.position;
        Rigidbody2D body = context.Player.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;

        context.PlayerSpawnPoint = destination;
        context.LevelManager?.SetSpawnPoint(destination);

        CameraTargetFollow follow = Object.FindFirstObjectByType<CameraTargetFollow>();
        follow?.SetRoom(
            DreamremainsLevelBootstrap.SceneBOrigin,
            DreamremainsLevelBootstrap.SceneBOrigin + 5.2f,
            0f,
            8f);
        TarotZoneQuery.EnterZone(4);
    }

    public void ConfigureDestination(Transform dest)
    {
        destination = dest;
        Configure("TRANS-A-B", InteractPrompt, 1.4f);
    }
}
