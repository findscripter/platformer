#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// 编辑器实测：用角色物理（moveX + 跳跃缓冲）走场景 A/B 主线，不传送。
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class MainRouteWalker : MonoBehaviour
{
    public string Status = "idle";
    public bool Finished;
    public bool Failed;

    private static readonly Vector2[] SceneAMain =
    {
        new Vector2(0.90f, 2.65f),
        new Vector2(4.10f, 2.65f),
        new Vector2(5.20f, 3.30f),
        new Vector2(6.30f, 2.30f),
        new Vector2(8.20f, 2.30f),
        new Vector2(9.90f, 2.90f),
        new Vector2(11.40f, 3.80f),
        new Vector2(12.00f, 4.10f),
        new Vector2(13.70f, 3.50f),
        new Vector2(15.85f, 5.65f),
        new Vector2(17.70f, 6.55f),
        new Vector2(20.20f, 6.55f),
        new Vector2(21.35f, 4.85f),
        new Vector2(23.00f, 4.85f),
        new Vector2(25.00f, 3.55f),
        new Vector2(27.50f, 3.00f),
        new Vector2(29.70f, 3.90f)
    };

    private static readonly Vector2[] SceneBMain =
    {
        new Vector2(34.90f, 2.65f),
        new Vector2(37.70f, 2.65f),
        new Vector2(38.80f, 3.45f),
        new Vector2(41.00f, 4.25f),
        new Vector2(43.30f, 5.25f),
        new Vector2(44.50f, 5.45f),
        new Vector2(46.00f, 6.00f),
        new Vector2(47.40f, 5.80f),
        new Vector2(49.90f, 5.05f),
        new Vector2(52.10f, 3.25f),
        new Vector2(52.40f, 3.25f),
        new Vector2(54.50f, 4.15f),
        new Vector2(56.80f, 5.15f),
        new Vector2(58.90f, 6.15f),
        new Vector2(60.70f, 3.00f),
        new Vector2(61.20f, 3.00f),
        new Vector2(62.50f, 3.85f),
        new Vector2(63.70f, 4.70f),
        new Vector2(64.90f, 5.55f),
        new Vector2(66.30f, 6.50f)
    };

    private PlayerController player;
    private FieldInfo moveXField;
    private FieldInfo jumpBufferField;
    private Vector2[] route = SceneAMain;
    private int phase;
    private int index;
    private float stuckTime;
    private float jumpCooldown;
    private float attackCooldown;
    private Vector3 lastPos;
    private readonly StringBuilder log = new StringBuilder();
    private int deaths;
    private bool wasDead;
    private Coroutine routine;

    public static MainRouteWalker Ensure()
    {
        MainRouteWalker existing = FindFirstObjectByType<MainRouteWalker>();
        if (existing != null)
            return existing;
        GameObject go = new GameObject("MainRouteWalker");
        return go.AddComponent<MainRouteWalker>();
    }

    public void BeginSceneA(PlayerController target)
    {
        player = target;
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        moveXField = typeof(PlayerController).GetField("moveX", flags);
        jumpBufferField = typeof(PlayerController).GetField("jumpBufferTimeRemaining", flags);

        route = SceneAMain;
        phase = 0;
        index = 0;
        stuckTime = 0f;
        deaths = 0;
        wasDead = false;
        Finished = false;
        Failed = false;
        log.Length = 0;
        lastPos = player != null ? player.transform.position : Vector3.zero;
        Status = "A 0/" + route.Length;
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(Watchdog());
    }

    private IEnumerator Watchdog()
    {
        float timeout = Time.time + 240f;
        while (!Finished && Time.time < timeout)
            yield return null;

        if (!Finished)
        {
            Failed = true;
            Status = "timeout p" + phase + " wp" + index + " deaths=" + deaths + " " + log;
            Finished = true;
        }
    }

    private void LateUpdate()
    {
        if (Finished || player == null)
            return;

        GameContext ctx = GameLoop.Instance != null ? GameLoop.Instance.Context : null;
        if (ctx == null)
            return;
        GameStateType currentState = ctx.StateMachine.CurrentStateType;
        if (currentState == GameStateType.LevelClear || currentState == GameStateType.EchoSpace || currentState == GameStateType.Result)
        {
            Finished = true;
            Failed = ctx.TarotResult != null && ctx.TarotResult.IsComplete() && !ctx.TarotResult.AllNodesActivated;
            Status = "finished state=" + currentState + " deaths=" + deaths + " " + log;
            return;
        }
        if (phase == 0 && player.transform.position.x >= 33f)
        {
            route = SceneBMain;
            phase = 1;
            index = 0;
            stuckTime = 0f;
            log.Append("ENTER_B ");
        }
        // T3 unlocks an upper route over B08. If a jump lands there, use its right exit
        // to B09 rather than waiting forever above a solid platform for a downward fall.
        if (phase == 1 && index == 8 && player.IsGrounded && player.transform.position.y > 6.5f)
        {
            index = 10;
            log.Append("T3_UPPER_EXIT ");
        }
        bool dead = player.IsDead || (ctx != null && ctx.IsPlayerDead);
        if (dead)
        {
            if (!wasDead)
            {
                deaths++;
                log.Append("DIE@p").Append(phase).Append("wp").Append(index)
                    .Append(" ").Append(player.transform.position.ToString("F2")).Append(" | ");
                Status = "dead p" + phase + " wp" + index + " n=" + deaths;
                if (deaths >= 10)
                {
                    Failed = true;
                    Finished = true;
                    Status = "too many deaths n=" + deaths + " " + log;
                }
            }

            wasDead = true;
            return;
        }

        if (wasDead)
        {
            wasDead = false;
            stuckTime = 0f;
            lastPos = player.transform.position;
            // Resume at the actual checkpoint, not at an old waypoint across a gap.
            index = 0;
            for (int i = 0; i < route.Length; i++)
                if (route[i].x <= player.transform.position.x + 0.2f) index = i;
        }

        if (currentState != GameStateType.Playing)
            return;

        if (index >= route.Length)
        {
            AdvancePhase(ctx);
            return;
        }

        Vector2 pos = player.transform.position;
        Vector2 goal = route[index];
        float dx = goal.x - pos.x;
        float dy = goal.y - pos.y;
        jumpCooldown = Mathf.Max(0f, jumpCooldown - Time.deltaTime);
        attackCooldown = Mathf.Max(0f, attackCooldown - Time.deltaTime);

        if (player.IsGrounded && Mathf.Abs(dx) < 0.45f && Mathf.Abs(pos.y - goal.y) < 0.6f)
        {
            log.Append("OK").Append(phase).Append(".").Append(index).Append("@").Append(pos.ToString("F2")).Append(" ");
            TryInteractNearby(ctx);
            index++;
            stuckTime = 0f;
            Status = (phase == 0 ? "A " : "B ") + index + "/" + route.Length + " deaths=" + deaths;
            if (index >= route.Length)
                AdvancePhase(ctx);
            return;
        }

        bool wantDrop = pos.y > goal.y + 1.15f;
        MovingPlatform lift = wantDrop ? null : MovingPlatformAhead(pos, dx);
        bool waitingForLift = false;
        bool onLift = false;
        bool boardLift = false;
        if (lift != null && dy > 1.05f)
        {
            float liftY = lift.transform.position.y;
            onLift = player.IsGrounded && Mathf.Abs(pos.x - lift.transform.position.x) < 1.4f
                     && pos.y > liftY - 0.35f && pos.y < liftY + 1.8f;
            if (onLift)
            {
                waitingForLift = liftY < goal.y - 1.2f;
            }
            else
            {
                waitingForLift = true;
                boardLift = Mathf.Abs(liftY - pos.y) < 1.2f;
            }
        }

        if (moveXField != null)
        {
            float move = 0f;
            if (waitingForLift && lift != null && !onLift && boardLift)
            {
                float lx = lift.transform.position.x - pos.x;
                move = Mathf.Abs(lx) >= 0.12f ? Mathf.Sign(lx) : 0f;
            }
            else if (!waitingForLift && Mathf.Abs(dx) >= 0.12f)
                move = Mathf.Sign(dx);
            moveXField.SetValue(player, move);
        }

        bool gapJump = !wantDrop && !waitingForLift && dy > -0.7f && GapAhead(pos, dx);
        bool threatAhead = !wantDrop && (SpikeAhead(pos, dx) || EnemyAhead(pos, dx, 1.8f, 1.2f) || gapJump);
        bool runUpJump = !wantDrop && !waitingForLift && player.IsGrounded && dy > 0.35f && Mathf.Abs(dx) < 2.2f;
        bool needJump = boardLift || (!waitingForLift && !wantDrop && (runUpJump || threatAhead || (stuckTime > 0.4f && player.IsGrounded)));
        if (needJump && jumpCooldown <= 0f && jumpBufferField != null)
        {
            jumpBufferField.SetValue(player, 0.18f);
            jumpCooldown = 0.22f;
        }

        if (player.IsGrounded && attackCooldown <= 0f && EnemyAhead(pos, dx, 0.85f, 0.9f))
        {
            player.TryAttack();
            attackCooldown = 0.35f;
        }

        if (waitingForLift)
            stuckTime = 0f;
        else if (Vector2.Distance(pos, lastPos) < 0.05f * Mathf.Max(0.0001f, Time.deltaTime))
            stuckTime += Time.deltaTime;
        else
            stuckTime = 0f;
        lastPos = pos;
        Status = (phase == 0 ? "A " : "B ") + index + "/" + route.Length + " deaths=" + deaths + " target=" + goal.ToString("F2");

        if (stuckTime > 6f)
        {
            log.Append("STUCK").Append(phase).Append(".").Append(index).Append("@")
                .Append(pos.ToString("F2")).Append(" want=").Append(goal.ToString("F2")).Append(" ");
            Failed = true;
            Status = "stuck p" + phase + " wp" + index + " " + pos.ToString("F2");
            Finished = true;
        }
    }

    private void AdvancePhase(GameContext ctx)
    {
        TryInteractNearby(ctx);
        if (phase == 0)
        {
            if (player.transform.position.x >= 33f)
            {
                route = SceneBMain;
                phase = 1;
                index = 0;
                stuckTime = 0f;
                Status = "B 0/" + route.Length + " deaths=" + deaths;
                log.Append("ENTER_B ");
                return;
            }

            Status = "A done waiting TRANS deaths=" + deaths;
            return;
        }

        Status = "B route reached; waiting for END state. deaths=" + deaths;
    }

    private static bool SpikeAhead(Vector2 pos, float dx)
    {
        return HazardAhead<SpikeTrap>(pos, dx, 1.6f, 1.1f);
    }

    private static bool EnemyAhead(Vector2 pos, float dx, float range, float ySlop)
    {
        return HazardAhead<PatrolEnemy>(pos, dx, range, ySlop);
    }

    private static bool GapAhead(Vector2 pos, float dx)
    {
        if (Mathf.Abs(dx) < 0.05f)
            return false;
        float dir = Mathf.Sign(dx);
        Vector2 origin = pos + new Vector2(dir * 0.55f, 0.25f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 1.5f, LayerMask.GetMask("Ground"));
        return hit.collider == null;
    }

    private static MovingPlatform MovingPlatformAhead(Vector2 pos, float dx)
    {
        if (Mathf.Abs(dx) < 0.05f)
            return null;
        float dir = Mathf.Sign(dx);
        MovingPlatform[] items = FindObjectsByType<MovingPlatform>(FindObjectsInactive.Exclude);
        MovingPlatform best = null;
        float bestSx = 99f;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;
            Vector2 s = items[i].transform.position;
            float sx = (s.x - pos.x) * dir;
            if (sx > -0.4f && sx < 3.2f && sx < bestSx)
            {
                bestSx = sx;
                best = items[i];
            }
        }

        return best;
    }

    private static bool HazardAhead<T>(Vector2 pos, float dx, float range, float ySlop) where T : Component
    {
        if (Mathf.Abs(dx) < 0.05f)
            return false;

        float dir = Mathf.Sign(dx);
        T[] items = FindObjectsByType<T>(FindObjectsInactive.Exclude);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;
            Vector2 s = items[i].transform.position;
            float sx = (s.x - pos.x) * dir;
            if (sx > 0.12f && sx < range && Mathf.Abs(s.y - pos.y) < ySlop)
                return true;
        }

        return false;
    }

    private static void TryInteractNearby(GameContext ctx)
    {
        if (ctx == null || ctx.Player == null)
            return;

        Vector2 p = ctx.Player.transform.position;
        TarotNodeInteractable[] nodes = FindObjectsByType<TarotNodeInteractable>(FindObjectsInactive.Exclude);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (Vector2.Distance(nodes[i].transform.position, p) < 1.7f && nodes[i].CanInteract)
                nodes[i].Interact(ctx);
        }

        CheckpointInteractable[] cps = FindObjectsByType<CheckpointInteractable>(FindObjectsInactive.Exclude);
        for (int i = 0; i < cps.Length; i++)
        {
            if (Vector2.Distance(cps[i].transform.position, p) < 1.7f)
                cps[i].Interact(ctx);
        }

        RegionGate[] gates = FindObjectsByType<RegionGate>(FindObjectsInactive.Exclude);
        for (int i = 0; i < gates.Length; i++)
        {
            if (Vector2.Distance(gates[i].transform.position, p) < 1.8f)
                gates[i].Interact(ctx);
        }
    }
}
#endif
