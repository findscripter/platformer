using UnityEngine;

/// <summary>
/// 终点缺失塔罗时提供必要回流；实际节点必须由玩家重新到达并交互。
/// 程序化关卡下塔罗点位置每局随机，回流目标改用 TarotNodeInteractable
/// 的静态注册表按 slot 查找，不再依赖旧关卡固定的场景路径字符串。
/// </summary>
public sealed class TarotReturnInteractable : MonoBehaviour, IInteractable
{
    private TarotResultData CurrentRun => GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null;

    public Transform Transform => transform;
    public string InteractableId => "END_Return";
    public bool CanInteract => FirstMissingNode(CurrentRun, TarotResultData.SlotCount) >= 0;
    public string InteractPrompt => ReturnPrompt(FirstMissingNode(CurrentRun, TarotResultData.SlotCount));

    // 直接复用 END 的原 BoxCollider2D trigger，不添加范围不同的第二个碰撞体。
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            InteractionManager.Instance?.Register(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            InteractionManager.Instance?.Unregister(this);
    }

    private void OnDisable()
    {
        InteractionManager.Instance?.Unregister(this);
    }

    public void Interact(GameContext context)
    {
        int missing = FirstMissingNode(context?.TarotResult, TarotResultData.SlotCount);
        if (missing >= 0 && ReturnToSafePoint(context, missing))
            context.InteractionManager?.Unregister(this);
    }

    internal static int FirstMissingNode(TarotResultData run, int count)
    {
        // 无牌/未完成抽牌与 PlayingState 的无牌通关分支一致，不增加主线门槛。
        if (run == null || !run.IsComplete())
            return -1;
        for (int slot = 0; slot < Mathf.Min(count, TarotResultData.SlotCount); slot++)
            if (!run.IsNodeActivated(slot))
                return slot;
        return -1;
    }

    internal static string ReturnPrompt(int slot)
    {
        if (slot < 0)
            return string.Empty;
        return "第" + (slot + 1) + "处塔罗尚未解读，返回" + (slot == 0 ? "起点" : "此前的梦核");
    }

    internal static bool ReturnToSafePoint(GameContext context, int slot)
    {
        Transform safe = TarotNodeInteractable.ByIndex(slot);
        if (safe == null)
        {
            Debug.LogWarning("[TarotReturn] 塔罗点 slot=" + slot + " 未在当前关卡中注册。");
            return false;
        }

        // slot 0/1 在 A 区，2/3 在 B 区；用生成器自己上报的区域边界，
        // 不再假设旧关卡固定的 SceneBOrigin=34、宽度 30/34。
        bool sceneA = slot < 2;
        LevelRegionInfo? region = LevelRegionRegistry.Get(sceneA);
        float minX = region.HasValue ? region.Value.MinX : safe.position.x - 15f;
        float maxX = region.HasValue ? region.Value.MaxX : safe.position.x + 15f;
        int zone = region.HasValue ? region.Value.ZoneAt(safe.position.x) : -1;

        return RegionGate.MovePreservingRun(context, safe, zone, minX, maxX, true);
    }
}
