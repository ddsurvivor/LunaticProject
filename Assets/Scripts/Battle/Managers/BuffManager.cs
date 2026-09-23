using System;
using UnityEngine;

/// <summary>
/// buff管理器
/// 负责添加、移除buff，每回合结算buff效果
/// </summary>
public class BuffManager : MonoBehaviour
{
    [Min(1)] public int defaultBarrierHealth = 100;
    [Min(1)] public int defaultRetaliationTurns = 3;

    public static bool CanTarget(PieceController attacker, PieceController target)
    {
        return attacker != null && target != null && (attacker.isPlayerPiece == target.isPlayerPiece ||
            target.unitAttrCenter.GetBuffStacks(BuffType.CognitiveProtection) == 0);
    }

    // 每次攻击每个目标触发一次，未命中也会招致反噬。
    public void OnAttacked(PieceController attacker, PieceController target)
    {
        if (attacker.isPlayerPiece == target.isPlayerPiece) return;
        var protection = target.unitAttrCenter.buffStates.Find(
            b => b.buffType == BuffType.OffensiveCognitiveProtection);
        if (protection == null || attacker.isDead) return;
        int turns = Mathf.Max(1, protection.retaliationTurns);
        var existing = attacker.unitAttrCenter.buffStates.Find(b => b.buffType == BuffType.MemeticRetaliation);
        if (existing == null) AddBuff(attacker.unitAttrCenter, BuffType.MemeticRetaliation, turns);
        else if (existing.stacks != -1)
        {
            existing.stacks = Mathf.Max(existing.stacks, turns);
            RefreshEnemyBuffs(attacker.unitAttrCenter);
        }
    }

    // 破盾的这一段攻击仍被完全吸收，不将溢出伤害传给生命值。
    public int AbsorbAttack(UnitAttrCenter unit, int damage)
    {
        var barrier = unit.buffStates.Find(b => b.buffType == BuffType.SlowingField);
        if (barrier == null) return damage;
        barrier.barrierHealth -= Mathf.Max(0, damage);
        if (barrier.barrierHealth <= 0) RemoveBuff(unit, BuffType.SlowingField, -1);
        else RefreshEnemyBuffs(unit);
        return 0;
    }

    public bool OnAction(UnitAttrCenter unit)
    {
        if (unit.pc.isDead || unit.GetBuffStacks(BuffType.Stun) != 0) return false;
        if (unit.GetBuffStacks(BuffType.SpontaneousMemeticAttack) != 0)
        {
            unit.TakeDamage(new AttackPack(Mathf.CeilToInt(unit.MaxHealth * 0.15f), DamageType.Electric));
            if (unit.pc.isDead) return false;
            if (UnityEngine.Random.Range(0, 100) < 30)
            {
                BuffType[] states = { BuffType.Disrupt, BuffType.Bind, BuffType.Stun };
                AddBuff(unit, states[UnityEngine.Random.Range(0, states.Length)]);
            }
        }
        return !unit.pc.isDead && unit.GetBuffStacks(BuffType.Stun) == 0;
    }

    //======= buff =======//
    // 添加buff
    public void AddBuff(UnitAttrCenter unit, BuffType buff, int stack = 1,
        int barrierHealth = 0, int retaliationTurns = 0)
    {
        if (stack == 0 || stack < -1) return;
        var existingBuff = unit.buffStates.Find(b => b.buffType == buff);
        if (existingBuff != null)
        {
            existingBuff.stacks = existingBuff.stacks == -1 || stack == -1
                ? -1 : existingBuff.stacks + stack;
        }
        else
        {
            unit.buffStates.Add(new BuffState(buff, stack));
            ApplyBuff(buff, unit, true);
        }

        var state = unit.buffStates.Find(b => b.buffType == buff);
        if (buff == BuffType.SlowingField)
        {
            state.stacks = -1; // 仅按耐久消耗，不随回合消失
            state.barrierHealth += Mathf.Max(1, barrierHealth > 0 ? barrierHealth : defaultBarrierHealth);
        }
        if (buff == BuffType.OffensiveCognitiveProtection)
            state.retaliationTurns = Mathf.Max(1, retaliationTurns > 0 ? retaliationTurns : defaultRetaliationTurns);
        if (buff == BuffType.Stun) unit.SetMovePoint(0);

        BattleScene.Ins.BM.tipTextManager.ShowBuffAdded(unit.transform, buff.ToChinese(), stack);
        RefreshEnemyBuffs(unit);
    }

