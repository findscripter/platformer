using UnityEngine;

/// <summary>关键节点的可见状态，只读取现有交互/存档状态，不创建交互或碰撞。</summary>
public sealed class LevelDecorationNode : MonoBehaviour
{
    private string kind;
    private int slot;
    private bool foldExit;
    private DoorInteractable door;
    private SpriteRenderer[] visuals;
    private Color[] colors;

    public void Configure(string nodeKind, int slotIndex, bool isFoldExit)
    {
        kind = nodeKind;
        slot = slotIndex;
        foldExit = isFoldExit;
        door = GetComponent<DoorInteractable>();
        visuals = GetComponentsInChildren<SpriteRenderer>();
        colors = new Color[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
            colors[i] = visuals[i].color;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (visuals == null)
            return;
        GameContext context = GameLoop.Instance != null ? GameLoop.Instance.Context : null;
        TarotResultData run = context != null ? context.TarotResult : null;
        Color tint = Color.white;
        bool visible = true;
        switch (kind)
        {
            case "door":
                visible = door == null || !door.IsOpen;
                break;
            case "tarot":
                // 翻到真实牌面后保留原色，不用半透明染色代替激活反馈。
                break;
            case "cp":
                // Checkpoints use text feedback. Lotus/scenery never changes colour or becomes loot.
                break;
            case "fold":
                tint.a = foldExit ? 0.3f : (run != null && run.FoldExitCharge > 0 ? 0.85f : 0.18f);
                break;
        }
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null)
                continue;
            visuals[i].enabled = visible;
            visuals[i].color = colors[i] * tint;
        }
    }
}
