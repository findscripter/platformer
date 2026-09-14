public class RespawnState : IGameState
{
    private readonly GameContext context;

    public RespawnState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        UnityEngine.Transform spawnPoint = context.PlayerSpawnPoint;

        if (spawnPoint == null)
        {
            spawnPoint = context.LevelManager.DefaultSpawnPoint;
        }

        if (spawnPoint != null)
        {
            bool sceneA = spawnPoint.position.x < DreamremainsLevelBootstrap.SceneBOrigin;
            float origin = sceneA ? 0f : DreamremainsLevelBootstrap.SceneBOrigin;
            int zone = DreamremainsLevelData.ZoneAt(spawnPoint.position.x - origin, sceneA);

            UnityEngine.Object.FindAnyObjectByType<CameraTargetFollow>()?.FollowCurrentRegion();
            TarotZoneQuery.ResetCurrent();
            TarotZoneQuery.EnterZone(zone);
            var checkpoint = spawnPoint.GetComponent<CheckpointInteractable>();
            context.Player.Respawn(
                checkpoint != null ? checkpoint.SafePosition(context.Player) : spawnPoint.position,
                refillHealth: false);
        }

        context.LevelManager.ResetLevel();
        context.DreamRun?.Log("retry", "从刚才停下的地方再走一次");
        TarotEffectApplier.Apply(context);

        context.StateMachine.ChangeState(GameStateType.Playing);
    }

    public void Update(float deltaTime)
    {
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
    }
}
