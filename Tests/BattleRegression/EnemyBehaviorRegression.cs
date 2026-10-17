using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

static partial class Program
{
    static (AIController ai,EnemyController enemy,PieceController a,PieceController b) BehaviorFixture(EnemyAIType mode)
    {
        BattleScene.Ins=new BattleScene();Physics.CoverObject=null;EnemyExplosiveTargets.Active.Clear();
        var ai=new GameObject().Add(new AIController());BattleScene.Ins.BM.AIController=ai;
        var enemy=(EnemyController)Unit(false,true);enemy.enemyAIType=mode;enemy.retreatWhenLowHealth=true;
        enemy.gameObject.Add(new NavMeshAgent());ai.pieces.Add(enemy);enemy.player=ai;
        enemy.pieceData.meleeAtk=BarrelAttack();enemy.pieceData.meleeAtk.rangeValue=2;
        enemy.pieceData.rangedAtk=BarrelAttack();enemy.pieceData.rangedAtk.rangeValue=10;
        var a=Unit();a.transform.position=new(0,0,7);var b=Unit();b.transform.position=new(6,0,0);
        BattleScene.Ins.BM.PlayerController.pieces.AddRange(new[]{a,b});
        ExplosiveColliders(enemy,a,b);UnityEngine.Random.value=.9f;
        return(ai,enemy,a,b);
    }

