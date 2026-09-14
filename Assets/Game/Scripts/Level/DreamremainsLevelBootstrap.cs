using UnityEngine;

/// <summary>
/// 按 v4 施工图在 Gameplay 里生成关卡（场景A 0–30，场景B 34–68）。
/// 1 格 = 1 Unity 单位；HTML Y 向下，世界 Y = 8 - htmlY。
/// </summary>
public sealed class DreamremainsARouteRecorder : MonoBehaviour
{
    private readonly string[][] gates={
        new[]{"BR-A01-CLIMB2","BR-A01-LOOKOUT","BR-A01-REJOIN"},
        new[]{"PL-A08","LINK-A3-1","LINK-A3-4"},
        new[]{"BR-B01-1","BR-B01-2","BR-B01-5"},
        new[]{"BR-B02-ENTRY","BR-B02-RETURN1","BR-B02-RETURN2"}
    };
    private readonly string[] records={"route_observe","route_risk","route_combat","route_puzzle"};
    private readonly int[] progress=new int[4];
    private TarotResultData previousRun;
    private Vector2 previousPosition;
    private bool tracking;
    void Update()
    {
        var context=GameLoop.Instance!=null?GameLoop.Instance.Context:null;
        var player=context?.Player;
        var run=context?.TarotResult;
        if(player==null || run==null || player.IsDead)
        {Clear();tracking=false;return;}
        Vector2 position=player.transform.position;
        if(run!=previousRun || (tracking && Vector2.Distance(position,previousPosition)>8f))Clear();
        previousRun=run;previousPosition=position;tracking=true;
        for(int route=0;route<gates.Length;route++)
        {
            if(progress[route]>=gates[route].Length)continue;
            string id=gates[route][progress[route]];
            float origin=route<2?0:DreamremainsLevelBootstrap.SceneBOrigin;
            foreach(var platform in route<2?DreamremainsLevelData.SceneA.Platforms:DreamremainsLevelData.SceneB.Platforms)
            {
                if(platform.Id!=id)continue;
                // Feet must actually arrive on the route surface, not just cross its X coordinate.
                if(position.x-origin>=platform.X && position.x-origin<=platform.X+platform.W &&
                    Mathf.Abs(position.y-(8f-platform.Y))<.3f)
                {
                    progress[route]++;
                    if(progress[route]==gates[route].Length)
                    {
                        run.RecordRoute(records[route]);
                        Debug.Log("[RouteComplete] "+records[route]);
                    }
                }
                break;
            }
        }
    }
    private void Clear(){for(int i=0;i<progress.Length;i++)progress[i]=0;}
}

public static class DreamremainsLevelBootstrap
{
    public const float GridHeight = 8f;
    public const float SceneBOrigin = DreamremainsExpandedLayout.ALength + 4f;
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
        DisableLegacyLevel(bridge);

        Transform existing = GameObject.Find(RootName)?.transform;
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        TarotZoneQuery.ResetCurrent();
        LevelRegionRegistry.Clear();

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
        BuildFirstTarotBranchPresentation(sceneA);
        BuildObservationBranchPresentation(sceneA);
        SeparateMountainEdges(sceneA,DreamremainsLevelData.SceneA,0);
        SeparateMountainEdges(sceneB,DreamremainsLevelData.SceneB,SceneBOrigin);
        root.gameObject.AddComponent<DreamremainsARouteRecorder>();
        root.gameObject.AddComponent<FirstEncounterGuide>();
#if UNITY_EDITOR
        root.gameObject.AddComponent<DreamremainsPlaytestControls>();
#endif

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

        LevelRegionRegistry.Register(true, new LevelRegionInfo(0f, DreamremainsExpandedLayout.ALength, 0, 4, 0));
        LevelRegionRegistry.Register(false, new LevelRegionInfo(SceneBOrigin, SceneBOrigin + DreamremainsExpandedLayout.BLength, 4, 5, 0));

