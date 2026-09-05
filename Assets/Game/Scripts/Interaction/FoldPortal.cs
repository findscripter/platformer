using UnityEngine;

/// <summary>
/// E06 折叠入口。消耗一次充能，传送到本 Zone 末端落点，不跳过 T/CP/TRANS/END。
/// </summary>
public sealed class FoldPortal : InteractableBase
{
    [SerializeField] private Transform exitPoint;

    public override string InteractPrompt => "折叠捷径";

    public override bool CanInteract
    {
        get
        {
            TarotResultData run = GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null;
            return run != null && run.FoldExitCharge > 0 && exitPoint != null;
        }
    }

    public override void Interact(GameContext context)
    {
        if (context?.TarotResult == null || exitPoint == null)
            return;
        if (!context.TarotResult.TryConsumeFoldCharge())
            return;

        PlayerController player = context.Player;
        if (player == null)
            return;

        player.transform.position = exitPoint.position;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    public void ConfigureExit(Transform exit)
    {
        exitPoint = exit;
        Configure(name, InteractPrompt, 1.2f);
    }
}
