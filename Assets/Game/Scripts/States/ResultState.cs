public class ResultState : IGameState
{
    private readonly GameContext context;

    public ResultState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        UnityEngine.Time.timeScale = 1f;

        context.UIManager.HideAll();
        DreamReplayPanel.Show(context);
        context.InputManager.EnableUIInput();
    }

    public void Update(float deltaTime)
    {
        if (context.InputManager.PausePressed)
        {
            context.GameLoop.ReturnToMainMenu();
        }
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
        DreamReplayPanel.Hide();
    }
}
