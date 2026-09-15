using System.Collections;

public class LevelClearState : IGameState
{
    private readonly GameContext context;
    private bool handedOff;

    public LevelClearState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        GuideSfx.PlayPickup();
        context.InputManager.DisableAllInput();
        if (context.Player != null)
            context.Player.FreezePhysics();

        context.LevelClearTimer = 0f;
        context.IsLevelClear = true;
        handedOff = false;

        context.GameLoop.StartCoroutine(FadeThenEcho());
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

    private IEnumerator FadeThenEcho()
    {
        if (handedOff)
            yield break;

        handedOff = true;

        var transition = context.SceneTransitionManager;
        if (transition != null)
            yield return transition.FadeToBlack(0.5f);

        context.StateMachine.ChangeState(GameStateType.EchoSpace);
    }
}
