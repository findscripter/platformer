public class MainMenuState : IGameState
{
    private readonly GameContext context;

    public MainMenuState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        UnityEngine.Time.timeScale = 1f;

        context.UIManager.ShowMainMenu();
        context.InputManager.EnableUIInput();

        context.ResetRuntimeFlags();
    }

    public void Update(float deltaTime)
    {
        if (context.InputManager.ConfirmPressed)
        {
            context.GameLoop.StartNewGame();
        }
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
    }
}
