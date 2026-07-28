public class PlayerDeadState : IGameState
{
    private readonly GameContext context;

    private const float DeadDuration = 1.0f;

    public PlayerDeadState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        context.InputManager.DisableAllInput();

        context.Player.StopMovement();
        context.DeadStateTimer = 0f;
        context.IsPlayerDead = true;
        context.UIManager.ShowDeathUI();
    }

    public void Update(float deltaTime)
    {
        context.DeadStateTimer += deltaTime;

        if (context.DeadStateTimer >= DeadDuration)
        {
            context.StateMachine.ChangeState(GameStateType.Respawning);
        }
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
        context.UIManager.HideDeathUI();
    }
}
