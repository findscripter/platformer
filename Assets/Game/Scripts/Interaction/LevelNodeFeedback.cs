using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>One-time, non-blocking first encounter hints. No route-design text in the player UI.</summary>
public sealed class FirstEncounterGuide : MonoBehaviour
{
    private readonly HashSet<string> seen=new HashSet<string>();
    private TarotResultData run;
    private TextMeshProUGUI hint;
    private float until,nextScan;
    void Start()
    {
        var canvasObject=new GameObject("FirstEncounterHints");canvasObject.transform.SetParent(transform,false);
        var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=70;
        var scale=canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scale.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1366,768);
        var label=new GameObject("Hint");label.transform.SetParent(canvasObject.transform,false);
        hint=label.AddComponent<TextMeshProUGUI>();GuideUiFont.Apply(hint);
        hint.fontSize=23;hint.alignment=TextAlignmentOptions.Center;hint.raycastTarget=false;
        hint.color=new Color(1,.97f,.86f);hint.outlineWidth=.2f;
        var rect=hint.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
        rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-8);rect.sizeDelta=new Vector2(760,40);
        hint.gameObject.SetActive(false);
    }
    void Update()
    {
        if(hint==null)return;
        var ctx=GameLoop.Instance?.Context;
        if(ctx?.Player==null)return;
        if(run!=ctx.TarotResult){run=ctx.TarotResult;seen.Clear();until=0;}
        bool playing=ctx.StateMachine.CurrentStateType==GameStateType.Playing && !ctx.Player.IsDead;
        hint.gameObject.SetActive(playing && Time.time<until && !LevelNodeFeedback.HasVisibleHint);
        if(!playing||Time.time<until||Time.time<nextScan)return;
        nextScan=Time.time+.25f;
        bool a=ctx.Player.transform.position.x<DreamremainsLevelBootstrap.SceneBOrigin;
        var scene=a?DreamremainsLevelData.SceneA:DreamremainsLevelData.SceneB;
        float origin=a?0:DreamremainsLevelBootstrap.SceneBOrigin;
        Vector2 pos=ctx.Player.transform.position;
        foreach(var p in scene.Points)
        {
            var point=DreamremainsLevelBootstrap.ToWorld(p.X,p.Y,origin);
            if(Vector2.Distance(pos,point)>3.4f)continue;
            if(p.Kind=="enemy" && Show("enemy","前方有怪物：可以击打，也可以跳跃避开。"))return;
            if(p.Kind=="item")
            {
                var node=transform.Find((a?"Scene_A/":"Scene_B/")+p.Id);
                if(node!=null && node.gameObject.activeInHierarchy && Show("fragment","靠近梦核碎片即可收集。"))return;
            }
            if(p.Kind=="tarot" && Show("tarot",run!=null&&run.IsComplete()?"在塔罗牌前站稳，唤醒本局塔罗的力量。":"本局尚未抽牌，请先完成梦境输入和抽牌。"))return;
        }
        foreach(var p in scene.Platforms)
        {
            if(Vector2.Distance(pos,DreamremainsLevelBootstrap.ToWorld(p.X+p.W*.5f,p.Y,origin))>3.5f)continue;
            if(p.Type=="fading" && Show("fading",run!=null&&run.HasActiveEffect(TarotEffectId.E12)?
                "当前牌效让浮台保持稳定。":"这种浮台踩踏后会消失，先观察下一处落脚点。"))return;
            if(p.Type=="moving" && Show("moving","观察浮台往返的时机，再跳过去。"))return;
        }
        foreach(var spike in scene.Spikes)
            if(Vector2.Distance(pos,DreamremainsLevelBootstrap.ToWorld(spike.X,spike.Y,origin))<3.2f &&
                Show("spikes","留意尖刺，先看清安全的落脚位置。"))return;
    }
    private bool Show(string id,string text)
    {
        if(!seen.Add(id))return false;
        hint.text=text;until=Time.time+4.5f;hint.gameObject.SetActive(true);return true;
    }
}

