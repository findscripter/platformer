public class EchoSpaceState : IGameState
{
    private readonly GameContext context;
    private EchoSpaceController controller;

    public EchoSpaceState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        context.InputManager.DisableAllInput();
        context.InputManager.EnableUIInput();
        context.UIManager.HideAll();

        if (context.Player != null)
            context.Player.StopMovement();

        controller = EchoSpaceController.Ensure();
        controller.Begin(context);
    }

    public void Update(float deltaTime)
    {
        if (controller != null && controller.IsFinished)
            context.StateMachine.ChangeState(GameStateType.Result);
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
        if (controller != null)
            controller.Hide();
    }
}
