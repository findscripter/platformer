using UnityEngine;

/// <summary>
/// 按 v4 施工图在 Gameplay 里生成关卡（场景A 0–30，场景B 34–68）。
/// 1 格 = 1 Unity 单位；HTML Y 向下，世界 Y = 8 - htmlY。
/// </summary>
public static class DreamremainsLevelBootstrap
{
    public const float GridHeight = 8f;
    public const float SceneBOrigin = 34f;
    public const string RootName = "OfficialLevel";
    public const string ArtRootName = "SceneArt";

    public static Transform SpawnA { get; private set; }
    public static Transform SpawnB { get; private set; }
    public static Transform Goal { get; private set; }

    public static Vector2 ToWorld(float htmlX, float htmlY, float originX)
    {
        return new Vector2(originX + htmlX, GridHeight - htmlY);
    }

    public static void Build(GameplaySceneBridge bridge)
    {
        DisableLegacySceneArt();

        // OfficialLevel 承载平台/机关/敌人等带运行时逻辑的对象；它们的下挂组件
        // 大量依赖非序列化字段（Configure() 注入），场景重新加载后这些引用会
        // 丢失，所以每次进入关卡都整棵销毁重建，不复用磁盘上保存的旧实例。
        Transform existingLevel = GameObject.Find(RootName)?.transform;
        if (existingLevel != null)
            Object.DestroyImmediate(existingLevel.gameObject);

        TarotZoneQuery.ResetCurrent();

        var root = new GameObject(RootName).transform;
        var sceneA = new GameObject("Scene_A").transform;
        sceneA.SetParent(root);
        var sceneB = new GameObject("Scene_B").transform;
        sceneB.SetParent(root);

        SpawnBackdrop(sceneA, 0f, "Assets/Game/Art/source/BG01.jpg");
        SpawnBackdrop(sceneB, SceneBOrigin, "Assets/Game/Art/Scenes/S02/S02_BG.jpg");
        BuildScene(sceneA, 0f, DreamremainsLevelData.SceneA);
        BuildScene(sceneB, SceneBOrigin, DreamremainsLevelData.SceneB);

        // SceneArt 只是纯 SpriteRenderer 摆放，没有运行时逻辑；一旦存在就保留，
        // 允许在编辑器里手动调整位置/裁切后随场景一起保存。
        EnsureSceneArt();

        SpawnA = root.Find("Scene_A/SavePoints/SP-A01");
 SpawnB = root.Find("Scene_B/SavePoints/IN-B01");
        Transform trans = root.Find("Scene_A/Transitions/TRANS-A-B");
 Transform end = root.Find("Scene_B/Transitions/END");

        if (trans != null && SpawnB != null)
        {
            RegionGate gate = trans.gameObject.AddComponent<RegionGate>();
            gate.ConfigureDestination(SpawnB);
        }

        Goal = end;
        if (end != null)
        {
            BoxCollider2D goalCol = end.gameObject.GetComponent<BoxCollider2D>();
            if (goalCol == null)
                goalCol = end.gameObject.AddComponent<BoxCollider2D>();
            goalCol.isTrigger = true;
            goalCol.size = new Vector2(1.4f, 2.2f);
            BindEndReturn(end);
        }

        PlayerController player = FindActivePlayer();
        if (player != null && SpawnA != null)
            player.transform.position = SpawnA.position + Vector3.up * 0.6f;

        CameraTargetFollow follow = Object.FindFirstObjectByType<CameraTargetFollow>();
        follow?.SetRoom(0f, 30f, 0f, 8f);
        if (follow != null && player != null)
            follow.player = player.transform;

        if (bridge != null)
        bridge.BindOfficial(player, SpawnA, Goal != null ? Goal.GetComponent<BoxCollider2D>() : null);
    }

    private static void EnsureSceneArt()
    {
        if (GameObject.Find(ArtRootName) != null)
            return;

        var artRoot = new GameObject(ArtRootName).transform;
        var artSceneA = new GameObject("Scene_A").transform;
        artSceneA.SetParent(artRoot);
        var artSceneB = new GameObject("Scene_B").transform;
        artSceneB.SetParent(artRoot);

        SpawnDecorations(artSceneA, true);
        SpawnDecorations(artSceneB, false);
    }