        PlayerController player = FindActivePlayer();
        if (player != null && SpawnA != null)
        {
            Vector3 spawnPos = SpawnA.position + Vector3.up * 0.6f;
            player.transform.position = spawnPos;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = spawnPos;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        CameraTargetFollow follow = Object.FindFirstObjectByType<CameraTargetFollow>();
        follow?.FollowCurrentRegion();
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

    private static void BuildFirstTarotBranchPresentation(Transform scene)
    {
        // Authored world coordinates in the existing T1 connecting passage.
        // Decorative assets have no gameplay collider or collectible component.
        Transform art=new GameObject("T1BranchArt").transform;
        art.SetParent(scene,false);
        Decoration(art,"BranchLotus","S01/17.png",1.1f,63.8f,1.1f,.52f,false);
        Atmosphere(art,"BranchMist","Mist FX.png",1.2f,58.2f,.8f,.24f);
        Atmosphere(art,"RejoinMist","Mist FX2.png",1f,64f,1.1f,.2f);
        RouteHint(scene,"T1BranchChoice",50.1f,4.55f,"前方上行：稳台主路\n右下：浮台试炼，前方汇合");
        RouteHint(scene,"T1BranchRest",63.7f,6f,"稳定落脚处\n等待浮台恢复后继续向右");
        RouteHint(scene,"T1BranchRejoin",65.1f,5.2f,"向右上汇合主路");
    }

    private static void RouteHint(Transform scene,string name,float x,float y,string text)
    {
        var hint=new GameObject(name);
        hint.transform.SetParent(scene,false);
        hint.transform.position=ToWorld(x,y,0f);
        hint.AddComponent<LevelNodeFeedback>().ConfigureRoute(text);
    }

    private static void BuildObservationBranchPresentation(Transform scene)
    {
        var art=new GameObject("ObservationBranchArt").transform;
        art.SetParent(scene,false);
        // A small arrangement of existing components, not a new mountain environment.
        AddArt(art,"LookoutColumn","Assets/Game/Art/Scenes/S01/9.png",2.4f,
            new Vector2(24.8f,5.1f),GameLayers.MidgroundSorting,-7);
        Decoration(art,"LowLotus","S01/17.png",.85f,18.8f,1.1f,.45f,false);
        Decoration(art,"ReturnLotus","S01/18.png",.85f,37.7f,2.6f,.43f,false);
        Atmosphere(art,"LookoutMist","Mist FX.png",1.1f,24f,5.7f,.18f);
        Atmosphere(art,"LowWaterMist","Mist FX2.png",.75f,22.5f,.8f,.2f);
        RouteHint(scene,"ObservationChoice",8.8f,5f,"右上：登高观察\n前方：低位通行");
        RouteHint(scene,"ObservationLookout",21.6f,1.3f,"前方逐级落下\n回到牌印前的汇合处");
        RouteHint(scene,"ObservationRejoin",37.2f,4.5f,"继续向右，汇合前往牌印");
    }

    private static void DisableLegacyLevel(GameplaySceneBridge bridge)
    {
        if (bridge == null)
            return;
        // 本分支 Gameplay 序列化的旧地图与正式地图不能同时参与物理、触发和渲染。
        // 只处理当前关卡场景的已审计旧层级；保留 Player、Camera 和管理器。
        foreach (GameObject root in bridge.gameObject.scene.GetRootGameObjects())
        {
            if (root.name == "test_grounds" || root.name == "test_backgrounds")
                root.SetActive(false);
            if (root.name == "SceneArt")
            {
                root.transform.Find("Scene_A")?.gameObject.SetActive(false);
                root.transform.Find("Scene_B")?.gameObject.SetActive(false);
            }
            if (root.name == "SceneRoot")
            {
                root.transform.Find("GameplayLayer/Runtime_Exits/GoalTrigger")?.gameObject.SetActive(false);
                root.transform.Find("GameplayLayer/Runtime_Traps")?.gameObject.SetActive(false);
                root.transform.Find("GameplayLayer/Runtime_Enemies")?.gameObject.SetActive(false);
                // 此容器还承载正式HUD共用的CollectibleTracker，只关闭旧示例奖励。
                Transform oldItems = root.transform.Find("GameplayLayer/Runtime_Collectibles");
                if (oldItems != null)
                    foreach (Transform item in oldItems)
                        item.gameObject.SetActive(false);
            }
        }
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
            if(p.Id.StartsWith("BR-A01-") || p.Id.StartsWith("BR-A02-HIGH") ||
                p.Type=="hidden" || p.MoveX2!=0f || p.Id.StartsWith("TH-") || p.Id.StartsWith("BR-B") || p.Id.StartsWith("E06-"))
            {
                var effector=platform.AddComponent<PlatformEffector2D>();
                effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=180f;
                platform.GetComponent<BoxCollider2D>().usedByEffector=true;
            }
            Sprite visiblePlatform = originX < 1f && p.W < 3.2f && i % 3 == 1
                ? LoadArt("Assets/Game/Art/Scenes/S01/12.png") : platSprite;
            if (originX >= 1f)
                visiblePlatform = OrangeBluePlatformSprite(p);
            AttachSurfaceVisual(platform, visiblePlatform, false, PlatformColor(p.Type, originX < 1f));
            if (originX >= 1f)
            {
                // Opaque content, excluding glow/transparent margins. Physics stays unchanged.
                Rect pixels = p.W >= 3f ? new Rect(4, 1, 249, 61) : new Rect(1, 0, 248, 43);
                if(UseWhiteShortPlatform(p))
                    pixels=p.W<=2.5f?new Rect(13,17,241,78):new Rect(12,18,183,58);
                var surfaceVisual=platform.GetComponentInChildren<LevelDecorationSurface>();
                surfaceVisual?.SetContentRect(pixels);
                surfaceVisual?.EnableOrangeBlueEdge();
            }
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
                if(p.MoveX2!=0f)b.x=originX+p.MoveX2;
                mover.Configure(a, b, p.Id == "LINK-B2-4" ? 1.15f : 1.5f);
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
                high.y=Mathf.Min(high.y,GridHeight-1.6f);
                raise.Configure(zone, center, high);
            }
        }


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
            new Vector3(originX + (originX < 1f ? DreamremainsExpandedLayout.ALength : DreamremainsExpandedLayout.BLength)*.5f, -1.6f, 0f),
            new Vector2((originX < 1f ? DreamremainsExpandedLayout.ALength : DreamremainsExpandedLayout.BLength)+4f, 1.2f),
            new Color(0.6f, 0.1f, 0.1f, 0.15f),
            trigger);
        fall.GetComponent<Collider2D>().isTrigger = true;
        fall.GetComponent<SpriteRenderer>().enabled = false;
        fall.AddComponent<KillZone>();

        for (int i = 0; i < spec.Spikes.Length; i++)
        {
            DreamremainsLevelData.SpikeSpec s = spec.Spikes[i];
            Vector2 surface = ToWorld(s.X + s.W * 0.5f, s.Y, originX);
            if (TrySupport(spec, s.X + s.W * 0.5f, s.Y, s.W, out var spikeSupport))
                surface.y = GridHeight - spikeSupport.Y;
            else Debug.LogWarning("[Placement] No fixed support under spike " + s.Id);
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
            if(point.Kind=="fold")continue; // E06 now reveals physical carriers, never teleports past a seal.
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
                var checkpoint=node.AddComponent<CheckpointInteractable>();
                checkpoint.Configure(point.Id, "记录复活点", 1.4f);
                if(TrySupport(spec,point.X,point.Y,.2f,out var bed))
                    checkpoint.ConfigureSupport(parent.Find(bed.Id)?.GetComponent<BoxCollider2D>());
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
                circle.radius = .32f;
                AddDreamCore(node.transform, 0.42f, Vector2.zero);
                node.AddComponent<CollectibleItem>().ConfigureRegion(originX < 1f);
            }
            else if(point.Kind=="supply")
            {
                node.layer=trigger>=0?trigger:0;
                var area=node.AddComponent<CircleCollider2D>();area.isTrigger=true;area.radius=.75f;
                AddArt(node.transform,"SupplyChest","Assets/Game/Art/Scenes/S01/25.png",.75f,new Vector2(0,.1f));
                node.AddComponent<DreamSupplyChest>();
                node.AddComponent<LevelNodeFeedback>().Configure("supply",0);
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
                bool hasSupport = TrySupport(spec, point.X, point.Y, .56f, out var enemySupport);
                if (hasSupport)
                    node.transform.position = new Vector3(pos.x, GridHeight-enemySupport.Y+.28f,0);
                else Debug.LogWarning("[Placement] No fixed support under enemy " + point.Id);
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
                    new Vector2((hasSupport ? Mathf.Max(point.PatrolX1,enemySupport.X+.3f) : point.PatrolX1) - point.X, 0f),
                    new Vector2((hasSupport ? Mathf.Min(point.PatrolX2,enemySupport.X+enemySupport.W-.3f) : point.PatrolX2) - point.X, 0f));
            }

            DecoratePoint(node, point, originX < 1f);
        }

        // 收集物创建完后再绑定隐藏路，避免平台隐藏了、碎片仍悬在空中。
        GroupHidden(parent, spec.HiddenSlot, spec.HiddenIds);
        GroupHidden(parent, spec.HiddenSlot2, spec.HiddenIds2);
        {
            string prefix=originX<1f?"E06-A-":"E06-B-";
            var shortcut=new GameObject(originX<1f?"E06_A_Shortcut":"E06_B_Shortcut");shortcut.transform.SetParent(parent,false);
            var targets=new System.Collections.Generic.List<GameObject>();
            foreach(Transform child in parent)
                if(child.name.StartsWith(prefix))targets.Add(child.gameObject);
            shortcut.AddComponent<HiddenPathController>().ConfigureEffect(TarotEffectId.E06,targets.ToArray());
        }
        if(originX>=1f)
        {
            var controller=new GameObject("B_MechanismReturn");controller.transform.SetParent(parent,false);
            var targets=new System.Collections.Generic.List<GameObject>();
            foreach(Transform child in parent)
                if(child.name.StartsWith("BR-B02-RETURN"))targets.Add(child.gameObject);
            controller.AddComponent<HiddenPathController>().ConfigureMechanism("TRG-B02",targets.ToArray());
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
            SpriteRenderer current = AddArt(volume.transform, "AirCurrent", "Assets/Game/Art/Effects/Wave FX.png", 0.32f, Vector2.zero,
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

    private static bool TrySupport(DreamremainsLevelData.SceneSpec spec,float x,float y,float width,
        out DreamremainsLevelData.PlatformSpec support)
    {
        support=default;
        float best=.85f;
        foreach(var p in spec.Platforms)
        {
            if(p.Type!="normal" || x-width*.5f<p.X || x+width*.5f>p.X+p.W)continue;
            float distance=Mathf.Abs(p.Y-y);
            if(distance>=best)continue;
            best=distance;support=p;
        }
        return best<.85f;
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
        // Preserve both scenes' source artwork; behaviour supplies temporary warnings only.
        return Color.white;
    }

    private static Sprite PlatformSprite(DreamremainsLevelData.PlatformSpec p, bool sceneA)
    {
        if (sceneA)
            return LoadArt(p.W >= 3.2f ? "Assets/Game/Art/Scenes/S01/13.png" : "Assets/Game/Art/Scenes/S01/10.png");
        // Keep the original root scale and collider dimensions. Only the Visual changes skin.
        return LoadArt(p.W >= 3f ? "Assets/Game/Art/Scenes/S02/15.png" : "Assets/Game/Art/Scenes/S02/16.png");
    }

    internal static Sprite OrangeBluePlatformSprite(DreamremainsLevelData.PlatformSpec p)
    {
        if(UseWhiteShortPlatform(p))
            return LoadArt(p.W<=2.5f?"Assets/Game/Art/Scenes/S02/6.png":"Assets/Game/Art/Scenes/S02/8.png");
        return LoadArt(p.W >= 3f ? "Assets/Game/Art/Scenes/S02/5.png" : "Assets/Game/Art/Scenes/S02/19.png");
    }
    private static bool UseWhiteShortPlatform(DreamremainsLevelData.PlatformSpec p)
        => p.W<=3.5f && p.Y<=3.8f;

    internal static void SpawnBackdrop(Transform parent, float originX, string path)
    {
        float regionMaxX = originX + (originX < 1f ? DreamremainsExpandedLayout.ALength : DreamremainsExpandedLayout.BLength);
        SpawnBackdrop(parent, originX, regionMaxX, path, originX < 1f, SceneBOrigin - 2f);
    }

    internal static void SpawnBackdrop(Transform parent, float regionMinX, float regionMaxX, string path,
        bool isSceneA, float boundaryX)
    {
        Sprite sprite = LoadArt(path);
        if (sprite == null)
            return;

        GameObject go = new GameObject("Backdrop");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3((regionMinX + regionMaxX) * 0.5f, 4f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = GameLayers.BackgroundSorting;
        sr.sortingOrder = -40;
        sr.color = Color.white;
        go.AddComponent<LevelDecorationBackdrop>().Configure(sr, regionMinX, regionMaxX, isSceneA, boundaryX);
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
                AddArt(root, "Tarot", "Assets/Game/Art/UI/Tarot/card-back.png", 0.9f, new Vector2(0f, 1.45f), GameLayers.InteractiveUpSorting, 40);
                break;
            case "cp":
                return; // Invisible automatic checkpoint: no pole, label or collectible marker.
            case "door":
                AddVisual(root, "Visual", LoadArt(sceneA ? "Assets/Game/Art/Scenes/S01/9.png" : "Assets/Game/Art/Scenes/S02/24.png"),
                    new Vector2(0.4f, 2.2f), Vector2.zero, GameLayers.InteractiveDownSorting, 12);
                break;
            case "trg":
                AddArt(root, "MechanismMarker", "Assets/Game/Art/Scenes/S01/22.png", 0.24f, new Vector2(0f, -0.18f));
                break;
            case "fold":
                AddArt(root, "FoldColumns", "Assets/Game/Art/Scenes/S01/8.png", point.Id.StartsWith("FE-") ? 0.8f : 0.5f, Vector2.zero);
                break;
            case "trans":
                break; // A to B is a region transition, with no doorway artwork.
            case "end":
                // Original black/white/gold doorway, already sliced as Elements01_3.
                float ground = root.position.y - .4f;
                float closest = float.PositiveInfinity;
                foreach (var bed in (sceneA ? DreamremainsLevelData.SceneA : DreamremainsLevelData.SceneB).Platforms)
                {
                    if (point.X < bed.X || point.X > bed.X + bed.W) continue;
                    float distance = Mathf.Abs(bed.Y - point.Y);
                    if (distance >= closest) continue;
                    closest = distance; ground = GridHeight - bed.Y;
                }
                // Native slice has 26 px beneath the visible door; align its base to the landing.
                AddArt(root, "OriginalSettlementGate", "Assets/Game/Art/source/old/Elements01.png",
                    3.2f, new Vector2(0f, ground - root.position.y + 1.6f - 26f * 3.2f / 422f),
                    GameLayers.InteractiveDownSorting, 9);
                break;
            default:
                return;
        }
        node.AddComponent<LevelDecorationNode>().Configure(point.Kind, point.Slot, point.Id.StartsWith("FA-"));
        node.AddComponent<LevelNodeFeedback>().Configure(point.Kind, point.Slot);
    }

    private static void SpawnDecorations(Transform scene, bool sceneA)
    {
        if(sceneA){BuildAScrollArt(scene);return;}
        BuildBScrollArt(scene);return;

    }

    private static void BuildBScrollArt(Transform scene)
    {
        var scroll=new GameObject("B_Scroll_Compositions").transform;
        scroll.SetParent(scene,false);
        scroll.localPosition=new Vector3(SceneBOrigin,0,0);
        float[] cloudCentres={10,32,55,78,101,125,151};
        for(int i=0;i<cloudCentres.Length;i++)
        {
            string path=i%3==2?"Assets/Game/Art/source/FX-Mist.png":
                "Assets/Game/Art/Effects/"+(i%2==0?"Mist FX.png":"Mist FX2.png");
            var cloud=AddArt(scroll,"CloudBank_"+i,path,i%2==0?7.5f:6.8f,
                new Vector2(cloudCentres[i],i%2==0?4.1f:4.5f),GameLayers.MidgroundSorting,-35);
            if(cloud!=null)
            {
                // These originals already contain very soft, low-alpha ink edges.
                cloud.color=new Color(1f,1f,1f,.85f);cloud.flipX=i%2!=0;
                cloud.sharedMaterial=Resources.Load<Material>("DreamremainsCloudSoftBorder");
            }
        }
        string[] anchors={"LINK-B1-1","LINK-B1-4","LINK-B2-1","LINK-B2-5",
            "LINK-B3-1","LINK-B3-4","LINK-B4-1","LINK-B4-5"};
        for(int i=0;i<anchors.Length;i++)
        foreach(var p in DreamremainsLevelData.SceneB.Platforms)
        {
            if(p.Id!=anchors[i])continue;
            float floor=8f-p.Y;
            var panel=new GameObject("Panel_B_"+(i+1).ToString("00")+"_"+p.Id).transform;
            panel.SetParent(scroll,false);panel.localPosition=new Vector3(p.X,0,0);
            // Mount scenery around a real landing, not a common off-screen baseline.
            ScrollArt(panel,"DistantPeak","19.png",i%2==0?2.8f:2.1f,new Vector2(-2.8f,1.25f),.25f,GameLayers.MidgroundSorting,-30,false);
            bool slope=i==2||i==6;
            ScrollArt(panel,"InkSlope",slope?"21.png":"15.png",slope?3.3f:2.6f,
                new Vector2(slope?5.5f:4.2f,slope?1.2f:1.05f),.4f,GameLayers.MidgroundSorting,-17,false);
            BScrollArt(panel,"LandingReeds","21.png",.65f,new Vector2(p.W*.5f,floor+.23f),.65f,-3);
            BScrollArt(panel,"UnderwaterReeds","11.png",1.15f,new Vector2(2.4f,.5f),.56f,-13);
            Atmosphere(panel,"Waterline","Wave FX.png",.8f,3.3f,.4f,.37f);
            if(i%2==0)
            {
                ScrollArt(panel,"Lotus","18.png",1.15f,new Vector2(p.W+1.5f,Mathf.Max(.7f,floor-.8f)),.64f,GameLayers.MidgroundSorting,-4,false);
                BScrollArt(panel,"GoldCurrent","10.png",.7f,new Vector2(1,Mathf.Min(6.8f,floor+1.7f)),.32f,-22);
            }
            else BScrollArt(panel,"HangingGold","24.png",1.8f,new Vector2(p.W*.45f,floor-1.05f),.5f,-8);
            break;
        }
        BScrollArt(scroll,"Moon","3.png",.75f,new Vector2(112,7.05f),.8f,-25);
        BScrollArt(scroll,"EntranceCurtain","24.png",3.1f,new Vector2(3,5.1f),.6f,-8);
        BuildLowerBanks(scroll,DreamremainsLevelData.SceneB,false);
    }
    private static void BuildSolidPillar(Transform parent,string id,Vector2 center,float width,float height)
    {
        var root=new GameObject(id);root.transform.SetParent(parent,false);root.transform.localPosition=center;
        int ground=LayerMask.NameToLayer(GameLayers.Ground);root.layer=ground>=0?ground:0;
        var solid=root.AddComponent<BoxCollider2D>();solid.size=new Vector2(width,height);
        var art=AddArt(root.transform,"PillarVisual","Assets/Game/Art/Scenes/S01/9.png",height,
            Vector2.zero,GameLayers.InteractiveDownSorting,2);
        if(art!=null)
        {
            var scale=art.transform.localScale;
            scale.x=width/Mathf.Max(.001f,art.sprite.bounds.size.x);art.transform.localScale=scale;
            art.color=new Color(1,1,1,.8f);
        }
        else root.SetActive(false); // Never create an invisible obstacle when artwork is unavailable.
    }
    private static void SeparateMountainEdges(Transform scene,DreamremainsLevelData.SceneSpec spec,float origin)
    {
        foreach(var art in scene.GetComponentsInChildren<SpriteRenderer>())
        {
            string id=art.name;
            if(!(id.Contains("Silhouette")||id.Contains("Peak")||id.Contains("InkSlope")||id.Contains("FarInk")))continue;
            Bounds bounds=art.bounds;
            foreach(var p in spec.Platforms)
            {
                if(bounds.max.x<origin+p.X||bounds.min.x>origin+p.X+p.W)continue;
                if(Mathf.Abs(bounds.max.y-(8-p.Y))>.65f)continue;
                art.transform.position+=Vector3.down*.8f;
                break;
            }
            // Keep mountain texture behind readable landing silhouettes.
            var colour=art.color;colour.a=Mathf.Min(colour.a,.4f);art.color=colour;
        }
    }
    private static void BScrollArt(Transform parent,string name,string file,float height,Vector2 position,float alpha,int order)
    {
        var art=AddArt(parent,name,"Assets/Game/Art/Scenes/S02/"+file,height,position,GameLayers.MidgroundSorting,order);
        if(art!=null)art.color=new Color(1,1,1,alpha);
    }

    // Authored scenic panels in expanded world coordinates. Children move as a composition;
    // their internal positions are never rolled independently by a random generator.
    private static void BuildAScrollArt(Transform scene)
    {
        var scroll=new GameObject("A_Scroll_Compositions").transform;
        scroll.SetParent(scene,false);
        // x, mountain height, mountain centre Y, lotus x offset, lotus centre Y.
        // Deliberately varied silhouettes; the playable surfaces remain untouched.
        float[,] panels={
            {4,3.6f,1.35f,4.5f,.8f}, {23,4.6f,1.65f,5.8f,.75f},
            {44,3.2f,1.45f,5.5f,2.05f}, {65,4.1f,1.6f,5.7f,1.35f},
            {85,3.5f,1.3f,6.3f,.7f}, {105,4.3f,1.55f,6.2f,1.15f},
            {126,3.3f,1.3f,7.5f,.25f}, {148,4.2f,1.55f,7.2f,.25f}
        };
        for(int i=0;i<panels.GetLength(0);i++)
        {
            var panel=new GameObject("Panel_A_"+(i+1).ToString("00")+"_Fixed").transform;
            panel.SetParent(scroll,false);panel.localPosition=new Vector3(panels[i,0],0,0);
            bool slanted=i==1||i==3||i==5||i==7;
            ScrollArt(panel,"FarInk",i%2==0?"19.png":"15.png",2.4f,
                new Vector2(-2.1f,1.3f),.27f,GameLayers.MidgroundSorting,-30,i%2!=0);
            ScrollArt(panel,"MiddleSilhouette",slanted?"21.png":"15.png",panels[i,1],
                new Vector2(1.1f,panels[i,2]),.53f,GameLayers.MidgroundSorting,-15,false);
            ScrollArt(panel,"LotusFocus",i%2==0?"18.png":"17.png",i==2?1.15f:1.45f,
                new Vector2(panels[i,3],panels[i,4]),.7f,GameLayers.MidgroundSorting,-4,false);
            Atmosphere(panel,"WaterVeil",i%2==0?"Mist FX.png":"Mist FX2.png",1.1f,3.2f,.55f,.28f);
            // Foreground stays at the lower frame edge, below the lowest playable surfaces.
            ScrollArt(panel,"ForegroundLotusStems","16.png",.85f,
                new Vector2(-3.5f,-.23f),.42f,GameLayers.ForegroundSorting,0,i%2==0);
        }
        // Sparse high silhouettes support high-route framing without filling every gap.
        ScrollArt(scroll,"FishAtEntry","7.png",.5f,new Vector2(8.7f,1.65f),.5f,GameLayers.MidgroundSorting,-3,false);
        ScrollArt(scroll,"FishBelowT1Hidden","20.png",.65f,new Vector2(75,3.1f),.32f,GameLayers.MidgroundSorting,-12,true);
        ScrollArt(scroll,"FishBeforeT2","7.png",.42f,new Vector2(117,1.4f),.42f,GameLayers.MidgroundSorting,-3,false);
        // Light pillars hang down from authored platform undersides. They are scenery, not doors.
        ScrollPillar(scroll,"PillarAtT1","PL-A05",2.3f);
        ScrollPillar(scroll,"PillarAtHighRoute","PL-A07",2.8f);
        ScrollPillar(scroll,"PillarBeforeT2","LINK-A3-6",2.4f);
        ScrollPillar(scroll,"PillarAtExit","PL-A12",2f);
        // A-only supports below low fixed landings; never an extra obstacle across an accepted route.
        foreach(var p in DreamremainsLevelData.SceneA.Platforms)
        {
            if(p.Id!="LINK-A1-4" && p.Id!="LINK-A2-5")continue;
            float h=p.Id=="LINK-A1-4"?.65f:.9f;
            BuildSolidPillar(scroll,"SupportPillar_"+p.Id,
                new Vector2(p.X+p.W*.8f,8-p.Y-.4f-h*.5f),.16f,h);
        }
    }

    private static void BuildLowerBanks(Transform scroll,DreamremainsLevelData.SceneSpec spec,bool a)
    {
        string[] beds=a?new[]{"LINK-A1-4","PL-A04","LINK-A2-6","LINK-A3-1","LINK-A4-6","TH-A2-LINK4"}:
            new[]{"BR-B01-2","LINK-B2-3","LINK-B2-6","LINK-B3-1","LINK-B4-5","TH-B2-LINK4"};
        foreach(string id in beds)
        foreach(var p in spec.Platforms)
        {
            if(p.Id!=id)continue;
            var bank=new GameObject("LowerBank_"+id).transform;bank.SetParent(scroll,false);
            float floor=8-p.Y;
            bank.localPosition=new Vector3(p.X,0,0);
            // A scenic shore at the playable height, not below the camera's world baseline.
            Atmosphere(bank,"ShoreWater","Wave FX.png",1.1f,p.W*.5f,floor-.42f,.8f);
            Atmosphere(bank,"ShoreMist","Mist FX2.png",1.05f,p.W*.6f,floor-.12f,.48f);
            // Existing scroll panels already own the lotus focal points; do not duplicate them.
            if(!a)BScrollArt(bank,"ShoreReeds","21.png",.48f,new Vector2(p.W*.5f,floor-.1f),.82f,-3);
            else ScrollArt(bank,"ShoreStems","16.png",.7f,new Vector2(-.55f,floor-.1f),.65f,GameLayers.MidgroundSorting,-5,false);
            break;
        }
    }

    private static void ScrollArt(Transform parent,string name,string file,float height,Vector2 centre,
        float alpha,string layer,int order,bool flip)
    {
        var art=AddArt(parent,name,"Assets/Game/Art/Scenes/S01/"+file,height,centre,layer,order);
        if(art==null)return;
        art.color=new Color(1,1,1,alpha);art.flipX=flip;
    }

    private static void ScrollPillar(Transform parent,string name,string supportId,float height)
    {
        foreach(var platform in DreamremainsLevelData.SceneA.Platforms)
        {
            if(platform.Id!=supportId)continue;
            ScrollArt(parent,name,"8.png",height,
                new Vector2(platform.X+platform.W*.7f,8-platform.Y-.4f-height*.5f),
                .36f,GameLayers.MidgroundSorting,-9,false);
            break;
        }
    }

    private static void Atmosphere(Transform parent, string name, string file, float height, float x, float y, float alpha)
    {
        var sr = AddArt(parent, name, "Assets/Game/Art/Effects/" + file, height,
            new Vector2(x, y), GameLayers.MidgroundSorting, -8);
        if (sr != null)
        {
            sr.color = new Color(1f, 1f, 1f, alpha);
            sr.sharedMaterial=Resources.Load<Material>("DreamremainsCloudSoftBorder");
        }
    }

    internal static void Decoration(Transform parent, string name, string file, float height, float x, float y, float alpha, bool flip)
    {
        SpriteRenderer sr = AddArt(parent, name, "Assets/Game/Art/Scenes/" + file, height, new Vector2(x, y), GameLayers.MidgroundSorting, -5);
        if (sr == null)
            return;
        sr.color = new Color(1f, 1f, 1f, alpha);
        sr.flipX = flip;
    }

    internal static void Decoration(Transform parent, string name, string file, float height, float x, float y,
        float alpha, bool flip, System.Random random, float cropFraction, bool flipY)
    {
        Decoration(parent, name, file, height, x, y, alpha, flip);
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

#if UNITY_EDITOR
// Editor-only controls: restart a fresh Play session to avoid retaining old card/route state.
public sealed class DreamremainsPlaytestControls : MonoBehaviour
{
    public static System.Action<int> Requested;
    public static bool ShowRouteNotes;
    bool expanded;
    void OnGUI()
    {
        if(GameLoop.Instance?.Context?.StateMachine.CurrentStateType!=GameStateType.Playing)return;
        float w=Mathf.Min(280,Screen.width-24);
        float bottom=Mathf.Max(0,Screen.height-44);
        if(GUI.Button(new Rect(12,bottom,w,32),expanded?"Close playtest / 收起":"Playtest / 试玩切换"))expanded=!expanded;
        if(!expanded)return;
        GUI.Box(new Rect(8,bottom-270,w+8,268),"Restart test / 重新开始本次试玩");
        ShowRouteNotes=GUI.Toggle(new Rect(12,bottom-243,w,28),ShowRouteNotes,"显示路线设计说明（仅试玩）");
        Button(5,"B area / 带牌组试玩 B 区",bottom-198,w);
        Button(4,"A split / 第一次分流",bottom-160,w);
        Button(1,"T1 Normal / 普通浮台",bottom-122,w);
        Button(2,"T1 E12 / 稳定浮台",bottom-84,w);
        Button(3,"T1 E18 / 加速消失",bottom-46,w);
    }
    void Button(int mode,string text,float y,float width)
    {
        if(GUI.Button(new Rect(12,y,width,34),text))
        {
            expanded=false;
            if(Requested!=null)Requested(mode);
            else Debug.LogWarning("试玩入口尚未就绪，请等待编辑器编译。");
        }
    }
}
#endif
