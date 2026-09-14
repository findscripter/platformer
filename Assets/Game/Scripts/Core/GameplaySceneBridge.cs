using UnityEngine;

public class GameplaySceneBridge : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Collider2D goalTrigger;

    // A/B 两区各一个生成器实例；坐标范围不重叠（B 区起点 = A 区终点 + 间隙）。
    [SerializeField] private ProceduralLevelGenerator sceneAGenerator;
    [SerializeField] private ProceduralLevelGenerator sceneBGenerator;
    [SerializeField] private float regionGap = 6f;

    public void Apply(GameLoop gameLoop)
    {
        CollectibleTracker.Ensure(transform).ResetRun();
        DreamremainsLevelBootstrap.Build(this);

        if (gameLoop == null)
            return;

        gameLoop.BindGameplay(player, spawnPoint, goalTrigger);
    }

    /// <summary>
    /// 每次进入 Gameplay 都重新随机生成：先建 A 区取得它的终点 X，
    /// 再把 B 区出生点摆到 A 区终点右侧 regionGap 处生成，保证两区坐标不重叠。
    /// 生成顺序体现依赖：先有地面（Generate 内部平台先行），再连区域，最后接终点判定。
    /// </summary>
    private void BuildProceduralLevel()
    {
        LevelRegionRegistry.Clear();
        CollectibleTracker.Ensure(transform).ResetRun();

        if (sceneAGenerator == null || sceneBGenerator == null)
        {
            Debug.LogError("GameplaySceneBridge: 缺少 sceneAGenerator/sceneBGenerator 引用，无法生成关卡。");
            return;
        }

        sceneAGenerator.Generate();

        Transform bSpawn = sceneBGenerator.GetSpawnPoint();
        if (bSpawn != null)
            bSpawn.position = new Vector3(sceneAGenerator.RegionMaxX + regionGap, bSpawn.position.y, 0f);
              sceneBGenerator.Generate();

        // 两区都生成完毕、边界已知后才能定出背景切换分界线（间隙中点）；
 // 提前调用会拿到上一次或默认的 RegionMaxX，导致背景在错误的位置切换。
        float boundaryX = (sceneAGenerator.RegionMaxX + sceneBGenerator.RegionMinX) * 0.5f;
        sceneAGenerator.SpawnBackdrop(boundaryX);
        sceneBGenerator.SpawnBackdrop(boundaryX);

        WireRegionGate();
        WireFinalGoal();

        Transform spawnA = sceneAGenerator.GetSpawnPoint();
        if (player != null)
        {
            int playerLayer = GameLayers.PlayerLayer;
            if (playerLayer >= 0)
                SetLayerRecursively(player.gameObject, playerLayer);
            if (spawnA != null)
            {
                Vector3 spawnPos = spawnA.position + Vector3.up * 0.6f;
                player.transform.position = spawnPos;
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.position = spawnPos;
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                }
            }
        }

        CameraTargetFollow follow = FindFirstObjectByType<CameraTargetFollow>();
        if (follow != null)
        {
            follow.SetRoom(sceneAGenerator.RegionMinX, sceneAGenerator.RegionMaxX, 0f, 16f);
            if (player != null)
                follow.player = player.transform;
        }

        if (spawnA != null)
            spawnPoint = spawnA;
    }

    /// <summary>A 区终点挂 RegionGate，目标指向 B 区出生点。</summary>
    private void WireRegionGate()
    {
        Transform aGoal = sceneAGenerator.GetGoalPoint();
        Transform bSpawn = sceneBGenerator.GetSpawnPoint();
        if (aGoal == null || bSpawn == null)
            return;

        RegionGate gate = aGoal.GetComponent<RegionGate>();
        if (gate == null)
        {
            if (aGoal.GetComponent<CircleCollider2D>() == null)
                aGoal.gameObject.AddComponent<CircleCollider2D>();
            gate = aGoal.gameObject.AddComponent<RegionGate>();
        }
        gate.ConfigureDestination(bSpawn);
    }

    /// <summary>B 区终点是整局的最终目标：挂判定用的 BoxCollider2D trigger 和回流交互。</summary>
    private void WireFinalGoal()
    {
        Transform bGoal = sceneBGenerator.GetGoalPoint();
        if (bGoal == null)
            return;

        BoxCollider2D box = bGoal.GetComponent<BoxCollider2D>();
        if (box == null)
            box = bGoal.gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.2f, 2.8f);
        box.offset = new Vector2(0f, 0.2f);
        int triggerLayer = GameLayers.TriggerLayer;
        if (triggerLayer >= 0)
            bGoal.gameObject.layer = triggerLayer;

        if (bGoal.GetComponent<TarotReturnInteractable>() == null)
            bGoal.gameObject.AddComponent<TarotReturnInteractable>();

        goalTrigger = box;
    }

    public void BindOfficial(PlayerController gameplayPlayer, Transform officialSpawn, Collider2D officialGoal)
    {
        if (gameplayPlayer != null)
            player = gameplayPlayer;
        if (officialSpawn != null)
            spawnPoint = officialSpawn;
        if (officialGoal != null)
            goalTrigger = officialGoal;
    }

    private void Reset()
    {
        player = FindPlayer();
        spawnPoint = FindTransform("SceneRoot/GameplayLayer/SpawnPoint");
        goalTrigger = FindCollider("SceneRoot/GameplayLayer/Runtime_Exits/GoalTrigger");
    }

    private PlayerController FindPlayer()
    {

        var testPlayer = GameObject.Find("test_player");
        if (testPlayer != null && testPlayer.activeInHierarchy)
            return testPlayer.GetComponent<PlayerController>();

        var playerObject = GameObject.Find("Runtime_Player");
        return playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
    }

    private Transform FindTransform(string path)
    {
        var segments = path.Split('/');
        Transform current = null;

        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == segments[0])
            {
                current = root.transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (var index = 1; index < segments.Length; index++)
        {
            current = current.Find(segments[index]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private Collider2D FindCollider(string path)
    {
        var transform = FindTransform(path);
        return transform != null ? transform.GetComponent<Collider2D>() : null;
    }

    private static void SetLayerRecursively(GameObject node, int layer)
    {
        node.layer = layer;
        Transform t = node.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }
}
