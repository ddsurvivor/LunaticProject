using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

static partial class Program
{
    static void Field(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    static List<TipText> TipPool(TipTextManager manager) => (List<TipText>)typeof(TipTextManager)
        .GetField("_tipPool", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
    static TipTextManager TextManager()
    {
        var manager = new GameObject().Add(new TipTextManager());
        var prefab = new GameObject(); prefab.Add(new TipText()); prefab.Add(new UnityEngine.UI.Text());
        Field(manager, "tipTextPrefab", prefab);
        BattleScene.Ins.BM.tipTextManager = manager;
        return manager;
    }
    static UnityEngine.UI.Text TextOf(TipText tip) => tip.GetComponent<UnityEngine.UI.Text>();
    static void FloatingTextRegression()
    {
        Test("Damage and multiple buffs appear together in upward order", () => {
            var m=TextManager(); var u=Unit();
            u.unitAttrCenter.TakeDamage(new AttackPack(10,DamageType.Melee));
            BattleScene.Ins.BM.buffManager.AddBuff(u.unitAttrCenter,BuffType.Disrupt);
            BattleScene.Ins.BM.buffManager.AddBuff(u.unitAttrCenter,BuffType.Bind);
            m.ProcessQueues(); var pool=TipPool(m);
            Assert(pool.Count==3 && pool.TrueForAll(t=>t.IsPlaying), "Messages still waited for damage to finish");
            Assert(TextOf(pool[0]).text=="10" && TextOf(pool[1]).text=="+干扰" && TextOf(pool[2]).text=="+束缚",
                "Arrival order lost");
            Assert(pool[0].transform.position.y>pool[1].transform.position.y &&
                pool[1].transform.position.y>pool[2].transform.position.y, "Messages not stacked upward");
        });
        Test("Different targets play their queues concurrently", () => {
            var m=TextManager(); var a=Unit(); var b=Unit(false,true);
            m.ShowDamage(a.transform,10); m.ShowHeal(b.transform,5); m.ProcessQueues();
            Assert(TipPool(m).Count==2 && TipPool(m).TrueForAll(t=>t.IsPlaying), "Targets serialized globally");
        });
        Test("Default damage is larger grows monotonically and has a readable cap", () => {
            var m=TextManager();
            Assert(m.GetDamageFontSize(0)>100 && m.GetDamageFontSize(10)<m.GetDamageFontSize(1000) &&
                m.GetDamageFontSize(int.MaxValue)<=220, "Damage font scale invalid");
        });
        Test("Critical damage is red and reuse resets normal font and color", () => {
            var m=TextManager(); var a=Unit();
            a.unitAttrCenter.TakeDamage(new AttackPack(0,DamageType.Melee,true));
            m.ProcessQueues(); var tip=TipPool(m)[0];
            Assert(TextOf(tip).color.r==1 && TextOf(tip).color.g==0 && TextOf(tip).fontSize>100, "Critical style not forwarded");
            tip.AdvanceAnimation(3f); m.ShowTip(a.transform,"提示"); m.ProcessQueues();
            Assert(TextOf(tip).fontSize==100 && TextOf(tip).color.g==1, "Pooled critical style leaked");
        });
        Test("Per-request color and font overrides survive queueing", () => {
            var m=TextManager(); var a=Unit();
            m.ShowDamage(a.transform,5,true,Color.green,180); m.ProcessQueues(); var tip=TipPool(m)[0];
            Assert(TextOf(tip).fontSize==180 && TextOf(tip).color.g==1 && TextOf(tip).color.r==0,
                "Optional style override ignored");
        });
        Test("Normal tips last twice their old duration and fade only in the second half", () => {
            var m=TextManager(); var a=Unit(); m.ShowTip(a.transform,"提示"); m.ProcessQueues(); var tip=TipPool(m)[0];
            tip.AdvanceAnimation(1.2f); Assert(tip.IsPlaying && TextOf(tip).color.a==1, "Old duration still used");
            tip.AdvanceAnimation(0.6f); Assert(TextOf(tip).color.a>0 && TextOf(tip).color.a<1, "Fade missing");
            tip.AdvanceAnimation(0.61f); Assert(!tip.IsPlaying, "Tip did not recycle");
        });
        Test("Queued text remains at event position after victim moves or is disabled", () => {
            var m=TextManager(); var a=Unit(); a.transform.position=new Vector3(3,0,0);
            m.ShowTip(a.transform,"首次"); m.ShowTip(a.transform,"后续");
            a.transform.position=new Vector3(90,0,0); a.gameObject.SetActive(false);
            m.ProcessQueues(); var tip=TipPool(m)[1];
            Assert(TextOf(tip).text=="后续" && tip.transform.position.x==3, "Queued death feedback lost or relocated");
        });
        Test("Unit child transforms share the owning unit queue", () => {
            var m=TextManager(); var a=Unit(); var child=new GameObject(); child.transform.parent=a.transform;
            m.ShowDamage(a.transform,10); m.ShowTip(child.transform,"子节点"); m.ProcessQueues();
            Assert(TipPool(m).Count==2 && TipPool(m)[0].transform.position.y>TipPool(m)[1].transform.position.y,
                "Child did not share the unit stack");
        });
        Test("Disabling the manager clears active and pending messages", () => {
            var m=TextManager(); var a=Unit(); m.ShowTip(a.transform,"一"); m.ShowTip(a.transform,"二"); m.ProcessQueues();
            typeof(TipTextManager).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(m,null);
            m.ProcessQueues(); Assert(TipPool(m).TrueForAll(t=>!t.IsPlaying), "Active text survived disable");
        });
        Test("New passive text pushes damage up without resetting damage lifetime", () => {
            var m=TextManager(); var a=Unit(); m.ShowDamage(a.transform,10); m.ProcessQueues();
            var damage=TipPool(m)[0]; damage.AdvanceAnimation(1f); float previousY=damage.transform.position.y;
            m.ShowTip(a.transform,"被动触发"); m.ProcessQueues();
            Assert(TipPool(m).Count==2 && damage.transform.position.y>previousY, "Damage did not make room");
            damage.AdvanceAnimation(2f); Assert(!damage.IsPlaying, "Stacking restarted damage lifetime");
        });
        Test("Different durations cannot make a later tip catch the older damage", () => {
            var m=TextManager(); var a=Unit(); m.ShowDamage(a.transform,0); m.ShowTip(a.transform,"被动"); m.ProcessQueues();
            var pool=TipPool(m);
            for(int i=0;i<20;i++)
            {
                foreach(var tip in pool) tip.AdvanceAnimation(0.1f);
                m.ProcessQueues();
                Assert(pool[0].transform.position.y-pool[1].transform.position.y>=3.35f, "Growing stack overlapped");
            }
        });
        Test("Reusing another target's finished tip does not link their stacks", () => {
            var m=TextManager(); var a=Unit(); var b=Unit(); m.ShowTip(a.transform,"旧目标"); m.ProcessQueues();
            TipPool(m)[0].AdvanceAnimation(2.4f);
            m.ShowTip(b.transform,"新目标一"); m.ShowTip(b.transform,"新目标二"); m.ProcessQueues();
            var pool=TipPool(m); Assert(pool.Count==2 && pool[0].transform.position.y>pool[1].transform.position.y,
                "Cross-target pool reuse corrupted stack");
        });
    }
}
