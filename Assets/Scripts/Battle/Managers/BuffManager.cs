
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
                existingBuff.stacks += stack;
            }
            else
            {
                unit.buffStates.Add(new BuffState(buff, stack));
            }
        }
        
        // 移除buff
        public void RemoveBuff(UnitAttrCenter unit, BuffType buff, int stack=1)
        {
            var existingBuff = unit.buffStates.Find(b => b.buffType == buff);
            if (existingBuff != null)
            {
                existingBuff.stacks -= stack;
                if (existingBuff.stacks <= 0)
                {
                    unit.buffStates.Remove(existingBuff);
                }
            }
        }
        
        
        // 回合结束时调用，处理一个单位身上所有的buff效果
        public void ProcessBuffs(UnitAttrCenter unit)
        {
            foreach (var buff in unit.buffStates)
            {
                switch (buff.buffType)
                {
                    case BuffType.Charge:
                        break;
                    case BuffType.AutoHeal:
                        // 恢复15%生命值
                        break;
                    case BuffType.Shield:
                        break;
                    case BuffType.Disrupt:
                        break;
                    case BuffType.Overload:
                        break;
                    case BuffType.Bind:
                        break;
                    case BuffType.Burn:
                        // 持续掉血
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