    // 移除buff
    public void RemoveBuff(UnitAttrCenter unit, BuffType buff, int stack = 1)
    {
        var existingBuff = unit.buffStates.Find(b => b.buffType == buff);
        if (existingBuff != null)
        {
            if (existingBuff.stacks == -1 && stack != -1) // 无限层数
            {
                return;
            }


            existingBuff.stacks = stack == -1 ? 0 : existingBuff.stacks - stack;
            if (existingBuff.stacks <= 0)
            {
                unit.buffStates.Remove(existingBuff);
                ApplyBuff(buff, unit, false);
            }
            RefreshEnemyBuffs(unit);
        }
    }

    private static void RefreshEnemyBuffs(UnitAttrCenter unit)
    {
        if (unit.pc is EnemyController enemy && enemy.enemyCanvas != null)
            enemy.enemyCanvas.UpdateBuffs(unit.buffStates);
    }

    public void ApplyBuff(BuffType buff, UnitAttrCenter unit, bool add = true)
    {
        float value = add ? 1 : -1;
        switch (buff)
        {
            case BuffType.Charge:
                // 伤害增加30%
                unit.AddBuffAttr(BuffAttrType.DamageIncrease, value * 30);
                break;
            case BuffType.Shield:
                // 伤害减免30%
                unit.AddBuffAttr(BuffAttrType.DamageReduction, value * 30);
                break;
            case BuffType.Disrupt:
                // 命中率减少30%
                unit.AddBuffAttr(BuffAttrType.HitRate, value * -30);
                break;
            case BuffType.Overload:
                // 移动范围减少50%
                unit.AddBuffAttr(BuffAttrType.MoveRangePercent, value * -50);
                break;
            case BuffType.Bind:
                // 移动范围减少100%
                unit.AddBuffAttr(BuffAttrType.MoveRangePercent, value * -100);
                break;
            case BuffType.Conceal:
                // 闪避率增加30%
                unit.AddBuffAttr(BuffAttrType.EvasionRate, value * 30);
                break;
            case BuffType.Frail:
                unit.AddBuffAttr(BuffAttrType.MeleeArmorPercent, value * -30);
                // 伤害减少
                unit.AddBuffAttr(BuffAttrType.DamageIncrease, value * -20);
                break;
            default:
                break;
        }
    }


    // 回合结束时调用，处理一个单位身上所有的buff效果
    // 改为回合开始时结算
    public void ProcessBuffs(UnitAttrCenter unit)
    {
        var snapshot = unit.buffStates.ToArray();
        for (var i = snapshot.Length - 1; i >= 0; i--)
        {
            if (unit.pc.isDead) break;
            var buff = snapshot[i];
            if (!unit.buffStates.Contains(buff)) continue;
            switch (buff.buffType)
            {
                case BuffType.SlowingField:
                    continue;
                case BuffType.Stun:
                    unit.SetMovePoint(0);
                    break;
                case BuffType.MemeticRetaliation:
                    unit.TakeDamage(new AttackPack(Mathf.CeilToInt(unit.MaxHealth * 0.20f), DamageType.Electric));
                    break;
                case BuffType.AutoHeal:
                    // 恢复15%生命值
                    int healAmount = Mathf.CeilToInt(unit.MaxHealth * 0.15f);
                    unit.Heal(healAmount);
                    break;
                case BuffType.Burn:
                    // 持续掉血
                    int burnDamage = unit.GetBuffStacks(BuffType.Burn);
                    unit.TakeDamage(new AttackPack(burnDamage, DamageType.Electric));
                    RemoveBuff(unit, BuffType.Burn, 3); // 每回合减少3层燃烧效果
                    continue;
                default:
                    break;
            }

            RemoveBuff(unit, buff.buffType, 1);
        }
    }
}
