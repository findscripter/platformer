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
            context.Player.Respawn(spawnPoint.position);
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
