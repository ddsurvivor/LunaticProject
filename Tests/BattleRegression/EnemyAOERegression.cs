using System.Linq;
using UnityEngine;

static partial class Program
{
    static void AOEPlayers(params PieceController[] players)
    {
        BattleScene.Ins.BM.PlayerController.pieces.AddRange(players);
        Physics.Results=players.Select(p=>p.gameObject.Add(new Collider())).ToArray();
        Physics.FilterByPosition=true;
    }

    static void EnemyAOERegression()
    {
        Test("AOE grenade chooses a midpoint to hit two instead of only the preferred target",()=>{
            var caster=(EnemyController)Unit(false,true);var a=Unit();var b=Unit();
            a.transform.position=new(3,0,-1.5f);b.transform.position=new(3,0,1.5f);AOEPlayers(a,b);
            var skill=Skill();skill.target=SkillTarget.EnemyAll;skill.rangeType=RangeType.Grenade;skill.rangeValue=5;skill.explodeRadius=1.6f;
            Assert(SkillTargeting.Query(caster,skill,a.transform.position).Count==1,"bad test baseline");
            Assert(EnemyAttackPosition.TryFind(caster,a,skill,out var aim),"no AOE aim");
            Assert(SkillTargeting.Query(caster,skill,aim).Count==2,"midpoint did not maximize hits");
            Assert(caster.transform.position.sqrMagnitude==0 && a.transform.position.z==-1.5f,"query moved pawns");
        });
        Test("AOE fan aims between two targets instead of directly at one",()=>{
            var caster=(EnemyController)Unit(false,true);var a=Unit();var b=Unit();
            a.transform.position=new(-1,0,3);b.transform.position=new(1,0,3);AOEPlayers(a,b);
            var skill=Skill();skill.target=SkillTarget.EnemyAll;skill.rangeType=RangeType.Fan;skill.rangeValue=5;skill.rangeAgle=40;
            Assert(SkillTargeting.Query(caster,skill,a.transform.position).Count==1,"bad fan baseline");
            Assert(EnemyAttackPosition.TryFind(caster,a,skill,out var aim) && SkillTargeting.Query(caster,skill,aim).Count==2,"fan did not aim between pawns");
        });
        Test("AOE selection respects cast range, layer restriction and invalid targets",()=>{
            var caster=(EnemyController)Unit(false,true);var a=Unit();var b=Unit();var above=Unit();
            a.transform.position=new(3.5f,0,0);b.transform.position=new(4,0,0);above.transform.position=new(2,2,0);AOEPlayers(a,b,above);
            var skill=Skill();skill.target=SkillTarget.EnemyAll;skill.rangeType=RangeType.Grenade;skill.rangeValue=3;skill.explodeRadius=1.1f;skill.layerSkill=true;
            Assert(EnemyAttackPosition.TryFind(caster,above,skill,out var aim),"in-range splash discarded because primary target is on another layer");
            Assert(aim.magnitude<=3.0001f && SkillTargeting.Query(caster,skill,aim).Count==2,"range or layer ignored");
            a.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-100);b.gameObject.SetActive(false);
            Assert(!EnemyAttackPosition.TryFind(caster,a,skill,out _),"invalid targets influenced aim");
        });
        Test("AOE arc samples orientation using the same ring geometry as damage targeting",()=>{
            var caster=(EnemyController)Unit(false,true);var a=Unit();var b=Unit();
            a.transform.position=new(Mathf.Sqrt(13)-3,0,2);b.transform.position=new(Mathf.Sqrt(13)-3+.03f,0,2.1f);AOEPlayers(a,b);
            var skill=Skill();skill.target=SkillTarget.EnemyAll;skill.rangeType=RangeType.Arc;
            skill.rangeValue=4;skill.arcCenterDis=3;skill.arcWeight=.5f;
            Assert(EnemyAttackPosition.TryFind(caster,a,skill,out var aim)&&SkillTargeting.Query(caster,skill,aim).Count==2,"arc failed to maximize hits");
        });
        Test("AOE ties retain primary target, single attacks stay single and nova uses its origin",()=>{
            var caster=(EnemyController)Unit(false,true);var a=Unit();var b=Unit();
            a.transform.position=new(-3,0,0);b.transform.position=new(3,0,0);AOEPlayers(a,b);
            var skill=Skill();skill.target=SkillTarget.EnemyAll;skill.rangeType=RangeType.Grenade;skill.rangeValue=5;skill.explodeRadius=1;
            Assert(EnemyAttackPosition.TryFind(caster,a,skill,out var aim) && SkillTargeting.Query(caster,skill,aim).Contains(a),"tie lost preferred target");
            skill.target=SkillTarget.Enemy;Assert(!EnemyAttackPosition.IsAOE(skill)&&!EnemyAttackPosition.TryFind(caster,a,skill,out _),"single-target skill was optimized as AOE");
            skill.target=SkillTarget.EnemyAll;skill.rangeType=RangeType.Nova;skill.explodeRadius=4;
            Assert(EnemyAttackPosition.TryFind(caster,a,skill,out aim)&&SkillTargeting.Query(caster,skill,aim).Count==2,"nova shape changed");
        });
    }
}
