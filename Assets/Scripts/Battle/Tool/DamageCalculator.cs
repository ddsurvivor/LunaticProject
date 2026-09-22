using System;

/// <summary>暗骰伤害规则；实战和预览共用确定性计算入口。</summary>
public static class DamageCalculator
{
    private static readonly Random Random = new Random();
    public const int MinDice = 4;
    public const int MaxDice = 24;

    public static int Roll4D6()
    {
        return Random.Next(1, 7) + Random.Next(1, 7) + Random.Next(1, 7) + Random.Next(1, 7);
    }

    public static int CalculateActualDamage(int atk, int def, bool isCrit, int critMultiplierPct)
    {
        return CalculateDamage(atk, def, isCrit, critMultiplierPct, isCrit ? MinDice : Roll4D6());
    }

    public static int CalculateDamage(int atk, int def, bool isCrit, int critMultiplierPct, int dice)
    {
        // 1. 暴击和暗骰大成功都使用同一倍率，并无视护甲。
        if (critMultiplierPct == 0) critMultiplierPct = 100;
        int check = atk + dice - def;
        if (isCrit || check > 24) return (int)Math.Floor(atk * (critMultiplierPct / 100f));

        // 2. 检定失败造成 20% 伤害，普通成功扣除护甲。
        if (check < 6) return (int)Math.Floor(atk * 0.2f);
        return Math.Max(0, atk - def);
    }

    public static DamagePreviewResult CalculateDamagePreview(int atk, int def,
        bool isCrit = false, int critMultiplierPct = 100)
    {
        // 3. 遍历所有可能点数；失败伤害可能高于普通成功，不能只取端点。
        int min = int.MaxValue;
        int max = int.MinValue;
        for (int dice = MinDice; dice <= MaxDice; dice++)
        {
            int damage = CalculateDamage(atk, def, isCrit, critMultiplierPct, dice);
            min = Math.Min(min, damage);
            max = Math.Max(max, damage);
        }
        return DamagePreviewResult.Create(min, max);
    }
}

public struct DamagePreviewResult
{
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public int YellowZoneValue { get; set; }
    public int RedZoneValue { get; set; }

    public static DamagePreviewResult Create(int min, int max)
    {
        return new DamagePreviewResult
        {
            MinDamage = min, MaxDamage = max, YellowZoneValue = min, RedZoneValue = max
        };
    }
}
