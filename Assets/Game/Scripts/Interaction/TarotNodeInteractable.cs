using UnityEngine;

/// <summary>
/// T1–T4。绑定抽牌槽位 0–3，交互后解锁该槽效果与对应隐藏路。不和 TRG 混用。
/// 自注册到静态表：随机生成后塔罗点位置每局不同，TarotReturnInteractable
/// 靠这张表按 slot 找回流目标，不再依赖旧关卡固定的场景路径字符串。
/// </summary>
public sealed class TarotNodeInteractable : InteractableBase
{
 [SerializeField, Range(-1, 3)] private int slotIndex = -1;

    private static readonly Transform[] BySlot = new Transform[4];

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
        BySlot[slotIndex] = transform;
    }

    protected override void Awake()
 {
    base.Awake();
        // slotIndex 默认是 -1 哨兵值：ConfigureSlot() 显式调用前不注册，
                // 避免 AddComponent 触发的这次 Awake 用未设置的字段污染 BySlot[0]。
        if (slotIndex >= 0 && slotIndex < BySlot.Length)
      BySlot[slotIndex] = transform;
    }

  private void OnDestroy()
    {
     if (slotIndex >= 0 && slotIndex < BySlot.Length && BySlot[slotIndex] == transform)
            BySlot[slotIndex] = null;
    }

    /// <summary>slot 对应的塔罗点当前 Transform；关卡未生成或已销毁时为 null。</summary>
    public static Transform ByIndex(int slot)
    {
        return slot >= 0 && slot < BySlot.Length ? BySlot[slot] : null;
    }
}
