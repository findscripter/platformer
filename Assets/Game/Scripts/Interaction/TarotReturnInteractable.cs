using UnityEngine;

/// <summary>终点缺失塔罗时提供必要回流；实际节点必须由玩家重新到达并交互。</summary>
public sealed class TarotReturnInteractable : MonoBehaviour, IInteractable
{
    private static readonly string[] SafePointPaths =
    {
        "Scene_A/SP-A01", "Scene_A/CP-A01", "Scene_B/CP-B01", "Scene_B/CP-B02"
    };

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
        if (missing >= 0 && ReturnToSafePoint(context, transform, missing))
            context.InteractionManager?.Unregister(this);
    }

    internal static int FirstMissingNode(TarotResultData run, int count)
    {
        // 无牌/未完成抽牌与 PlayingState 的无牌通关分支一致，不增加主线门槛。
        if (run == null || !run.IsComplete())
            return -1;
        return run.FirstMissingRequiredNode(count);
    }

    internal static string ReturnPrompt(int slot)
    {
        if (slot < 0)
            return string.Empty;
        return "第" + (slot + 1) + "张牌印尚未领取，返回" + (slot == 0 ? "起点" : "此前的记录点");
    }

    internal static bool ReturnToSafePoint(GameContext context, Transform origin, int slot)
    {
        if (origin == null || slot < 0 || slot >= SafePointPaths.Length)
            return false;
        Transform level = origin;
        while (level.parent != null && level.name != DreamremainsLevelBootstrap.RootName)
            level = level.parent;
        Transform safe = level.name == DreamremainsLevelBootstrap.RootName ? level.Find(SafePointPaths[slot]) : null;
        if (safe == null)
        {
            Debug.LogWarning("[TarotReturn] Missing safe point: " + SafePointPaths[slot]);
            return false;
        }

        bool sceneA = slot < 2;
        float originX = sceneA ? 0f : DreamremainsLevelBootstrap.SceneBOrigin;
        float localX = safe.position.x - originX;
        float minX = originX;
        float maxX = originX + (sceneA ? DreamremainsExpandedLayout.ALength : DreamremainsExpandedLayout.BLength);
        int zone = -1;
        DreamremainsLevelData.SceneSpec spec = sceneA ? DreamremainsLevelData.SceneA : DreamremainsLevelData.SceneB;
        for (int i = 0; i < spec.Rooms.Length; i++)
        {
            DreamremainsLevelData.RoomSpec room = spec.Rooms[i];
            if (localX < room.X1 || localX >= room.X2)
                continue;
            zone = room.ZoneIndex;
            minX = originX + room.X1;
            maxX = originX + room.X2;
            break;
        }
        return RegionGate.MovePreservingRun(context, safe, zone, minX, maxX, true);
    }
}
