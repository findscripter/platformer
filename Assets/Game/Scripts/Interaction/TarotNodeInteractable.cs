using UnityEngine;

/// <summary>
/// T1–T4。绑定抽牌槽位 0–3，交互后解锁该槽效果与对应隐藏路。不和 TRG 混用。
/// </summary>
public sealed class TarotNodeInteractable : InteractableBase
{
    [SerializeField, Range(0, 3)] private int slotIndex;

    public override string InteractPrompt => "解读塔罗";

    public override bool CanInteract
    {
        get
        {
            TarotResultData run = GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null;
            return run != null && run.IsComplete() && !run.IsNodeActivated(slotIndex);
        }
    }

    public override void Interact(GameContext context)
    {
        if (context?.TarotResult == null)
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
    }
}
