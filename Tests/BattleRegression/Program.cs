using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

static partial class Program
{
    private static int passed;
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action body)
    {
        GM.Ins = new GM(); BattleScene.Ins = new BattleScene(); TestEvents.Reset(); Physics.Results=Array.Empty<Collider>();
        UnityEngine.Random.value = 0; UnityEngine.Random.NextValue = 99;
        body(); passed++; Console.WriteLine("PASS " + name);
    }
    static PieceController Unit(bool player = true, bool enemy = false)
    {
        var go = new GameObject();
        PieceController pc = enemy ? go.Add(new EnemyController()) : go.Add(new PieceController());
        pc.isPlayerPiece = player; pc.player = new PlayerController();
        var unit = go.Add(new UnitAttrCenter()); pc.unitAttrCenter = unit; unit.pc = pc;
        unit.SetData(new PieceData()); unit.FullMovePoint(); Attach(pc,new EventSpy());
        return pc;
    }
    static T Attach<T>(PieceController pc,T passive) where T:SkillSystem.BasePassiveSkill
    {
        var manager=BattleScene.Ins.BM.characterSkillManager;
        var registry=(Dictionary<GameObject,List<SkillSystem.BasePassiveSkill>>)typeof(SkillSystem.CharacterSkillManager).GetField("_runtimeRegistry",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(manager);
        if(!registry.TryGetValue(pc.gameObject,out var skills)) registry[pc.gameObject]=skills=new();
        passive.Initialize(new(),pc.gameObject,manager); skills.Add(passive); return passive;
    }
    static SkillPack Skill(int mana = 3, params ItemPack[] items) => new SkillPack { mpCost=mana, consumeItems=items.ToList() };
    static void Stock(int a=5,int b=5) { GM.Ins.PLAYERPROFILE.Inventory[ItemName.A]=a; GM.Ins.PLAYERPROFILE.Inventory[ItemName.B]=b; }
    static void Balances(UnitAttrCenter unit,int mp,int mana,int a,int b)
    {
        Assert(unit.CurMovePoint==mp && unit.ManaPoint==mana && GM.Ins.PLAYERPROFILE.GetItemNum(ItemName.A)==a && GM.Ins.PLAYERPROFILE.GetItemNum(ItemName.B)==b,
            $"Unexpected balances: AP={unit.CurMovePoint}, mana={unit.ManaPoint}, A={GM.Ins.PLAYERPROFILE.GetItemNum(ItemName.A)}, B={GM.Ins.PLAYERPROFILE.GetItemNum(ItemName.B)}");
    }
    static void Main()
    {
        Test("Default kinetic armor and Frail application/removal agree with preview", () => {
            var a=Unit(); var t=Unit(false,true); t.unitAttrCenter.attr.SetArmor(DamageType.Melee,10);
            var s=Skill(); s.attackPacks.Add(new AttackPack(20,DamageType.Melee));
            var d=new DamageManager(BattleScene.Ins.BM);
            Assert(d.PreviewSkill(a,t,s).Total.MinDamage==10,"Default armor must remain active");
            BattleScene.Ins.BM.buffManager.AddBuff(t.unitAttrCenter,BuffType.Frail);
            Assert(d.PreviewSkill(a,t,s).Total.MinDamage==13,"Frail should reduce armor to 7");
            BattleScene.Ins.BM.buffManager.RemoveBuff(t.unitAttrCenter,BuffType.Frail,-1);
            Assert(d.PreviewSkill(a,t,s).Total.MinDamage==10,"Removal restores armor");
        });
        Test("Actual kinetic damage uses remaining armor multiplier", () => {
            var a=Unit(); var t=Unit(false,true); t.unitAttrCenter.attr.SetArmor(DamageType.Melee,10);
            var s=Skill(); s.attackPacks.Add(new AttackPack(20,DamageType.Melee));
            var d=new DamageManager(BattleScene.Ins.BM);
            // Call the real per-segment calculation with a fixed roll, without changing production RNG.
            var context=typeof(DamageManager).GetMethod("CreateContext",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,new object[]{a,t,s,CheckResult.None,false});
            var burstType=typeof(DamageManager).GetNestedType("BurstState",BindingFlags.NonPublic);
            var args=new object[]{context,s.attackPacks[0],false,4,Activator.CreateInstance(burstType)};
            var damage=(int)typeof(DamageManager).GetMethod("CalculateDamage",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
            Assert(damage==10,"Normal hit must subtract the full base armor");
        });
        Test("Negative armor multiplier does not create negative armor", () => {
            var a=Unit(); var t=Unit(false,true); t.unitAttrCenter.attr.SetArmor(DamageType.Melee,10);
            t.unitAttrCenter.buffAttrDic[BuffAttrType.MeleeArmorPercent]=-30;
            var s=Skill(); s.attackPacks.Add(new AttackPack(20,DamageType.Melee));
            var p=new DamageManager(BattleScene.Ins.BM).PreviewSkill(a,t,s);
            Assert(p.Total.MinDamage==20 && p.Total.MaxDamage==20,"Armor must clamp to zero");
        });
        Test("All materials deducted once", () => {
            var u=Unit().unitAttrCenter; Stock();
            Assert(u.TryCostSkill(Skill(3,new ItemPack(ItemName.A,2),new ItemPack(ItemName.B,3))),"Cast rejected"); Balances(u,2,7,3,2);
        });
        Test("Missing second material leaves every resource unchanged", () => {
            var u=Unit().unitAttrCenter; Stock(5,0);
            Assert(!u.TryCostSkill(Skill(3,new ItemPack(ItemName.A,2),new ItemPack(ItemName.B,1))),"Cast accepted"); Balances(u,3,10,5,0);
        });
        Test("Insufficient mana leaves AP and materials unchanged", () => {
            var u=Unit().unitAttrCenter; Stock(); Assert(!u.TryCostSkill(Skill(11,new ItemPack(ItemName.A,2))),"Cast accepted"); Balances(u,3,10,5,5);
        });
        Test("Insufficient AP leaves mana and materials unchanged", () => {
            var u=Unit().unitAttrCenter; u.SetMovePoint(0); Stock(); Assert(!u.TryCostSkill(Skill(3,new ItemPack(ItemName.A,2))),"Cast accepted"); Balances(u,0,10,5,5);
        });
        Test("Duplicate materials checked as a total", () => {
            var u=Unit().unitAttrCenter; Stock(3,5); var items=new List<ItemPack>{new ItemPack(ItemName.A,2),new ItemPack(ItemName.A,2)};
            Assert(!u.HasItem(items) && !u.CostItem(items),"Duplicate costs accepted"); Balances(u,3,10,3,5);
        });
        Test("Duplicate materials deducted as a total", () => {
            var u=Unit().unitAttrCenter; Stock(); Assert(u.CostItem(new(){new ItemPack(ItemName.A,2),new ItemPack(ItemName.A,1),new ItemPack(ItemName.B,2)}),"Cost rejected"); Balances(u,3,10,2,3);
        });
        Test("Direct CostItem is atomic when a later material is missing", () => {
            var u=Unit().unitAttrCenter; Stock(5,0); Assert(!u.CostItem(new(){new ItemPack(ItemName.A,2),new ItemPack(ItemName.B,1)}),"Cost accepted"); Balances(u,3,10,5,0);
        });
        Test("Invalid negative, null and overflowing costs are rejected", () => {
            var u=Unit().unitAttrCenter; Stock();
            Assert(!u.TryCostSkill(Skill(-1)),"Negative mana");
            Assert(!u.CostMana(-1),"Negative mana direct");
            Assert(!u.CostItem(new(){new ItemPack(ItemName.A,-1)}),"Negative items");
            Assert(!u.CostItem(new(){null}),"Null item");
            Assert(!u.HasItem(new(){new ItemPack(ItemName.A,int.MaxValue),new ItemPack(ItemName.A,1)}),"Overflow"); Balances(u,3,10,5,5);
        });
        Test("Free and empty costs are valid", () => {
            var u=Unit().unitAttrCenter; Stock(0,0); Assert(u.CostItem(null),"Null list"); Assert(u.TryCostSkill(Skill(0,new ItemPack(ItemName.A,0))),"Free cast"); Balances(u,2,10,0,0);
        });
        Test("Delayed skill pays the same resources", () => {
            var u=Unit().unitAttrCenter; Stock(); var s=Skill(3,new ItemPack(ItemName.A,2)); s.isDelaySkill=true;
            Assert(u.TryCostSkill(s),"Delayed cast rejected"); Balances(u,2,7,3,5);
        });
        Test("Existing stun rejects cast before payment", () => {
            var u=Unit().unitAttrCenter; Stock(); u.buffStates.Add(new(BuffType.Stun,1)); Assert(!u.TryCostSkill(Skill(3,new ItemPack(ItemName.A,2))),"Stunned cast"); Balances(u,3,10,5,5);
        });
        Test("Lethal action debuff consumes AP only", () => {
            var u=Unit().unitAttrCenter; Stock(); u.SetHealth(10); u.buffStates.Add(new(BuffType.SpontaneousMemeticAttack,1));
            Assert(!u.TryCostSkill(Skill(3,new ItemPack(ItemName.A,2))),"Dead unit cast"); Assert(u.pc.isDead,"Must die"); Balances(u,2,10,5,5);
        });
        Test("Action-triggered stun prevents mana and material payment", () => {
            var u=Unit().unitAttrCenter; Stock(); u.buffStates.Add(new(BuffType.SpontaneousMemeticAttack,1)); UnityEngine.Random.NextValue=2;
            Assert(!u.TryCostSkill(Skill(3,new ItemPack(ItemName.A,2))),"Stun cast accepted"); Balances(u,0,10,5,5);
        });
        Test("Three-hit lethal attack sends one kill and one hit", () => {
            var a=Unit(); a.unitAttrCenter.critRate=100; var t=(EnemyController)Unit(false,true); t.unitAttrCenter.SetHealth(20);
            var s=Skill(); for(int i=0;i<3;i++)s.attackPacks.Add(new(30,DamageType.Melee));
            var result=new DamageManager(BattleScene.Ins.BM).ResolveSkillTarget(a,t,s);
            Assert(TestEvents.Kills==1 && TestEvents.Hits==1 && t.Deaths==1,"Duplicate death events");
            Assert(t.RecordedDamage==30 && result.DamageInfos.Count==1,"Post-death damage recorded");
        });
        Test("Already-dead target generates no hit or kill", () => {
            var a=Unit(); var t=Unit(false,true); t.unitAttrCenter.SetHealth(0); var s=Skill(); s.attackPacks.Add(new(30,DamageType.Melee));
            new DamageManager(BattleScene.Ins.BM).ResolveSkillTarget(a,t,s); Assert(TestEvents.Kills==0 && TestEvents.Hits==0,"Dead target events");
        });
        Test("Nonlethal multi-hit preserves one hit event per segment", () => {
            var a=Unit(); a.unitAttrCenter.critRate=100; var t=Unit(false,true); var s=Skill(); for(int i=0;i<3;i++)s.attackPacks.Add(new(10,DamageType.Melee));
            new DamageManager(BattleScene.Ins.BM).ResolveSkillTarget(a,t,s);
            Assert(TestEvents.Kills==0 && TestEvents.Hits==3 && t.unitAttrCenter.CurHealth==70,"Lost nonlethal hits");
        });
        Test("Friendly death does not count as enemy kill", () => {
            var a=Unit(); a.unitAttrCenter.critRate=100; var t=Unit(); var s=Skill(); s.target=SkillTarget.All; s.attackPacks.Add(new(200,DamageType.Melee));
            new DamageManager(BattleScene.Ins.BM).ResolveSkillTarget(a,t,s); Assert(TestEvents.Kills==0,"Friendly kill reward");
        });
        Test("Zero damage still sends hit event", () => {
            var a=Unit(); var t=Unit(false,true); var s=Skill(); s.attackPacks.Add(new(0,DamageType.Melee));
            new DamageManager(BattleScene.Ins.BM).ResolveSkillTarget(a,t,s); Assert(TestEvents.Hits==1 && t.unitAttrCenter.CurHealth==100,"Zero hit contract");
        });
        Test("Barrier blocks all segments even after breaking", () => {
            var a=Unit(); a.unitAttrCenter.critRate=100; var t=Unit(false,true); BattleScene.Ins.BM.buffManager.AddBuff(t.unitAttrCenter,BuffType.SlowingField,barrierHealth:5);
            var s=Skill(); s.attackPacks.Add(new(10,DamageType.Melee)); s.attackPacks.Add(new(10,DamageType.Melee));
            var r=new DamageManager(BattleScene.Ins.BM).ResolveSkillTarget(a,t,s);
            Assert(t.unitAttrCenter.CurHealth==100 && !r.CanApplyEffects && TestEvents.Hits==0,"Barrier regression");
        });
        Test("Micromechanical healing is applied to owner, never attacker", () => {
            var owner=Unit(); var attacker=Unit(false,true); var p=new SkillSystem.SkillMicromechanicalDamageControl(); p.Initialize(new(),owner.gameObject);
            p.OnTakeDamage(owner.gameObject,attacker.gameObject);
            Assert(owner.unitAttrCenter.GetBuffStacks(BuffType.AutoHeal)==3 && attacker.unitAttrCenter.GetBuffStacks(BuffType.AutoHeal)==0,"Wrong heal target");
        });
        Test("Micromechanical healing respects failed roll and event owner", () => {
            var owner=Unit(); var attacker=Unit(false,true); var p=new SkillSystem.SkillMicromechanicalDamageControl(); p.Initialize(new(),owner.gameObject);
            UnityEngine.Random.value=0.21f; p.OnTakeDamage(owner.gameObject,attacker.gameObject);
            UnityEngine.Random.value=0; p.OnTakeDamage(attacker.gameObject,owner.gameObject);
            Assert(owner.unitAttrCenter.GetBuffStacks(BuffType.AutoHeal)==0 && attacker.unitAttrCenter.GetBuffStacks(BuffType.AutoHeal)==0,"Unexpected proc");
        });
        Batch2();
        Console.WriteLine($"{passed} regression tests passed.");
    }
}
