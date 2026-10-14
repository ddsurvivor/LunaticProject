using System;
using System.Linq;
using UnityEngine;
static class Program
{
    static int passed;
    static void Assert(bool condition,string message) { if(!condition)throw new Exception(message); }
    static void Near(float a,float b,string message) { Assert(Math.Abs(a-b)<0.0001f,message); }
    static BattleScene Fixture()
    {
        var s=new BattleScene();BattleScene.Ins=s;Time.unscaledTime=0;
        s.BM.PlayerController.pieces.Add(new PieceController {isPlayerPiece=true,transform=new Transform {position=new Vector3(3,0,0)}});
        s.BM.PlayerController.pieces.Add(new PieceController {isPlayerPiece=true,transform=new Transform {position=new Vector3(8,0,0)}});
        s.BM.AIController.pieces.Add(new EnemyController {player=s.BM.AIController});
        s.BM.AIController.pieces.Add(new EnemyController {player=s.BM.AIController,transform=new Transform {position=new Vector3(10,0,0)}});
        return s;
    }
    static EnemyController Enemy(BattleScene s,int n=0)=>(EnemyController)s.BM.AIController.pieces[n];
    static void Test(string name,Action body) { body();passed++;Console.WriteLine("PASS "+name); }
    static void Main()
    {
        Test("Nearest and lowest-health targeting ignore standard threat bonuses",()=>{
            var s=Fixture();var e=Enemy(s);var a=s.BM.PlayerController.pieces[0];var b=s.BM.PlayerController.pieces[1];
            b.unitAttrCenter.TauntValue=10;e.damageDic[b]=100;e.targetStrategy=EnemyTargetStrategy.Nearest;
            Assert(s.BM.AIController.CheckEnemyTarget(e)==a,"nearest followed threat");
            Assert(s.BM.AIController.CheckEnemyTarget(e,a,new Vector3(20,0,0))==b,"nearest preview ignored destination");
            a.unitAttrCenter.CurHealth=50;b.unitAttrCenter.CurHealth=10;e.targetStrategy=EnemyTargetStrategy.LowestHealth;
            Assert(s.BM.AIController.CheckEnemyTarget(e)==b,"lowest health failed");
            b.Blocked=true;Assert(s.BM.AIController.CheckEnemyTarget(e)==a,"untargetable lowest-health pawn selected");
        });
        Test("Conditional strategy requires all conditions, respects priority and recovers baseline",()=>{
            var s=Fixture();var e=Enemy(s);e.strategyRules.Add(new EnemyStrategyRule {
                targetStrategy=EnemyTargetStrategy.LowestHealth,actionStrategy=EnemyAIType.Shoot,
                conditions=new(){new(){type=EnemyStrategyConditionType.HealthRatioAtMost,healthRatio=.3f},
                    new(){type=EnemyStrategyConditionType.TargetCountAtMost,count=1}}});
            e.unitAttrCenter.CurHealth=30;Assert(e.ResolveStrategy().Action==EnemyAIType.Combine,"partial match triggered");
            s.BM.PlayerController.pieces[1].isDead=true;Assert(e.ResolveStrategy().Action==EnemyAIType.Shoot,"threshold match failed");
            e.strategyRules.Add(new EnemyStrategyRule {targetStrategy=EnemyTargetStrategy.Nearest,actionStrategy=EnemyAIType.SkillUser,
                conditions=new(){new(){type=EnemyStrategyConditionType.AllyCountAtMost,count=1}}});
            Assert(e.ResolveStrategy().Target==EnemyTargetStrategy.LowestHealth,"later rule overrode first");
            e.unitAttrCenter.CurHealth=31;Assert(e.ResolveStrategy().Action==EnemyAIType.SkillUser,"ally-count switch failed");
            e.strategyRules[1].enabled=false;Assert(e.ResolveStrategy().Target==EnemyTargetStrategy.StandardThreat,"baseline not restored");
        });
        Test("Strategy switch invalidates committed target in aggro presentation",()=>{
            var s=Fixture();var e=Enemy(s);var a=s.BM.PlayerController.pieces[0];var b=s.BM.PlayerController.pieces[1];
            s.BM.AIController.CommitForTest(e,b);e.strategyRules.Add(new EnemyStrategyRule {
                targetStrategy=EnemyTargetStrategy.Nearest,actionStrategy=EnemyAIType.Shoot,
                conditions=new(){new(){type=EnemyStrategyConditionType.HealthRatioAtMost,healthRatio=.3f}}});
            Assert(s.BM.AIController.PreviewEnemyTarget(e)==b,"valid committed plan changed");
            e.unitAttrCenter.CurHealth=30;Assert(s.BM.AIController.PreviewEnemyTarget(e)==a,"obsolete committed plan survived switch");
        });
        Test("Clicked enemy shows ranged ring and follows position and range changes",()=>{
            var s=Fixture();var e=Enemy(s);s.CM.Hover(e);
            Assert(s.CM.InspectionRange==null,"hover opened attack range");
            s.CM.PinEnemyTargetLine(e);var ring=s.CM.InspectionRange;
            Assert(ring.gameObject.activeSelf,"clicked range hidden");Near(ring.Radius,5,"ranged range not preferred");
            e.transform.position=new Vector3(7,0,0);e.pieceData.rangedAtk.rangeValue=9;s.CM.Tick();
            Near(ring.Position.x,7,"range center stale");Near(ring.Radius,9,"range radius stale");
            s.CM.Hover(null);Assert(ring.gameObject.activeSelf,"pointer exit hid clicked range");
        });
        Test("Zero or missing ranged attack falls back to melee and missing attacks hide ring",()=>{
            var s=Fixture();var e=Enemy(s);e.pieceData.rangedAtk.rangeValue=0;s.CM.PinEnemyTargetLine(e);
            Near(s.CM.InspectionRange.Radius,2,"zero ranged did not use melee");
            e.pieceData.rangedAtk=null;s.CM.Tick();Near(s.CM.InspectionRange.Radius,2,"missing ranged did not use melee");
            e.pieceData.meleeAtk=null;s.CM.Tick();Assert(!s.CM.InspectionRange.gameObject.activeSelf,"missing attacks left range visible");
        });
        Test("Switching pinned enemy reuses ring and interaction and invalid state clear it",()=>{
            var s=Fixture();var e=Enemy(s);var other=Enemy(s,1);s.CM.PinEnemyTargetLine(e);var ring=s.CM.InspectionRange;
            other.pieceData.rangedAtk.rangeValue=11;s.CM.PinEnemyTargetLine(other);
            Assert(ring==s.CM.InspectionRange,"created a second ring");Near(ring.Radius,11,"old enemy radius retained");
            other.isDead=true;s.CM.Tick();Assert(!ring.gameObject.activeSelf,"dead enemy range retained");
            other.isDead=false;s.CM.PinEnemyTargetLine(other);s.CM.ClearEnemyTargetLineInteraction();Assert(!ring.gameObject.activeSelf,"cancel retained range");
            s.CM.PinEnemyTargetLine(e);s.CM.Drag(s.BM.PlayerController.pieces[0],true);s.CM.Tick();Assert(!ring.gameObject.activeSelf,"movement retained range");
            s.CM.EndDrag();s.CM.Tick();Assert(ring.gameObject.activeSelf,"range did not recover");
            s.BM.CanInspectEnemyTargets=false;s.CM.Tick();Assert(!ring.gameObject.activeSelf,"blocked inspection retained range");
        });
        Test("Hover enter displays and empty-space exit hides",()=>{
            var s=Fixture();var e=Enemy(s);s.CM.Hover(e);Assert(e.tagetLine.enabled&&e.Outline,"enter failed");
            s.CM.Hover(null);Assert(!e.tagetLine.enabled&&!e.Outline,"exit failed");
        });
        Test("Pinned line survives pointer exit and new pin closes the previous line",()=>{
            var s=Fixture();var e=Enemy(s);var other=Enemy(s,1);s.CM.Hover(e);e.ShowTargetLine();s.CM.Hover(null);
            Assert(e.tagetLine.enabled,"pin lost on exit");s.CM.PinEnemyTargetLine(other);
            Assert(!e.tagetLine.enabled&&other.tagetLine.enabled,"old pin survived selection");
        });
        Test("Selecting another piece clears pin even when pointer stays over enemy",()=>{
            var s=Fixture();var e=Enemy(s);s.CM.Hover(e);s.CM.PinEnemyTargetLine(e);s.CM.ClearEnemyTargetLines(true);s.CM.Tick();
            Assert(!e.tagetLine.enabled,"line reopened on same pointer");s.CM.Hover(null);s.CM.Hover(e);Assert(e.tagetLine.enabled,"hover failed to recover");
        });
        Test("Hovering another enemy does not remove a clicked pin",()=>{
            var s=Fixture();var e=Enemy(s);var other=Enemy(s,1);s.CM.PinEnemyTargetLine(e);s.CM.Hover(other);
            Assert(e.tagetLine.enabled&&other.tagetLine.enabled,"independent visibility failed");s.CM.Hover(null);
            Assert(e.tagetLine.enabled&&!other.tagetLine.enabled,"hover exit affected pin");
        });
        Test("Visible endpoints and selected threat update after enemy and player movement",()=>{
            var s=Fixture();var e=Enemy(s);s.CM.PinEnemyTargetLine(e);Near(e.tagetLine.Positions.Last().x,3,"original target");
            e.transform.position=new Vector3(9,0,0);s.CM.Tick();Near(e.tagetLine.Positions[0].x,9,"enemy endpoint stale");Near(e.tagetLine.Positions.Last().x,8,"new nearest target stale");
            s.BM.PlayerController.pieces[1].transform.position=new Vector3(8.5f,0,0);s.CM.Tick();Near(e.tagetLine.Positions.Last().x,8.5f,"target endpoint stale");
        });
        Test("Destination preview can switch target without mutating positions or damage records",()=>{
            var s=Fixture();var e=Enemy(s);var p=s.BM.PlayerController.pieces[0];var before=p.transform.position;
            var target=s.BM.AIController.CheckEnemyTarget(e,p,new Vector3(20,0,0));
            Assert(target==s.BM.PlayerController.pieces[1],"preview failed to switch target");Near(p.transform.position.x,before.x,"real pawn moved");Assert(e.damageDic.Count==0,"preview modified aggro");
        });
        Test("Preview shares taunt and historical damage selection rules",()=>{
            var s=Fixture();var e=Enemy(s);var a=s.BM.PlayerController.pieces[0];var b=s.BM.PlayerController.pieces[1];
            e.damageDic[b]=10;Assert(s.BM.AIController.CheckEnemyTarget(e)==b,"highest damage bonus lost");
            a.unitAttrCenter.TauntValue=4;Assert(s.BM.AIController.CheckEnemyTarget(e,a,new Vector3(20,0,0))==a,"taunt ignored in preview");
        });
        Test("Dead inactive and untargetable players cannot be forecast targets",()=>{
            var s=Fixture();var e=Enemy(s);var a=s.BM.PlayerController.pieces[0];var b=s.BM.PlayerController.pieces[1];
            a.isDead=true;b.Blocked=true;Assert(s.BM.AIController.CheckEnemyTarget(e)==null,"invalid target accepted");
            b.Blocked=false;b.gameObject.activeInHierarchy=false;Assert(s.BM.AIController.CheckEnemyTarget(e)==null,"inactive target accepted");
        });
        Test("Normal intent follows committed plan and move preview forecasts next selection",()=>{
            var s=Fixture();var e=Enemy(s);var a=s.BM.PlayerController.pieces[0];var b=s.BM.PlayerController.pieces[1];
            s.BM.AIController.CommitForTest(e,b);Assert(s.BM.AIController.PreviewEnemyTarget(e)==b,"committed plan hidden");
            Assert(s.BM.AIController.PreviewEnemyTarget(e,a,new Vector3(1,0,0))==a,"forecast did not recalculate");
        });
        Test("Movement icon uses navigable destination and breathes only threats to moving pawn",()=>{
            var s=Fixture();var p=s.BM.PlayerController.pieces[0];var m=s.BM.moveManager;
            m.ValidPreview=true;m.PreviewPawn=p.gameObject;m.Destination=new Vector3(1,0,0);s.CM.Drag(p);s.CM.Tick();
            Assert(Enemy(s).tagetLine.enabled && Enemy(s).tagetLine.gameObject.activeSelf,"missing moving-pawn threat");
            Assert(!Enemy(s,1).tagetLine.enabled && !Enemy(s,1).tagetLine.gameObject.activeSelf,"unrelated target line shown");
            Near(Enemy(s).tagetLine.Positions.Last().x,1,"endpoint used actual position instead of valid destination");
            Near(p.transform.position.x,3,"forecast moved pawn");
            float alpha=Enemy(s).tagetLine.startColor.a;Time.unscaledTime=0.3f;s.CM.Tick();Assert(Enemy(s).tagetLine.startColor.a!=alpha,"no breathing");
        });
        Test("Initially inactive line object is activated when it threatens moving pawn",()=>{
            var s=Fixture();var e=Enemy(s);var p=s.BM.PlayerController.pieces[0];
            e.tagetLine.gameObject.SetActive(false);e.tagetLine.enabled=false;
            s.CM.Drag(p,true);s.CM.Tick();
            Assert(e.tagetLine.enabled && e.tagetLine.gameObject.activeInHierarchy,"renderer enabled without object activation");
        });
        Test("Position changes remove old threats and activate new threats every frame",()=>{
            var s=Fixture();var p=s.BM.PlayerController.pieces[0];var m=s.BM.moveManager;
            m.ValidPreview=true;m.PreviewPawn=p.gameObject;m.Destination=new Vector3(1,0,0);s.CM.Drag(p);s.CM.Tick();
            Assert(Enemy(s).tagetLine.enabled&&!Enemy(s,1).tagetLine.enabled,"initial threat set wrong");
            m.Destination=new Vector3(11,0,0);s.CM.Tick();
            Assert(!Enemy(s).tagetLine.enabled&&!Enemy(s).tagetLine.gameObject.activeSelf,"old threat remained visible");
            Assert(Enemy(s,1).tagetLine.enabled&&Enemy(s,1).tagetLine.gameObject.activeSelf,"new threat was not activated");
            Near(Enemy(s,1).tagetLine.Positions.Last().x,11,"new threat aimed at wrong location");
            float alpha=Enemy(s,1).tagetLine.startColor.a;Time.unscaledTime=0.3f;s.CM.Tick();
            Assert(Enemy(s,1).tagetLine.startColor.a!=alpha,"new threat did not breathe");
        });
        Test("Pinned and hovered unrelated enemies are hidden during movement",()=>{
            var s=Fixture();var p=s.BM.PlayerController.pieces[0];var unrelated=Enemy(s,1);
            s.CM.PinEnemyTargetLine(unrelated);s.CM.Hover(unrelated);s.CM.Drag(p,true);s.CM.Tick();
            Assert(Enemy(s).tagetLine.enabled&&!unrelated.tagetLine.enabled,"pin or hover bypassed movement filter");
            s.CM.EndDrag();s.CM.Tick();Assert(unrelated.tagetLine.enabled,"normal pinned visibility did not recover");
        });
        Test("Actual movement changes threat set and invalid enemies stay hidden",()=>{
            var s=Fixture();var p=s.BM.PlayerController.pieces[0];s.BM.moveManager.MovingPiece=p;s.CM.Tick();
            Assert(Enemy(s).tagetLine.enabled&&!Enemy(s,1).tagetLine.enabled,"initial actual-move set wrong");
            p.transform.position=new Vector3(11,0,0);s.CM.Tick();
            Assert(!Enemy(s).tagetLine.enabled&&Enemy(s,1).tagetLine.enabled,"actual movement did not update threats");
            Enemy(s,1).isActived=false;s.CM.Tick();Assert(!Enemy(s,1).tagetLine.enabled,"unactivated enemy shown");
            Enemy(s,1).isActived=true;Enemy(s,1).isDead=true;s.CM.Tick();Assert(!Enemy(s,1).tagetLine.enabled,"dead enemy shown");
        });
        Test("Hiding a line on enemy root never disables the enemy",()=>{
            var s=Fixture();var e=Enemy(s);e.tagetLine.gameObject=e.gameObject;e.RenderTargetLine();e.HideTargetLine();
            Assert(e.gameObject.activeSelf&&!e.tagetLine.enabled,"root enemy was disabled");
        });
        Test("Invalid navigation preview never uses a stale destination",()=>{
            var s=Fixture();var p=s.BM.PlayerController.pieces[0];var m=s.BM.moveManager;
            m.ValidPreview=true;m.PreviewPawn=p.gameObject;m.Destination=new Vector3(1,0,0);s.CM.Drag(p);s.CM.Tick();
            m.ValidPreview=false;s.CM.Tick();Assert(!Enemy(s).tagetLine.enabled&&!Enemy(s,1).tagetLine.enabled,"stale invalid forecast survived");
        });
        Test("Direct drag and actual movement continuously follow pawn position",()=>{
            var s=Fixture();var p=s.BM.PlayerController.pieces[0];s.CM.Drag(p,true);p.transform.position=new Vector3(1,0,0);s.CM.Tick();Near(Enemy(s).tagetLine.Positions.Last().x,1,"drag endpoint stale");
            s.CM.EndDrag();s.BM.moveManager.MovingPiece=p;p.transform.position=new Vector3(2,0,0);s.CM.Tick();Near(Enemy(s).tagetLine.Positions.Last().x,2,"moving endpoint stale");
            s.BM.moveManager.MovingPiece=null;s.CM.Tick();Assert(!Enemy(s).tagetLine.enabled,"movement preview never ended");
        });
        Test("Breathing style restores after preview and enemy pool disable",()=>{
            var s=Fixture();var e=Enemy(s);e.RenderTargetLine(breathe:true);Assert(e.tagetLine.widthMultiplier!=2,"width unchanged");
            e.HideTargetLine();Near(e.tagetLine.startColor.a,0.8f,"alpha leaked");Near(e.tagetLine.widthMultiplier,2,"width leaked");
            e.RenderTargetLine(breathe:true);e.DisableForTest();Assert(!e.tagetLine.enabled,"disabled enemy retained line");Near(e.tagetLine.endColor.a,0.6f,"end alpha leaked");
        });
        Test("Round end and interaction-manager disable clear all lines",()=>{
            var s=Fixture();s.CM.PinEnemyTargetLine(Enemy(s));s.CM.Hover(Enemy(s,1));s.BM.CanInspectEnemyTargets=false;s.CM.Tick();
            Assert(!Enemy(s).tagetLine.enabled&&!Enemy(s,1).tagetLine.enabled,"round end leaked lines");
            s.BM.CanInspectEnemyTargets=true;s.CM.PinEnemyTargetLine(Enemy(s));s.CM.DisableForTest();Assert(!Enemy(s).tagetLine.enabled,"manager disable leaked line");
        });
        Test("Death deactivation and removal clear previously visible enemy lines",()=>{
            var s=Fixture();var e=Enemy(s);s.CM.PinEnemyTargetLine(e);e.isDead=true;s.CM.Tick();Assert(!e.tagetLine.enabled,"dead line survived");
            e.isDead=false;s.CM.PinEnemyTargetLine(e);s.BM.AIController.pieces.Remove(e);s.CM.Tick();Assert(!e.tagetLine.enabled,"removed line survived");
        });
        Console.WriteLine($"{passed} enemy target line regression tests passed.");
    }
}
