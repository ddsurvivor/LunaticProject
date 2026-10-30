using System;
using System.Collections.Generic;

/// <summary>本场战斗的模因暴露与抗性，不写入角色永久解锁或身体类型。</summary>
public sealed class MemeUnitState
{
    private bool manualNetworkLinked; // 兼容旧调用的独立暴露来源，不能覆盖节点连接。
    private readonly HashSet<MemeNetworkNode> networkSources = new(); // 每个节点只登记一次。
    /// <summary>任一来源仍连接即暴露；旧布尔赋值仅设置独立的手动来源。</summary>
    public bool NetworkLinked { get => manualNetworkLinked || networkSources.Count > 0; set => manualNetworkLinked = value; }
    /// <summary>当前连接来源数，包含旧接口的手动来源。</summary>
    public int NetworkSourceCount => networkSources.Count + (manualNetworkLinked ? 1 : 0);

    internal void AddNetworkSource(MemeNetworkNode source) => networkSources.Add(source);
    internal void RemoveNetworkSource(MemeNetworkNode source) => networkSources.Remove(source);

    /// <summary>死亡、重建或战斗退出时解除所有暴露来源；临时隐藏棋子不调用。</summary>
    public void ClearNetworkLinks()
    {
        manualNetworkLinked = false;
        // 双向解除，防止节点持有死亡或对象池复用前的旧连接。
        foreach (var source in new List<MemeNetworkNode>(networkSources)) source.DisconnectState(this);
        networkSources.Clear();
    }
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
        ClearNetworkLinks();
        Paralyzed = Violent = Inverted = AttackForbidden = false;
        TimedMoveMultiplier = 1f;
        resistance.Clear();
    }
}
