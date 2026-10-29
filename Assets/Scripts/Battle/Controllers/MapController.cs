using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>为敌人选择连接当前层与目标层、两端均可达的楼梯。</summary>
public class MapController : MonoBehaviour
{
    public List<GameObject> groundList = new();
    public List<LadderArea> ladderAreas = new();
    [Min(0f)] public float ladderApproachWeight = 1f; // 入口路程在选梯代价中的权重。
    [Min(0f)] public float ladderRemainingWeight = 1f; // 出口至目标路程在选梯代价中的权重。
    [Min(0f)] public float ladderHeightWeight = 1f; // 攀爬高度在选梯代价中的权重。
    private const float FloorTolerance = 0.75f; // 小于现有一米层级判定，防止吸附到相邻层。
    public const float LadderReach = 2f; // AI 可发起攀爬的最大入口路程。

    /// <summary>按完整路径代价选择楼梯；无有效路线返回false，不修改Agent或消耗AP。相同代价按列表顺序选择。</summary>
    public bool TryGetLadder(NavMeshAgent agent, Vector3 target, out LadderArea ladder,
        out Vector3 entrance, out float approachLength)
    {
        ladder = null;
        entrance = default;
        approachLength = 0f;
        if (!CanQuery(agent) || ladderAreas == null) return false;
        var filter = CreateFilter(agent);
        float bestCost = float.PositiveInfinity;
        foreach (var candidate in ladderAreas)
        {
            if (!TryGetEnds(candidate, agent.transform.position, target, out var entry, out var exit) ||
                !TryPath(agent.transform.position, entry, filter, out var reachableEntry, out float approach) ||
                !TryPath(exit, target, filter, out _, out float remaining)) continue;
            float cost = approach * Mathf.Max(0f, ladderApproachWeight) +
                remaining * Mathf.Max(0f, ladderRemainingWeight) +
                Mathf.Abs(exit.y - entry.y) * Mathf.Max(0f, ladderHeightWeight);
            if (cost >= bestCost) continue;
            bestCost = cost;
            ladder = candidate;
            entrance = reachableEntry;
            approachLength = approach;
        }
        return ladder != null;
    }

    /// <summary>攀爬提交前复核入口和保留水平偏移后的落点；入口隔墙、落点失效或无法到达目标时返回false。</summary>
    public bool TryGetLadderLanding(NavMeshAgent agent, LadderArea ladder, Vector3 target, out Vector3 landing)
    {
        landing = default;
        if (!CanQuery(agent) || !TryGetEnds(ladder, agent.transform.position, target, out var entry, out var exit)) return false;
        var filter = CreateFilter(agent);
        if (!TryPath(agent.transform.position, entry, filter, out var reachableEntry, out float approach) ||
            approach > LadderReach || NavMesh.Raycast(agent.transform.position, reachableEntry, out _, filter)) return false;
        Vector3 offset = agent.transform.position - entry;
        offset.y = 0f;
        Vector3 desired = exit + offset;
        // 落点只允许很小的吸附，不能越过出口附近的墙或跳到另一片平台。
        if (!NavMesh.SamplePosition(desired, out var hit, 0.1f, filter) ||
            Vector3.Distance(desired, hit.position) > 0.1f ||
            !TryPath(hit.position, target, filter, out _, out _)) return false;
        landing = hit.position;
        return true;
    }

    private static bool CanQuery(NavMeshAgent agent) =>
        agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    private static NavMeshQueryFilter CreateFilter(NavMeshAgent agent)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        for (int i = 0; i < 32; i++) filter.SetAreaCost(i, agent.GetAreaCost(i));
        return filter;
    }

    private static bool TryGetEnds(LadderArea ladder, Vector3 origin, Vector3 target, out Vector3 entry, out Vector3 exit)
    {
        entry = exit = default;
        if (ladder == null || !ladder.isActiveAndEnabled || !ladder.ableToTrigger ||
            ladder.upPos == null || ladder.downPos == null || Mathf.Abs(origin.y - target.y) <= 1f) return false;
        bool fromUpper = Mathf.Abs(origin.y - ladder.upPos.position.y) < 1f;
        entry = fromUpper ? ladder.upPos.position : ladder.downPos.position;
        exit = fromUpper ? ladder.downPos.position : ladder.upPos.position;
        return Mathf.Abs(origin.y - entry.y) < 1f && Mathf.Abs(target.y - exit.y) < 1f;
    }

    private static bool TryPath(Vector3 from, Vector3 to, NavMeshQueryFilter filter, out Vector3 end, out float length)
    {
        end = default;
        length = 0f;
        if (!NavMesh.SamplePosition(from, out var start, FloorTolerance, filter) ||
            Mathf.Abs(start.position.y - from.y) > FloorTolerance ||
            !NavMesh.SamplePosition(to, out var finish, FloorTolerance, filter) ||
            Mathf.Abs(finish.position.y - to.y) > FloorTolerance) return false;
        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(start.position, finish.position, filter, path) ||
            path.status != NavMeshPathStatus.PathComplete || path.corners.Length == 0) return false;
        // 同点的一角路径合法；其余路径必须确实连接两端，不能把空路径当成可达。
        if (Vector3.Distance(path.corners[0], start.position) > 0.1f ||
            Vector3.Distance(path.corners[path.corners.Length - 1], finish.position) > 0.1f) return false;
        for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        end = finish.position;
        return true;
    }
}
