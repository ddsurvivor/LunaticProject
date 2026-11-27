using System;

/// <summary>聚能资源规则；一次发动统一消耗50，参与人数及容量升级不提高费用。</summary>
[Serializable]
public sealed class BurstGauge
{
    public const float ActorCost = 50f; // 一次发动的统一费用；保留常量名兼容已有调用。
    public float Charge; // 当前能量，扣费后保留余量。
    public int Cooldown { get; private set; } // 尚需完成的普通棋子回合数。
    public float Capacity { get; private set; } = 100f; // 当前等级允许储存的最大能量。
    public float ChargeMultiplier { get; private set; } = 1f; // 动力脊对单次充能的倍率。
    /// <summary>兼容旧人数查询；满足一次发动费用即可支持三名主角，不再按余额划分参与人数。</summary>
    public int AffordableActors => Ready ? 3 : 0;
    public bool Ready => Cooldown == 0 && Charge >= ActorCost;

    /// <summary>按动力脊等级 0–3 初始化并清空能量与冷却；序章/教学规则强制使用未升级数值。</summary>
    public void Reset(int level, bool prologue)
    {
        level = prologue ? 0 : Math.Max(0, Math.Min(3, level));
        Capacity = new[] { 100f, 130f, 160f, 200f }[level];
        // 三级获得率在二级基础上再乘 1.1。
        ChargeMultiplier = new[] { 1f, 1f, 1.1f, 1.21f }[level];
        Charge = 0;
        Cooldown = 0;
    }
    /// <summary>按升级倍率增加基础充能量并限制容量；冷却期间仍可充能，忽略无效或非正数输入。</summary>
    public void Add(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return;
        Charge = Math.Min(Capacity, Charge + amount * ChargeMultiplier);
    }
    /// <summary>有效参战人数大于零时统一扣50；冷却或余额不足时不扣费，人数不作为费用倍率。</summary>
    public bool Spend(int actors)
    {
        if (!Ready || actors <= 0) return false;
        Charge -= ActorCost;
        return true;
    }
    /// <summary>退出聚能时开始三个普通棋子回合的冷却。</summary>
    public void BeginCooldown() => Cooldown = 3;
    /// <summary>由调度器在一个可计入冷却的普通棋子回合结束时调用一次。</summary>
    public void CompleteNormalTurn() { if (Cooldown > 0) Cooldown--; }
}
