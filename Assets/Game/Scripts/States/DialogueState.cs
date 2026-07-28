public class DialogueState : IGameState
{
    private readonly GameContext context;

    public DialogueState(GameContext context)
    {
        this.context = context;
    }

    public void Enter()
    {
        UnityEngine.Time.timeScale = 1f;
        context.InputManager.EnableUIInput();
        context.Player?.SetInputEnabled(false);
    }

    public void Update(float deltaTime)
    {
        context.DialogueManager?.Tick();

        if (context.DialogueManager == null)
            return;

        if (context.DialogueManager.IsWaitingForConfirm && context.InputManager.ConfirmPressed)
            context.DialogueManager.OnConfirmPressed();

        if (context.InputManager.PausePressed)
            context.DialogueManager.EndDialogue();
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
        context.Player?.SetInputEnabled(true);
        context.InputManager.EnableGameplayInput();
    }
}
