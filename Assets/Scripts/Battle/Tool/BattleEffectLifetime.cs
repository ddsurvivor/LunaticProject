using UnityEngine;
using UnityEngine.AI;

/// <summary>延迟效果的对象池租期；隐藏或回收后，旧回调不能作用于重新启用的同一对象。</summary>
public sealed class BattleEffectLifetime : MonoBehaviour
{
    public int Version { get; private set; }
    private NavMeshAgent pendingAgent;
    private bool pendingStopped;
    internal void RestoreAgent(NavMeshAgent agent, bool stopped)
    {
        pendingAgent = agent;
        pendingStopped = stopped;
        RestorePendingAgent();
    }
    private void RestorePendingAgent()
    {
        if (pendingAgent == null || !pendingAgent.enabled || !pendingAgent.isOnNavMesh) return;
        pendingAgent.isStopped = pendingStopped;
        pendingAgent = null;
    }
    private void Update() => RestorePendingAgent();
    private void OnDisable() => Version++;
}