/// <summary>固定节点的近距离提示和已激活状态，不移动节点或增加激活门槛。</summary>
public sealed class LevelNodeFeedback : MonoBehaviour
{
    string kind; int slot;
    TextMeshProUGUI label;
    SpriteRenderer card;
    bool shownActive;
    string routeText;
    public void ConfigureRoute(string text)
    {
        Configure("route",0);
        routeText=text;
    }
    static readonly List<LevelNodeFeedback> Nodes=new List<LevelNodeFeedback>();
    public static bool HasVisibleHint => Nodes.Exists(n=>n!=null && n.label!=null && n.label.gameObject.activeInHierarchy);
    public void Configure(string nodeKind,int cardSlot)
    {
        kind=nodeKind;slot=cardSlot;
        Nodes.Add(this);
        var canvasObject=new GameObject("NodeHintHUD");canvasObject.transform.SetParent(transform,false);
        var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=69;
        var scaler=canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1366,768);
        var go=new GameObject("StateLabel");go.transform.SetParent(canvasObject.transform,false);
        label=go.AddComponent<TextMeshProUGUI>();GuideUiFont.Apply(label);
        label.fontSize=20;label.alignment=TextAlignmentOptions.Top;label.raycastTarget=false;
        var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
        rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-8);rect.sizeDelta=new Vector2(760,56);
        label.color=new Color(1,.96f,.84f);label.outlineWidth=.15f;
        var face=transform.Find("Tarot");if(face!=null)card=face.GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        var ctx=GameLoop.Instance!=null?GameLoop.Instance.Context:null;
        if(label==null)return;
        // Checkpoints retain their visual marker and respawn behaviour, without status text.
        if(kind=="cp"){label.gameObject.SetActive(false);return;}
        if(kind=="route")
        {
#if UNITY_EDITOR
            if(!DreamremainsPlaytestControls.ShowRouteNotes){label.gameObject.SetActive(false);return;}
#else
            label.gameObject.SetActive(false);return;
#endif
        }
        bool near=ctx?.Player!=null && Vector2.Distance(ctx.Player.transform.position,transform.position)<2.2f;
        bool resonant=kind=="tarot" && ctx?.TarotResult!=null &&
            ctx.TarotResult.HasActiveEffect(TarotEffectId.E09) && !ctx.TarotResult.IsNodeActivated(slot);
        bool playing=ctx!=null && ctx.StateMachine.CurrentStateType==GameStateType.Playing && ctx.Player!=null && !ctx.Player.IsDead;
        // A single relevant node owns the top HUD, including the nearest distant E09 seal.
        label.gameObject.SetActive(playing && (near || resonant) && IsPriorityLabel(ctx.Player.transform.position));
        if(kind=="tarot")
        {
            var run=ctx?.TarotResult;
            bool active=run!=null && run.IsNodeActivated(slot);
            label.text=run==null || !run.IsComplete()?"尚未抽取本局塔罗":active?
                run.FormatSlot(slot)+"\n已激活 · 隐藏路线已开启":run.FirstMissingRequiredNode(4)==slot?"在牌前站稳 · 激活第"+(slot+1)+"处塔罗节点":"请先激活第"+(run.FirstMissingRequiredNode(4)+1)+"处塔罗节点";
            if(resonant && !near)label.text="第"+(slot+1)+"处塔罗节点 · 尚未激活";
            if(active && !shownActive && near){shownActive=true;StartCoroutine(Flip(run));}
        }
        else if(kind=="trg")label.text=GetComponent<MechanismTrigger>()?.HasFired==true?"机关已触发":"靠近触发机关";
        else if(kind=="supply")label.text=ctx?.Player!=null && ctx.Player.CurrentHealth>=ctx.Player.MaxHealth?
            "生命已满 · 补给保留":"靠近补给 · 恢复 1 点生命";
        else if(kind=="fold")
        {
            var portal=GetComponent<FoldPortal>();
            label.text=portal==null?"折返落点":portal.CanInteract?"[E] 折叠捷径":"尚未获得折叠充能";
        }
        else if(kind=="end")
        {
            var exit=GetComponent<TarotReturnInteractable>();
            label.text=exit!=null && exit.CanInteract ? "[E] "+exit.InteractPrompt : "梦境出口 · 走近留下梦笺";
        }
        else if(kind=="trans")label.text=GetComponent<RegionGate>()?.CanInteract==true
            ? "[E] "+GetComponent<RegionGate>().InteractPrompt : "前方通往下一片梦境";
        else if(kind=="route")
        {
            label.text=routeText;
            if(ctx?.TarotResult!=null && ctx.TarotResult.HasActiveEffect(TarotEffectId.E12))
                label.text+="\n牌效：浮台保持稳定";
            else if(ctx?.TarotResult!=null && ctx.TarotResult.HasActiveEffect(TarotEffectId.E18))
                label.text+="\n牌效：浮台更快消失";
        }
    }
    void OnDestroy(){Nodes.Remove(this);}
    bool IsPriorityLabel(Vector2 player)
    {
        LevelNodeFeedback best=null;float score=float.PositiveInfinity;
        foreach(var node in Nodes)
        {
            if(node==null || !node.isActiveAndEnabled)continue;
            if(node.kind=="cp")continue;
            if(node.kind=="route")
            {
#if UNITY_EDITOR
                if(!DreamremainsPlaytestControls.ShowRouteNotes)continue;
#else
                continue;
#endif
            }
            float distance=Vector2.Distance(player,node.transform.position);
            var run=GameLoop.Instance?.Context?.TarotResult;
            bool remoteSeal=node.kind=="tarot" && run!=null && run.HasActiveEffect(TarotEffectId.E09) && !run.IsNodeActivated(node.slot);
            if(distance>=2.2f && !remoteSeal)continue;
            // Nearby tarot takes priority over a mechanism/checkpoint sharing the landing.
            float candidate=distance+(node.kind=="tarot"?0:10)+(distance>=2.2f?1000:0);
            if(candidate<score){score=candidate;best=node;}
        }
        return best==this;
    }
    IEnumerator Flip(TarotResultData run)
    {
        if(card==null || run.GetCard(slot)?.CardFace==null){Debug.LogWarning("[TarotFeedback] Missing card face at "+name);yield break;}
        var tr=card.transform;var scale=tr.localScale;
        Debug.Log("[TarotFeedback] Flip started "+name);
        for(float t=0;t<.35f;t+=Time.unscaledDeltaTime){tr.localScale=new Vector3(Mathf.Lerp(scale.x,.001f,t/.35f),scale.y,scale.z);yield return null;}
        float oldHeight=card.sprite.bounds.size.y;
        card.sprite=run.GetCard(slot).CardFace;
        scale*=oldHeight/Mathf.Max(.001f,card.sprite.bounds.size.y);
        tr.localRotation=Quaternion.Euler(0,0,run.IsReversed(slot)?180:0);
        for(float t=0;t<.35f;t+=Time.unscaledDeltaTime){tr.localScale=new Vector3(Mathf.Lerp(.001f,scale.x,t/.35f),scale.y,scale.z);yield return null;}
        tr.localScale=scale;
        Debug.Log("[TarotFeedback] Flip finished "+name);
    }
}
