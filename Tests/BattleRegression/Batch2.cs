using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SkillSystem;

static partial class Program
{
    static void Batch2()
    {
        Test("Successor removes only its own bonus after healing", () => {
            var pc=Unit(); var u=pc.unitAttrCenter; u.ATK=100; Attach(pc,new SkillSuccessor());
            u.SetHealth(20); Assert(u.ATK==120 && u.MoveRange==15,"Bonus not applied");
            u.ModifyAttribute(UnitAttrType.ATK,5); u.Heal(20);
            Assert(u.ATK==105 && u.MoveRange==5,"Healing failed to remove original bonus");
            u.SetHealth(20); Assert(u.ATK==126,"Reactivation wrong"); u.FullHealth();
            Assert(u.ATK==105 && u.MoveRange==5,"Repeated activation drifted");
        });
        Test("Successor removal covers death, direct health mutation and max health", () => {
            var pc=Unit(); var u=pc.unitAttrCenter; u.ATK=100; Attach(pc,new SkillSuccessor());
            u.ModifyAttribute(UnitAttrType.CurHealth,-80); Assert(u.ATK==120,"Direct change missing");
            u.TakeDamage(new AttackPack(30,DamageType.Melee)); Assert(pc.isDead && u.ATK==100,"Death retained bonus");
            u.SetHealth(20); Assert(u.ATK==120,"Revival missing");
            u.ModifyAttribute(UnitAttrType.MaxHealth,20); Assert(u.ATK==100,"Max health change missing");
        });
        Test("Passive unregister and registry reset revoke bonuses", () => {
            var pc=Unit(); var u=pc.unitAttrCenter; u.ATK=100; Attach(pc,new SkillSuccessor()); u.SetHealth(20);
            var manager=BattleScene.Ins.BM.characterSkillManager; manager.UnregisterPiece(pc.gameObject);
            Assert(u.ATK==100 && u.MoveRange==5,"Unregister leaked bonus");
            Attach(pc,new SkillSuccessor()); manager.NotifyHpChanged(pc.gameObject,u.CurHealth,u.MaxHealth);
            manager.ClearAllRegistry(); Assert(u.ATK==100 && u.MoveRange==5,"Reset leaked bonus");
        });
        Test("Registration deduplicates skills and initializes low-health passive", () => {
            var pc=Unit(); var u=pc.unitAttrCenter; u.ATK=100; u.SetHealth(20);
            var manager=BattleScene.Ins.BM.characterSkillManager;
            typeof(CharacterSkillManager).GetField("skillConfigSO",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,new PassiveSkillConfigSO());
            var types=new List<PassiveSkillType>{PassiveSkillType.Successor,PassiveSkillType.Successor};
            manager.RegisterPiece(pc.gameObject,types); Assert(u.ATK==120 && u.MoveRange==15,"Registration duplicated/missed bonus");
            manager.RegisterPiece(pc.gameObject,types); Assert(u.ATK==120 && u.MoveRange==15,"Re-registration stacked bonus");
        });
        Test("Offensive analysis uses a real failed recognition roll", () => {
            var a=Unit(); var t=Unit(false,true); t.unitAttrCenter.CON=30; Attach(a,new SkillOffensiveAnalysis());
            var manager=BattleScene.Ins.BM.characterSkillManager;
            Assert(manager.EvaluateDamageMultiplier(a.gameObject,t.gameObject)==1,"Unexpected immediate bonus");
            Assert(manager.PreviewDamageMultiplier(a.gameObject,t.gameObject)==1 && TestEvents.Recognition==0,"Failed roll empowered attack");
        });
        Test("Offensive analysis preview is read-only and next attack consumes once", () => {
            var a=Unit(); var t=Unit(false,true); t.unitAttrCenter.CON=8; Attach(a,new SkillOffensiveAnalysis());
            var manager=BattleScene.Ins.BM.characterSkillManager; UnityEngine.Random.NextValue=6;
            Assert(manager.EvaluateDamageMultiplier(a.gameObject,t.gameObject)==1 && TestEvents.Recognition==1,"Recognition should prime next attack");
            var s=Skill(); s.attackPacks.Add(new AttackPack(10,DamageType.Melee)); a.unitAttrCenter.critRate=100;
            var damage=new DamageManager(BattleScene.Ins.BM);
            for(int i=0;i<3;i++) Assert(damage.PreviewSkill(a,t,s).Total.MaxDamage==25,"Preview consumed/ignored bonus");
            float multiplier=manager.EvaluateDamageMultiplier(a.gameObject,t.gameObject);
            Assert(multiplier==2.5f && manager.PreviewDamageMultiplier(a.gameObject,t.gameObject)==1,"Bonus not consumed once");
            damage.ResolveSkillTarget(a,t,s,passiveMultiplier:multiplier);
            Assert(t.unitAttrCenter.CurHealth==75,"Actual damage disagrees with preview");
        });
        Test("Real manager dispatches cast and turn events only to owner", () => {
            var a=Unit(); var t=Unit(false,true); var manager=BattleScene.Ins.BM.characterSkillManager;
            manager.NotifyCastActiveSkill(a.gameObject); manager.NotifyTurnEnd(a.gameObject);
            Assert(TestEvents.Casts==1 && TestEvents.Turns==1,"Event broadcast/duplication");
            manager.UnregisterPiece(a.gameObject); manager.NotifyTurnEnd(a.gameObject); Assert(TestEvents.Turns==1,"Unregistered passive invoked");
        });
        Test("Enemy-side allies use relative faction", () => {
            var caster=Unit(false,true); var ally=Unit(false,true); var enemy=Unit(); var skill=Skill(); skill.target=SkillTarget.Ally;
            var result=SkillTargeting.Filter(caster,new[]{enemy,ally},skill,ally.transform.position);
            Assert(result.Count==1 && result[0]==ally,"Enemy caster selected opponent as ally");
        });
        Test("Enemy filtering rejects friends, corpses and inactive units", () => {
            var caster=Unit(); var friend=Unit(); var alive=Unit(false,true); var dead=Unit(false,true); dead.unitAttrCenter.SetHealth(0);
            var inactive=Unit(false,true); inactive.gameObject.SetActive(false);
            var result=SkillTargeting.Filter(caster,new[]{friend,alive,dead,inactive,alive},Skill(),caster.transform.position);
            Assert(result.Count==1 && result[0]==alive,"Invalid or duplicate enemy selected");
        });
        Test("Revive targets accept only allied corpses", () => {
            var caster=Unit(); var alive=Unit(); var dead=Unit(); dead.unitAttrCenter.SetHealth(0); var enemy=Unit(false,true); enemy.unitAttrCenter.SetHealth(0);
            var skill=Skill(); skill.target=SkillTarget.AllyBody;
            var result=SkillTargeting.Filter(caster,new[]{alive,dead,enemy},skill,dead.transform.position);
            Assert(result.Count==1 && result[0]==dead,"Invalid revival target");
        });
        Test("Cognitive protection blocks enemies but not allied skills", () => {
            var caster=Unit(); var target=Unit(false,true); BattleScene.Ins.BM.buffManager.AddBuff(target.unitAttrCenter,BuffType.CognitiveProtection);
            Assert(!SkillTargeting.IsValid(caster,target,Skill()),"Protected enemy selectable");
            var friend=Unit(false,true); var skill=Skill(); skill.target=SkillTarget.Ally;
            Assert(SkillTargeting.IsValid(friend,target,skill),"Protected ally rejected");
        });
        Test("Layer filtering agrees with final damage eligibility", () => {
            var caster=Unit(); var target=Unit(false,true); target.transform.position=new(0,1,0);
            var skill=Skill(); skill.layerSkill=true; skill.attackPacks.Add(new AttackPack(10,DamageType.Melee));
            Assert(!SkillTargeting.IsValid(caster,target,skill),"Wrong layer selected");
            var damage=new DamageManager(BattleScene.Ins.BM); Assert(damage.PreviewSkill(caster,target,skill).HitRate==0,"Wrong layer preview");
            damage.ResolveSkillTarget(caster,target,skill); Assert(target.unitAttrCenter.CurHealth==100,"Wrong layer damaged");
        });
        Test("Single target chooses nearest aim point independent of input order", () => {
            var caster=Unit(); var near=Unit(false,true); var far=Unit(false,true); near.transform.position=new(2,0,0); far.transform.position=new(3,0,0);
            var skill=Skill(); skill.target=SkillTarget.Enemy;
            foreach(var inputs in new[]{new[]{near,far},new[]{far,near}}) {
                var result=SkillTargeting.Filter(caster,inputs,skill,new(2,0,0)); Assert(result.Count==1 && result[0]==near,"Unstable selection");
            }
        });
        Test("Farthest enemy selection measures from caster", () => {
            var caster=Unit(); var near=Unit(false,true); var far=Unit(false,true); near.transform.position=new(2,0,0); far.transform.position=new(8,0,0);
            var skill=Skill(); skill.target=SkillTarget.FarthestEnemy;
            var result=SkillTargeting.Filter(caster,new[]{near,far},skill,far.transform.position); Assert(result.Count==1 && result[0]==far,"Wrong farthest");
        });
        Test("Empty query cannot retain previous targets", () => {
            var caster=Unit(); var target=Unit(false,true); var manager=new SkillManager(); Physics.Results=new[]{target.gameObject.Add(new Collider())};
            var first=manager.GetTargets(caster,target.transform,Skill()); Assert(first.Count==1,"First query failed");
            Physics.Results=Array.Empty<Collider>(); var second=manager.GetTargets(caster,target.transform,Skill());
            Assert(second.Count==0 && first.Count==1,"Query retained or mutated previous result");
        });
        Test("Self target does not depend on physics colliders", () => {
            var caster=Unit(); var skill=Skill(); skill.target=SkillTarget.Self;
            var result=new SkillManager().GetTargets(caster,caster.transform,skill); Assert(result.Count==1 && result[0]==caster,"Self target missing");
        });
        Test("Area remains a positional effect without direct targets", () => {
            var caster=Unit(); var target=Unit(false,true); var skill=Skill(); skill.target=SkillTarget.Area;
            Assert(SkillTargeting.Filter(caster,new[]{caster,target},skill,caster.transform.position).Count==0,"Area unexpectedly became unit-targeted");
        });
    }
}
