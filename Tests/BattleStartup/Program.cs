using System;
using System.Linq;
using UnityEngine;
static class Program
{
    static int passed;
    static void Assert(bool condition,string message) { if(!condition)throw new Exception(message); }
    static BattleManager Fixture(int enemies=3,bool initialized=true)
    {
        var b=new BattleManager();BattleScene.Ins=new BattleScene {BM=b};
        b.PlayerController.pieces.Add(new PieceController());
        for(int i=0;i<enemies;i++)b.AIController.pieces.Add(new EnemyController());
        if(initialized)b.InitializeFixture();return b;
    }
    static void Finish(Coroutine c,bool ignoreStop=false) { int steps=0;while(c.Step(ignoreStop))if(++steps>100)throw new Exception("hung startup"); }
    static void Test(string name,Action body) { body();passed++;Console.WriteLine("PASS "+name); }
    static void Main()
    {
        Test("Repeated start and public sweep keep exactly one routine and initialization",()=>{
            var b=Fixture();b.StartBattle();var c=b.Routines.Single();c.Step();
            b.StartBattle();b.SweepActiveEnemies();Assert(b.Routines.Count==1 && b.characterSkillManager.InitCalls==1,"duplicate startup");
            b.OnClickSkip();Assert(c.Stopped && b.PlayerStarts==1,"skip lost original handle");
            Finish(c,true);Assert(b.PlayerStarts==1,"old routine entered twice");
        });
        Test("Skip during initial delay cancels before first enemy",()=>{
            var b=Fixture();b.StartBattle();var c=b.Routines.Single();b.OnClickSkip();Finish(c,true);
            Assert(b.cameraController.FollowHistory.Count==1 && b.cameraController.FollowHistory[0]==b.PlayerController.pieces[0].transform,"enemy followed after skip");
            Assert(b.PlayerStarts==1 && !BattleScene.Ins.UM.skipButton.gameObject.activeInHierarchy,"skip did not enter");
        });
        Test("Skip while moving stops further enemies and stale start requests",()=>{
            var b=Fixture();b.StartBattle();var c=b.Routines.Single();c.Step();b.OnClickSkip();
            int follows=b.cameraController.FollowHistory.Count;b.StartBattle();b.OnClickSkip();Finish(c,true);
            Assert(b.PlayerStarts==1 && b.battleDialogueManager.Calls==1 && b.cameraController.FollowHistory.Count==follows,"late patrol or repeated entry");
        });
        Test("Natural end and subsequent skip enter battle only once",()=>{
            var b=Fixture();b.StartBattle();Finish(b.Routines.Single());b.OnClickSkip();b.StartBattle();
            Assert(b.PlayerStarts==1 && b.tutorialManager.Calls==1 && b.Routines.Count==1,"duplicated natural end");
            Assert(b.cameraController.FollowHistory.Count==4,"did not visit each enemy then restore player");
        });
        Test("No enemies or missing camera still enters battle",()=>{
            foreach(bool missingCamera in new[]{false,true}){
                var b=Fixture(0);if(missingCamera)b.cameraController=null;
                b.StartBattle();Finish(b.Routines.Single());Assert(b.PlayerStarts==1,"empty patrol hung");
            }
        });
        Test("Patrol snapshots list and excludes dead inactive and non-enemy entries",()=>{
            var b=Fixture();b.AIController.pieces[0].isDead=true;b.AIController.pieces[1].gameObject.activeInHierarchy=false;
            b.AIController.pieces.Add(new PieceController());b.StartBattle();var c=b.Routines.Single();c.Step();
            b.AIController.pieces.Add(new EnemyController());Finish(c);
            Assert(b.cameraController.FollowHistory.Count==2,"unexpected enemy or list mutation");
        });
        Test("Tutorial entry completion is idempotent and cannot restart patrol",()=>{
            var b=Fixture();b.tutorialManager.HasTutorial=true;b.StartBattle();b.OnClickSkip();
            Assert(b.PlayerStarts==0 && b.tutorialManager.Calls==1,"tutorial skipped");
            b.StartBattle();b.OnClickSkip();b.CompleteBattleIntroduction();b.CompleteBattleIntroduction();
            Assert(b.PlayerStarts==1 && b.battleDialogueManager.Calls==1 && b.Routines.Count==1,"tutorial replayed entry");
        });
        Test("Early start waits for initialization and duplicate requests coalesce",()=>{
            var b=Fixture(initialized:false);b.StartBattle();b.StartBattle();Assert(b.Routines.Count==0,"started before initialization");
            b.InitializeFixture();Finish(b.Routines.Single());Assert(b.PlayerStarts==1 && b.characterSkillManager.InitCalls==1,"deferred request lost");
        });
        Test("Reentrant skip from skill initialization cannot leave a routine behind",()=>{
            var b=Fixture();b.characterSkillManager.OnInit=b.OnClickSkip;b.StartBattle();
            Assert(b.Routines.Count==0 && b.PlayerStarts==1,"reentrant startup survived skip");
        });
        Test("Reentrant skip from button activation is handled before handle assignment",()=>{
            var b=Fixture();BattleScene.Ins.UM.skipButton.gameObject.ActiveChanged=visible=>{if(visible)b.OnClickSkip();};b.StartBattle();
            Assert(b.Routines.Count==0 && b.PlayerStarts==1,"button activation left patrol running");
        });
        Test("Disable or scene exit invalidates even a forced stale coroutine continuation",()=>{
            foreach(bool disable in new[]{false,true}){
                var b=Fixture();b.StartBattle();var c=b.Routines.Single();c.Step();
                if(disable)b.DisableFixture();else b.EndFixture();
                int follows=b.cameraController.FollowHistory.Count;Finish(c,true);b.OnClickSkip();b.CompleteBattleIntroduction();
                Assert(b.PlayerStarts==0 && follows==b.cameraController.FollowHistory.Count,"cancelled scene entered");
            }
        });
        Test("Old startup generation cannot affect a newly initialized battle",()=>{
            var b=Fixture();b.StartBattle();var old=b.Routines.Single();old.Step();b.DisableFixture();
            b.isActiveAndEnabled=true;b.InitializeFixture();b.StartBattle();var current=b.Routines.Last();
            Finish(old,true);Assert(b.PlayerStarts==0,"old generation entered new battle");
            Finish(current);Assert(b.PlayerStarts==1,"new generation failed");
        });
        Test("Old scene manager cannot change new scene skip UI",()=>{
            var old=Fixture();old.StartBattle();var current=Fixture();current.StartBattle();old.DisableFixture();
            Assert(BattleScene.Ins.UM.skipButton.gameObject.activeInHierarchy,"old scene hid new skip button");
        });
        Test("Exit from battle-start dialogue prevents a late player turn",()=>{
            var b=Fixture();b.battleDialogueManager.OnStart=b.EndFixture;b.StartBattle();b.OnClickSkip();
            Assert(b.PlayerStarts==0 && b.battleDialogueManager.Calls==1,"player turn started after exit");
        });
        Console.WriteLine($"{passed} battle startup regression tests passed.");
    }
}
