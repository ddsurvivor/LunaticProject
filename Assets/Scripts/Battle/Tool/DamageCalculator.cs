using System;

public static class DamageCalculator
{
    private static readonly Random _random = new Random();

    /// <summary>
    /// 核心检定阈值常量
    /// </summary>
    private const int FAIL_THRESHOLD = 6;
    private const int CRIT_THRESHOLD = 24;

    /// <summary>
    /// 实际伤害计算（暗骰判定）
    /// </summary>
    /// <param name="atk">攻击力</param>
    /// <param name="def">对应的防御属性值（动能/热能/火种）</param>
    /// <param name="critMultiplierPct">暴击倍率（单位为%，例如150表示150%）</param>
    /// <returns>最终造成的实际伤害（向下取整）</returns>
    public static int CalculateActualDamage(int atk, int def,bool isCrit, int critMultiplierPct)
    {
        if(critMultiplierPct == 0)critMultiplierPct = 100; // 默认1倍
        if (isCrit)
        {
            //暴击/大成功：无视防御力的暴击伤害 = 攻击力 * (暴击倍率 / 100f)
            float finalCritDamage = atk * (critMultiplierPct / 100f);
            return (int)Math.Floor(finalCritDamage); 
        }
        // 1. 暗骰 4D6 判定
        int diceResult = Roll4D6();
    
        // 2. 计算检定结果：攻击力 + 4D6 - 防御力
        int checkResult = atk + diceResult - def;

        // 3. 根据检定结果判定伤害区间
        if (checkResult < FAIL_THRESHOLD)
        {
            // 失败：造成攻击力 20% 的伤害
            return (int)Math.Floor(atk * 0.2f);
        }
        else if (checkResult <= CRIT_THRESHOLD)
        {
            // 普通命中：造成 攻击力 - 防御属性 的伤害（确保不为负数）
            return Math.Max(0, atk - def);
        }
        else
        {
            // 暴击/大成功：无视防御力的暴击伤害 = 攻击力 * (暴击倍率 / 100f)
            float finalCritDamage = atk * (critMultiplierPct / 100f);
            return (int)Math.Floor(finalCritDamage); 
        }
    }

    /// <summary>
    /// 伤害预期预览（用于UI视觉引导：红黄方块区间）
    /// </summary>
    /// <param name="atk">攻击力</param>
    /// <param name="def">对应的防御属性值</param>
    /// <returns>返回包含最大、最小伤害及UI颜色区间的结构体</returns>
    public static DamagePreviewResult CalculateDamagePreview(int atk, int def)
    {
        // 4D6 的极端值：最小 4，最大 24
        int minDice = 4;
        int maxDice = 24;

        int minCheck = atk + minDice - def;
        int maxCheck = atk + maxDice - def;

        int minDamage = GetDamageByCheckResult(atk, def, minCheck);
        int maxDamage = GetDamageByCheckResult(atk, def, maxCheck);

        // 视觉引导逻辑：
        // 黄色方块表示最低伤害区间，红色方块表示最大伤害区间。
        // 这里将具体数值直接返回给UI层去渲染方块长度或文本
        return new DamagePreviewResult
        {
            MinDamage = minDamage,
            MaxDamage = maxDamage,
            YellowZoneValue = minDamage, // 黄色引导：最低保障伤害
            RedZoneValue = maxDamage     // 红色引导：最高上限伤害
        };
    }

    /// <summary>
    /// 内部工具：根据检定结果计算对应伤害
    /// </summary>
    private static int GetDamageByCheckResult(int atk, int def, int checkResult)
    {
        if (checkResult < FAIL_THRESHOLD)
        {
            return (int)Math.Floor(atk * 0.2f);
        }
        else if (checkResult <= CRIT_THRESHOLD)
        {
            return Math.Max(0, atk - def);
        }
        else
        {
            return atk;
        }
    }

    /// <summary>
    /// 模拟 4D6 掷骰
    /// </summary>
    private static int Roll4D6()
    {
        return _random.Next(1, 7) + _random.Next(1, 7) + _random.Next(1, 7) + _random.Next(1, 7);
    }
}

/// <summary>
/// 伤害预览数据结构
/// </summary>
public struct DamagePreviewResult
{
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    
    // UI 引导映射：你可以直接用这两个值来决定血条上黄色和红色方块的占比/长度
    public int YellowZoneValue { get; set; } // 最低伤害（黄色）
    public int RedZoneValue { get; set; }    // 最高伤害（红色）
}