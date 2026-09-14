using UnityEngine;

/// <summary>
/// CP 检查点。更新复活点；死亡不清除塔罗状态。
/// </summary>
public sealed class CheckpointInteractable : InteractableBase
{
    private BoxCollider2D support;
    public override string InteractPrompt => "记录复活点";
    public void ConfigureSupport(BoxCollider2D platform) => support = platform;

    public Vector3 SafePosition(PlayerController player)
    {
        if(support == null || player == null) return transform.position;
        Collider2D body=player.GetComponent<Collider2D>();
        float halfWidth=body!=null?body.bounds.extents.x:.25f;
        float footOffset=body!=null?player.transform.position.y-body.bounds.min.y:.5f;
        Bounds bed=support.bounds;
        float margin=Mathf.Min(halfWidth+.12f,bed.extents.x*.8f);
        return new Vector3(Mathf.Clamp(transform.position.x,bed.min.x+margin,bed.max.x-margin),
            bed.max.y+footOffset+.08f,transform.position.z);
    }

    public override void Interact(GameContext context)
    {
        Save(context);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        Save(GameLoop.Instance != null ? GameLoop.Instance.Context : null);
    }

    private void Save(GameContext context)
    {
        if (context == null)
            return;

        context.PlayerSpawnPoint = transform;
        context.LevelManager?.SetSpawnPoint(transform);
        Debug.Log("[Checkpoint] Recorded "+name);
    }
}