    private static void DisableLegacySceneArt()
    {
        GameObject testGrounds = GameObject.Find("test_grounds");
        if (testGrounds != null)
            testGrounds.SetActive(false);

        // Gameplay.unity 里的旧试玩背景会与正式 A/B 背景、装饰和梦滴叠加，
        // 只保留正式运行时生成的 OfficialLevel 美术。
        GameObject testBackgrounds = GameObject.Find("test_backgrounds");
        if (testBackgrounds != null)
            testBackgrounds.SetActive(false);
    }

    private static void BindEndReturn(Transform end)
    {
        if (end == null)
            return;
        if (end.GetComponent<TarotReturnInteractable>() == null)
            end.gameObject.AddComponent<TarotReturnInteractable>();
    }

    private static PlayerController FindActivePlayer()
    {
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].gameObject.activeInHierarchy)
                return players[i];
        }

        return players.Length > 0 ? players[0] : null;
    }

    /// <summary>按 v4 施工说明的分类取分组节点，不存在就建。</summary>
    private static Transform Category(Transform scene, string name)
    {
        Transform existing = scene.Find(name);
        if (existing != null)
            return existing;

        var group = new GameObject(name);
        group.transform.SetParent(scene, false);
        return group.transform;
    }

    /// <summary>在整棵子树里按名字找节点；分组后的对象不再都是直接子级。</summary>
    private static Transform FindDeep(Transform parent, string name)
    {
        Transform direct = parent.Find(name);
        if (direct != null)
            return direct;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeep(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void BuildScene(Transform parent, float originX, DreamremainsLevelData.SceneSpec spec)
    {
        int ground = LayerMask.NameToLayer(GameLayers.Ground);
        int trigger = LayerMask.NameToLayer(GameLayers.Trigger);
        int enemyLayer = LayerMask.NameToLayer(GameLayers.Enemy);
        int mech = LayerMask.NameToLayer(GameLayers.Mechanism);
        if (ground < 0) ground = 0;

        Transform terrain = Category(parent, "Terrain");
        Transform hazards = Category(parent, "Hazards");
        Transform enemiesGroup = Category(parent, "Enemies");
        Transform itemsGroup = Category(parent, "Items");
        Transform tarotGroup = Category(parent, "TarotMechanisms");
        Transform savePoints = Category(parent, "SavePoints");
        Transform transitions = Category(parent, "Transitions");
        Transform cameraRooms = Category(parent, "CameraRooms");

        for (int i = 0; i < spec.Platforms.Length; i++)
        {
            DreamremainsLevelData.PlatformSpec p = spec.Platforms[i];
            Vector2 surface = ToWorld(p.X + p.W * 0.5f, p.Y, originX);
            float height = 0.4f;
            Vector3 center = new Vector3(surface.x, surface.y - height * 0.5f, 0f);
            int zone = DreamremainsLevelData.ZoneAt(p.X + p.W * 0.5f, originX < 1f);

            Sprite platSprite = PlatformSprite(p, originX < 1f);
            GameObject platform = CreateBox(terrain, p.Id, center, new Vector2(p.W, height), PlatformColor(p.Type, originX < 1f), ground, platSprite);
            Sprite visiblePlatform = originX < 1f && p.W < 3.2f && i % 3 == 1
                ? LoadArt("Assets/Game/Art/Scenes/S01/12.png") : platSprite;
            AttachSurfaceVisual(platform, visiblePlatform, false, PlatformColor(p.Type, originX < 1f));
            if (p.Type == "moving")
            {
                Rigidbody2D body = platform.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                MovingPlatform mover = platform.AddComponent<MovingPlatform>();
                Vector2 a = ToWorld(p.MoveX, p.MoveY1, originX);
                Vector2 b = ToWorld(p.MoveX, p.MoveY2, originX);
                a.x = center.x;
                b.x = center.x;
                mover.Configure(a, b, 1.5f);
            }
            else if (p.Type == "fading")
            {
                Rigidbody2D fadeBody = platform.AddComponent<Rigidbody2D>();
                fadeBody.bodyType = RigidbodyType2D.Kinematic;
                fadeBody.gravityScale = 0f;
                fadeBody.freezeRotation = true;
                platform.AddComponent<FadingPlatform>().Configure(zone);
            }
            else if (p.Type == "hidden")
            {
                platform.SetActive(false);
            }

            if (p.Raise)
            {
                Rigidbody2D raiseBody = platform.GetComponent<Rigidbody2D>();
                if (raiseBody == null)
                    raiseBody = platform.AddComponent<Rigidbody2D>();
                raiseBody.bodyType = RigidbodyType2D.Kinematic;
                raiseBody.gravityScale = 0f;
                raiseBody.freezeRotation = true;
                RaisePlatform raise = platform.AddComponent<RaisePlatform>();
                Vector3 high = center + Vector3.up * 1.6f;
                raise.Configure(zone, center, high);
            }
        }

        GroupHidden(terrain, spec.HiddenSlot, spec.HiddenIds);
        GroupHidden(terrain, spec.HiddenSlot2, spec.HiddenIds2);

        for (int i = 0; i < spec.Abysses.Length; i++)
        {
            DreamremainsLevelData.AbyssSpec h = spec.Abysses[i];
            float x1 = originX + h.X1;
            float x2 = originX + h.X2;
            float w = x2 - x1;
            var zone = CreateBox(hazards, h.Id, new Vector3(x1 + w * 0.5f, 0.4f, 0f), new Vector2(w, 1.2f), new Color(0.6f, 0.1f, 0.1f, 0.15f), trigger);
            zone.GetComponent<Collider2D>().isTrigger = true;
            zone.GetComponent<SpriteRenderer>().enabled = false;
            zone.AddComponent<KillZone>();
        }

        GameObject fall = CreateBox(
            hazards,
            originX < 1f ? "H-FALL-A" : "H-FALL-B",
            new Vector3(originX + 15f, -1.6f, 0f),
            new Vector2(34f, 1.2f),
            new Color(0.6f, 0.1f, 0.1f, 0.15f),
            trigger);
        fall.GetComponent<Collider2D>().isTrigger = true;
        fall.GetComponent<SpriteRenderer>().enabled = false;
        fall.AddComponent<KillZone>();

        for (int i = 0; i < spec.Spikes.Length; i++)
        {
            DreamremainsLevelData.SpikeSpec s = spec.Spikes[i];
            Vector2 surface = ToWorld(s.X + s.W * 0.5f, s.Y, originX);
            Sprite spikeSprite = LoadArt("Assets/Game/Art/Scenes/S01/11.png");
            var spike = CreateBox(hazards, s.Id, new Vector3(surface.x, surface.y + 0.25f, 0f), new Vector2(s.W, 0.5f), Color.white, mech, spikeSprite);
            AttachSurfaceVisual(spike, spikeSprite, true, Color.white);
            spike.GetComponent<Collider2D>().isTrigger = true;
            int spikeZone = DreamremainsLevelData.ZoneAt(s.X, originX < 1f);
            spike.AddComponent<SpikeTrap>().ConfigureZone(spikeZone);
            spike.AddComponent<SpikeGrowth>().Configure(spikeZone, 0.7f);
        }

        for (int i = 0; i < spec.Points.Length; i++)
        {
            DreamremainsLevelData.PointSpec point = spec.Points[i];
            Vector2 pos = ToWorld(point.X, point.Y, originX);
            GameObject node = new GameObject(point.Id);
            node.transform.SetParent(PointCategory(point.Kind, terrain, enemiesGroup, itemsGroup, tarotGroup, savePoints, transitions));
            node.transform.position = new Vector3(pos.x, pos.y + 0.15f, 0f);
            int zone = DreamremainsLevelData.ZoneAt(point.X, originX < 1f);

            if (point.Kind == "tarot")
            {
                SetTriggerLayer(node, trigger);
                TarotNodeInteractable tarot = node.AddComponent<TarotNodeInteractable>();
                tarot.ConfigureSlot(point.Slot);
            }
            else if (point.Kind == "cp")
            {
                SetTriggerLayer(node, trigger);
                node.AddComponent<CheckpointInteractable>().Configure(point.Id, "记录梦核", 1.4f);
            }
            else if (point.Kind == "trg")
            {
                BoxCollider2D box = node.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(0.8f, 2f);
                node.layer = trigger >= 0 ? trigger : 0;
                node.AddComponent<MechanismTrigger>().Configure(point.Id, true);
            }
            else if (point.Kind == "door")
            {
                node.layer = ground;
                BoxCollider2D block = node.AddComponent<BoxCollider2D>();
                block.isTrigger = false;
                block.size = Vector2.one;
                node.transform.localScale = new Vector3(0.4f, 2.2f, 1f);
                DoorInteractable door = node.AddComponent<DoorInteractable>();
                door.Configure(point.Id, false, "Door_Open");
            }
            else if (point.Kind == "fold")
            {
                SetTriggerLayer(node, trigger);
            }
            else if (point.Kind == "item")
            {
                node.layer = LayerMask.NameToLayer(GameLayers.Collectible);
                CircleCollider2D circle = node.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.35f;
                node.transform.localScale = Vector3.one * 0.45f;
                AddDreamCore(node.transform, 0.42f, Vector2.zero);
                node.AddComponent<CollectibleItem>();
            }
            else if (point.Kind == "enemy")
            {
                node.layer = enemyLayer >= 0 ? enemyLayer : 0;
                Rigidbody2D body = node.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.freezeRotation = true;
                CircleCollider2D circle = node.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.4f;
                node.transform.localScale = Vector3.one * 0.7f;
                // 只复用原 Prefab 的 Visual 素材/动画；不复制其胶囊碰撞、生命值或移动参数。
                Sprite enemySprite = LoadArt("Assets/Game/Art/Characters/monster/walk/1.png");
                SpriteRenderer visual = AddVisual(node.transform, "Visual", enemySprite,
                    enemySprite != null ? (Vector2)enemySprite.bounds.size * 0.126f : Vector2.one, Vector2.zero, GameLayers.InteractiveUpSorting, 5);
                if (visual != null)
                {
                    // 原怪物帧以脚底为 pivot，沿用 Prefab 0.18 × 当前根缩放 0.7 的比例。
                    visual.transform.localPosition = Vector3.down * circle.radius;
                    Animator animator = visual.gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Enemy/PatrolEnemy");
                    animator.applyRootMotion = false;
                }
                PatrolEnemy enemy = node.AddComponent<PatrolEnemy>();
                enemy.ConfigurePatrol(
                    new Vector2(point.PatrolX1 - point.X, 0f),
                    new Vector2(point.PatrolX2 - point.X, 0f));
            }

            DecoratePoint(node, point, originX < 1f);
        }

        for (int i = 0; i < spec.Folds.Length; i++)
        {
            DreamremainsLevelData.FoldSpec fold = spec.Folds[i];
            Transform enter = FindDeep(parent, fold.EnterId);
            Transform exit = FindDeep(parent, fold.ExitId);
            if (enter == null || exit == null)
                continue;
            FoldPortal portal = enter.gameObject.AddComponent<FoldPortal>();
            portal.ConfigureExit(exit);
        }

        for (int i = 0; i < spec.AirWalks.Length; i++)
        {
            DreamremainsLevelData.RectSpec aw = spec.AirWalks[i];
            Vector2 min = ToWorld(aw.X, aw.Y + aw.H, originX);
            Vector2 max = ToWorld(aw.X + aw.W, aw.Y, originX);
            Vector3 center = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            GameObject volume = CreateBox(tarotGroup, aw.Id, center, new Vector2(aw.W, aw.H), new Color(0.3f, 0.6f, 1f, 0.12f), trigger);
            volume.GetComponent<Collider2D>().isTrigger = true;
            volume.AddComponent<AirWalkVolume>().Configure(DreamremainsLevelData.ZoneAt(aw.X + aw.W * 0.5f, originX < 1f));
            SpriteRenderer current = AddArt(volume.transform, "AirCurrent", "Assets/Game/Art/Scenes/S02/7.png", 0.32f, Vector2.zero,
                GameLayers.MidgroundSorting, -4);
            if (current != null)
            {
                current.color = new Color(1f, 1f, 1f, 0.2f);
                volume.GetComponent<SpriteRenderer>().forceRenderingOff = true;
            }
        }

        for (int i = 0; i < spec.Rooms.Length; i++)
        {
            DreamremainsLevelData.RoomSpec room = spec.Rooms[i];
            float minX = originX + room.X1;
            float maxX = originX + room.X2;
            GameObject rt = new GameObject(room.RtId);
            rt.transform.SetParent(cameraRooms);
            rt.transform.position = new Vector3(minX, 4f, 0f);
            rt.layer = trigger >= 0 ? trigger : 0;
            BoxCollider2D box = rt.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.4f, 8f);
            rt.AddComponent<ZoneTrigger>().Configure(room.ZoneIndex, minX, maxX, 0f, 8f);
        }
    }

    /// <summary>点位按玩法职责归类到 v4 施工说明的分组。</summary>
    private static Transform PointCategory(string kind, Transform terrain, Transform enemies,
        Transform items, Transform tarot, Transform savePoints, Transform transitions)
    {
        switch (kind)
        {
            case "tarot":
            case "trg":
            case "fold":
            // D1–D3 是响应 E08/E10/E11 的可选支路门，属塔罗机关而非危险物。
            case "door":
                return tarot;
            case "cp":
            case "spawn":
                return savePoints;
            case "trans":
            case "end":
                return transitions;
            case "item":
                return items;
            case "enemy":
                return enemies;
            default:
                return terrain;
        }
    }

    private static void GroupHidden(Transform parent, int slot, string[] ids)
    {
        if (ids == null || ids.Length == 0)
            return;

        var targets = new GameObject[ids.Length];
        int count = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            Transform found = FindDeep(parent, ids[i]);
            if (found == null)
                continue;
            targets[count++] = found.gameObject;
        }

        if (count == 0)
            return;

        var compact = new GameObject[count];
        System.Array.Copy(targets, compact, count);
        GameObject group = new GameObject("Hidden_T" + (slot + 1));
        // 让分组节点真正容纳它管辖的隐藏平台，而不是留一个空壳挂在同级。
        foreach (GameObject target in compact)
        {
            if (target != null)
                target.transform.SetParent(group.transform, true);
        }

        group.transform.SetParent(parent);
        group.AddComponent<HiddenPathController>().Configure(slot, compact);
    }

    private static Color PlatformColor(string type, bool sceneA)
    {
        if (type == "moving")
            return new Color(0.78f, 0.92f, 1f);
        if (type == "fading")
            return new Color(1f, 0.91f, 0.68f);
        if (type == "hidden")
            return new Color(0.86f, 0.83f, 1f);
        return Color.white;
    }

    private static Sprite PlatformSprite(DreamremainsLevelData.PlatformSpec p, bool sceneA)
    {
        if (sceneA)
            return LoadArt(p.W >= 3.2f ? "Assets/Game/Art/Scenes/S01/13.png" : "Assets/Game/Art/Scenes/S01/10.png");
        return LoadArt(p.W >= 3f ? "Assets/Game/Art/Scenes/S02/15.png" : "Assets/Game/Art/Scenes/S02/16.png");
    }

       private static void SpawnBackdrop(Transform parent, float originX, string path)
    {
   Sprite sprite = LoadArt(path);
        if (sprite == null)
      return;

        GameObject go = new GameObject("Backdrop");
 go.transform.SetParent(parent);
        go.transform.position = new Vector3(originX + 15f, 4f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = GameLayers.BackgroundSorting;
        sr.sortingOrder = -40;
     sr.color = Color.white;
     go.AddComponent<LevelDecorationBackdrop>().Configure(sr, originX, originX < 1f ? 30f : 68f, originX < 1f, 32f);
    }

    // ProceduralLevelGenerator 复用：不再假设固定的 originX+15 中心和 30f/68f 边界，
    // 直接按两区实际随机生成后的边界与切换分界线摆放。
    internal static GameObject SpawnBackdrop(Transform parent, float roomMin, float roomMax, string path, bool sceneASide, float boundaryX)
    {
     Sprite sprite = LoadArt(path);
        if (sprite == null)
            return null;

        GameObject go = new GameObject("Backdrop");
      go.transform.SetParent(parent);
        go.transform.position = new Vector3((roomMin + roomMax) * 0.5f, 4f, 1f);
   SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
  sr.sprite = sprite;
        sr.sortingLayerName = GameLayers.BackgroundSorting;
        sr.sortingOrder = -40;
  sr.color = Color.white;
        go.AddComponent<LevelDecorationBackdrop>().Configure(sr, roomMin, roomMax, sceneASide, boundaryX);
        return go;
    }

    internal static Sprite LoadArt(string path)
    {
        return GuideArt.LoadPath(path);
    }

    internal static void AttachSurfaceVisual(GameObject root, Sprite sprite, bool fillCollider, Color tint)
    {
        if (sprite == null)
            return;
        SpriteRenderer source = root.GetComponent<SpriteRenderer>();
        SpriteRenderer visual = AddVisual(root.transform, "Visual", sprite, Vector2.one, Vector2.zero,
            GameLayers.InteractiveDownSorting, 10);
        // FadingPlatform 仍写入根 SpriteRenderer，呈现层只读并转发它的颜色/可见状态。
        source.forceRenderingOff = true;
        visual.gameObject.AddComponent<LevelDecorationSurface>().Configure(root.GetComponent<BoxCollider2D>(), source, visual, fillCollider, tint);
    }

    internal static SpriteRenderer AddVisual(Transform parent, string name, Sprite sprite, Vector2 worldSize,
        Vector2 offset, string sortingLayer, int order)
    {
        if (sprite == null)
            return null;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        Vector3 native = sprite.bounds.size;
        Vector3 scale = new Vector3(worldSize.x / Mathf.Max(0.001f, native.x), worldSize.y / Mathf.Max(0.001f, native.y), 1f);
        Vector3 inherited = parent.lossyScale;
        go.transform.localScale = new Vector3(scale.x / inherited.x, scale.y / inherited.y, 1f);
        go.transform.position = parent.position + (Vector3)offset - Vector3.Scale(sprite.bounds.center, scale);
        return sr;
    }

    internal static SpriteRenderer AddArt(Transform parent, string name, string path, float height, Vector2 offset,
        string sortingLayer = GameLayers.InteractiveDownSorting, int order = 14)
    {
        Sprite sprite = LoadArt(path);
        if (sprite == null)
            return null;
        Vector2 size = sprite.bounds.size;
        return AddVisual(parent, name, sprite, size * (height / Mathf.Max(0.001f, size.y)), offset, sortingLayer, order);
    }

    internal static SpriteRenderer AddDreamCore(Transform parent, float height, Vector2 offset)
    {
        Sprite sprite = LoadArt("Assets/Game/Art/Scenes/S01/26.png");
        if (sprite == null)
            return null;
        // 26.png 的泪滴位于 120×264 透明画布的 (31,112)-(86,207)（左上坐标）。
        // 只补偿可见内容的尺寸和中心，不读像素、不改源 Sprite 或物理根节点。
        Vector2 canvas = sprite.rect.size / sprite.pixelsPerUnit;
        float scale = height / (canvas.y * (95f / 264f));
        Vector2 contentCenter = new Vector2(canvas.x * (58.5f / 120f), canvas.y * (104.5f / 264f)) - sprite.pivot / sprite.pixelsPerUnit;
        Vector2 centerCorrection = contentCenter - (Vector2)sprite.bounds.center;
        return AddVisual(parent, "DreamCore", sprite, (Vector2)sprite.bounds.size * scale, offset - centerCorrection * scale,
            GameLayers.InteractiveUpSorting, 14);
    }

    private static void DecoratePoint(GameObject node, DreamremainsLevelData.PointSpec point, bool sceneA)
    {
        Transform root = node.transform;
        switch (point.Kind)
        {
            case "tarot":
                // 塔罗点此前先后套用整张卡背、梦泡（太透明）、DreamCore（与检查点重复）；
                // 改用独立的金色菱形标记，与 cp 的石鼓+水滴、trg 的橙色方块区分开。
                SpriteRenderer tarotVisual = AddVisual(root, "TarotMark", WhiteSprite(), new Vector2(0.34f, 0.34f),
                    new Vector2(0f, 0.22f), GameLayers.InteractiveDownSorting, 13);
                if (tarotVisual != null)
                {
                    tarotVisual.color = new Color(0.92f, 0.78f, 0.35f, 0.95f);
                    tarotVisual.transform.Rotate(0f, 0f, 45f);
                }
                break;
            case "cp":
                AddArt(root, "DreamShrine", "Assets/Game/Art/Scenes/S01/25.png", 0.38f, new Vector2(0f, -0.24f));
                SpriteRenderer checkpointCore = AddDreamCore(root, 0.32f, new Vector2(0f, 0.14f));
                if (checkpointCore != null)
                    checkpointCore.sortingLayerName = GameLayers.InteractiveDownSorting;
                break;
            case "door":
                // 素材库里没有门/门框专用图；S01/9（全出血竖条）和 S02/24（金色雨帘）
                // 形状都不对，硬套会在场景里出现突兀的竖线。先用纯色占位，等美术补图。
                SpriteRenderer doorVisual = AddVisual(root, "Visual", WhiteSprite(), new Vector2(0.4f, 2.2f),
                    Vector2.zero, GameLayers.InteractiveDownSorting, 12);
                if (doorVisual != null)
                    doorVisual.color = new Color(0.14f, 0.13f, 0.16f, 0.9f);
                break;
            case "trg":
                // 之前复用检查点的石鼓图标，玩家无法区分机关开关和存档点；
                // 改用独立配色的小圆点，与 cp/tarot 的视觉语言区分开。
                SpriteRenderer switchVisual = AddVisual(root, "SwitchMark", WhiteSprite(), new Vector2(0.22f, 0.22f),
                    new Vector2(0f, -0.18f), GameLayers.InteractiveDownSorting, 12);
                if (switchVisual != null)
                    switchVisual.color = new Color(0.85f, 0.62f, 0.28f, 0.85f);
                break;
            case "fold":
                AddArt(root, "FoldEcho", "Assets/Game/Art/Scenes/S01/5.png", point.Id.StartsWith("FE-") ? 0.55f : 0.32f, Vector2.zero);
                break;
            case "trans":
            case "end":
                AddArt(root, "Passage", "Assets/Game/Art/Scenes/S01/5.png", 1.1f, Vector2.zero);
                AddArt(root, "LightLeft", "Assets/Game/Art/Scenes/S01/8.png", 1.4f, new Vector2(-0.38f, 0f));
                AddArt(root, "LightRight", "Assets/Game/Art/Scenes/S01/8.png", 1.4f, new Vector2(0.38f, 0f));
                if (point.Kind == "end")
                    AddDreamCore(root, 0.48f, Vector2.zero);
                break;
            default:
                return;
        }
        node.AddComponent<LevelDecorationNode>().Configure(point.Kind, point.Slot, point.Id.StartsWith("FA-"));
    }

    private static void SpawnDecorations(Transform scene, bool sceneA)
    {
        // 参考图是“开场定妆图”：玩家出生瞬间镜头看到的第一屏，不是贯穿全关卡的构图。
        // 出生点局部坐标(0.9,5.35)，镜头 clamp 后第一屏可见范围约 x:[0,14.2] y:[0,8]。
        // 有意逐处构图，不按 Zone 复制整段；均在玩家与平台后方，无 Collider/交互组件。
        var variation = new System.Random(sceneA ? 1701 : 2701);
        Transform group = new GameObject("Decorations").transform;
        group.SetParent(scene, false);
        group.position = new Vector3(sceneA ? 0f : SceneBOrigin, 0f, 0f);
        SpawnReferenceComposition(group, sceneA);
        if (sceneA)
        {
            // 参考图荷花贴近出生点附近水面，配一只蜻蜓；鱼群贴水面而非高空。
            Decoration(group, "LotusMain", "S01/17.png", 1.2f, 8.0f, 0.75f, 0.55f, false, variation);
            Decoration(group, "Dragonfly", "S01/23.png", 0.55f, 9.6f, 1.9f, 0.85f, false, variation);
            Decoration(group, "LowFish", "S01/7.png", 0.24f, 4.7f, 0.22f, 0.3f, false, variation, 0.5f, false);
        }
        else
        {
            Decoration(group, "ReedsLow", "S02/11.png", 0.8f, 4.0f, 0.35f, 0.35f, true, variation, 0.75f, false);
            Decoration(group, "CloudRibbon", "S02/7.png", 0.6f, 9.0f, 6.0f, 0.23f, false, variation, 0.5f, false);
            Decoration(group, "ReedEnd", "S02/12.png", 0.7f, 12.0f, 0.5f, 0.34f, false, variation);
        }
    }

    private static void SpawnReferenceComposition(Transform parent, bool sceneA)
    {
        if (sceneA)
        {
            // A 区参考图（出生点第一屏）：左侧两座贴地雾丘，中段三条竖直光柱错落分布，
            // 右侧一道大斜坡扫向画面右边缘；均在玩法层之后，只负责取景。
            SceneArt(parent, "A_HillNear", "S01/15.png", 1.8f, 1.5f, 0.9f, GameLayers.MidgroundSorting, -21);
            SceneArt(parent, "A_HillSmall", "S01/19.png", 1.1f, 3.5f, 0.55f, GameLayers.MidgroundSorting, -20);
            SceneArt(parent, "A_LightBeam1", "S01/8.png", 2.0f, 4.6f, 1.3f, GameLayers.MidgroundSorting, -17);
            SceneArt(parent, "A_LightBeam2", "S01/8.png", 2.0f, 5.15f, 1.3f, GameLayers.MidgroundSorting, -17);
            SceneArt(parent, "A_LightBeam3", "S01/8.png", 2.0f, 5.7f, 1.3f, GameLayers.MidgroundSorting, -17);
            SceneArt(parent, "A_FinSlope", "S01/21.png", 4.75f, 10.7f, 2.4f, GameLayers.MidgroundSorting, -22);
        }
        else
        {
            // B 区参考图（出生点第一屏）：云海斜坡在出生点附近贯穿视野，顶部挂月，
            // 底部深蓝水面配芦苇；不使用任何金色纹样（参考图中没有该元素）。
            // 素材库缺亭子/塔剪影与仙鹤剪影，参考图左上角建筑与飞鸟暂无对应素材可复刻。
            SceneArt(parent, "B_CloudSea", "S02/22.png", 4.6f, 6.5f, 3.2f, GameLayers.MidgroundSorting, -22);
            SceneArt(parent, "B_GrassBand", "S02/21.png", 1.2f, 6.0f, 0.5f, GameLayers.MidgroundSorting, -20);
            SceneArt(parent, "B_Moon", "S02/3.png", 0.6f, 12.4f, 7.1f, GameLayers.MidgroundSorting, -14);
            SceneArt(parent, "B_DarkRibbon", "S02/5.png", 0.85f, 2.2f, 6.5f, GameLayers.MidgroundSorting, -14);
        }
    }

    private static SpriteRenderer SceneArt(Transform parent, string name, string file, float height, float x, float y,
        string sortingLayer, int order, bool flipX = false)
    {
        SpriteRenderer sr = AddArt(parent, name, "Assets/Game/Art/Scenes/" + file, height,
            new Vector2(x, y), sortingLayer, order);
        if (sr != null)
            sr.flipX = flipX;
        return sr;
    }

    internal static void Decoration(Transform parent, string name, string file, float height, float x, float y, float alpha,
        bool flip, System.Random variation, float cropFraction = 1f, bool cropFromRight = false)
    {
        Sprite sprite = LoadArt("Assets/Game/Art/Scenes/" + file);
        if (sprite == null)
            return;

        if (cropFraction < 0.999f)
            sprite = CropDecorationSprite(sprite, cropFraction, cropFromRight);

        float jitterX = NextSigned(variation, 0.22f);
        float jitterY = NextSigned(variation, 0.08f);
        float scale = 0.92f + Next01(variation) * 0.16f;
        float opacity = Mathf.Clamp01(alpha * (0.88f + Next01(variation) * 0.18f));
        SpriteRenderer sr = AddVisual(parent, name, sprite,
            (Vector2)sprite.bounds.size * (height * scale / Mathf.Max(0.001f, sprite.bounds.size.y)),
            new Vector2(x + jitterX, y + jitterY), GameLayers.MidgroundSorting, -5);
        if (sr == null)
            return;
        sr.color = new Color(1f, 1f, 1f, opacity);
        sr.flipX = flip ^ (variation.Next(2) == 0);
    }

    private static Sprite CropDecorationSprite(Sprite source, float fraction, bool fromRight)
    {
        fraction = Mathf.Clamp(fraction, 0.25f, 1f);
        Rect sourceRect = source.rect;
        float cropWidth = Mathf.Max(1f, Mathf.Round(sourceRect.width * fraction));
        float x = fromRight ? sourceRect.xMax - cropWidth : sourceRect.x;
        Rect crop = new Rect(x, sourceRect.y, cropWidth, sourceRect.height);
        // Sprite.pivot 是像素坐标，Sprite.Create 需要 0–1 的归一化坐标。
        Vector2 pivot = new Vector2(fromRight ? 1f : 0f, source.pivot.y / Mathf.Max(1f, sourceRect.height));
        Sprite cropped = Sprite.Create(source.texture, crop, pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        cropped.name = source.name + (fromRight ? "_CropRight" : "_CropLeft") + Mathf.RoundToInt(fraction * 100f);
        return cropped;
    }

    private static float Next01(System.Random random)
    {
        return random == null ? 0.5f : (float)random.NextDouble();
    }

    private static float NextSigned(System.Random random, float range)
    {
        return (Next01(random) * 2f - 1f) * range;
    }

    // 保留原 CreateBox 的物理计算和原 PlatformSprite / 地刺引用。
    // 它们仅初始化一次物理 root；替换/动画/裁切只作用于下挂 Visual。
    // 尤其不可把 E14 使用的根缩放改为 1，否则 extraHeight 的世界距离会改变。
    internal static GameObject CreateBox(Transform parent, string name, Vector3 center, Vector2 size, Color color, int layer, Sprite sprite = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.layer = layer >= 0 ? layer : 0;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite use = sprite != null ? sprite : WhiteSprite();
        sr.sprite = use;
        sr.color = sprite != null ? Color.white : color;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.sortingLayerName = GameLayers.InteractiveDownSorting;
        sr.sortingOrder = 10;
        Vector2 native = use.bounds.size;
        float nx = Mathf.Max(0.01f, native.x);
        float ny = Mathf.Max(0.01f, native.y);
        go.transform.localScale = new Vector3(Mathf.Max(0.05f, size.x / nx), Mathf.Max(0.05f, size.y / ny), 1f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(nx, ny);
        return go;
    }

    private static void SetTriggerLayer(GameObject node, int trigger)
    {
        node.layer = trigger >= 0 ? trigger : 0;
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
}
