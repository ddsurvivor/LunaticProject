
    using System;
    using UnityEngine;

    public class BuffManager: MonoBehaviour
    {
        //======= buff =======//
        // 添加buff
        public void AddBuff(UnitAttrCenter unit, BuffType buff, int stack=1)
        {
            var existingBuff = unit.buffStates.Find(b => b.buffType == buff);
            if (existingBuff != null)
            {
                existingBuff.stacks += stack;// 相同buff叠加层数
            }
            else
            {
                unit.buffStates.Add(new BuffState(buff, stack));
                ApplyBuff(buff, unit, true);
            }
        }

        // 移除buff
        public void RemoveBuff(UnitAttrCenter unit, BuffType buff, int stack=1)
        {
            var existingBuff = unit.buffStates.Find(b => b.buffType == buff);
            if (existingBuff != null)
            {
                if (existingBuff.stacks == -1 && stack!=-1)// 无限层数
                {
                    return;
                }
                
                
                existingBuff.stacks -= stack;
                if (existingBuff.stacks <= 0)
                {
                    unit.buffStates.Remove(existingBuff);
                    ApplyBuff(buff, unit, false);
                }
            }
        }

        public void ApplyBuff(BuffType buff, UnitAttrCenter unit, bool add = true)
        {
            float value = add ? 1 : -1;
            switch (buff)
            {
                case BuffType.Charge:
                    // 伤害增加30%
                    unit.AddBuffAttr(BuffAttrType.DamageIncrease,  value* 30);
                    break;
                case BuffType.Shield:
                    // 伤害减免30%
                    unit.AddBuffAttr(BuffAttrType.DamageReduction, value*30);
                    break;
                case BuffType.Disrupt:
                    // 命中率减少30%
                    unit.AddBuffAttr(BuffAttrType.HitRate, value*-30);
                    break;
                case BuffType.Overload:
                    // 移动范围减少50%
                    unit.AddBuffAttr(BuffAttrType.MoveRangePercent, value*-50);
                    break;
                case BuffType.Bind:
                    // 移动范围减少100%
                    unit.AddBuffAttr(BuffAttrType.MoveRangePercent, value*-100);
                    break;
                case BuffType.Conceal:
                    // 闪避率增加30%
                    unit.AddBuffAttr(BuffAttrType.EvasionRate, value*30);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buff), buff, null);
            }
        }
        
        
        // 回合结束时调用，处理一个单位身上所有的buff效果
        public void ProcessBuffs(UnitAttrCenter unit)
        {
            for (var i = unit.buffStates.Count - 1; i >= 0; i--)
            {
                var buff = unit.buffStates[i];
                switch (buff.buffType)
                {
                    case BuffType.AutoHeal:
                        // 恢复15%生命值
                        int healAmount = Mathf.CeilToInt(unit.MaxHealth * 0.15f);
                        unit.Heal(healAmount);
                        break;
                    case BuffType.Burn:
                        // 持续掉血
                        int burnDamage = unit.GetBuffStacks(BuffType.Burn);
                        unit.TakeDamage(burnDamage);
                        RemoveBuff(unit, BuffType.Burn, 3); // 每回合减少3层燃烧效果
                        return;
                    default:
                        break;
                }

                RemoveBuff(unit, buff.buffType, 1);
            }
        }
    }
