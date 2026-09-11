using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 程序化关卡生成器（简单模式）。生成带正式美术、碰撞体和玩法脚本的完整关卡。
///
/// 生成顺序体现依赖关系：先定平台（地面），再在平台之上放敌人/道具/机关，
/// 任何需要落地的对象都从所属平台取坐标，避免悬空。
///
/// 间距依据玩家实测能力：移动 4、跳跃力 8、重力 -9.81
///   → 单跳水平约 6.5、爬升约 3.26
/// 取 3.5–5.5（约 60%–85% 极限）保证可跳且不无脑。
/// </summary>
public partial class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("生成参数")]
    [SerializeField] private int platformCount = 25;
    [SerializeField] private Vector2 horizontalGapRange = new Vector2(3.5f, 5.5f);
    [SerializeField] private Vector2 verticalOffsetRange = new Vector2(-1.4f, 1.6f);
    [SerializeField] private Vector2 platformSizeRange = new Vector2(2.0f, 5f);
    // 相机房间高度是 0–8；纯累加会让高度漂出房间，夹紧又会全贴天花板。
    // 用「随机步进 + 向中线回拉」在房间内起伏。
    [SerializeField] private float minY = 1.5f;
    [SerializeField] private float maxY = 6.5f;

    [Header("对象密度")]
    [SerializeField, Range(0f, 1f)] private float enemySpawnChance = 0.3f;
    [SerializeField, Range(0f, 1f)] private float collectibleSpawnChance = 0.18f;
    [SerializeField] private int savePointInterval = 12;
    [SerializeField] private int tarotPointCount = 4;

    [Header("机关")]
    [SerializeField, Range(0f, 1f)] private float movingPlatformChance = 0.12f;
    [SerializeField, Range(0f, 1f)] private float fadingPlatformChance = 0.1f;

    [Header("背景/装饰")]
    // A 区用 BG01.jpg（荷花/远山/光柱主题），B 区用 S02_BG.jpg（芦苇/云海/月亮主题）；
    // 具体由 isSceneA 决定，见 ResolveBackdropPath()。
    [SerializeField, Range(0f, 1f)] private float smallDecorChance = 0.75f;
    [SerializeField, Range(0f, 1f)] private float bigDecorChance = 0.7f;

    [Header("区域 / Zone")]
    // 每隔多少个平台切一段 Zone，和旧 DreamremainsLevelData 的 RoomSpec 语义一致：
    // 塔罗效果按“当前所在 Zone”结算，没有真正的 ZoneTrigger 就只会退化成全局判定。
    [SerializeField, Min(2)] private int platformsPerZone = 8;
    [SerializeField] private int zoneIndexBase = 0;
 // 决定 slot 0/1 (T1/T2) 还是 slot 2/3 (T3/T4) 落在这个实例里；
    // 也是 LevelRegionRegistry 里 A/B 两条记录的键。
    [SerializeField] private bool isSceneA = true;

    [Header("锚点")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform goalPoint;

    public float RegionMinX { get; private set; }
    public float RegionMaxX { get; private set; }
    public int ZoneCount { get; private set; }

    public Transform GetSpawnPoint() => spawnPoint;
    public Transform GetGoalPoint() => goalPoint;

    private const float PlatformHeight = 0.5f;
    private const string LongPlatformArt = "Assets/Game/Art/Scenes/S01/13.png";
    private const string ShortPlatformArt = "Assets/Game/Art/Scenes/S01/10.png";
    private const string CrackedPlatformArt = "Assets/Game/Art/Scenes/S01/12.png";
    private const string EnemyArt = "Assets/Game/Art/Characters/monster/walk/1.png";
    private const string SceneABackdrop = "Assets/Game/Art/source/BG01.jpg";
    private const string SceneBBackdrop = "Assets/Game/Art/Scenes/S02/S02_BG.jpg";

    private readonly List<Platform> platforms = new List<Platform>();
    private int enemyLayer, groundLayer, triggerLayer, collectibleLayer, mechanismLayer;
    private System.Random decorRandom;

    private struct Platform
    {
        public Vector2 center;
        public float width;
      public float top;      // 站立面高度
    }

    private readonly struct DecorEntry
    {
    public readonly string File;
        public readonly float Height;
        public readonly float Alpha;
      public readonly bool Flip;
     public readonly float CropFraction;

        public DecorEntry(string file, float height, float alpha, bool flip = false, float cropFraction = 1f)
    {
          File = file;
            Height = height;
  Alpha = alpha;
       Flip = flip;
   CropFraction = cropFraction;
        }
    }

    // 小型重复散布：贴近地面/水面的近景小品，逐平台按概率撒点。
    private static readonly DecorEntry[] SceneASmallDecor =
    {
        new DecorEntry("S01/17.png", 1.2f, 0.55f),
        new DecorEntry("S01/23.png", 0.55f, 0.85f),
        new DecorEntry("S01/7.png", 0.24f, 0.3f, false, 0.5f),
    };

private static readonly DecorEntry[] SceneBSmallDecor =
    {
        new DecorEntry("S02/11.png", 0.8f, 0.35f, true, 0.75f),
        new DecorEntry("S02/12.png", 0.7f, 0.34f),
        new DecorEntry("S02/7.png", 0.6f, 0.23f),
    };

    // 大型远景剪影：山丘/斜坡/云海/月亮等，低频出现，摆在画面偏高处制造纵深。
    private static readonly DecorEntry[] SceneABigDecor =
    {
      new DecorEntry("S01/15.png", 1.8f, 0.9f),
        new DecorEntry("S01/19.png", 1.1f, 0.9f),
        new DecorEntry("S01/8.png", 2.0f, 0.5f),
        new DecorEntry("S01/21.png", 4.0f, 0.85f),
 };

    private static readonly DecorEntry[] SceneBBigDecor =
    {
        new DecorEntry("S02/22.png", 4.0f, 0.85f),
        new DecorEntry("S02/21.png", 1.2f, 0.9f),
        new DecorEntry("S02/3.png", 0.6f, 0.9f),
        new DecorEntry("S02/5.png", 0.85f, 0.9f),
    };

    public void Generate()
    {
  Clear();
     CacheLayers();
decorRandom = new System.Random(Random.Range(int.MinValue, int.MaxValue));

        Vector2 cursor = spawnPoint != null ? (Vector2)spawnPoint.position : Vector2.zero;
   cursor.y = isSceneA ? 3.2f : 3.55f;
      RegionMinX = cursor.x;

    // 起始台按 cursor.y 摆放，spawnPoint 本身的 y 也要跟着夹紧后的值走，
     // 否则 GameplaySceneBridge 用 spawnPoint.position 摆玩家时会悬空。
        if (spawnPoint != null)
     spawnPoint.position = new Vector3(cursor.x, cursor.y, spawnPoint.position.z);

        Transform terrain = Category("Terrain");
        Transform enemiesGroup = Category("Enemies");
  Transform itemsGroup = Category("Items");
 Transform tarotGroup = Category("TarotMechanisms");
   Transform savePoints = Category("SavePoints");
        Transform cameraRooms = Category("CameraRooms");
Transform decorGroup = Category("Decorations");
        Transform hazards = Category("Hazards");

// 出生点正下方必须有一块起始台，否则玩家落地瞬间脚下空空会直接掉死。
        BuildPlatform(terrain, -1, cursor, 5.4f, true);

      float[] stepY = { 0.55f, 0.4f, -0.3f, 0f, -0.5f, 0.65f, 0.15f, -0.2f };
      float[] stepX = { 4.05f, 4.15f, 3.95f, 4.3f, 4.1f, 4.0f, 4.2f, 4.35f };
      float[] widths = { 3.7f, 2.9f, 3.4f, 4.3f, 3.2f, 2.8f, 3.8f, 4.1f };
      for (int i = 0; i < platformCount; i++)
        {
        int phrase = i % 8;
       float w = widths[phrase];
   cursor.x += stepX[phrase];

        bool isLast = i == platformCount - 1;
     float offsetY = stepY[phrase];
        if (i >= platformCount - 3)
            offsetY = Mathf.Max(0.2f, Mathf.Abs(offsetY) * 0.45f);
        if (isLast)
            w = Mathf.Max(w, 4.8f);
    cursor.y = Mathf.Clamp(cursor.y + offsetY, minY, maxY);

 BuildPlatform(terrain, i, cursor, w, isLast);

   // 最后一个平台只做终点，不再叠玩法对象。
            if (isLast)
    break;

          if (i > 0)
            {
       MaybeSpawnEnemy(enemiesGroup, cursor, w, i);
        if (i < platformCount - 4)
   MaybeSpawnCollectible(itemsGroup, cursor, w);
      }
    }

        RegionMaxX = platforms.Count > 0
            ? platforms[platforms.Count - 1].center.x + platforms[platforms.Count - 1].width * 0.5f + 12f
            : RegionMinX;
        SpawnZoneTriggers(cameraRooms);
        SpawnCheckpoints(savePoints);
        SpawnTarotPoints(tarotGroup);
        ComposeDesignedScenery(decorGroup);
        EnsureMinimumCollectibles(itemsGroup);
        PlaceGoal();
        SpawnTarotCarriers(terrain, hazards, tarotGroup, itemsGroup);

        LevelRegionRegistry.Register(isSceneA,
            new LevelRegionInfo(RegionMinX, RegionMaxX, zoneIndexBase, ZoneCount, platformsPerZone));
    }

    /// <summary>
  /// 挂载整屏底图，等比覆盖当前相机视口（复用 LevelDecorationBackdrop）。
    /// boundaryX 由 GameplaySceneBridge 在 A/B 两区都随机生成完毕后算出
    /// （两区间隙的中点），用于两张底图之间的可见性切换，不依赖旧关卡固定的 32f。
    /// 必须在 Generate() 之后调用；Generate() 内部的 Clear() 会连带清掉上一次的底图，
    /// 所以每次重新进入关卡都会用这次的新边界重建。
    /// </summary>
 public void SpawnBackdrop(float boundaryX)
    {
        string path = isSceneA ? SceneABackdrop : SceneBBackdrop;
        Transform bgGroup = Category("Background");
    DreamremainsLevelBootstrap.SpawnBackdrop(bgGroup, RegionMinX, RegionMaxX, path, isSceneA, boundaryX);
    }

    // ---- 平台 ----

    private void BuildPlatform(Transform parent, int index, Vector2 center, float width, bool isGoalPlatform)
    {
        Vector3 boxCenter = new Vector3(center.x, center.y - PlatformHeight * 0.5f, 0f);
      Color tint = Color.white;

        string art = width >= 4f ? LongPlatformArt : ShortPlatformArt;
        if (index % 5 == 2)
            art = CrackedPlatformArt;

  Sprite sprite = DreamremainsLevelBootstrap.LoadArt(art);
        GameObject platform = DreamremainsLevelBootstrap.CreateBox(
            parent, $"PL-{index:D2}", boxCenter, new Vector2(width, PlatformHeight), tint, groundLayer, sprite);

        // 正式表现层：按碰撞面宽度拉伸贴图，而不是留占位色块。
        DreamremainsLevelBootstrap.AttachSurfaceVisual(platform, sprite, false, tint);

        // 机关平台：移动 / 消失
        if (!isGoalPlatform && Random.value < movingPlatformChance)
        {
       Rigidbody2D body = platform.AddComponent<Rigidbody2D>();
      body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
          body.freezeRotation = true;
   MovingPlatform mover = platform.AddComponent<MovingPlatform>();
            Vector2 a = center;
            Vector2 b = center + Vector2.up * Random.Range(1.2f, 2.2f);
       mover.Configure(a, b, Random.Range(1.2f, 1.8f));
        }
        else if (!isGoalPlatform && (index == 7 || index == 15 || Random.value < fadingPlatformChance))
        {
            Rigidbody2D body = platform.AddComponent<Rigidbody2D>();
    body.bodyType = RigidbodyType2D.Kinematic;
       body.gravityScale = 0f;
            body.freezeRotation = true;
            platform.AddComponent<FadingPlatform>().Configure(ZoneAt(index));
        }

   platforms.Add(new Platform { center = center, width = width, top = center.y });
    }

    // ---- 玩法对象 ----

    private void MaybeSpawnEnemy(Transform parent, Vector2 platformCenter, float platformWidth, int index)
    {
        if (Random.value >= enemySpawnChance || platformWidth < 3f)
         return;

   // 巡逻范围收在平台内，避免走到边缘掉下去。
        float half = Mathf.Max(0.8f, platformWidth * 0.5f - 1f);
  Vector2 center = platformCenter + Vector2.up * 0.7f;

        GameObject node = new GameObject($"EN-{index:D2}");
    node.transform.SetParent(parent);
        node.transform.position = center;
    node.layer = enemyLayer >= 0 ? enemyLayer : 0;

     Rigidbody2D body = node.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
  body.freezeRotation = true;

      CircleCollider2D circle = node.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
    circle.radius = 0.4f;
        node.transform.localScale = Vector3.one * 0.7f;

Sprite enemySprite = DreamremainsLevelBootstrap.LoadArt(EnemyArt);
        SpriteRenderer visual = DreamremainsLevelBootstrap.AddVisual(node.transform, "Visual", enemySprite,
    enemySprite != null ? (Vector2)enemySprite.bounds.size * 0.126f : Vector2.one,
            Vector2.zero, GameLayers.InteractiveUpSorting, 5);
        if (visual != null)
      {
      // 怪物帧以脚底为 pivot：下移一个碰撞半径让脚落在平台面。
    visual.transform.localPosition = Vector3.down * circle.radius;
          Animator animator = visual.gameObject.AddComponent<Animator>();
       animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Enemy/PatrolEnemy");
   animator.applyRootMotion = false;
        }

        PatrolEnemy enemy = node.AddComponent<PatrolEnemy>();
      enemy.ConfigurePatrol(new Vector2(-half, 0f), new Vector2(half, 0f));
    }

    private void MaybeSpawnCollectible(Transform parent, Vector2 platformCenter, float platformWidth)
    {
    if (Random.value >= collectibleSpawnChance)
          return;

        if (platforms.Count >= 2)
        {
            float drop = platforms[platforms.Count - 2].center.y - platformCenter.y;
            if (drop > 1.2f)
                return;
        }

        SpawnCollectible(parent, platformCenter);
    }

    /// <summary>
    /// A 区传送门要收齐梦核碎片。随机可能一个都不出，这里在主路上补到至少 2 个，
    /// 并且不放在大落差平台上，避免捡完回不到门。
    /// </summary>
    private void EnsureMinimumCollectibles(Transform parent)
    {
        if (!isSceneA || parent == null)
            return;

        const int minCount = 2;
        int have = CollectibleTracker.Active != null ? CollectibleTracker.Active.AreaASpawned : 0;
        if (have >= minCount || platforms.Count < 6)
            return;

        for (int i = 2; i < platforms.Count - 4 && have < minCount; i++)
        {
            Platform p = platforms[i];
            float drop = platforms[i - 1].center.y - p.center.y;
            if (drop > 1.2f)
                continue;

            SpawnCollectible(parent, p.center);
            have++;
        }

        for (int i = 2; i < platforms.Count - 4 && have < minCount; i++)
        {
            SpawnCollectible(parent, platforms[i].center);
            have++;
        }
    }

    private void SpawnCollectible(Transform parent, Vector2 platformCenter)
    {
        Vector2 pos = platformCenter + Vector2.up * 1.05f;
        GameObject node = new GameObject("IT-" + Mathf.RoundToInt(pos.x));
        node.transform.SetParent(parent);
        node.transform.position = pos;
        node.layer = collectibleLayer >= 0 ? collectibleLayer : 0;

        CircleCollider2D circle = node.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.7f;

        DreamremainsLevelBootstrap.AddDreamCore(node.transform, 0.42f, Vector2.zero);
        node.AddComponent<CollectibleItem>().ConfigureRegion(isSceneA);
    }

        /// <summary>
    /// A 区负责 slot 0/1（T1/T2），B 区负责 slot 2/3（T3/T4）——按塔罗系统的固定语义
    /// 分配，不能让两个实例都生成 4 个，否则静态注册表会被互相覆盖。
    /// </summary>
    private void SpawnTarotPoints(Transform parent)
    {
        int slotStart = isSceneA ? 0 : 2;
        int countHere = Mathf.Clamp(isSceneA ? tarotPointCount / 2 + tarotPointCount % 2 : tarotPointCount / 2, 0, 2);
        if (platforms.Count < countHere + 2 || countHere <= 0)
         return;

        int step = Mathf.Max(1, platforms.Count / (countHere + 1));
        for (int i = 1; i <= countHere; i++)
    {
       int slot = slotStart + i - 1;
         int idx = Mathf.Clamp(i * step, 1, platforms.Count - 2);
       Vector2 pos = platforms[idx].center + Vector2.up * 1.5f;

      GameObject node = new GameObject("T" + (slot + 1));
            node.transform.SetParent(parent);
    node.transform.position = pos;
            node.layer = triggerLayer >= 0 ? triggerLayer : 0;

  TarotNodeInteractable tarot = node.AddComponent<TarotNodeInteractable>();
            tarot.ConfigureSlot(slot);
            tarot.Configure("T" + (slot + 1), tarot.InteractPrompt, 2.0f);

            // 金色菱形标记，与检查点/机关区分开。
          SpriteRenderer mark = DreamremainsLevelBootstrap.AddVisual(node.transform, "TarotMark",
WhiteSprite(), new Vector2(0.34f, 0.34f), new Vector2(0f, 0.22f),
      GameLayers.InteractiveDownSorting, 13);
         if (mark != null)
          {
       mark.color = new Color(0.92f, 0.78f, 0.35f, 0.95f);
          mark.transform.Rotate(0f, 0f, 45f);
   }
        }
    }

    /// <summary>按关卡长度均分检查点。</summary>
    private void SpawnCheckpoints(Transform parent)
    {
        if (platforms.Count < savePointInterval)
    return;

        for (int i = savePointInterval; i < platforms.Count - 1; i += savePointInterval)
        {
      Vector2 pos = platforms[i].center + Vector2.up * 1.2f;
          GameObject node = new GameObject("CP-" + i);
  node.transform.SetParent(parent);
            node.transform.position = pos;
            node.layer = triggerLayer >= 0 ? triggerLayer : 0;

   node.AddComponent<CheckpointInteractable>().Configure("CP-" + i, "记录梦核", 1.8f);

        DreamremainsLevelBootstrap.AddArt(node.transform, "DreamShrine",
 "Assets/Game/Art/Scenes/S01/25.png", 0.38f, new Vector2(0f, -0.24f));
    SpriteRenderer core = DreamremainsLevelBootstrap.AddDreamCore(node.transform, 0.32f, new Vector2(0f, 0.14f));
            if (core != null)
          core.sortingLayerName = GameLayers.InteractiveDownSorting;
        }
    }

    private void PlaceGoal()
  {
  if (platforms.Count == 0)
            return;

        Platform last = platforms[platforms.Count - 1];
        if (goalPoint != null)
        {
            goalPoint.position = new Vector3(last.center.x, last.top + 0.95f, 0f);
            if (goalPoint.Find("Passage") == null)
                DreamremainsLevelBootstrap.AddArt(goalPoint, "Passage", "Assets/Game/Art/Scenes/S01/5.png", 1.1f, Vector2.zero);
        }
    }

    // ---- 基础设施 ----

    private Transform Category(string name)
  {
        Transform existing = transform.Find(name);
        if (existing != null)
   return existing;

        var group = new GameObject(name);
        group.transform.SetParent(transform, false);
        return group.transform;
    }

    private void CacheLayers()
    {
        groundLayer = LayerMask.NameToLayer(GameLayers.Ground);
        enemyLayer = LayerMask.NameToLayer(GameLayers.Enemy);
        triggerLayer = LayerMask.NameToLayer(GameLayers.Trigger);
        collectibleLayer = LayerMask.NameToLayer(GameLayers.Collectible);
        mechanismLayer = LayerMask.NameToLayer(GameLayers.Mechanism);
        if (groundLayer < 0) groundLayer = 0;
    }

/// <summary>用平台下标而非世界坐标分 zone，和 SpawnZoneTriggers 的分段边界严格对齐。</summary>
    private int ZoneAt(int platformIndex)
    {
        return zoneIndexBase + platformIndex / Mathf.Max(2, platformsPerZone);
    }

    /// <summary>
    /// 按 platformsPerZone 把平台序列切成若干段，每段生成一个 ZoneTrigger。
    /// 覆盖该段起始平台到下一段起始平台之间的 X 范围；镜头房间用同样的边界，
    /// 和旧 DreamremainsLevelData.RoomSpec 的语义一致。
    /// </summary>
    private void SpawnZoneTriggers(Transform parent)
    {
    if (platforms.Count == 0)
            return;

        int step = Mathf.Max(2, platformsPerZone);
        int zoneCount = Mathf.CeilToInt(platforms.Count / (float)step);
        ZoneCount = zoneCount;

      for (int z = 0; z < zoneCount; z++)
        {
            int startIdx = z * step;
  int endIdx = Mathf.Min(startIdx + step, platforms.Count) - 1;
  float startX = platforms[startIdx].center.x - platforms[startIdx].width * 0.5f - 1f;
    float endX = endIdx + 1 < platforms.Count
        ? platforms[endIdx + 1].center.x - platforms[endIdx + 1].width * 0.5f
                : platforms[endIdx].center.x + platforms[endIdx].width * 0.5f + 1f;

      GameObject node = new GameObject($"RT-{zoneIndexBase + z:D2}");
    node.transform.SetParent(parent);
        float midX = (startX + endX) * 0.5f;
   node.transform.position = new Vector3(midX, 4f, 0f);
    node.layer = triggerLayer >= 0 ? triggerLayer : 0;

            BoxCollider2D box = node.AddComponent<BoxCollider2D>();
      box.isTrigger = true;
  box.size = new Vector2(Mathf.Max(1f, endX - startX), 16f);

            node.AddComponent<ZoneTrigger>().Configure(zoneIndexBase + z, startX, endX, -4f, 12f);
      }
    }

  private static Sprite whiteSprite;
    private static Sprite WhiteSprite()
    {
        if (whiteSprite != null)
        return whiteSprite;

      Texture2D tex = Texture2D.whiteTexture;
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 4f);
        return whiteSprite;
    }

    private void Clear()
    {
     platforms.Clear();
        var doomed = new List<GameObject>();
        foreach (Transform child in transform)
     {
     if (child == spawnPoint || child == goalPoint)
        continue;
       doomed.Add(child.gameObject);
        }

        foreach (GameObject go in doomed)
        {
            if (go == null)
                continue;
          // Play 模式下 Destroy 延迟到帧末执行；销毁前不摘出父子关系的话，
     // 本帧内 Category() 的 Find 会命中这个“待死”节点，把新内容挂进去，
            // 帧末一并被销毁。先 SetParent(null) 断开，再销毁。
            go.transform.SetParent(null);
    if (Application.isPlaying)
            Destroy(go);
     else
      DestroyImmediate(go);
  }
    }

    private void OnValidate()
    {
      if (verticalOffsetRange.x > verticalOffsetRange.y)
   verticalOffsetRange.y = verticalOffsetRange.x;
        if (horizontalGapRange.x > horizontalGapRange.y)
         horizontalGapRange.y = horizontalGapRange.x;
        if (platformSizeRange.x > platformSizeRange.y)
         platformSizeRange.y = platformSizeRange.x;
  maxY = Mathf.Max(maxY, minY + 1f);
   savePointInterval = Mathf.Max(4, savePointInterval);
   tarotPointCount = Mathf.Clamp(tarotPointCount, 0, 4);
 platformCount = Mathf.Max(6, platformCount);
    }
}
