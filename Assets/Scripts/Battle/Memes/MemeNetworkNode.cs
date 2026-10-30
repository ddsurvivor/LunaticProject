using System.Collections.Generic;
using UnityEngine;

/// <summary>信息节点的连接来源；范围或交互模块明确登记进入、离开，不修改身体或剧情标签。</summary>
public sealed class MemeNetworkNode : MonoBehaviour
{
    private readonly HashSet<MemeUnitState> targets = new(); // 按棋子状态去重，不按碰撞体累加。
    private BattleManager owner; // 连接归属本场战斗，结束后不能残留到下一场。
    public int ConnectionCount => targets.Count;

    /// <summary>登记本场存活目标；重复登记幂等。节点重新启用或目标重建后须重新登记。</summary>
    public bool TryConnect(PieceController target)
    {
        var battle = BattleScene.Ins?.BM;
        if (!isActiveAndEnabled || target == null || target.isDead || battle == null ||
            !battle.isActiveAndEnabled || !battle.IsBattleRunning || battle.LastOutcome != BattleOutcome.None) return false;
        if (owner != null && owner != battle) DisconnectAll();
        owner = battle;
        owner.RegisterNetworkNode(this);
        targets.Add(target.MemeState);
        target.MemeState.AddNetworkSource(this);
        return true;
    }

    /// <summary>目标离开本节点时调用，只解除本节点来源；范围模块应在最后一个接触退出时调用。</summary>
    public void Disconnect(PieceController target)
    {
        if (target != null) DisconnectState(target.MemeState);
    }

    internal void DisconnectState(MemeUnitState state)
    {
        targets.Remove(state);
        state.RemoveNetworkSource(this);
        if (targets.Count == 0)
        {
            if (owner != null) owner.UnregisterNetworkNode(this);
            owner = null;
        }
    }

    /// <summary>节点失效、拆除或重建时解除全部目标；其他节点来源继续有效。</summary>
    public void DisconnectAll()
    {
        foreach (var state in new List<MemeUnitState>(targets)) DisconnectState(state);
    }

    private void OnDisable() => DisconnectAll();
    private void OnDestroy() => DisconnectAll();
}
