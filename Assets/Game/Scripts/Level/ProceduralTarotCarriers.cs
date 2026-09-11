using UnityEngine;

/// <summary>
/// 按《塔罗牌机制组合_关卡实现规则》把 AW/HP/SG/D1–D3/折叠/隐藏路铺进程序化 A/B 区。
/// 全部放在主线外侧：无牌也能跳过，打开后只给捷径或奖励。
/// </summary>
public partial class ProceduralLevelGenerator
{
    private void SpawnTarotCarriers(Transform terrain, Transform hazards, Transform tarotGroup, Transform items)
    {
        if (platforms.Count < 10)
            return;

        SpawnFallKill(hazards);

        int aw = Mathf.Clamp(3, 2, platforms.Count - 3);
        int hp = Mathf.Clamp(8, 3, platforms.Count - 3);
        int sg = Mathf.Clamp(6, 3, platforms.Count - 3);
        int fold = Mathf.Clamp(9, 3, platforms.Count - 4);
        int hiddenA = Mathf.Clamp(10, 4, platforms.Count - 3);
        int hiddenB = Mathf.Clamp(13, 5, platforms.Count - 3);

        SpawnAirWalk(tarotGroup, aw);
        SpawnRaiseIsland(terrain, items, hp);
        SpawnSpikePit(hazards, sg);

        if (isSceneA)
        {
            SpawnDoorBranch(terrain, items, "D1", PickIndex(0.28f, 3, 5), 2.5f);
            SpawnDoorBranch(terrain, items, "D2", PickIndex(0.52f, 4, 4), 1.45f);
            SpawnFold(tarotGroup, fold, Mathf.Min(fold + 3, platforms.Count - 3));
            SpawnHiddenPath(terrain, items, 0, hiddenA);
            SpawnHiddenPath(terrain, items, 1, hiddenB);
        }
        else
        {
            SpawnDoorBranch(terrain, items, "D3", PickIndex(0.42f, 3, 5), 2.2f);
            SpawnFold(tarotGroup, fold, Mathf.Min(fold + 3, platforms.Count - 3));
            SpawnHiddenPath(terrain, items, 2, hiddenA);
            SpawnHiddenPath(terrain, items, 3, hiddenB);
        }
    }

