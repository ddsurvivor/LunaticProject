using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>敌人可配置的每级成长属性，数值编号保持稳定以兼容已保存的数据。</summary>
public enum EnemyGrowthAttribute
{
    [LabelText("最大生命值")] MaxHealth = 1,
    [LabelText("最大行动力")] MaxActionPoints = 2,
    [LabelText("移动范围")] MoveRange = 3,
    [LabelText("最大能量值")] MaxMana = 4,
    [LabelText("最大弹药量")] MaxAmmo = 5,
    [LabelText("通用攻击力")] Attack = 6,
    [LabelText("对抗值")] Constitution = 7,
    [LabelText("动能攻击力")] KineticAttack = 8,
    [LabelText("热能攻击力")] ThermalAttack = 9,
    [LabelText("火种攻击力")] SparkAttack = 10,
    [LabelText("动能护甲")] KineticArmor = 11,
    [LabelText("热能护甲")] ThermalArmor = 12,
    [LabelText("火种护甲")] SparkArmor = 13,
    [LabelText("命中率（百分点）")] HitRate = 14,
    [LabelText("闪避率（百分点）")] EvasionRate = 15,
    [LabelText("暴击率（百分点）")] CriticalRate = 16,
    [LabelText("暴击倍率（百分点）")] CriticalDamage = 17,
    [LabelText("伤害增加（百分点）")] DamageIncrease = 18,
    [LabelText("伤害减免（百分点）")] DamageReduction = 19,
    [LabelText("嘲讽值")] Taunt = 20
}

/// <summary>1～100 级线性成长；仅计算数值，不修改 SO 或运行时单位。</summary>
public static class EnemyLevelGrowth
{
    public const int MinLevel = 1;
    public const int MaxLevel = 100;

    public static int Calculate(float baseValue, float perLevel, int level)
    {
        // 1. 等级 1 使用基础值，等级 100 累计 99 次成长。
        int steps = Mathf.Clamp(level, MinLevel, MaxLevel) - MinLevel;
        if (float.IsNaN(perLevel) || float.IsInfinity(perLevel)) perLevel = 0f;
        if (float.IsNaN(baseValue) || float.IsInfinity(baseValue)) baseValue = 0f;
        // 2. 用十进制计算小数成长，避免 0.02 × 50 的浮点误差导致少加 1。
        decimal value = (decimal)Mathf.Clamp(baseValue, -1000000000f, 1000000000f)
            + (decimal)Mathf.Clamp(perLevel, -1000000000f, 1000000000f) * steps;
        return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, decimal.Truncate(value)));
    }

    public static Dictionary<EnemyGrowthAttribute, float> CreateDefaults(PieceData data)
    {
        // 3. 百级默认：生命约三倍，攻防稳步增长，行动力和移动范围只少量增加。
        return new Dictionary<EnemyGrowthAttribute, float>
        {
            { EnemyGrowthAttribute.MaxHealth, Mathf.Max(1f, data.maxHealth * 0.02f) },
            { EnemyGrowthAttribute.MaxActionPoints, 0.025f },
            { EnemyGrowthAttribute.MoveRange, 0.03f },
            { EnemyGrowthAttribute.MaxMana, Mathf.Max(0f, data.maxMana * 0.01f) },
            { EnemyGrowthAttribute.MaxAmmo, data.maxAmmoCount > 0 ? 0.02f : 0f },
            { EnemyGrowthAttribute.Attack, 0.2f },
            { EnemyGrowthAttribute.Constitution, 0.05f },
            { EnemyGrowthAttribute.KineticAttack, 0f },
            { EnemyGrowthAttribute.ThermalAttack, 0f },
            { EnemyGrowthAttribute.SparkAttack, 0f },
            { EnemyGrowthAttribute.KineticArmor, 0.1f },
            { EnemyGrowthAttribute.ThermalArmor, 0.1f },
            { EnemyGrowthAttribute.SparkArmor, 0.1f },
            { EnemyGrowthAttribute.HitRate, 0.1f },
            { EnemyGrowthAttribute.EvasionRate, 0.05f },
            { EnemyGrowthAttribute.CriticalRate, 0.05f },
            { EnemyGrowthAttribute.CriticalDamage, 0.25f },
            { EnemyGrowthAttribute.DamageIncrease, 0f },
            { EnemyGrowthAttribute.DamageReduction, 0f },
            { EnemyGrowthAttribute.Taunt, 0f }
        };
    }
}
