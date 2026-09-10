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

    public static Transform SpawnA { get; private set; }
    public static Transform SpawnB { get; private set; }
    public static Transform Goal { get; private set; }

    public static Vector2 ToWorld(float htmlX, float htmlY, float originX)
    {
        return new Vector2(originX + htmlX, GridHeight - htmlY);
    }

    public static void Build(GameplaySceneBridge bridge)
    {
        Transform existing = GameObject.Find(RootName)?.transform;
        if (existing != null)
        {
            SpawnA = existing.Find("Scene_A/SP-A01");
            SpawnB = existing.Find("Scene_B/IN-B01");
            Goal = existing.Find("Scene_B/END");
            BindEndReturn(Goal);
            PlayerController existingPlayer = FindActivePlayer();
            bridge?.BindOfficial(existingPlayer, SpawnA, Goal != null ? Goal.GetComponent<BoxCollider2D>() : null);
            return;
        }

        TarotZoneQuery.ResetCurrent();

        GameObject testGrounds = GameObject.Find("test_grounds");
        if (testGrounds != null)
            testGrounds.SetActive(false);

        var root = new GameObject(RootName).transform;
        var sceneA = new GameObject("Scene_A").transform;
        sceneA.SetParent(root);
        var sceneB = new GameObject("Scene_B").transform;
        sceneB.SetParent(root);

        SpawnBackdrop(sceneA, 0f, "Assets/Game/Art/source/BG01.jpg");
        SpawnBackdrop(sceneB, SceneBOrigin, "Assets/Game/Art/Scenes/S02/S02_BG.jpg");
        BuildScene(sceneA, 0f, DreamremainsLevelData.SceneA);
        BuildScene(sceneB, SceneBOrigin, DreamremainsLevelData.SceneB);
        SpawnDecorations(sceneA, true);
        SpawnDecorations(sceneB, false);

        SpawnA = root.Find("Scene_A/SP-A01");
        SpawnB = root.Find("Scene_B/IN-B01");
        Transform trans = root.Find("Scene_A/TRANS-A-B");
        Transform end = root.Find("Scene_B/END");

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

    private static void BuildScene(Transform parent, float originX, DreamremainsLevelData.SceneSpec spec)
    {
        int ground = LayerMask.NameToLayer(GameLayers.Ground);
        int trigger = LayerMask.NameToLayer(GameLayers.Trigger);
        int enemyLayer = LayerMask.NameToLayer(GameLayers.Enemy);
        int mech = LayerMask.NameToLayer(GameLayers.Mechanism);
        if (ground < 0) ground = 0;

        for (int i = 0; i < spec.Platforms.Length; i++)
        {
            DreamremainsLevelData.PlatformSpec p = spec.Platforms[i];
            Vector2 surface = ToWorld(p.X + p.W * 0.5f, p.Y, originX);
            float height = 0.4f;
            Vector3 center = new Vector3(surface.x, surface.y - height * 0.5f, 0f);
            int zone = DreamremainsLevelData.ZoneAt(p.X + p.W * 0.5f, originX < 1f);

            Sprite platSprite = PlatformSprite(p, originX < 1f);
            GameObject platform = CreateBox(parent, p.Id, center, new Vector2(p.W, height), PlatformColor(p.Type, originX < 1f), ground, platSprite);
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

        GroupHidden(parent, spec.HiddenSlot, spec.HiddenIds);
        GroupHidden(parent, spec.HiddenSlot2, spec.HiddenIds2);

        for (int i = 0; i < spec.Abysses.Length; i++)
        {
            DreamremainsLevelData.AbyssSpec h = spec.Abysses[i];
            float x1 = originX + h.X1;
            float x2 = originX + h.X2;
            float w = x2 - x1;
            var zone = CreateBox(parent, h.Id, new Vector3(x1 + w * 0.5f, 0.4f, 0f), new Vector2(w, 1.2f), new Color(0.6f, 0.1f, 0.1f, 0.15f), trigger);
            zone.GetComponent<Collider2D>().isTrigger = true;
            zone.GetComponent<SpriteRenderer>().enabled = false;
            zone.AddComponent<KillZone>();
        }

        GameObject fall = CreateBox(
            parent,
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
            var spike = CreateBox(parent, s.Id, new Vector3(surface.x, surface.y + 0.25f, 0f), new Vector2(s.W, 0.5f), Color.white, mech, spikeSprite);
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
            node.transform.SetParent(parent);
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
            Transform enter = parent.Find(fold.EnterId);
            Transform exit = parent.Find(fold.ExitId);
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
            GameObject volume = CreateBox(parent, aw.Id, center, new Vector2(aw.W, aw.H), new Color(0.3f, 0.6f, 1f, 0.12f), trigger);
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
            rt.transform.SetParent(parent);
            rt.transform.position = new Vector3(minX, 4f, 0f);
            rt.layer = trigger >= 0 ? trigger : 0;
            BoxCollider2D box = rt.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.4f, 8f);
            rt.AddComponent<ZoneTrigger>().Configure(room.ZoneIndex, minX, maxX, 0f, 8f);
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
            Transform found = parent.Find(ids[i]);
            if (found == null)
                continue;
            targets[count++] = found.gameObject;
        }

        if (count == 0)
            return;

        var compact = new GameObject[count];
        System.Array.Copy(targets, compact, count);
        GameObject group = new GameObject("Hidden_T" + (slot + 1));
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
        go.AddComponent<LevelDecorationBackdrop>().Configure(sr, originX, originX < 1f ? 30f : 68f);
    }

    private static Sprite LoadArt(string path)
    {
        return GuideArt.LoadPath(path);
    }

    private static void AttachSurfaceVisual(GameObject root, Sprite sprite, bool fillCollider, Color tint)
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

    private static SpriteRenderer AddVisual(Transform parent, string name, Sprite sprite, Vector2 worldSize,
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

    private static SpriteRenderer AddArt(Transform parent, string name, string path, float height, Vector2 offset,
        string sortingLayer = GameLayers.InteractiveDownSorting, int order = 14)
    {
        Sprite sprite = LoadArt(path);
        if (sprite == null)
            return null;
        Vector2 size = sprite.bounds.size;
        return AddVisual(parent, name, sprite, size * (height / Mathf.Max(0.001f, size.y)), offset, sortingLayer, order);
    }

    private static SpriteRenderer AddDreamCore(Transform parent, float height, Vector2 offset)
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
                AddArt(root, "Tarot", "Assets/Game/Art/UI/Tarot/card-back.png", 0.64f, new Vector2(0f, 0.22f));
                AddArt(root, "Seal", "Assets/Game/Art/Scenes/S01/5.png", 0.7f, new Vector2(0f, 0.22f), GameLayers.InteractiveDownSorting, 13);
                break;
            case "cp":
                AddArt(root, "DreamShrine", "Assets/Game/Art/Scenes/S01/25.png", 0.38f, new Vector2(0f, -0.24f));
                SpriteRenderer checkpointCore = AddDreamCore(root, 0.32f, new Vector2(0f, 0.14f));
                if (checkpointCore != null)
                    checkpointCore.sortingLayerName = GameLayers.InteractiveDownSorting;
                break;
            case "door":
                AddVisual(root, "Visual", LoadArt(sceneA ? "Assets/Game/Art/Scenes/S01/9.png" : "Assets/Game/Art/Scenes/S02/24.png"),
                    new Vector2(0.4f, 2.2f), Vector2.zero, GameLayers.InteractiveDownSorting, 12);
                break;
            case "trg":
                AddArt(root, "SwitchSeal", "Assets/Game/Art/Scenes/S01/25.png", 0.24f, new Vector2(0f, -0.18f));
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
        // 有意逐处构图，不按 Zone 复制整段；均在玩家与平台后方，无 Collider/交互组件。
        Transform group = new GameObject("Decorations").transform;
        group.SetParent(scene, false);
        group.position = new Vector3(sceneA ? 0f : SceneBOrigin, 0f, 0f);
        if (sceneA)
        {
            Decoration(group, "LotusNear", "S01/18.png", 1.1f, 3.8f, 0.55f, 0.44f, false);
            Decoration(group, "LowMist", "S01/24.png", 1.8f, 8.3f, 0.45f, 0.2f, true);
            Decoration(group, "DistantLotus", "S01/17.png", 1.4f, 11.1f, 0.65f, 0.35f, true);
            Decoration(group, "HighFish", "S01/7.png", 0.42f, 15.6f, 7.9f, 0.32f, false);
            Decoration(group, "MistReturn", "S01/4.png", 2.3f, 19.1f, 0.2f, 0.22f, false);
            Decoration(group, "LotusFar", "S01/18.png", 0.85f, 24.2f, 0.4f, 0.38f, true);
            Decoration(group, "Dragonfly", "S01/23.png", 0.26f, 27.3f, 6.8f, 0.4f, false);
        }
        else
        {
            Decoration(group, "CloudEntry", "S02/8.png", 0.6f, 2.5f, 6.6f, 0.3f, false);
            Decoration(group, "ReedsLow", "S02/11.png", 0.8f, 7.3f, 0.35f, 0.35f, true);
            Decoration(group, "CloudHigh", "S02/6.png", 0.55f, 10.2f, 7.7f, 0.27f, true);
            Decoration(group, "CloudRibbon", "S02/7.png", 0.75f, 15.2f, 0.2f, 0.23f, false);
            Decoration(group, "GoldFan", "S02/25.png", 0.8f, 18.6f, 7.45f, 0.25f, false);
            Decoration(group, "CloudCurl", "S02/19.png", 0.48f, 22.8f, 0.6f, 0.3f, true);
            Decoration(group, "ReedEnd", "S02/12.png", 0.7f, 28.3f, 0.5f, 0.34f, false);
            Decoration(group, "CloudEnd", "S02/8.png", 0.55f, 32.1f, 4.45f, 0.25f, true);
        }
    }

    private static void Decoration(Transform parent, string name, string file, float height, float x, float y, float alpha, bool flip)
    {
        SpriteRenderer sr = AddArt(parent, name, "Assets/Game/Art/Scenes/" + file, height, new Vector2(x, y), GameLayers.MidgroundSorting, -5);
        if (sr == null)
            return;
        sr.color = new Color(1f, 1f, 1f, alpha);
        sr.flipX = flip;
    }

    // 保留原 CreateBox 的物理计算和原 PlatformSprite / 地刺引用。
    // 它们仅初始化一次物理 root；替换/动画/裁切只作用于下挂 Visual。
    // 尤其不可把 E14 使用的根缩放改为 1，否则 extraHeight 的世界距离会改变。
    private static GameObject CreateBox(Transform parent, string name, Vector3 center, Vector2 size, Color color, int layer, Sprite sprite = null)
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
