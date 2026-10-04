using UnityEngine;

static partial class Program
{
    static void BuffRegression()
    {
        Test("Overload changes movement range and restores it on expiry", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager;
            bm.AddBuff(u, BuffType.Overload);
            Assert(u.MoveRange == 2.5f, "Overload movement multiplier missing");
            bm.ProcessBuffs(u);
            Assert(u.MoveRange == 2.5f, "One-turn buff expired before action");
            bm.ProcessBuffDurations(u);
            Assert(u.MoveRange == 5f, "Movement did not recover");
        });
        Test("Bind plus Overload clamps range and rejects movement", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager;
            bm.AddBuff(u, BuffType.Bind); bm.AddBuff(u, BuffType.Overload);
            bm.ProcessBuffs(u);
            Assert(u.MoveRange == 0 && !u.CostMP(ActionType.移动), "Bind did not prevent movement");
            bm.RemoveBuff(u, BuffType.Bind, -1);
            Assert(u.MoveRange == 2.5f, "Removing Bind lost Overload");
        });
        Test("One-turn attribute buffs remain usable throughout the turn", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager;
            bm.AddBuff(u, BuffType.Charge); bm.AddBuff(u, BuffType.Shield);
            bm.ProcessBuffs(u);
            Assert(u.buffAttrDic[BuffAttrType.DamageIncrease] == 30 && u.buffAttrDic[BuffAttrType.DamageReduction] == 30,
                "Buff removed before turn actions");
            bm.ProcessBuffDurations(u); bm.ProcessBuffDurations(u);
            Assert(u.buffAttrDic[BuffAttrType.DamageIncrease] == 0 && u.buffAttrDic[BuffAttrType.DamageReduction] == 0,
                "Expiry should reverse attributes exactly once");
        });
        Test("Mid-turn buffs survive that turn's duration settlement", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager;
            bm.ProcessBuffs(u); bm.AddBuff(u, BuffType.Disrupt);
            bm.ProcessBuffDurations(u);
            Assert(u.GetBuffStacks(BuffType.Disrupt) == 1, "New buff lost a turn");
            bm.ProcessBuffs(u); bm.ProcessBuffDurations(u);
            Assert(u.GetBuffStacks(BuffType.Disrupt) == 0, "New buff never expired");
        });
        Test("Stun prevents burst refills and AP recovery until turn end", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager;
            bm.AddBuff(u, BuffType.Stun); bm.ProcessBuffs(u);
            u.FullMovePoint(); u.AddMP(2); u.SetMovePoint(3);
            Assert(u.CurMovePoint == 0 && !u.CostMP(0), "Stun bypassed by AP refill");
            bm.ProcessBuffDurations(u); u.FullMovePoint();
            Assert(u.CurMovePoint == 3, "Stun did not expire");
        });
        Test("Cover removal preserves independently applied skill buffs", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager; var cover = new object();
            bm.AddBuff(u, BuffType.Shield, 2); bm.AddBuffSource(u, BuffType.Shield, cover);
            bm.AddBuffSource(u, BuffType.Shield, cover);
            Assert(u.buffAttrDic[BuffAttrType.DamageReduction] == 30, "Cover stacked attribute twice");
            bm.RemoveBuffSource(u, BuffType.Shield, cover);
            Assert(u.GetBuffStacks(BuffType.Shield) == 2 && u.buffAttrDic[BuffAttrType.DamageReduction] == 30,
                "Leaving cover erased skill buff");
        });
        Test("Cover-only status remains until the final source leaves", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager;
            var a = new object(); var b = new object();
            bm.AddBuffSource(u, BuffType.Conceal, a); bm.AddBuffSource(u, BuffType.Conceal, b);
            bm.ProcessBuffs(u); bm.ProcessBuffDurations(u); bm.RemoveBuffSource(u, BuffType.Conceal, a);
            Assert(u.GetBuffStacks(BuffType.Conceal) == -1, "Remaining source lost");
            bm.RemoveBuffSource(u, BuffType.Conceal, b);
            Assert(u.GetBuffStacks(BuffType.Conceal) == 0 && u.buffAttrDic[BuffAttrType.EvasionRate] == 0,
                "Cover attributes did not recover");
        });
        Test("Timed buff expires under cover without removing cover protection", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager; var cover = new object();
            bm.AddBuffSource(u, BuffType.Shield, cover); bm.AddBuff(u, BuffType.Shield);
            bm.ProcessBuffs(u); bm.ProcessBuffDurations(u);
            Assert(u.GetBuffStacks(BuffType.Shield) == -1, "Cover expired with timed buff");
            bm.RemoveBuffSource(u, BuffType.Shield, cover);
            Assert(u.GetBuffStacks(BuffType.Shield) == 0, "Expired timed buff revived after leaving cover");
        });
        Test("Burn deals positive damage for permanent states and decays finite layers", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager;
            bm.AddBuff(u, BuffType.Burn, 5); bm.ProcessBuffs(u);
            Assert(u.CurHealth == 95 && u.GetBuffStacks(BuffType.Burn) == 2, "Finite burn settlement wrong");
            bm.ProcessBuffDurations(u);
            Assert(u.GetBuffStacks(BuffType.Burn) == 2, "Burn decayed twice");
            bm.RemoveBuff(u, BuffType.Burn, -1); bm.AddBuff(u, BuffType.Burn, -1); bm.ProcessBuffs(u);
            Assert(u.CurHealth == 94 && u.GetBuffStacks(BuffType.Burn) == -1, "Permanent burn became negative damage");
        });
        Test("Retaliation resolves on exactly three owner turns", () => {
            var u = Unit().unitAttrCenter; var bm = BattleScene.Ins.BM.buffManager;
            bm.AddBuff(u, BuffType.MemeticRetaliation, 3);
            for (int i = 0; i < 3; i++) { bm.ProcessBuffs(u); bm.ProcessBuffDurations(u); }
            bm.ProcessBuffs(u);
            Assert(u.CurHealth == 40 && u.GetBuffStacks(BuffType.MemeticRetaliation) == 0, "Wrong retaliation duration");
        });
        Test("Hostile non-damaging skills do not trigger attack retaliation", () => {
            var a = Unit(); var t = Unit(false, true);
            BattleScene.Ins.BM.buffManager.AddBuff(t.unitAttrCenter, BuffType.OffensiveCognitiveProtection);
            new DamageManager(BattleScene.Ins.BM).ResolveSkillTarget(a, t, Skill());
            Assert(a.unitAttrCenter.GetBuffStacks(BuffType.MemeticRetaliation) == 0, "Debuff-only skill counted as attack");
        });
        Test("Reinitialization clears stale buffs and their attribute modifiers", () => {
            var u = Unit().unitAttrCenter;
            BattleScene.Ins.BM.buffManager.AddBuff(u, BuffType.Overload);
            u.Init();
            Assert(u.buffStates.Count == 0 && u.MoveRange == 5f, "Reused unit kept stale state");
        });
    }
}
