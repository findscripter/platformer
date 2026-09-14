using UnityEngine;

/// <summary>
/// T1–T4。绑定抽牌槽位 0–3，按阶段领取后解锁该槽效果与对应隐藏路。不和 TRG 混用。
/// </summary>
public sealed class TarotNodeInteractable : InteractableBase
{
    [SerializeField, Range(-1, 3)] private int slotIndex = -1;
    private static readonly Transform[] BySlot = new Transform[4];

    public override string InteractPrompt => "领取牌印";

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        TryPickup(other);
    }

    private void OnTriggerStay2D(Collider2D other) => TryPickup(other);

    private void TryPickup(Collider2D other)
    {
        var context = GameLoop.Instance != null ? GameLoop.Instance.Context : null;
        if (context?.Player == null || context.Player.IsDead ||
            context.StateMachine.CurrentStateType != GameStateType.Playing ||
            other.GetComponentInParent<PlayerController>() != context.Player || !CanInteract)
            return;
        Interact(context);
    }

    public override bool CanInteract
    {
        get
        {
            var context=GameLoop.Instance != null ? GameLoop.Instance.Context : null;
            TarotResultData run = context?.TarotResult;
            if(slotIndex>=2)
            {
                var player=context?.Player;
                if(player==null || player.IsDead || !player.IsGrounded)return false;
                var body=player.GetComponent<Collider2D>();
                if(body==null || Mathf.Abs(body.bounds.center.x-transform.position.x)>.6f ||
                    Mathf.Abs(body.bounds.min.y-(transform.position.y-.35f))>.45f)return false;
            }
            return run != null && run.IsComplete() && run.FirstMissingRequiredNode(TarotResultData.SlotCount) == slotIndex;
        }
    }

    public override void Interact(GameContext context)
    {
        if (context?.TarotResult == null || !CanInteract)
            return;

        if (!context.TarotResult.ActivateNode(slotIndex))
            return;

        TarotEffectApplier.Apply(context);
        EventCenter.Publish(GameEvents.TarotNodeActivated, slotIndex);
        Debug.Log("[TarotNode] T" + (slotIndex + 1) + " " + context.TarotResult.FormatSlot(slotIndex));
    }

    public void ConfigureSlot(int slot)
    {
        slotIndex = Mathf.Clamp(slot, 0, 3);
        Configure("T" + (slotIndex + 1), InteractPrompt, 1.5f);
        BySlot[slotIndex] = transform;
    }

    protected override void Awake()
    {
        base.Awake();
        if (slotIndex >= 0 && slotIndex < BySlot.Length)
            BySlot[slotIndex] = transform;
    }

    private void OnDestroy()
    {
        if (slotIndex >= 0 && slotIndex < BySlot.Length && BySlot[slotIndex] == transform)
            BySlot[slotIndex] = null;
    }

    public static Transform ByIndex(int slot)
    {
        return slot >= 0 && slot < BySlot.Length ? BySlot[slot] : null;
    }
}
