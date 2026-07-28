public class PauseState : IGameState
{
    private readonly GameContext context;

    public PauseState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        UnityEngine.Time.timeScale = 0f;

        context.Player.ClearJumpBuffer();
        context.UIManager.ShowPauseMenu();
        context.InputManager.EnableUIInput();
    }

    public void Update(float deltaTime)
    {
        if (context.InputManager.PausePressed)
        {
            context.StateMachine.ChangeState(GameStateType.Playing);
        }
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
        UnityEngine.Time.timeScale = 1f;

        context.UIManager.HidePauseMenu();
    }
}
