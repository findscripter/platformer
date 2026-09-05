using UnityEngine;

/// <summary>
/// 按 v4 施工图在 Gameplay 里生成白盒关卡（场景A 0–30，场景B 34–64）。
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
            PlayerController existingPlayer = FindActivePlayer();
            bridge?.BindOfficial(existingPlayer, SpawnA, Goal != null ? Goal.GetComponent<Collider2D>() : null);
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

        BuildScene(sceneA, 0f, DreamremainsLevelData.SceneA);
        BuildScene(sceneB, SceneBOrigin, DreamremainsLevelData.SceneB);

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
        }

        PlayerController player = FindActivePlayer();
        if (player != null && SpawnA != null)
            player.transform.position = SpawnA.position + Vector3.up * 0.6f;

        CameraTargetFollow follow = Object.FindFirstObjectByType<CameraTargetFollow>();
        follow?.SetRoom(0f, 5.7f, 0f, 8f);
        if (follow != null && player != null)
            follow.player = player.transform;

        if (bridge != null)
            bridge.BindOfficial(player, SpawnA, Goal != null ? Goal.GetComponent<Collider2D>() : null);
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

            GameObject platform = CreateBox(parent, p.Id, center, new Vector2(p.W, height), PlatformColor(p.Type, originX < 1f), ground);
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

        for (int i = 0; i < spec.Spikes.Length; i++)
        {
            DreamremainsLevelData.SpikeSpec s = spec.Spikes[i];
            Vector2 surface = ToWorld(s.X + s.W * 0.5f, s.Y, originX);
            var spike = CreateBox(parent, s.Id, new Vector3(surface.x, surface.y + 0.25f, 0f), new Vector2(s.W, 0.5f), new Color(0.75f, 0.12f, 0.12f), mech);
            spike.GetComponent<Collider2D>().isTrigger = true;
            spike.AddComponent<SpikeTrap>();
            spike.AddComponent<SpikeGrowth>().Configure(DreamremainsLevelData.ZoneAt(s.X, originX < 1f), 0.7f);
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
                SpriteRenderer sr = node.AddComponent<SpriteRenderer>();
                sr.sprite = WhiteSprite();
                sr.color = new Color(0.45f, 0.2f, 0.55f);
                sr.sortingLayerName = GameLayers.InteractiveDownSorting;
                sr.sortingOrder = 12;
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
                SpriteRenderer sr = node.AddComponent<SpriteRenderer>();
                sr.sprite = WhiteSprite();
                sr.color = new Color(0.95f, 0.8f, 0.2f);
                sr.sortingLayerName = GameLayers.InteractiveUpSorting;
                sr.sortingOrder = 5;
                node.transform.localScale = Vector3.one * 0.45f;
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
                SpriteRenderer sr = node.AddComponent<SpriteRenderer>();
                sr.sprite = WhiteSprite();
                sr.color = new Color(0.7f, 0.15f, 0.15f);
                sr.sortingLayerName = GameLayers.InteractiveUpSorting;
                sr.sortingOrder = 5;
                node.transform.localScale = Vector3.one * 0.7f;
                PatrolEnemy enemy = node.AddComponent<PatrolEnemy>();
                enemy.ConfigurePatrol(
                    new Vector2(point.PatrolX1 - point.X, 0f),
                    new Vector2(point.PatrolX2 - point.X, 0f));
            }
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
            return new Color(0.45f, 0.75f, 1f);
        if (type == "fading")
            return new Color(1f, 0.85f, 0.25f);
        if (type == "hidden")
            return new Color(0.75f, 0.5f, 1f);
        return sceneA ? new Color(0.85f, 0.85f, 0.88f) : new Color(0.55f, 0.75f, 0.9f);
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 center, Vector2 size, Color color, int layer)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.layer = layer >= 0 ? layer : 0;
        go.transform.localScale = new Vector3(Mathf.Max(0.05f, size.x), Mathf.Max(0.05f, size.y), 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color = color;
        sr.sortingLayerName = GameLayers.InteractiveDownSorting;
        sr.sortingOrder = 10;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
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
