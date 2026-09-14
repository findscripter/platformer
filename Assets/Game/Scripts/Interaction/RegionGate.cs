using UnityEngine;

/// <summary>
/// 同场景区域传送（TRANS-A-B）。塔罗状态保留在 GameContext。
/// </summary>
public sealed class RegionGate : InteractableBase
{
    [SerializeField] private Transform destination;
    private bool presenting;

    // Completed A nodes transition automatically; manual interaction remains only for missing-node return.
    public override bool CanInteract => TarotReturnInteractable.FirstMissingNode(GameLoop.Instance?.Context?.TarotResult, 2) >= 0;

    private void OnTriggerStay2D(Collider2D other)
    {
        var context = GameLoop.Instance?.Context;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null || player != context?.Player || player.IsDead || presenting || !player.IsGrounded) return;
        if (context.StateMachine.CurrentStateType != GameStateType.Playing || context.TarotResult == null || !context.TarotResult.IsComplete()) return;
        if (TarotReturnInteractable.FirstMissingNode(context.TarotResult, 2) >= 0) return;
        // Require arrival at the transition, not merely grazing the outer trigger from another floor.
        if (Mathf.Abs(player.transform.position.x-transform.position.x) > .6f ||
            Mathf.Abs(other.bounds.min.y-transform.position.y) > .8f) return;
        Interact(context);
    }

    public override string InteractPrompt
    {
        get
        {
            TarotResultData run = GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null;
            int missing = TarotReturnInteractable.FirstMissingNode(run, 2);
            return missing >= 0 ? TarotReturnInteractable.ReturnPrompt(missing) : "进入下一区域";
        }
    }

    public override void Interact(GameContext context)
    {
        if (context?.Player == null)
            return;
        if(presenting)return;

        int missing = TarotReturnInteractable.FirstMissingNode(context.TarotResult, 2);
        if(missing<0 && context.TarotResult!=null && !context.TarotResult.TransitionDialogueSeen)
        {
            if(context.DialogueManager==null || context.DialogueManager.IsRunning)return;
            StartCoroutine(PresentTransition(context));
            return;
        }
        bool moved = missing >= 0
            ? TarotReturnInteractable.ReturnToSafePoint(context, transform, missing)
            : MovePreservingRun(context, destination, 4,
                DreamremainsLevelBootstrap.SceneBOrigin, DreamremainsLevelBootstrap.SceneBOrigin + DreamremainsExpandedLayout.BLength, false);
        if (moved)
            context.InteractionManager?.Unregister(this);
    }

    private System.Collections.IEnumerator PresentTransition(GameContext context)
    {
        presenting=true;
        var run=context.TarotResult;
        var dialogue=ScriptableObject.CreateInstance<DialogueSO>();
        dialogue.dialogueId="FF-TRANS";
        dialogue.startNode=new TextNode{nodeId="FF-TRANS",speaker="腓腓",
            text="你走出来了。前面的梦开始有颜色了，我们在那里再见。"};
        context.DialogueManager.BeginDialogue(dialogue,context);
        while(context.DialogueManager!=null && context.DialogueManager.IsRunning)yield return null;
        if(context.TarotResult==run && context.Player!=null && !context.Player.IsDead &&
            MovePreservingRun(context,destination,4,DreamremainsLevelBootstrap.SceneBOrigin,
                DreamremainsLevelBootstrap.SceneBOrigin+DreamremainsExpandedLayout.BLength,false))
        {
            run.TransitionDialogueSeen=true;
            context.InteractionManager?.Unregister(this);
        }
        Destroy(dialogue);presenting=false;
    }

    /// <summary>普通跨区与必要回流共用；不复活、不重新应用牌效、不重建本局数据。</summary>
    internal static bool MovePreservingRun(GameContext context, Transform target, int zone, float minX, float maxX, bool returning)
    {
        PlayerController player = context?.Player;
        if (player == null || player.IsDead || target == null)
            return false;

        // 只清空移动意图、跳跃缓冲与离开区域的 airwalk 锁，保留生命、空跳额度及动画状态。
        bool inputWasEnabled = player.IsInputEnabled;
        player.SetInputEnabled(false);
        player.ClearAirWalkLocks();
        player.transform.position = target.position;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = target.position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        player.SetInputEnabled(inputWasEnabled);

        context.PlayerSpawnPoint = target;
        context.LevelManager?.SetSpawnPoint(target);

        CameraTargetFollow follow = player.GetComponentInChildren<CameraTargetFollow>();
        if (follow == null)
            follow = Object.FindAnyObjectByType<CameraTargetFollow>();
        follow?.FollowCurrentRegion();

        // 回流不是重新结算区域：仅切换查询指针，避免 EnterZone 冻结尚未结算的离开区。
        // 正常 A→B 保留原有离区快照语义；已存在的 zone 快照两种路径都不修改。
        if (returning)
            TarotZoneQuery.ResetCurrent();
        TarotZoneQuery.EnterZone(zone);
        CollectibleTracker.Active?.NotifyRegionChanged();
        return true;
    }

    public void ConfigureDestination(Transform dest)
    {
        destination = dest;
        Configure("TRANS-A-B", InteractPrompt, 1.4f);
    }
}
