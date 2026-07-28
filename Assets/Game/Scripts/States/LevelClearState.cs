public class LevelClearState : IGameState
{
    private readonly GameContext context;

    private const float ClearDuration = 1.0f;

    public LevelClearState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        context.InputManager.DisableAllInput();

        context.Player.StopMovement();
        context.UIManager.ShowResult();
        context.InputManager.EnableUIInput();

        context.LevelClearTimer = 0f;
        context.IsLevelClear = true;
    }

    public void Update(float deltaTime)
    {
        context.LevelClearTimer += deltaTime;

        if (context.LevelClearTimer >= ClearDuration)
        {
            context.StateMachine.ChangeState(GameStateType.Result);
        }
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
    }
}
