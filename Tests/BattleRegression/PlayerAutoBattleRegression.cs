using System;
using System.IO;
using System.Linq;
using Lunatic.BattleTesting;
using UnityEngine;
using UnityEngine.AI;

static partial class Program
{
    static (BattleManager battle,PieceController actor,EnemyController enemy,GameObject owner) AutoFixture(float distance=4)
    {
        var battle=BattleScene.Ins.BM;var actor=Unit();var enemy=(EnemyController)Unit(false,true);
        actor.gameObject.Add(new NavMeshAgent());enemy.gameObject.Add(new NavMeshAgent());enemy.transform.position=new(0,0,distance);
        actor.pieceData.meleeAtk=BarrelAttack();actor.pieceData.meleeAtk.rangeValue=2;actor.pieceData.rangedAtk=BarrelAttack();actor.pieceData.rangedAtk.rangeValue=8;
        battle.PlayerController.pieces.Add(actor);battle.AIController.pieces.Add(enemy);battle.ActivePiece=actor;
        ExplosiveColliders(actor,enemy);var owner=new GameObject();Assert(battle.TryAcquirePlayerControl(owner),"lease acquisition failed");return(battle,actor,enemy,owner);
    }
    static void PlayerAutoBattleRegression()
    {
        Test("External player control is exclusive and rejects wrong owner, enemy and non-current actors",()=>{
            var(b,p,e,owner)=AutoFixture();var command=new PlayerBattleCommand{kind=PlayerBattleCommandKind.Ranged,position=e.transform.position};
            Assert(!b.TryAcquirePlayerControl(new GameObject()),"second owner acquired lease");
            Assert(b.TryIssuePlayerCommand(new GameObject(),p,command)==BattleCommandResult.Rejected,"wrong owner issued command");
            Assert(b.TryIssuePlayerCommand(owner,e,command)==BattleCommandResult.Rejected,"enemy received player command");
            b.ActivePiece=null;Assert(b.TryIssuePlayerCommand(owner,p,command)==BattleCommandResult.Rejected,"inactive actor acted");
            b.ReleasePlayerControl(new GameObject());Assert(b.HasExternalPlayerControl,"wrong owner released control");b.ReleasePlayerControl(owner);Assert(!b.HasExternalPlayerControl,"lease leaked");
        });
        Test("Programmatic attack uses authoritative query and charges AP and ammo once before presentation",()=>{
            var(b,p,e,owner)=AutoFixture();int ap=p.unitAttrCenter.CurMovePoint,ammo=p.unitAttrCenter.AmmoCount;
            var result=b.TryIssuePlayerCommand(owner,p,new PlayerBattleCommand{kind=PlayerBattleCommandKind.Ranged,position=e.transform.position});
            Assert(result==BattleCommandResult.Executed&&p.IsPerformingAction&&p.CommandTargets.SequenceEqual(new[]{e}),"formal combat path not entered");
            Assert(p.unitAttrCenter.CurMovePoint==ap-1&&p.unitAttrCenter.AmmoCount==ammo-1,"wrong attack costs");
            b.HasCombatPresentation=true;Assert(b.TryIssuePlayerCommand(owner,p,new PlayerBattleCommand{kind=PlayerBattleCommandKind.Ranged,position=e.transform.position})==BattleCommandResult.Rejected,"overlapping attack accepted");
        });
        Test("Programmatic commands reject locked actions, invalid aim, empty targets and failed resource checks without charging",()=>{
            var(b,p,e,owner)=AutoFixture();int ap=p.unitAttrCenter.CurMovePoint;p.availableActions.Remove(ActionType.远程攻击);
            var shot=new PlayerBattleCommand{kind=PlayerBattleCommandKind.Ranged,position=e.transform.position};
            Assert(b.TryIssuePlayerCommand(owner,p,shot)==BattleCommandResult.Rejected,"locked action accepted");p.availableActions.Add(ActionType.远程攻击);
            shot.position=new(float.NaN,0,0);Assert(b.TryIssuePlayerCommand(owner,p,shot)==BattleCommandResult.Rejected,"invalid aim accepted");
            shot.position=new(8,0,0);Assert(b.TryIssuePlayerCommand(owner,p,shot)==BattleCommandResult.Rejected,"empty attack charged");
            var skill=BarrelAttack();skill.mpCost=99;p.availableSkills.Add(skill);
            Assert(b.TryIssuePlayerCommand(owner,p,new PlayerBattleCommand{kind=PlayerBattleCommandKind.Skill,skill=skill,position=e.transform.position})==BattleCommandResult.Rejected,"unaffordable skill accepted");
            Assert(p.unitAttrCenter.CurMovePoint==ap,"rejection spent AP");
        });
        Test("Action-triggered interruption consumes the real cost but cannot begin an attack",()=>{
            var(b,p,e,owner)=AutoFixture();BattleScene.Ins.BM.buffManager.AddBuff(p.unitAttrCenter,BuffType.SpontaneousMemeticAttack,3);
            p.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-99);int ap=p.unitAttrCenter.CurMovePoint;
            var result=b.TryIssuePlayerCommand(owner,p,new PlayerBattleCommand{kind=PlayerBattleCommandKind.Ranged,position=e.transform.position});
            Assert(result==BattleCommandResult.Interrupted&&p.unitAttrCenter.CurMovePoint==ap-1&&!p.IsPerformingAction,"interrupted command did not preserve costs");
        });
        Test("External outcome is observed exactly once and remains intercepted after control is released",()=>{
            var(b,p,e,owner)=AutoFixture();int observed=0;b.BattleCompleted+=outcome=>{observed++;b.ReleasePlayerControl(owner);};
            Assert(b.CompleteForTest(BattleOutcome.PlayerVictory),"callback released suppression too early");
            Assert(b.CompleteForTest(BattleOutcome.PlayerVictory)&&observed==1&&!b.IsBattleRunning,"duplicate result escaped interception");
        });
        Test("Player AI selects legal fire, reloads empty magazines and ends unreachable turns",()=>{
            var(b,p,e,owner)=AutoFixture();var policy=new PlayerAutoBattlePolicy(new());
            Assert(policy.Decide(b,p).command.kind==PlayerBattleCommandKind.Ranged,"AI failed to shoot reachable enemy");
            while(p.unitAttrCenter.AmmoCount>0)p.unitAttrCenter.CostAmmo();Assert(policy.Decide(b,p).command.kind==PlayerBattleCommandKind.Reload,"empty magazine not reloaded");
            p.unitAttrCenter.FullAmmo();e.transform.position=new(0,0,100);b.moveManager.NavigationAllowed=false;
            Assert(policy.Decide(b,p).command.kind==PlayerBattleCommandKind.EndTurn,"unreachable actor failed to yield");
        });
        Test("Player AI moves with real path budgeting and favors AOE skills that hit more targets",()=>{
            var(b,p,e,owner)=AutoFixture(20);var policy=new PlayerAutoBattlePolicy(new());var move=policy.Decide(b,p);
            Assert(move.command.kind==PlayerBattleCommandKind.Move&&Vector3.Distance(p.transform.position,move.command.position)<=p.unitAttrCenter.MoveRange+.01f,"move exceeded real path budget");
            e.transform.position=new(0,0,5);var other=(EnemyController)Unit(false,true);other.transform.position=new(1,0,5);b.AIController.pieces.Add(other);ExplosiveColliders(p,e,other);
            var skill=BarrelAttack();skill.target=SkillTarget.EnemyAll;skill.rangeType=RangeType.Grenade;skill.explodeRadius=2;p.availableSkills.Add(skill);
            Assert(policy.Decide(b,p).command.skill==skill,"multi-target skill did not win scoring");
        });
        Test("Player AI healing and conservative resource settings do not mutate actors during planning",()=>{
            var(b,p,e,owner)=AutoFixture();p.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-70);int hp=p.unitAttrCenter.CurHealth,ap=p.unitAttrCenter.CurMovePoint;
            var heal=Skill();heal.enemySkillRole=EnemySkillRole.Healing;heal.target=SkillTarget.Self;p.availableSkills.Add(heal);
            var choice=new PlayerAutoBattlePolicy(new()).Decide(b,p);Assert(choice.command.skill==heal,"injured player did not heal");
            Assert(p.unitAttrCenter.CurHealth==hp&&p.unitAttrCenter.CurMovePoint==ap,"planning mutated combat state");
            p.availableSkills.Clear();var expensive=BarrelAttack();expensive.consumeItems.Add(new ItemPack(ItemName.A,1));p.availableSkills.Add(expensive);Stock();
            Assert(new PlayerAutoBattlePolicy(new()).Decide(b,p).command.skill!=expensive,"consumable skill used without opt-in");
        });
        Test("Player AI commands can defeat an enemy through real damage settlement without UI or an AI service",()=>{
            var(b,p,e,owner)=AutoFixture();e.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-85);var decision=new PlayerAutoBattlePolicy(new()).Decide(b,p);
            Assert(b.TryIssuePlayerCommand(owner,p,decision.command)==BattleCommandResult.Executed,"AI command failed");
            foreach(var target in p.CommandTargets)b.damageManager.ResolveSkillTarget(p,target,p.LastSkill);
            Assert(e.isDead&&p.unitAttrCenter.CurMovePoint<3,"AI did not kill through normal settlement");
        });
        Test("Balance reports preserve failure categories and append a parseable CSV summary",()=>{
            string directory=Path.Combine(Environment.CurrentDirectory,"obj","PlayerAutoBattleReports",Guid.NewGuid().ToString("N"));
            var report=new BattleBalanceReport{run=1,seed=123,scene="test,level",result=BattleBalanceResult.Stalled,policy=new(),message="no progress"};
            string path=BattleBalanceReportWriter.Write(directory,report);report.run=2;report.result=BattleBalanceResult.Victory;BattleBalanceReportWriter.Write(directory,report);
            string json=File.ReadAllText(path);using var parsed=System.Text.Json.JsonDocument.Parse(json);
            Assert(parsed.RootElement.GetProperty("seed").GetInt32()==123&&File.ReadAllLines(Path.Combine(directory,"summary.csv")).Length==3,"report export failed");
            Assert(File.ReadAllText(Path.Combine(directory,"summary.csv")).Contains("\"test,level\""),"CSV escaping failed");
            var summary=new BattleBalanceSummary();report.result=BattleBalanceResult.Stalled;summary.Add(report);report.result=BattleBalanceResult.Victory;summary.Add(report);
            Assert(summary.incomplete==1&&summary.victories==1&&summary.winRateAllRuns==.5f&&summary.winRateCompletedRuns==1f,"unfinished run inflated completed result count");
            BattleBalanceReportWriter.WriteSummary(directory,summary);Assert(File.Exists(Path.Combine(directory,"suite-summary.json")),"suite summary missing");
        });
        Test("End-turn commands drain only the active AP and do not end an interrupted normal turn during burst",()=>{
            var(b,p,e,owner)=AutoFixture();p.player.isBursting=true;int changes=b.TurnChanges;
            Assert(b.TryIssuePlayerCommand(owner,p,new PlayerBattleCommand{kind=PlayerBattleCommandKind.EndTurn})==BattleCommandResult.Executed,"end turn rejected");
            Assert(p.unitAttrCenter.CurMovePoint==0&&p.isIdle&&b.TurnChanges==changes,"burst end leaked into normal turn completion");
        });
    }
}
