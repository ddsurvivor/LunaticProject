using System.Collections.Generic;
using UnityEngine;

public partial class AIController
{
    public PieceController ExplosiveChoice(EnemyController enemy)=>TryCreateExplosivePlan(enemy,out var plan)?plan.Target:null;
    public ActionType ExplosiveAction(EnemyController enemy)=>TryCreateExplosivePlan(enemy,out var plan)?plan.Action:0;
}

static partial class Program
{
    private class TestExplosive : IEnemyExplosiveTarget
    {
        public PieceController Piece {get;set;}
        public bool Available=true;
        public bool CanBeTriggered=>Available && !Piece.isDead && Piece.gameObject.activeInHierarchy;
        public SkillPack Explosion;
        public float Radius=2;
        public LayerMask Mask=1;
        public int CountThreatenedPlayers()=>CanBeTriggered?EnemyExplosiveTargets.CountPlayers(Piece,Piece.transform.position,Radius,Mask,Explosion):0;
    }
    static TestExplosive Barrel(Vector3 position)
    {
        var pc=Unit(false);pc.transform.position=position;
        var explosion=Skill();explosion.attackPacks.Add(new AttackPack(20,DamageType.Melee));
        var barrel=new TestExplosive {Piece=pc,Explosion=explosion};
        EnemyExplosiveTargets.Active.Add(barrel);return barrel;
    }
    static SkillPack BarrelAttack()
    {
        var skill=Skill();skill.target=SkillTarget.Enemy;skill.rangeValue=10;
        skill.attackPacks.Add(new AttackPack(20,DamageType.Melee));return skill;
    }
    static void ExplosiveColliders(params PieceController[] pieces)
    {
        var colliders=new List<Collider>();foreach(var p in pieces) colliders.Add(p.gameObject.Add(new Collider()));
        Physics.Results=colliders.ToArray();Physics.FilterByPosition=true;
    }
    static void EnemyExplosiveRegression()
    {
        Test("Explosive attack is disabled by default and opt-in allows same-faction barrels",()=>{
            var enemy=(EnemyController)Unit(false,true);var player=Unit();player.transform.position=new(4,0,0);
            var barrel=Barrel(new(3,0,0));ExplosiveColliders(player,barrel.Piece);
            var skill=BarrelAttack();enemy.pieceData.rangedAtk=skill;var ai=new AIController();
            Assert(!enemy.attackExplosives && ai.ExplosiveChoice(enemy)==null,"default enemy chose a barrel");
            enemy.attackExplosives=true;Assert(ai.ExplosiveChoice(enemy)==barrel.Piece,"opt-in did not choose reachable barrel");
            Assert(skill.target==SkillTarget.Enemy && barrel.Piece.unitAttrCenter.CurHealth==100,"read-only selection mutated data");
        });
        Test("Explosive selection rejects no-player, spent, hidden, out-of-range and zero-damage opportunities",()=>{
            var enemy=(EnemyController)Unit(false,true);enemy.attackExplosives=true;enemy.pieceData.rangedAtk=BarrelAttack();
            var player=Unit();player.transform.position=new(8,0,0);var barrel=Barrel(new(3,0,0));ExplosiveColliders(player,barrel.Piece);
            var ai=new AIController();Assert(ai.ExplosiveChoice(enemy)==null,"empty explosion chosen");
            player.transform.position=new(4,0,0);barrel.Available=false;Assert(ai.ExplosiveChoice(enemy)==null,"already-triggered barrel chosen");
            barrel.Available=true;barrel.Piece.gameObject.SetActive(false);Assert(ai.ExplosiveChoice(enemy)==null,"hidden barrel chosen");
            barrel.Piece.gameObject.SetActive(true);enemy.pieceData.rangedAtk.rangeValue=1;Assert(ai.ExplosiveChoice(enemy)==null,"out-of-range barrel chosen");
            enemy.pieceData.rangedAtk.rangeValue=10;enemy.pieceData.rangedAtk.attackPacks.Clear();Assert(ai.ExplosiveChoice(enemy)==null,"nondamaging attack chosen");
            enemy.pieceData.rangedAtk=BarrelAttack();barrel.Explosion.attackPacks.Clear();Assert(ai.ExplosiveChoice(enemy)==null,"nondamaging explosion chosen");
        });
        Test("Explosion prediction uses its collision mask and ignores dead or protected players",()=>{
            var barrel=Barrel(new(3,0,0));var player=Unit();player.transform.position=new(4,0,0);ExplosiveColliders(player,barrel.Piece);
            Assert(barrel.CountThreatenedPlayers()==1,"valid player excluded");player.gameObject.layer=1;
            Assert(barrel.CountThreatenedPlayers()==0,"effect mask ignored");player.gameObject.layer=0;
            BattleScene.Ins.BM.buffManager.AddBuff(player.unitAttrCenter,BuffType.CognitiveProtection);
            Assert(barrel.CountThreatenedPlayers()==0,"protected player counted");
            BattleScene.Ins.BM.buffManager.RemoveBuff(player.unitAttrCenter,BuffType.CognitiveProtection,-1);
            player.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-100);Assert(barrel.CountThreatenedPlayers()==0,"dead player counted");
        });
        Test("Enemy chooses barrel hitting more players and respects action and resource constraints",()=>{
            var enemy=(EnemyController)Unit(false,true);enemy.attackExplosives=true;enemy.enemyAIType=EnemyAIType.Shoot;
            enemy.pieceData.rangedAtk=BarrelAttack();var near=Barrel(new(2,0,0));var far=Barrel(new(6,0,0));
            var a=Unit();a.transform.position=new(3,0,0);var b=Unit();b.transform.position=new(6,0,1);var c=Unit();c.transform.position=new(6,0,-1);
            ExplosiveColliders(near.Piece,far.Piece,a,b,c);var ai=new AIController();
            Assert(ai.ExplosiveChoice(enemy)==far.Piece && ai.ExplosiveAction(enemy)==ActionType.远程攻击,"blast-count/action preference wrong");
            enemy.unitAttrCenter.ModifyAttribute(UnitAttrType.CurMovePoint,-100);Assert(ai.ExplosiveChoice(enemy)==null,"no-action-point attack selected");
            enemy.unitAttrCenter.FullMovePoint();enemy.enemyAIType=EnemyAIType.Special;Assert(ai.ExplosiveChoice(enemy)==null,"special AI replaced by barrel opportunity");
        });
        Test("Dead-caster explosion copy resolves damage while ordinary dead-caster skills remain blocked",()=>{
            var barrel=Barrel(new(3,0,0));var player=Unit();player.transform.position=new(4,0,0);
            barrel.Piece.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-100);
            var normal=barrel.Explosion;var explosion=normal.WithDeadCaster();
            Assert(!normal.AllowDeadCaster && explosion.AllowDeadCaster,"copy changed original skill");
            Assert(!SkillTargeting.IsValid(barrel.Piece,player,normal) && SkillTargeting.IsValid(barrel.Piece,player,explosion),"dead-caster gate incorrect");
            var damage=BattleScene.Ins.BM.damageManager;int hp=player.unitAttrCenter.CurHealth;
            damage.ResolveSkillTarget(barrel.Piece,player,normal);Assert(player.unitAttrCenter.CurHealth==hp,"ordinary dead caster damaged player");
            damage.ResolveSkillTarget(barrel.Piece,player,explosion);Assert(player.unitAttrCenter.CurHealth<hp,"dead barrel explosion did not damage player");
        });
        Test("Explosive plans reserve reload cost and skill users obey mana availability",()=>{
            var enemy=(EnemyController)Unit(false,true);enemy.attackExplosives=true;enemy.enemyAIType=EnemyAIType.Shoot;
            enemy.pieceData.rangedAtk=BarrelAttack();var barrel=Barrel(new(3,0,0));var player=Unit();player.transform.position=new(4,0,0);
            ExplosiveColliders(barrel.Piece,player);var ai=new AIController();
            while(enemy.unitAttrCenter.AmmoCount>0) enemy.unitAttrCenter.CostAmmo();
            enemy.unitAttrCenter.ModifyAttribute(UnitAttrType.CurMovePoint,-2);
            Assert(ai.ExplosiveChoice(enemy)==null,"one action point covered reload plus shot");
            enemy.unitAttrCenter.FullMovePoint();Assert(ai.ExplosiveChoice(enemy)==barrel.Piece,"affordable reload plus shot was rejected");
            enemy.enemyAIType=EnemyAIType.SkillUser;enemy.pieceData.rangedAtk=null;
            var skill=BarrelAttack();skill.mpCost=3;enemy.availableSkills.Add(skill);
            Assert(ai.ExplosiveAction(enemy)==ActionType.技能,"skill user's preferred skill not used");
            enemy.unitAttrCenter.CostMana(enemy.unitAttrCenter.ManaPoint);
            Assert(ai.ExplosiveChoice(enemy)==null,"mana-free explosive skill allowed");
        });
    }
}
