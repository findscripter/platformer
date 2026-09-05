using UnityEngine;

/// <summary>
/// CP 检查点。更新复活点；死亡不清除塔罗状态。
/// </summary>
public sealed class CheckpointInteractable : InteractableBase
{
    public override string InteractPrompt => "记录梦核";

    public override void Interact(GameContext context)
    {
        if (context == null)
            return;

        context.PlayerSpawnPoint = transform;
        context.LevelManager?.SetSpawnPoint(transform);
        Debug.Log("[Checkpoint] " + InteractableId);
    }
}
