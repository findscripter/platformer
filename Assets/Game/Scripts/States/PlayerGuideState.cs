public class PlayerGuideState : IGameState
{
    private readonly GameContext context;

    public PlayerGuideState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        UnityEngine.Time.timeScale = 1f;

        context.UIManager.HideAll();
        context.InputManager.EnableGameplayInput();
        context.InputManager.EnableUIInput();
        context.ResetRuntimeFlags();
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
