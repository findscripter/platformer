using System;
using System.Collections.Generic;
using UnityEngine;
using static DreamremainsLevelData;

/// <summary>
/// Expand the compact drawing by translating existing encounter groups, never scaling jumps or sprites.
/// Eight authored connecting passages have no random positions. Original node IDs and branches survive.
/// This is a playable iteration, not a claim of 20-minute acceptance.
/// </summary>
public static class DreamremainsExpandedLayout
{
    public const float PassageLength = 34f;
    public const float ALength = 166f;
    public const float BLength = 170f;
    static readonly float[] CutsA = { 5.7f, 12.3f, 19.3f, 25.9f };
    static readonly float[] CutsB = { 5.2f, 12f, 18.8f, 25.2f };
    public static float Map(float x, bool a)
    {
        float result=x;
        foreach(float cut in a?CutsA:CutsB) if(x>=cut)result+=PassageLength;
        return result;
    }

    public static SceneSpec Expand(SceneSpec source, bool a)
    {
        var result=source;
        var platforms=new List<PlatformSpec>();
        foreach(var original in source.Platforms)
        {
            var p=original;float shift=Map(p.X,a)-p.X;
            p.X+=shift;p.MoveX+=shift;platforms.Add(p);
        }
        result.Points=(PointSpec[])source.Points.Clone();
        for(int i=0;i<result.Points.Length;i++)
        {
            var p=result.Points[i];float shift=Map(p.X,a)-p.X;
            // Keep supported objects with their platform when a drawing boundary crosses its edge.
            float nearest=1.2f;
            foreach(var support in source.Platforms)
            {
                if(support.Type=="fading" || support.Type=="moving")continue;
                if(p.X<support.X-.1f || p.X>support.X+support.W+.1f)continue;
                float dy=Mathf.Abs(support.Y-p.Y);
                if(dy>=nearest)continue;
                nearest=dy;shift=Map(support.X,a)-support.X;
            }
            p.X+=shift;p.PatrolX1+=shift;p.PatrolX2+=shift;
            result.Points[i]=p;
        }
        result.Abysses=(AbyssSpec[])source.Abysses.Clone();
        for(int i=0;i<result.Abysses.Length;i++)
        {
            var h=result.Abysses[i];float shift=Map(h.X1,a)-h.X1;
            h.X1+=shift;h.X2+=shift;result.Abysses[i]=h;
        }
        result.AirWalks=(RectSpec[])source.AirWalks.Clone();
        for(int i=0;i<result.AirWalks.Length;i++)
        {
            var r=result.AirWalks[i];r.X=Map(r.X,a);result.AirWalks[i]=r;
        }
        result.Rooms=(RoomSpec[])source.Rooms.Clone();
        for(int i=0;i<result.Rooms.Length;i++)
        {
            var r=result.Rooms[i];r.X1=Map(r.X1,a);r.X2=Map(r.X2,a);result.Rooms[i]=r;
        }
        // Columns are left edge, HTML standing-surface Y, width. Each passage has its own profile.
        if(a)
        {
            // The lower route descends into a low encounter pocket instead of
            // following the same ascending silhouette as the upper route.
            Passage(platforms,"A1",7, new float[,]{{0,5,3},{4.6f,5.9f,2.5f},{8.5f,6.4f,3},{13,6.4f,5},{19.5f,6.2f,3},{24,5.2f,3},{28.5f,6,3.5f}});
            Passage(platforms,"A2",Map(12.3f,a)-34+2, new float[,]{{0,4.2f,3},{4.5f,3.4f,2.5f},{8.5f,2.6f,3},{13,3.3f,3},{17.5f,4.1f,5},{24,4.8f,3},{28.5f,4.8f,2.5f}});
            Passage(platforms,"A3",Map(19.3f,a)-34+2, new float[,]{{0,5.9f,4},{5.5f,5.1f,2.8f},{9.8f,4.3f,3},{14.3f,3.5f,4},{19.8f,2.8f,3},{24.3f,2.8f,3},{28.8f,3.5f,3}});
            Passage(platforms,"A4",Map(25.9f,a)-34+2, new float[,]{{0,4.8f,4.5f},{6,4.1f,3},{10.5f,3.4f,2.5f},{14.5f,3.4f,4},{20,4.2f,3.5f},{25,5,3},{29.5f,5.3f,2}});
            // A's original high route remains optional; a ladder of terrain connects the two layers.
            // PL-A04 below is a traversable encounter, not a crawl tunnel.
            // With the 0.4-thick upper collider this leaves 2.4 units of headroom.
            platforms.Add(P("LINK-A-UP1",Map(6,a)+.4f,3.2f,1.8f));
            platforms.Add(P("LINK-A-UP2",Map(6,a)+2.6f,3.3f,1.6f));
            // BR-A01: climb, overlook the lower encounter, then descend toward T1.
            // All new upper surfaces are one-way; they do not become ceilings for the original route.
            platforms.Add(P("BR-A01-CLIMB1",10.1f,3.5f,1.2f));
            platforms.Add(P("BR-A01-CLIMB2",12.8f,2.3f,2f));
            platforms.Add(P("BR-A01-RIDGE",16.2f,1.6f,3f));
            platforms.Add(P("BR-A01-LOOKOUT",20.6f,1.3f,5.2f));
            platforms.Add(P("BR-A01-TRAVERSE",27.1f,2.3f,3f));
            platforms.Add(P("BR-A01-DESCENT",31.5f,3.5f,3f));
            platforms.Add(P("BR-A01-REJOIN",36f,4.5f,2.8f));
            // T1 demonstration branch: forward-only split/rejoin, main route untouched.
            // Stable landing before and after each fading carrier; no enemy on the branch.
            platforms.Add(P("BR-A-T1-ENTRY",52.5f,5.4f,3f));
            var trial=P("BR-A-T1-FADE1",57f,6f,1.3f);trial.Type="fading";platforms.Add(trial);
            trial=P("BR-A-T1-FADE2",59.2f,6f,1.3f);trial.Type="fading";platforms.Add(trial);
            platforms.Add(P("BR-A-T1-REST",62f,6f,2.5f));
            // This step occupies the overhead gap, not the underside of the main platform.
            platforms.Add(P("BR-A-T1-EXIT",64.75f,5.2f,.8f));
        }
        else
        {
            Passage(platforms,"B1",7, new float[,]{{0,3.9f,3},{4.5f,2.9f,3},{9,2.3f,3.5f},{14,1.6f,4},{19.5f,2.4f,2.5f},{23.5f,3.1f,3},{28,3.1f,3}});
            // Descend into a sheltered bank, then climb to the original moving-platform exit.
            Passage(platforms,"B2",Map(12,a)-34+2, new float[,]{{0,3.8f,3},{4.5f,4.6f,3},{9,5,4.5f},{15,5.6f,3},{19.5f,5.2f,4},{25,4.6f,2.5f},{29,5.3f,2}});
            Passage(platforms,"B3",Map(18.8f,a)-34+2, new float[,]{{0,5.1f,4},{5.5f,4.3f,2.5f},{9.5f,3.5f,3},{14,2.7f,4},{19.5f,3.4f,3},{24,4.2f,3},{28.5f,4.2f,2.5f}});
            Passage(platforms,"B4",Map(25.2f,a)-34+2, new float[,]{{0,2.2f,3.5f},{5,3,3},{9.5f,3.8f,4},{15,4.6f,3},{19.5f,5.4f,4},{25,5,3},{29.5f,5,2}});
        }
        if(a)CompleteAPaths(platforms,ref result);
        else CompleteBPaths(platforms,ref result);
        ComposeEncounterPassages(platforms,a);
        result.Platforms=platforms.ToArray();
        // Explicit support assignments correct pre-existing floating hazards; no random relocation.
        result.Spikes=(SpikeSpec[])source.Spikes.Clone();
        string[] beds=a?new[]{"PL-A03","PL-A08","LINK-A4-1"}:new[]{"PL-B06","LINK-B3-4","LINK-B4-5"};
        for(int i=0;i<result.Spikes.Length;i++)
        {
            var s=result.Spikes[i];var bed=platforms.Find(p=>p.Id==beds[i]);
            s.W=Mathf.Min(s.W,bed.W*.4f);s.X=bed.X+bed.W*.5f-s.W*.5f;s.Y=bed.Y;
            result.Spikes[i]=s;
        }
        var points=new List<PointSpec>(result.Points);
        // Fixed first-pass occupants of local encounter slots; no changes to route topology.
        // Reserve these supports as whole slots, never roll loot and enemies onto the same landing.
        string supplyBed=a?"LINK-A3-6":"LINK-B2-5";
        var supply=platforms.Find(p=>p.Id==supplyBed);
        points.Add(new PointSpec{Id=a?"SUP-A01":"SUP-B01",Kind="supply",X=supply.X+supply.W*.55f,Y=supply.Y-.3f});
        // Visible early main-route supplies, independent of tarot/hidden route activation.
        foreach(string bedId in a?new[]{"LINK-A1-1","LINK-A2-3"}:new[]{"LINK-B1-1","LINK-B3-5"})
        {
            var bed=platforms.Find(p=>p.Id==bedId);
            points.Add(new PointSpec{Id="SUP-"+bedId,Kind="supply",X=bed.X+bed.W*.65f,Y=bed.Y-.3f});
        }
        if(!a)
        {
            var encounter=platforms.Find(p=>p.Id=="LINK-B3-3");
            points.Add(new PointSpec{Id="EN-B-LOCAL01",Kind="enemy",X=encounter.X+1.5f,Y=encounter.Y-.35f});
        }
        if(a)points.Add(new PointSpec{Id="CP-BR-A01",Kind="cp",X=23.4f,Y=.95f});
        // Safe rest at each passage entry; encounters live farther along a wide landing.
        // No additional fragments: the four authored branch rewards retain their IDs.
        string region=a?"A":"B";
        for(int i=1;i<=4;i++)
        {
            var rest=platforms.Find(p=>p.Id=="LINK-"+region+i+"-1");
            if(i<4) points.Add(new PointSpec{Id="CP-LINK-"+region+i,Kind="cp",X=rest.X+.6f,Y=rest.Y-.35f});
            int landing=a?(i==2?5:4):(i==2?3:4);
            if(!a && i==3)landing=1; // keep the B3 spike bed separate
            var bed=platforms.Find(p=>p.Id=="LINK-"+region+i+"-"+landing);
            if(bed.W>=3.5f && !(!a && i==3))
                points.Add(new PointSpec{Id="EN-LINK-"+region+i,Kind="enemy",X=bed.X+bed.W*.6f,Y=bed.Y-.35f,PatrolX1=bed.X+1.1f,PatrolX2=bed.X+bed.W-.5f});
        }
        // Every stationary enemy owns a fixed support; avoid a patrol extending into thin air.
        for(int i=0;i<points.Count;i++)
        {
            var point=points[i];if(point.Kind!="enemy")continue;
            PlatformSpec bed=default;float best=float.PositiveInfinity;
            foreach(var p in platforms)
            {
                if(p.Type!="normal")continue;
                float dx=Mathf.Max(p.X-point.X,point.X-(p.X+p.W),0);
                float dy=Mathf.Abs(p.Y-point.Y);
                if(dx>1.5f || dy>1.2f || dx+dy>=best)continue;
                best=dx+dy;bed=p;
            }
            if(bed.Id==null)continue;
            float left=bed.X+.35f,right=bed.X+bed.W-.35f;
            // B's first half: reserve the incoming left edge for landing/reaction.
            // Keep occupants and loot counts; random variants must respect this same reserved space.
            if (!a && bed.X < 85f && bed.W >= 2.8f)
                left = bed.X + Mathf.Min(1.5f, bed.W * .45f);
            // Patrol on one side of a hazard, never through the spike bed.
            foreach(var spike in result.Spikes)
                if(Mathf.Abs(spike.Y-bed.Y)<.01f && spike.X>=bed.X && spike.X<bed.X+bed.W)
                {
                    if(spike.X-left>right-(spike.X+spike.W))right=spike.X-.4f;
                    else left=spike.X+spike.W+.4f;
                }
            if(right<left){point.PatrolX1=point.PatrolX2=point.X=Mathf.Clamp(point.X,bed.X+.3f,bed.X+bed.W-.3f);}
            else{point.X=Mathf.Clamp(point.X,left,right);point.PatrolX1=left;point.PatrolX2=right;}
            point.Y=bed.Y-.35f;points[i]=point;
        }
        result.Points=points.ToArray();
        return result;
    }
    static PlatformSpec P(string id,float x,float y,float w) => new PlatformSpec{Id=id,X=x,Y=y,W=w,Type="normal"};
    // These edits affect passage interiors only. Original seals, entrances and exits remain authored.
    static void ComposeEncounterPassages(List<PlatformSpec> platforms,bool a)
    {
        if(a)
        {
            // A2: observe from a broad landing, board a short ferry, disembark in the encounter pocket.
            Ferry(platforms,"LINK-A2-4",1.4f,2.4f);
            SetSurface(platforms,"LINK-A2-5",5.3f,5f);
            // The T1 collection route descends toward a small lift and climbs back to the original reward.
            SetSurface(platforms,"TH-A1-LINK3",2.5f,2f);
            SetSurface(platforms,"TH-A1-LINK4",2.4f,2f);
            Lift(platforms,"TH-A1-LINK4",2.4f,1.4f);
            // A3: a stable waiting shelf, a lift, then an elevated enemy landing; later a recovery box.
            Lift(platforms,"LINK-A3-3",4.3f,2.4f);
            SetSurface(platforms,"LINK-A3-4",1.9f,4f);
            // A4: two neighbouring pieces form one resting ledge rather than another identical jump.
            JoinLanding(platforms,"LINK-A4-3","LINK-A4-4");
        }
        else
        {
            // B1 keeps the lower combat choice; upper route has a standing shelf before the descent.
            // Separate a short approach from the higher encounter island; do not concatenate two skins.
            SetSurface(platforms,"LINK-B1-3",2.3f,3.5f);
            SetSurface(platforms,"LINK-B1-4",1.6f,3.4f);
            // B2: board below the hidden collection route, ride to the opposite bank, then recover.
            Ferry(platforms,"LINK-B2-4",1.4f,2.5f);
            // T3's optional upper route has a short lift rather than a second static platform chain.
            Lift(platforms,"TH-B1-LINK3",2.8f,1.6f);
            // B3: enemy first, stable waiting shelf, lift to the fixed spike challenge, safe landing after.
            Lift(platforms,"LINK-B3-2",4.3f,2.3f);
            SetSurface(platforms,"LINK-B3-3",2f,3f);
            // B4: extended fixed observation ledge, followed by the existing final hazard and recovery.
            JoinLanding(platforms,"LINK-B4-2","LINK-B4-3");
            // After the fixed hazard bed, wait for the last carrier and disembark on the fixed exit shelf.
            Ferry(platforms,"LINK-B4-6",1.4f,2.2f);
            // T4 optional route alternates stable islands and two short fading steps; never carries a box/enemy.
            SetKind(platforms,"TH-B2-LINK3","fading");
            SetKind(platforms,"TH-B2-LINK6","fading");
        }
    }
    static void SetSurface(List<PlatformSpec> ps,string id,float y,float width)
    {
        int i=ps.FindIndex(p=>p.Id==id);var p=ps[i];p.Y=y;p.W=width;ps[i]=p;
    }
    static void SetKind(List<PlatformSpec> ps,string id,string type)
    {
        int i=ps.FindIndex(p=>p.Id==id);var p=ps[i];p.Type=type;ps[i]=p;
    }
    static void Ferry(List<PlatformSpec> ps,string id,float width,float travel)
    {
        int i=ps.FindIndex(p=>p.Id==id);var p=ps[i];p.W=width;p.Type="moving";
        p.MoveX=p.X+width*.5f;p.MoveX2=p.MoveX+travel;p.MoveY1=p.MoveY2=p.Y+.2f;ps[i]=p;
    }
    static void Lift(List<PlatformSpec> ps,string id,float low,float high)
    {
        int i=ps.FindIndex(p=>p.Id==id);var p=ps[i];p.Y=low;p.Type="moving";
        p.MoveX=p.X+p.W*.5f;p.MoveY1=low+.2f;p.MoveY2=high+.2f;ps[i]=p;
    }
    static void JoinLanding(List<PlatformSpec> ps,string leftId,string rightId)
    {
        int i=ps.FindIndex(p=>p.Id==leftId),j=ps.FindIndex(p=>p.Id==rightId);
        var left=ps[i];var right=ps[j];left.W=right.X-left.X;right.Y=left.Y;
        ps[i]=left;ps[j]=right;
    }
    static void CompleteBPaths(List<PlatformSpec> platforms,ref SceneSpec scene)
    {
        // T3's authored upper collection path starts just after the seal and reconnects at MP-B01.
        var first=new List<string>(scene.HiddenIds);
        float[,] upper={{52.5f,1.4f,2.6f},{57,2f,3.5f},{62,2.8f,2.4f},{66.5f,2.4f,2.6f},
            {71,1.5f,2.5f},{75.5f,2f,2.2f},{79.2f,1.4f,1.5f}};
        for(int i=0;i<upper.GetLength(0);i++)
        {
            var p=P("TH-B1-LINK"+(i+1),upper[i,0],upper[i,1],upper[i,2]);p.Type="hidden";
            platforms.Add(p);first.Add(p.Id);
        }
        scene.HiddenIds=first.ToArray();
        var second=new List<string>(scene.HiddenIds2);
        float[,] lower={{128.3f,6.4f,3},{132.7f,6.5f,3},{137.1f,6.4f,3},
            {141.5f,6.5f,3},{145.9f,7.2f,3},{150.3f,7.2f,3},{154.7f,6.9f,2.7f},{158.8f,6.4f,1.7f}};
        for(int i=0;i<lower.GetLength(0);i++)
        {
            var p=P("TH-B2-LINK"+(i+1),lower[i,0],lower[i,1],lower[i,2]);p.Type="hidden";
            platforms.Add(p);second.Add(p.Id);
        }
        scene.HiddenIds2=second.ToArray();
        // Ground-combat alternative to the first high platform sequence; rejoins before T3.
        float[,] combat={{15.5f,5.4f,3},{20,6.2f,5},{26.5f,6.2f,4},{32,5.4f,3},{36.5f,4.8f,2}};
        for(int i=0;i<combat.GetLength(0);i++)platforms.Add(P("BR-B01-"+(i+1),combat[i,0],combat[i,1],combat[i,2]));
        // Mechanism branch starts under T4; completing TRG-B02 unlocks a forward return to PL-B12.
        platforms.Add(P("BR-B02-ENTRY",121.8f,6.1f,2.2f));
        platforms.Add(P("BR-B02-RETURN1",125f,4.8f,1.2f));
        platforms.Add(P("BR-B02-RETURN2",127.5f,3.6f,1.4f));
        platforms.Add(P("E06-B-STEP1",62f,2f,3));
        platforms.Add(P("E06-B-STEP2",66.5f,2f,3));
        platforms.Add(P("E06-B-STEP3",71f,3.2f,2));
        for(int i=0;i<platforms.Count;i++)
        {
            var p=platforms[i];if(p.Id=="TH-B01"||p.Id=="TH-B02")p.Y=1.4f;platforms[i]=p;
        }
        for(int i=0;i<scene.Points.Length;i++)
        {
            var p=scene.Points[i];
            if(p.Id=="IT-B01"){p.X=84.7f;p.Y=1.05f;}
            if(p.Id=="EN-B02"){p.X=22.5f;p.Y=5.85f;}
            if(p.Id=="TRG-B02"){p.X=122.8f;p.Y=5.75f;}
            scene.Points[i]=p;
        }
    }
    static void CompleteAPaths(List<PlatformSpec> platforms,ref SceneSpec scene)
    {
        // Extend the authored T1 high collection line from the learning section.
        var first=new List<string>(scene.HiddenIds);
        float[,] high={{60.9f,1.5f,2.6f},{64.9f,1.3f,3.5f},{69.9f,1.3f,2.5f},{73.8f,1.4f,2.5f},{77.5f,1.4f,2}};
        for(int i=0;i<high.GetLength(0);i++)
        {
            var p=P("TH-A1-LINK"+(i+1),high[i,0],high[i,1],high[i,2]);p.Type="hidden";
            platforms.Add(p);first.Add(p.Id);
        }
        scene.HiddenIds=first.ToArray();
        // Leave full character clearance below the camera top on the original collection platforms.
        for(int i=0;i<platforms.Count;i++)
        {
            var p=platforms[i];
            if(p.Id=="TH-A01"||p.Id=="TH-A02")p.Y=1.4f;
            if(p.Id=="LINK-A4-7")p.W=1.2f; // room for the hidden route's rejoin step
            platforms[i]=p;
        }
        for(int i=0;i<scene.Points.Length;i++)
        {
            var p=scene.Points[i];
            if(p.Id=="IT-A01"){p.X=81.7f;p.Y=1.05f;}
            scene.Points[i]=p;
        }
        // High main continuation and lower combat route meet again before T2.
        platforms.Add(P("BR-A02-HIGH1",89.6f,2.6f,3));
        platforms.Add(P("BR-A02-HIGH2",94.1f,1.8f,4.4f));
        platforms.Add(P("BR-A02-HIGH3",100f,1.7f,2.5f));
        // T2 lower collection line continues to CP-A02 instead of ending at the collectible.
        var second=new List<string>(scene.HiddenIds2);
        float[,] low={{130,7.2f,3.6f},{134.8f,7,3},{139,6.4f,3},{143.2f,6.8f,4},{148.6f,7,3},{153,7,3},{157.3f,7.2f,2.7f},{160.9f,6.2f,1.3f}};
        for(int i=0;i<low.GetLength(0);i++)
        {
            var p=P("TH-A2-LINK"+(i+1),low[i,0],low[i,1],low[i,2]);p.Type="hidden";
            platforms.Add(p);second.Add(p.Id);
        }
        scene.HiddenIds2=second.ToArray();
        // E06 is a separate traversable shortcut after T2, never a teleport over a tarot phase.
        platforms.Add(P("E06-A-STEP1",128.7f,3.3f,3));
        platforms.Add(P("E06-A-STEP2",133.1f,2.3f,3));
        platforms.Add(P("E06-A-STEP3",137.5f,2.3f,2));
    }
    static void Passage(List<PlatformSpec> target,string id,float x,float[,] points)
    {
        for(int i=0;i<points.GetLength(0);i++)
            target.Add(P("LINK-"+id+"-"+(i+1),x+points[i,0],points[i,1],points[i,2]));
    }
}