    private int PickIndex(float t, int padLeft, int padRight)
    {
        int min = Mathf.Clamp(padLeft, 1, platforms.Count - 2);
        int max = Mathf.Clamp(platforms.Count - 1 - padRight, min, platforms.Count - 2);
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(min, max, t)), min, max);
    }

    private int ZoneFor(int listIndex)
    {
        return ZoneAt(Mathf.Max(0, listIndex - 1));
    }

    private void SpawnFallKill(Transform hazards)
    {
        float width = Mathf.Max(8f, RegionMaxX - RegionMinX + 8f);
        GameObject zone = new GameObject(isSceneA ? "H-FALL-A" : "H-FALL-B");
        zone.transform.SetParent(hazards);
        zone.transform.position = new Vector3((RegionMinX + RegionMaxX) * 0.5f, -8f, 0f);
        zone.layer = triggerLayer >= 0 ? triggerLayer : 0;
        BoxCollider2D box = zone.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width, 2.4f);
        zone.AddComponent<KillZone>();
    }

    private void SpawnAirWalk(Transform parent, int index)
    {
        if (index < 0 || index + 1 >= platforms.Count)
            return;

        Platform a = platforms[index];
        Platform b = platforms[index + 1];
        float left = a.center.x + a.width * 0.5f;
        float right = b.center.x - b.width * 0.5f;
        float width = Mathf.Max(1.2f, right - left);
        float y = Mathf.Max(a.top, b.top) + 0.35f;
        Vector3 center = new Vector3((left + right) * 0.5f, y, 0f);

        GameObject volume = new GameObject(isSceneA ? "AW-A01" : "AW-B01");
        volume.transform.SetParent(parent);
        volume.transform.position = center;
        volume.layer = triggerLayer >= 0 ? triggerLayer : 0;
        BoxCollider2D box = volume.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width, 0.9f);
        volume.AddComponent<AirWalkVolume>().Configure(ZoneFor(index));
        SpriteRenderer current = DreamremainsLevelBootstrap.AddArt(
            volume.transform, "AirCurrent", "Assets/Game/Art/Scenes/S02/7.png",
            0.32f, Vector2.zero, GameLayers.BackgroundSorting, -4);
        if (current != null)
            current.color = new Color(1f, 1f, 1f, 0.22f);
    }

    private void SpawnRaiseIsland(Transform terrain, Transform items, int index)
    {
        if (index < 1 || index >= platforms.Count)
            return;

        Platform p = platforms[index];
        Vector3 restTop = SideLedge(p, 1.4f, 0.7f, 2.4f);
        GameObject island = MakeSidePlatform(terrain, isSceneA ? "HP-A01" : "HP-B01", restTop, 2.4f);
        Rigidbody2D body = island.GetComponent<Rigidbody2D>();
        if (body == null)
            body = island.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        Vector3 rest = island.transform.position;
        island.AddComponent<RaisePlatform>().Configure(ZoneFor(index), rest, rest + Vector3.up * 1.6f);
        SpawnBonusCollectible(items, new Vector2(restTop.x, restTop.y + 1.05f));
    }

    private void SpawnSpikePit(Transform hazards, int index)
    {
        if (index < 0 || index + 1 >= platforms.Count)
            return;

        Platform a = platforms[index];
        float x = a.center.x;
        float y = a.top - 1.7f;
        Sprite spikeSprite = DreamremainsLevelBootstrap.LoadArt("Assets/Game/Art/Scenes/S01/11.png");
        int layer = mechanismLayer >= 0 ? mechanismLayer : 0;
        GameObject spike = DreamremainsLevelBootstrap.CreateBox(
            hazards, isSceneA ? "SG-A01" : "SG-B01",
            new Vector3(x, y, 0f), new Vector2(1.3f, 0.5f), Color.white, layer, spikeSprite);
        DreamremainsLevelBootstrap.AttachSurfaceVisual(spike, spikeSprite, true, Color.white);
        Collider2D col = spike.GetComponent<Collider2D>();
        col.isTrigger = true;
        int zone = ZoneFor(index);
        spike.AddComponent<SpikeTrap>().ConfigureZone(zone);
        spike.AddComponent<SpikeGrowth>().Configure(zone, 0.7f);
    }

    private void SpawnDoorBranch(Transform terrain, Transform items, string doorId, int index, float heightOffset)
    {
        if (index < 1 || index >= platforms.Count)
            return;

        Platform p = platforms[index];
        Vector3 extra = SideLedge(p, 1.5f, Mathf.Max(0.55f, heightOffset * 0.35f), 2.8f);
        extra.y = Mathf.Clamp(extra.y, minY, maxY + 2f);
        MakeSidePlatform(terrain, doorId + "-PL", extra, 2.8f);

        GameObject door = new GameObject(doorId);
        door.transform.SetParent(terrain);
        door.transform.position = new Vector3(extra.x, extra.y + 1.1f, 0f);
        door.layer = groundLayer >= 0 ? groundLayer : 0;
        BoxCollider2D block = door.AddComponent<BoxCollider2D>();
        block.size = new Vector2(0.45f, 2.2f);
        block.isTrigger = false;
        SpriteRenderer visual = DreamremainsLevelBootstrap.AddVisual(
            door.transform, "Visual", DreamremainsLevelBootstrap.LoadArt("Assets/Game/Art/Scenes/S01/9.png"),
            new Vector2(0.4f, 2.2f), Vector2.zero, GameLayers.InteractiveDownSorting, 12);
        if (visual != null)
            visual.color = new Color(0.16f, 0.14f, 0.18f, 0.95f);
        door.AddComponent<DoorInteractable>().Configure(doorId, false, "Door_Open");

        SpawnBonusCollectible(items, new Vector2(extra.x + 0.95f, extra.y + 1.05f));
    }

    private void SpawnFold(Transform parent, int enterIndex, int exitIndex)
    {
        if (enterIndex < 1 || exitIndex <= enterIndex || exitIndex >= platforms.Count)
            return;

        Platform enterPlat = platforms[enterIndex];
        Platform exitPlat = platforms[exitIndex];

        GameObject exit = new GameObject(isSceneA ? "FA-A01" : "FA-B01");
        exit.transform.SetParent(parent);
        exit.transform.position = new Vector3(exitPlat.center.x, exitPlat.top + 0.7f, 0f);

        GameObject enter = new GameObject(isSceneA ? "FE-A01" : "FE-B01");
        enter.transform.SetParent(parent);
        enter.transform.position = new Vector3(enterPlat.center.x, enterPlat.top + 1.2f, 0f);
        enter.layer = triggerLayer >= 0 ? triggerLayer : 0;
        enter.AddComponent<CircleCollider2D>();
        FoldPortal portal = enter.AddComponent<FoldPortal>();
        portal.ConfigureExit(exit.transform);
        DreamremainsLevelBootstrap.AddArt(enter.transform, "FoldEcho",
            "Assets/Game/Art/Scenes/S01/5.png", 0.55f, Vector2.zero);
    }

    private void SpawnHiddenPath(Transform terrain, Transform items, int slot, int index)
    {
        if (index < 2 || index + 1 >= platforms.Count)
            return;

        Platform p = platforms[index];
        Vector3 a = SideLedge(p, 1.3f, 0.55f, 2.2f);
        Vector3 b = new Vector3(a.x + 2.2f * 0.5f + 0.5f + 1.1f, a.y + 0.35f, 0f);
        a.y = Mathf.Clamp(a.y, minY, maxY + 2.2f);
        b.y = Mathf.Clamp(b.y, minY, maxY + 2.2f);

        GameObject plA = MakeSidePlatform(terrain, "TH-" + (slot + 1) + "-A", a, 2.2f);
        GameObject plB = MakeSidePlatform(terrain, "TH-" + (slot + 1) + "-B", b, 2.2f);
        GameObject loot = SpawnBonusCollectible(items, new Vector2(b.x, b.y + 1.05f));

        GameObject group = new GameObject("Hidden_T" + (slot + 1));
        group.transform.SetParent(terrain);
        HiddenPathController controller = group.AddComponent<HiddenPathController>();
        controller.Configure(slot, new[] { plA, plB, loot });
    }

    private static Vector3 SideLedge(Platform p, float gap, float dy, float width)
    {
        float x = p.center.x + p.width * 0.5f + gap + width * 0.5f;
        return new Vector3(x, p.top + dy, 0f);
    }

    private GameObject MakeSidePlatform(Transform parent, string id, Vector3 topCenter, float width)
    {
        Vector3 boxCenter = new Vector3(topCenter.x, topCenter.y - PlatformHeight * 0.5f, 0f);
        Sprite sprite = DreamremainsLevelBootstrap.LoadArt(width >= 4f ? LongPlatformArt : ShortPlatformArt);
        GameObject platform = DreamremainsLevelBootstrap.CreateBox(
            parent, id, boxCenter, new Vector2(width, PlatformHeight), Color.white, groundLayer, sprite);
        DreamremainsLevelBootstrap.AttachSurfaceVisual(platform, sprite, false, Color.white);
        return platform;
    }

    private GameObject SpawnBonusCollectible(Transform parent, Vector2 pos)
    {
        GameObject node = new GameObject("IT-" + Mathf.RoundToInt(pos.x * 10f));
        node.transform.SetParent(parent);
        node.transform.position = pos;
        node.layer = collectibleLayer >= 0 ? collectibleLayer : 0;
        CircleCollider2D circle = node.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.7f;
        DreamremainsLevelBootstrap.AddDreamCore(node.transform, 0.42f, Vector2.zero);
        node.AddComponent<CollectibleItem>().ConfigureRegion(isSceneA);
        return node;
    }
}
