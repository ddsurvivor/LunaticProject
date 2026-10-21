using System;
using System.Collections.Generic;

/// <summary>本场战斗的模因暴露与抗性，不写入角色永久解锁或身体类型。</summary>
public sealed class MemeUnitState
{
    public bool NetworkLinked { get; set; }
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
        resistance.Clear();
    }
}