    static void EnemyBehaviorRegression()
    {
        Test("Mode1 targets nearby weak health and obeys half-range and empty-ammo melee rules",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.NearestAdaptive);a.transform.position=new(0,0,8);b.transform.position=new(0,0,7);
            a.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-70);
            Assert(ai.CheckEnemyTarget(e)==a,"range-local weak health ignored");ai.DecideForTest(e);Assert(e.AIActions.Last()=="Ranged","far target was not shot");
            Assert(!EnemyBehaviorRules.PreferRanged(e,e.enemyAIType,5),"exact half-range should be melee");
            while(e.unitAttrCenter.AmmoCount>0)e.unitAttrCenter.CostAmmo();e.unitAttrCenter.FullMovePoint();int shots=e.AIActions.Count;
            ai.DecideForTest(e);Assert(e.AIActions.Count==shots && BattleScene.Ins.BM.moveManager.Moves>0,"empty-ammo mode1 reloaded instead of approaching");
        });
        Test("Modes3-6 use exact historical damage and nearest fallback instead of taunt",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.DamageHunterAOE);e.damageDic[a]=3;e.damageDic[b]=7;
            Assert(ai.CheckEnemyTarget(e)==b,"highest damage ignored");e.damageDic.Clear();Assert(ai.CheckEnemyTarget(e)==b,"no-damage fallback was not nearest");
            e.enemyAIType=EnemyAIType.DamageHunterMeleeOnly;e.damageDic[a]=10;ai.DecideForTest(e);
            Assert(!e.AIActions.Contains("Ranged") && !e.AIActions.Contains("Reload"),"melee-only mode fired or reloaded");
        });
        Test("Mode7 supports lowest maximum-health or lowest armor targeting",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.FragileMeleeOnly);a.unitAttrCenter.ModifyAttribute(UnitAttrType.MaxHealth,-40);
            Assert(ai.CheckEnemyTarget(e)==a,"maximum-health priority failed");e.fragilePriority=EnemyFragilePriority.Defense;
            a.unitAttrCenter.attr.SetArmor(DamageType.Ranged,20);b.unitAttrCenter.attr.SetArmor(DamageType.Ranged,1);
            Assert(ai.CheckEnemyTarget(e)==b,"defense priority failed");
        });
        Test("Mode2 shoots before evading any nearby player and moves around cover",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.WeakestDefenseShooter);b.transform.position=new(2,0,0);b.unitAttrCenter.attr.SetArmor(DamageType.Ranged,5);
            a.unitAttrCenter.attr.SetArmor(DamageType.Ranged,0);ai.DecideForTest(e);
            Assert(e.AIActions.Last()=="Ranged" && e.LastTarget==a && BattleScene.Ins.BM.moveManager.Moves==0,"evaded before shooting weak armor");
            ai.DecideForTest(e);Assert(BattleScene.Ins.BM.moveManager.Moves==1,"failed to evade a different nearby player");
            var(ai2,e2,a2,b2)=BehaviorFixture(EnemyAIType.WeakestDefenseShooter);a2.unitAttrCenter.attr.SetArmor(DamageType.Ranged,-1);
            a2.CurCaverSlot=new CaverSlot();a2.CurCaverSlot.gameObject.Add(new Collider());Physics.CoverObject=a2.CurCaverSlot.gameObject;
            ai2.DecideForTest(e2);Assert(BattleScene.Ins.BM.moveManager.Moves==1 && e2.AIActions.Count==0,"covered target fired on without flanking");
            ai2.DecideForTest(e2);Assert(e2.AIActions.Contains("Ranged"),"flanking plan failed to shoot");
        });
        Test("Mode4 and mode5 probability branches respect distance and skill role",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.DamageHunterRangedSkill);e.damageDic[a]=10;
            var ranged=BarrelAttack();ranged.enemySkillRole=EnemySkillRole.RangedAttack;e.availableSkills.Add(ranged);
            UnityEngine.Random.value=.2f;ai.DecideForTest(e);Assert(e.LastSkill==ranged && e.AIActions.Last()=="Skill","one-third ranged-skill branch failed");
            e.unitAttrCenter.FullMovePoint();UnityEngine.Random.value=.9f;ai.DecideForTest(e);Assert(e.AIActions.Last()=="Ranged","two-thirds ordinary attack branch failed");
            e.enemyAIType=EnemyAIType.DamageHunterMeleeSkill;a.transform.position=new(0,0,1.5f);
            var melee=BarrelAttack();melee.enemySkillRole=EnemySkillRole.MeleeAttack;melee.rangeValue=2;e.availableSkills.Add(melee);
            e.unitAttrCenter.FullMovePoint();UnityEngine.Random.value=.2f;ai.DecideForTest(e);Assert(e.LastSkill==melee,"close skill branch used a ranged skill");
        });
        Test("Mode4 prioritizes an executable skill against low health without probability roll",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.DamageHunterRangedSkill);e.damageDic[a]=10;
            b.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-80);var skill=BarrelAttack();e.availableSkills.Add(skill);
            UnityEngine.Random.value=.99f;ai.DecideForTest(e);Assert(e.AIActions.Last()=="Skill" && e.LastTarget==b,"low-health priority failed");
        });
        Test("Modes3 and4 prioritize a multi-target skill over ordinary single attacks",()=>{
            foreach(var mode in new[]{EnemyAIType.DamageHunterAOE,EnemyAIType.DamageHunterRangedSkill}) {
                var(ai,e,a,b)=BehaviorFixture(mode);a.transform.position=new(0,0,7);b.transform.position=new(1,0,7);
                var skill=BarrelAttack();skill.target=SkillTarget.EnemyAll;skill.rangeType=RangeType.Grenade;skill.explodeRadius=2;e.availableSkills.Add(skill);
                ai.DecideForTest(e);Assert(e.LastSkill==skill && e.AIActions.Last()=="Skill","AOE skill not prioritized");
            }
        });
        Test("Healing preempts attack, includes self, rejects enemy and strict fifty-percent boundary",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.HealingSupport);var ally=(EnemyController)Unit(false,true);ally.transform.position=new(1,0,0);ai.pieces.Add(ally);
            ally.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-60);var heal=Skill();heal.enemySkillRole=EnemySkillRole.Healing;heal.target=SkillTarget.Ally;
            e.availableSkills.Add(heal);ExplosiveColliders(e,ally,a,b);ai.DecideForTest(e);
            Assert(e.AIActions.Last()=="Heal" && e.LastTarget==ally,"injured ally did not preempt offense");
            Assert(!EnemyBehaviorRules.BelowHealth(a,.5f),"healthy enemy considered injured");
            e.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-50);Assert(!EnemyBehaviorRules.BelowHealth(e,.5f),"exact fifty percent considered below threshold");
            e.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-1);Assert(EnemyBehaviorRules.BelowHealth(e,.5f),"self not considered injured");
        });
        Test("Low-health enemies attack once then reach outer edge while bosses and exact thirty-percent do not flee",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.NearestAdaptive);e.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-71);
            ai.DecideForTest(e);Assert(e.AIActions.Last()=="Ranged" && BattleScene.Ins.BM.moveManager.Moves==0,"fled before attacking");
            ai.DecideForTest(e);Assert(BattleScene.Ins.BM.moveManager.Moves==1 && e.AIActions.Count==1,"wounded enemy attacked twice or failed to flee");
            e.isBoss=true;Assert(!EnemyBehaviorRules.ShouldFlee(e),"boss inherited automatic retreat");e.isBoss=false;e.unitAttrCenter.Heal(1);
            Assert(!EnemyBehaviorRules.ShouldFlee(e),"exact thirty percent triggered retreat");
        });
        Test("Low-health retreat attacks another reachable player when its damage target is out of range",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.DamageHunterMeleeOnly);a.transform.position=new(0,0,40);b.transform.position=new(1,0,0);
            e.damageDic[a]=100;e.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-71);ai.DecideForTest(e);
            Assert(e.AIActions.Last()=="Melee"&&e.LastTarget==b&&BattleScene.Ins.BM.moveManager.Moves==0,"fled without taking a reachable attack opportunity");
            ai.DecideForTest(e);Assert(BattleScene.Ins.BM.moveManager.Moves==1&&e.AIActions.Count==1,"failed to retreat after opportunistic attack");
        });
        Test("Healing can move then cast, can target self, and does not repeat delayed healing in the same turn",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.HealingSupport);var ally=(EnemyController)Unit(false,true);ally.transform.position=new(4,0,0);ai.pieces.Add(ally);
            ally.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-60);var heal=Skill();heal.target=SkillTarget.Ally;heal.rangeValue=2;
            heal.buffPacks.Add(new BuffPack {buffType=BuffType.AutoHeal,target=SkillTarget.Ally,stacks=2,rate=100});e.availableSkills.Add(heal);
            ExplosiveColliders(e,ally,a,b);ai.DecideForTest(e);Assert(BattleScene.Ins.BM.moveManager.Moves==1&&e.AIActions.Count==0,"healer failed to approach injured ally");
            ai.DecideForTest(e);Assert(e.AIActions.Last()=="Heal"&&e.LastTarget==ally,"moving heal plan failed");
            e.unitAttrCenter.FullMovePoint();ai.DecideForTest(e);Assert(e.AIActions.Count(action=>action=="Heal")==1,"delayed heal repeated in one turn");
            var(ai2,e2,a2,b2)=BehaviorFixture(EnemyAIType.HealingSupport);e2.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-51);
            var self=Skill();self.enemySkillRole=EnemySkillRole.Healing;self.target=SkillTarget.Self;self.rangeValue=0;e2.availableSkills.Add(self);
            ai2.DecideForTest(e2);Assert(e2.AIActions.Last()=="Heal"&&e2.LastTarget==e2,"self heal not supported");
        });
        Test("Mode2 explosive priority obeys its opt-in switch and melee-only modes never shoot barrels",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.WeakestDefenseShooter);var barrel=Barrel(new(3,0,0));a.transform.position=new(4,0,0);
            ExplosiveColliders(e,a,b,barrel.Piece);e.attackExplosives=true;ai.DecideForTest(e);
            Assert(e.AIActions.Last()=="Explosive","mode2 did not prioritize a useful barrel");
            e.unitAttrCenter.FullMovePoint();ai.DecideForTest(e);Assert(BattleScene.Ins.BM.moveManager.Moves==1,"repeated barrel shots starved evasion");
            e.enemyAIType=EnemyAIType.DamageHunterMeleeOnly;e.transform.position=new(0,0,0);
            Assert(ai.ExplosiveChoice(e)==null,"melee-only mode selected a distant barrel using a gun");
        });
        Test("Mode5 exact half-range does not roll a melee skill and unusable heal falls back to offense",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.DamageHunterMeleeSkill);e.damageDic[a]=10;a.transform.position=new(0,0,5);
            var melee=BarrelAttack();melee.rangeValue=5;melee.enemySkillRole=EnemySkillRole.MeleeAttack;e.availableSkills.Add(melee);
            UnityEngine.Random.value=.1f;ai.DecideForTest(e);Assert(!e.AIActions.Contains("Skill"),"exact half range triggered close-only skill");
            var(ai2,e2,a2,b2)=BehaviorFixture(EnemyAIType.HealingSupport);var heal=Skill();heal.target=SkillTarget.Self;heal.enemySkillRole=EnemySkillRole.Healing;heal.mpCost=99;
            e2.availableSkills.Add(heal);e2.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-51);ai2.DecideForTest(e2);
            Assert(e2.AIActions.Last()=="Ranged","unaffordable healing prevented ordinary offense");
        });
        Test("Design modes retain configured cross-floor ladder navigation",()=>{
            var(ai,e,a,b)=BehaviorFixture(EnemyAIType.DamageHunterAOE);e.navigate=true;e.damageDic[a]=100;a.transform.position=new(0,3,2);
            Assert(!ai.DecideForTest(e)&&e.AIActions.Count==0,"design mode bypassed missing ladder and attacked across floors");
        });
    }
}
