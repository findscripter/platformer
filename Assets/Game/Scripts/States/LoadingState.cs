using System.Collections;

public class LoadingState : IGameState
{
    private readonly GameContext context;

    public LoadingState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        context.InputManager.DisableAllInput();
        context.UIManager.HideAll();
        context.SceneTransitionManager?.ShowBlackImmediate();
        context.GameLoop.StartCoroutine(EnterRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        context.LevelManager.LoadLevel();

        UnityEngine.Transform spawnPoint = context.PlayerSpawnPoint;

        if (spawnPoint == null)
        {
            spawnPoint = context.LevelManager.DefaultSpawnPoint;
        }

        if (spawnPoint != null)
        {
            context.Player.Respawn(spawnPoint.position);
        }

        context.IsLevelLoaded = true;
        context.ResetRuntimeFlags();

        if (context.SceneTransitionManager != null)
        {
            yield return context.SceneTransitionManager.PlayGameplayEnterFade();
        }

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
