using System;
using System.Linq;
using UnityEngine;

static partial class Program
{
    static void TargetingRegression()
    {
        Test("SelectAllies uses caster faction, deduplicates and optionally excludes self", () => {
            var caster=Unit(false,true); var ally=Unit(false,true); var enemy=Unit(); var corpse=Unit(false,true);
            corpse.unitAttrCenter.ModifyAttribute(UnitAttrType.CurHealth,-100);
            var result=SkillTargeting.SelectAllies(caster,new[]{caster,ally,ally,enemy,corpse},false);
            Assert(result.SequenceEqual(new[]{ally}),"Wrong ally collection");
        });
        Test("Circle and grenade queries clamp position and use distinct hit radii", () => {
            var caster=Unit(); var skill=Skill(); skill.rangeValue=4;
            SkillTargeting.Query(caster,skill,new(0,8,10));
            Assert((Physics.LastCenter-new Vector3(0,0,4)).sqrMagnitude<0.0001f && Physics.LastRadius==1,"Circle query");
            skill.rangeType=RangeType.Grenade; skill.explodeRadius=2;
            SkillTargeting.Query(caster,skill,new(0,0,3));
            Assert((Physics.LastCenter-new Vector3(0,0,3)).sqrMagnitude<0.0001f && Physics.LastRadius==2,"Grenade query");
        });
        Test("Fan excludes rear, out of range and close dead zone for either faction", () => {
            foreach(bool player in new[]{true,false}) {
                var caster=Unit(player,!player); var front=Unit(!player,player); var rear=Unit(!player,player);
                var near=Unit(!player,player); var far=Unit(!player,player);
                front.transform.position=new(0,0,3); rear.transform.position=new(0,0,-2);
                near.transform.position=new(0,0,.5f); far.transform.position=new(0,0,8);
                Physics.Results=new[]{front,rear,near,far,front}.Select(p=>p.gameObject.Add(new Collider())).ToArray();
                var skill=Skill(); skill.rangeType=RangeType.Fan; skill.rangeValue=5; skill.rangeAgle=90;
                Assert(SkillTargeting.Query(caster,skill,new(0,0,5)).SequenceEqual(new[]{front}),"Fan geometry/faction");
            }
        });
        Test("Nova ignores aim position and uses fallback radius", () => {
            var caster=Unit(); caster.transform.position=new(2,0,3); var skill=Skill(); skill.rangeType=RangeType.Nova;
            skill.explodeRadius=2; SkillTargeting.Query(caster,skill,new(50,0,50));
            Assert((Physics.LastCenter-caster.transform.position).sqrMagnitude==0 && Physics.LastRadius==2,"Nova origin");
            skill.explodeRadius=0; SkillTargeting.Query(caster,skill,new());
            Assert(Physics.LastRadius==skill.rangeValue,"Nova fallback");
        });
        Test("Arc uses ring thickness and half central angle, including rotated aim", () => {
            foreach(bool rotated in new[]{false,true}) {
                var caster=Unit(); var onArc=Unit(false,true); var outside=Unit(false,true); var rear=Unit(false,true);
                float edge=MathF.Sqrt(13)-3;
                onArc.transform.position=rotated?new(2,0,-edge):new(edge,0,2);
                outside.transform.position=rotated?new(2,0,-2):new(2,0,2);
                rear.transform.position=rotated?new(-2,0,0):new(0,0,-2);
                Physics.Results=new[]{onArc,outside,rear}.Select(p=>p.gameObject.Add(new Collider())).ToArray();
                var skill=Skill(); skill.rangeType=RangeType.Arc; skill.rangeValue=4; skill.arcCenterDis=3; skill.arcWeight=.5f;
                var result=SkillTargeting.Query(caster,skill,rotated?new(4,0,0):new(0,0,4));
                Assert(result.SequenceEqual(new[]{onArc}),"Arc geometry/orientation");
            }
        });
        Test("Degenerate arc has finite radius and angle", () => {
            var skill=Skill(); skill.rangeValue=0; skill.arcCenterDis=0;
            Assert(SkillTargeting.GetArcRadius(skill)==0 && SkillTargeting.GetArcHalfAngle(skill)==0,"Degenerate arc");
        });
        Test("Enemy range UI is created once from player template and bound to its owner", () => {
            var player=Unit(); player.rangeUI=new GameObject().Add(new RangeUI());
            player.rangeUI.transform.localPosition=new(0,.1f,0);
            BattleScene.Ins.BM.PlayerController.pieces.Add(player);
            var caster=Unit(false,true); var manager=new SkillManager();
            var range=manager.EnsureRangeUI(caster);
            Assert(range!=null && range!=player.rangeUI && range.Owner==caster && range.CloseCalls==1,"Missing range initialization");
            Assert(range.transform.parent==caster.transform && manager.EnsureRangeUI(caster)==range,"Range ownership/reuse");
        });
    }
}
