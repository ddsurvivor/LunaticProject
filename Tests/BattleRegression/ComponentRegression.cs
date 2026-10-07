using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static partial class Program
{
    static ComponentData Component(int id, ComponentType type = ComponentType.Normal,
        ComponentEffect effect = ComponentEffect.PassiveEffect)
    {
        var data = new ComponentData { id = id, type = type, effectType = effect };
        GM.Ins.DM.componentConfig.componentList.Add(data);
        return data;
    }

    static void ComponentRegression()
    {
        Test("Component configuration flags duplicate ids and unfinished effects", () => {
            Component(1); Component(1, effect: ComponentEffect.ActiveSkill);
            var invalid = Component(2, effect: ComponentEffect.AttrBonus); invalid.attrId = 99;
            Assert(GM.Ins.DM.componentConfig.ValidateConfiguration().Count == 4, "Incomplete configuration passed validation");
        });
        Test("Unowned components cannot be equipped or created by unequipping", () => {
            Component(5); var p = new Player();
            Assert(!p.TryEquip(5) && !p.TryUnequip(5) && GM.Ins.PLAYERPROFILE.componentInventory.Count == 0,
                "Inventory duplication exploit");
        });
        Test("Equip and unequip move exactly one inventory entry", () => {
            Component(5); var p = new Player(); GM.Ins.PLAYERPROFILE.componentInventory.Add(5);
            Assert(p.TryEquip(5) && p.normalSlots[0] == 5 && !p.TryEquip(5), "Equip ownership failure");
            Assert(p.TryUnequip(5) && !p.TryUnequip(5) && GM.Ins.PLAYERPROFILE.componentInventory.SequenceEqual(new[] { 5 }),
                "Repeated unequip duplicated inventory");
        });
        Test("Full component slots leave inventory unchanged", () => {
            Component(3, ComponentType.Weapon); var p = new Player { weaponSlots = new[] { 1, 2 } };
            GM.Ins.PLAYERPROFILE.componentInventory.Add(3);
            Assert(!p.TryEquip(3) && GM.Ins.PLAYERPROFILE.componentInventory.Contains(3), "Full slot lost item");
        });
        Test("Legacy null slots recover with the correct category", () => {
            Component(3, ComponentType.Weapon); var p = new Player { normalSlots = null, weaponSlots = null };
            GM.Ins.PLAYERPROFILE.componentInventory.Add(3);
            Assert(p.TryEquip(3) && p.weaponSlots[0] == 3 && p.normalSlots.All(i => i == 0), "Category mismatch");
        });
        Test("Equipped effects deduplicate corrupt slots and ignore invalid categories", () => {
            Component(1); Component(2, ComponentType.Weapon);
            var p = new Player { normalSlots = new[] { 1, 1, 2 }, weaponSlots = new[] { 2, 2 } };
            Assert(ComponentEquipment.Equipped(p).Select(d => d.id).SequenceEqual(new[] { 1, 2 }), "Duplicate effects");
        });
        Test("Attribute plugins leave permanent player stats intact", () => {
            var data = Component(4, effect: ComponentEffect.AttrBonus); data.attrId = 2; data.attrValue = 3;
            var p = new Player { PHYSIQUE = 7 }; GM.Ins.PLAYERPROFILE.componentInventory.Add(4);
            p.TryEquip(4);
            Assert(p.AccessAttribute(2, AttrOp.Get) == 10 && p.PHYSIQUE == 7, "Bonus mutated save data");
            p.AccessAttribute(2, AttrOp.Add, 1); p.TryUnequip(4);
            Assert(p.AccessAttribute(2, AttrOp.Get) == 8, "Unequip removed permanent progression");
        });
        Test("Real skill payment includes component mana surcharge atomically", () => {
            var data = Component(6, ComponentType.Weapon); data.extraSkillMana = 2;
            var u = Unit().unitAttrCenter; u.pc.playerData = new Player { weaponSlots = new[] { 6, 0 } }; Stock();
            var s = Skill(3, new ItemPack(ItemName.A, 2));
            Assert(u.GetSkillManaCost(s) == 5 && s.mpCost == 3 && u.TryCostSkill(s), "Incorrect plugin cost");
            Balances(u, 2, 5, 3, 5);
        });
        Test("Surcharge resource failure consumes no action points or materials", () => {
            var data = Component(6, ComponentType.Weapon); data.extraSkillMana = 2;
            var u = Unit().unitAttrCenter; u.pc.playerData = new Player { weaponSlots = new[] { 6, 0 } }; Stock();
            Assert(!u.TryCostSkill(Skill(9, new ItemPack(ItemName.A, 2))), "Unaffordable plugin cast accepted");
            Balances(u, 3, 10, 5, 5);
        });
        Test("Long-term interests refunds the full plugin cost at the 35 percent boundary", () => {
            var data = Component(6, ComponentType.Weapon); data.extraSkillMana = 2;
            var a = Unit(); a.playerData = new Player { weaponSlots = new[] { 6, 0 } };
            a.availablePassives.Add(PassiveType.LongTermInterests); var s = Skill(3);
            Assert(a.unitAttrCenter.TryCostSkill(s), "Cast failed");
            UnityEngine.Random.NextValue = 35; ComponentEquipment.OnSkillUsed(a, s);
            Assert(a.unitAttrCenter.ManaPoint == 5, "35 must not refund");
            UnityEngine.Random.NextValue = 34; ComponentEquipment.OnSkillUsed(a, s);
            Assert(a.unitAttrCenter.ManaPoint == 10 && s.mpCost == 3, "Full surcharge not refunded");
        });
        Test("Auxiliary single-target accuracy matches preview and excludes area attacks", () => {
            var a = Unit(); var t = Unit(false, true); t.unitAttrCenter.buffAttrDic[BuffAttrType.EvasionRate] = 50;
            a.unitAttrCenter.buffAttrDic[BuffAttrType.SingleTargetHitRate] = 30;
            var s = Skill(); s.target = SkillTarget.Enemy; s.attackPacks.Add(new AttackPack(10, DamageType.Melee));
            var d = new DamageManager(BattleScene.Ins.BM);
            Assert(d.PreviewSkill(a, t, s).HitRate == 80, "Accuracy missing from preview");
            s.target = SkillTarget.EnemyAll;
            Assert(d.PreviewSkill(a, t, s).HitRate == 100, "Area guaranteed-hit rule changed");
        });
        Test("Lash and Implosion require a resolved hit and trigger once per attack", () => {
            var a = Unit(); var t = Unit(false, true); a.unitAttrCenter.CostMana(5);
            a.availablePassives.AddRange(new[] { PassiveType.Lash, PassiveType.Implosion });
            GM.Ins.DM.skillPackListSO.Skill = Skill();
            ComponentEquipment.OnAttackResolved(a, new(), Skill(), ActionType.近战攻击, default);
            Assert(a.unitAttrCenter.ManaPoint == 5 && BattleScene.Ins.BM.ExtraAttacks == 0, "Miss triggered plugins");
            ComponentEquipment.OnAttackResolved(a, new() { t }, Skill(), ActionType.近战攻击, default);
            Assert(a.unitAttrCenter.ManaPoint == 8 && BattleScene.Ins.BM.ExtraAttacks == 1, "Hit triggered wrong count");
        });
        Test("Spider silk affects hit targets and two distinct unhit enemies once", () => {
            var a = Unit(); a.availablePassives.Add(PassiveType.SpiderSilk);
            var hit = Unit(false, true); var b = Unit(false, true); var c = Unit(false, true); var e = Unit(false, true);
            BattleScene.Ins.BM.AIController.pieces.AddRange(new[] { hit, b, b, c, e });
            ComponentEquipment.OnAttackResolved(a, new() { hit, hit }, Skill(), ActionType.技能, default);
            Assert(hit.unitAttrCenter.CurHealth == 85 && hit.unitAttrCenter.GetBuffStacks(BuffType.Stun) == 2, "Hit target duplicated");
            Assert(new[] { b, c, e }.Count(t => t.unitAttrCenter.CurHealth == 85) == 2, "Wrong random target count");
        });
        Test("Spider true damage bypasses armor and barriers but respects cognitive targeting", () => {
            var a = Unit(); a.availablePassives.Add(PassiveType.SpiderSilk);
            var hit = Unit(false, true); var b = Unit(false, true); var protectedUnit = Unit(false, true);
            b.unitAttrCenter.attr.SetArmor(DamageType.Electric, 1000);
            BattleScene.Ins.BM.buffManager.AddBuff(b.unitAttrCenter, BuffType.SlowingField);
            BattleScene.Ins.BM.buffManager.AddBuff(protectedUnit.unitAttrCenter, BuffType.CognitiveProtection);
            BattleScene.Ins.BM.AIController.pieces.AddRange(new[] { b, protectedUnit });
            ComponentEquipment.OnAttackResolved(a, new() { hit }, Skill(), ActionType.技能, default);
            Assert(b.unitAttrCenter.CurHealth == 85 && protectedUnit.unitAttrCenter.CurHealth == 100, "True damage eligibility wrong");
        });
        Test("Spider silk does not trigger for misses or melee attacks", () => {
            var a = Unit(); var t = Unit(false, true); a.availablePassives.Add(PassiveType.SpiderSilk);
            ComponentEquipment.OnAttackResolved(a, new(), Skill(), ActionType.技能, default);
            ComponentEquipment.OnAttackResolved(a, new() { t }, Skill(), ActionType.近战攻击, default);
            Assert(t.unitAttrCenter.CurHealth == 100 && t.unitAttrCenter.GetBuffStacks(BuffType.Stun) == 0, "Spider triggered on wrong action");
        });
        Test("Attribute refresh preserves health mana action points and existing debuffs", () => {
            var u = Unit().unitAttrCenter; u.SetHealth(50); u.CostMana(5); u.SetMovePoint(2);
            BattleScene.Ins.BM.buffManager.AddBuff(u, BuffType.Disrupt);
            u.RefreshPlayerAttributes(new int[5], new[] { 2, 3, 4, 0, 2 });
            Assert(u.MaxHealth == 108 && u.CurHealth == 50 && u.ManaPoint == 5 && u.CurMovePoint == 2 &&
                u.GetBuffStacks(BuffType.Disrupt) == 1 && u.ATK == 1 && u.CON == 1, "Equipment reset battle state");
        });
        Test("Repeated player initialization does not accumulate derived attack stats", () => {
            var u = Unit().unitAttrCenter; var p = new Player { TACTICS = 7, RECOGNITION = 5 };
            u.SetData(new PieceData(), p); u.SetData(new PieceData(), p);
            Assert(u.ATK == 3 && u.CON == 2, "Repeated initialization stacked player bonuses");
        });
        Test("Miss and barrier settlements never report a plugin attack hit", () => {
            var a = Unit(); var t = Unit(false, true); var s = Skill(); s.target = SkillTarget.Enemy;
            s.attackPacks.Add(new AttackPack(10, DamageType.Melee));
            t.unitAttrCenter.buffAttrDic[BuffAttrType.EvasionRate] = 100;
            var d = new DamageManager(BattleScene.Ins.BM);
            Assert(!d.ResolveSkillTarget(a, t, s).HitAttack, "Miss became a plugin hit");
            t.unitAttrCenter.buffAttrDic[BuffAttrType.EvasionRate] = 0;
            BattleScene.Ins.BM.buffManager.AddBuff(t.unitAttrCenter, BuffType.SlowingField);
            Assert(!d.ResolveSkillTarget(a, t, s).HitAttack, "Barrier became a plugin hit");
        });
    }
}
