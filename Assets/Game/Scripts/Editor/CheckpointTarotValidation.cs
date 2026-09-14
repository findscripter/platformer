#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Actual Play component test with diagnostic repositioning; not a route playthrough.</summary>
[InitializeOnLoad]
public static class CheckpointTarotValidation
{
    const string Key="Dreamremains.CheckpointTarotQA";
    static int phase;
    static double deadline,next;
    static Transform saved,tarot;
    static Vector3 faceScale;
    static bool sawFlip;
    static Keyboard keyboard;
    static bool sceneB;
    static CheckpointTarotValidation(){EditorApplication.update+=Tick;}
    [MenuItem("Dreamremains/QA/Checkpoint and Tarot Play")]
    public static void Run()
    {
        Directory.CreateDirectory("QA");File.WriteAllText("QA/checkpoint-tarot.txt","Actual Play test; diagnostic relocation, not route completion.\n");
        phase=0;deadline=EditorApplication.timeSinceStartup+120;sawFlip=false;
        SessionState.SetBool(Key,true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
    }
    [MenuItem("Dreamremains/QA/Checkpoint and Tarot B Play")]
    public static void RunB(){Run();sceneB=true;SessionState.SetBool(Key+".B",true);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+120;
        if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL timeout");return;}
        if(!EditorApplication.isPlaying || !Application.isPlaying)return;
        Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();
        var ctx=GameLoop.Instance?.Context;
        if(ctx?.Player==null)return;
        try
        {
            double now=EditorApplication.timeSinceStartup;
            if(phase==0 && ctx.StateMachine.CurrentStateType==GameStateType.Playing)
            {
                sceneB=SessionState.GetBool(Key+".B",false);
                var deck=TarotCatalog.LoadDeck();if(deck.Length<4)throw new Exception("deck missing");
                ctx.TarotResult=new TarotResultData();ctx.TarotResult.BeginRun(new[]{deck[0],deck[1],deck[2],deck[3]},new bool[4]);
                // This targeted checkpoint test starts at T2/T4; prepare preceding phases explicitly.
                for(int prior=0;prior<(sceneB?3:1);prior++)ctx.TarotResult.ActivateNode(prior);
                var root=GameObject.Find(DreamremainsLevelBootstrap.RootName).transform;
                saved=root.Find(sceneB?"Scene_B/CP-B02":"Scene_A/CP-A01");tarot=root.Find(sceneB?"Scene_B/T4":"Scene_A/T2");
                ctx.Player.Respawn(saved.GetComponent<CheckpointInteractable>().SafePosition(ctx.Player));
                next=now+1.5;phase=1;
            }
            else if(phase==1 && now>next)
            {
                if(ctx.PlayerSpawnPoint!=saved)throw new Exception("checkpoint trigger did not record");
                foreach(var sr in saved.GetComponentsInChildren<SpriteRenderer>())
                    if(sr.color!=Color.white)throw new Exception("checkpoint tinted");
                File.AppendAllText("QA/checkpoint-tarot.txt","PASS physical checkpoint trigger recorded; original colour retained.\n");
                ctx.Player.Respawn(tarot.position);
                float origin=sceneB?DreamremainsLevelBootstrap.SceneBOrigin:0;
                UnityEngine.Object.FindAnyObjectByType<CameraTargetFollow>()?.SetRoom(origin,origin+(sceneB?DreamremainsExpandedLayout.BLength:DreamremainsExpandedLayout.ALength),0,8);
                faceScale=tarot.Find("Tarot").localScale;
                keyboard=Keyboard.current??InputSystem.AddDevice<Keyboard>();next=now+.1;phase=2;
            }
            else if(phase==2 && now>next)
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.E));next=now+.12;phase=3;
            }
            else if(phase==3)
            {
                if(tarot.Find("Tarot").localScale.x<faceScale.x*.8f)sawFlip=true;
                if(now>next){InputSystem.QueueStateEvent(keyboard,new KeyboardState());next=now+1;phase=4;}
            }
            else if(phase==4)
            {
                if(tarot.Find("Tarot").localScale.x<faceScale.x*.8f)sawFlip=true;
                if(now<next)return;
                int slot=sceneB?3:1;
                if(!ctx.TarotResult.IsNodeActivated(slot))throw new Exception("E did not activate "+tarot.name);
                if(!sawFlip)throw new Exception("flip scale did not animate");
                if(tarot.Find("Tarot").GetComponent<SpriteRenderer>().sprite!=ctx.TarotResult.GetCard(slot).CardFace)throw new Exception("face missing after flip");
                File.AppendAllText("QA/checkpoint-tarot.txt","PASS pickup activates "+tarot.name+"; observed animated flip and final card face.\n");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("QA/tarot-active-"+(sceneB?"B":"A")+".png"));
                next=now+.5;phase=7;
            }
            else if(phase==7 && now>next){ctx.Player.TryKill();next=now+2.5;phase=5;}
            else if(phase==5 && now>next)
            {
                var safe=saved.GetComponent<CheckpointInteractable>().SafePosition(ctx.Player);
                if(ctx.Player.IsDead || ctx.StateMachine.CurrentStateType!=GameStateType.Playing)throw new Exception("respawn state failed");
                if(Vector2.Distance(ctx.Player.transform.position,safe)>.6f)throw new Exception("wrong respawn position");
                var screen=Camera.main.WorldToViewportPoint(ctx.Player.transform.position);
                if(screen.x<0 || screen.x>1 || screen.y<0 || screen.y>1)throw new Exception("respawn off camera");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("QA/checkpoint-respawn-"+(sceneB?"B":"A")+".png"));
                File.AppendAllText("QA/checkpoint-tarot.txt","PASS death -> automatic respawn at recorded support, player visible in camera.\n");
                next=now+.5;phase=6;
            }
            else if(phase==6 && now>next)Finish("PASS targeted Play checks only; full route not tested.");
        }
        catch(Exception ex){Finish("FAIL "+ex);}
    }
    static void Finish(string message)
    {
        if(keyboard!=null)InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        SessionState.SetBool(Key,false);File.AppendAllText("QA/checkpoint-tarot.txt",message+"\n");
        File.Copy("QA/checkpoint-tarot.txt","QA/checkpoint-tarot-"+(sceneB?"B":"A")+".txt",true);
        SessionState.SetBool(Key+".B",false);
        EditorApplication.isPlaying=false;
    }
}
// User-operated preview. Changes only the current Play session, never scene assets or saved decks.
[InitializeOnLoad]
public static class FirstTarotBranchPreview
{
    const string Key="Dreamremains.T1BranchPreview";
    static double deadline;
    static FirstTarotBranchPreview()
    {
        DreamremainsPlaytestControls.Requested=mode=>
        {
            SessionState.SetInt(Key+".Restart",mode);
            EditorApplication.isPlaying=false;
        };
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetInt(Key,0);deadline=0;
                int restart=SessionState.GetInt(Key+".Restart",0);
                SessionState.SetInt(Key+".Restart",0);
                if(restart>0)EditorApplication.delayCall+=()=>Start(restart);
            }
        };
    }
    [MenuItem("Dreamremains/试玩 A区T1/普通牌（浮台会消失）")]
    public static void Normal()=>Start(1);
    [MenuItem("Dreamremains/试玩 A区T1/E12（浮台稳定）")]
    public static void Stable()=>Start(2);
    [MenuItem("Dreamremains/试玩 A区T1/E18（加速消失）")]
    public static void Fast()=>Start(3);
    [MenuItem("Dreamremains/试玩 A区T1/从第一次分流开始")]
    public static void FirstSplit()=>Start(4);
    [MenuItem("Dreamremains/试玩 A区T1/带牌组从B区开始")]
    public static void SceneB()=>Start(5);
    static void Start(int mode)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("先停止 Play，再选择另一种试玩牌效。");return;
        }
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Game/Scenes/Gameplay.unity")
        {
            EditorUtility.DisplayDialog("试玩 A 区 T1","请先打开 Assets/Game/Scenes/Gameplay.unity，再选择此菜单。","知道了");return;
        }
        SessionState.SetInt(Key,mode);deadline=0;
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        int mode=SessionState.GetInt(Key,0);if(mode==0)return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
        if(EditorApplication.timeSinceStartup>deadline){SessionState.SetInt(Key,0);Debug.LogError("T1试玩入口等待超时，请查看 Console。");return;}
        if(!Application.isPlaying)return;
        var ctx=GameLoop.Instance?.Context;
        if(ctx?.Player==null || ctx.StateMachine.CurrentStateType!=GameStateType.Playing)return;
        var root=GameObject.Find(DreamremainsLevelBootstrap.RootName)?.transform;
        var node=root?.Find(mode==5?"Scene_B/IN-B01":mode==4?"Scene_A/CP-LINK-A1":"Scene_A/T1");if(node==null)return;
        var deck=TarotCatalog.LoadDeck();
        string first=mode==2?"Card_Emperor":mode==3?"Card_Empress":"Card_Hierophant";
        string[] ids={first,"Card_Hermit","Card_Sun","Card_World"};
        var hand=new TarotCardData[4];
        for(int i=0;i<4;i++)hand[i]=Array.Find(deck,c=>c.CardId==ids[i]);
        if(Array.Exists(hand,c=>c==null)){SessionState.SetInt(Key,0);Debug.LogError("T1试玩缺少牌资源。");return;}
        ctx.TarotResult=new TarotResultData();
        ctx.TarotResult.BeginRun(hand,new[]{mode!=2,true,true,true});
        if(mode==5)
        {
            for(int i=0;i<2;i++)
            {
                ctx.TarotResult.ActivateNode(i);
                EventCenter.Publish(GameEvents.TarotNodeActivated,i);
            }
        }
        TarotEffectApplier.Apply(ctx);
        // Land in the real T1 pickup collider; activation is performed by normal physics contact.
        var checkpoint=node.GetComponent<CheckpointInteractable>();
        ctx.Player.Respawn(checkpoint!=null?checkpoint.SafePosition(ctx.Player):node.position+Vector3.up*.2f);
        float origin=mode==5?DreamremainsLevelBootstrap.SceneBOrigin:0;
        UnityEngine.Object.FindAnyObjectByType<CameraTargetFollow>()?.SetRoom(mode==5?origin:mode==4?0:39.7f,mode==5?origin+40:mode==4?39.7f:80.3f,0,8);
        TarotZoneQuery.EnterZone(DreamremainsLevelData.ZoneAt(node.position.x-origin,mode!=5));
        // The existing checkpoint at the branch entrance records naturally as the user passes it.
        SessionState.SetInt(Key,0);
        Debug.Log("[T1Preview] 临时测试牌组；从分流或真实T1领取点开始。不是自动通关测试。");
    }
}
#endif
