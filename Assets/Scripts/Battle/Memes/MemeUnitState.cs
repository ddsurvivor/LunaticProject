using System;
using System.Collections.Generic;

/// <summary>本场战斗的模因暴露与抗性，不写入角色永久解锁或身体类型。</summary>
public sealed class MemeUnitState
{
    public bool NetworkLinked { get; set; }
    /// <summary>付费主动行动受麻痹判定；不会永久冻结输入。</summary>
    public bool Paralyzed { get; internal set; }
    /// <summary>AI代管并优先攻击原友军，受伤或指定治疗可解除。</summary>
    public bool Violent { get; internal set; }
    /// <summary>AI代管并反转行为目标关系，原阵营不变。</summary>
    public bool Inverted { get; internal set; }
    /// <summary>禁止主动进攻及免费攻击反应，AI优先解控或逃离。</summary>
    public bool AttackForbidden { get; internal set; }
    public float TimedMoveMultiplier { get; internal set; } = 1f; // 精确状态的独立移动倍率，不累加进旧Buff属性。
    private readonly Dictionary<int, int> resistance = new(); // 每种模因独立的豁免百分比。
    public int GetResistance(int memeId) => resistance.TryGetValue(memeId, out int value) ? value : 0;

    /// <summary>设置本场词条抗性；自然到期或指定解除后的抗性由效果处理器明确调用。</summary>
    public void SetResistance(int memeId, int percent)
    {
        if (memeId <= 0) return;
        percent = Math.Max(0, Math.Min(100, percent));
        if (percent == 0) resistance.Remove(memeId);
        else resistance[memeId] = percent;
    }

    /// <summary>棋子重新初始化或对象池复用时清除上场状态。</summary>
    public void Reset()
    {
        NetworkLinked = false;
        Paralyzed = Violent = Inverted = AttackForbidden = false;
        TimedMoveMultiplier = 1f;
        resistance.Clear();
    }
}
