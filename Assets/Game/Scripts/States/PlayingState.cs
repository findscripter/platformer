using System.Diagnostics;

public class PlayingState : IGameState
{
    private readonly GameContext context;

    public PlayingState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        UnityEngine.Time.timeScale = 1f;

        context.UIManager.ShowGameplayUI();
        context.InputManager.EnableGameplayInput();

        context.IsPlayerDead = false;
        context.IsLevelClear = false;
    }

    public void Update(float deltaTime)
    {
        if (context.InputManager.PausePressed)
        {
            context.StateMachine.ChangeState(GameStateType.Paused);
            return;
        }

        context.InteractionManager?.Tick(context);

        if (context.InteractionManager != null &&
            context.InteractionManager.TryInteract(context))
        {
            return;
        }

        context.Player.HandleInput(context.InputManager);

        CheckPlayerDead();
        CheckLevelClear();
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        context.Player.FixedTick(fixedDeltaTime);
    }

    public void Exit()
    {
    }

    private void CheckPlayerDead(bool forceDead = false)
    {
        bool dead = context.LevelManager.CheckPlayerDead(context.Player);

        if (dead || forceDead)
        {
            context.Player.TryKill();
            context.StateMachine.ChangeState(GameStateType.PlayerDead);
        }
    }

    private void CheckLevelClear()
    {
        bool clear = context.LevelManager.CheckLevelClear(context.Player);

        if (clear)
        {
            context.StateMachine.ChangeState(GameStateType.LevelClear);
        }
    }
}
