using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public partial class BattleManager
{
    /// <summary>只读查询当前场景的击退落点与首次障碍；实战与预览共用，不移动棋子或改变导航代理。</summary>
    public MoveResult QueryKnockback(PieceController caster, PieceController target, float distance)
    {
        if (caster == null || !CanReact(target) || !target.ableMove) return new MoveResult(target != null ? target.transform.position : Vector3.zero);
        Vector3 origin = target.transform.position;
        Vector3 direction = target.transform.position - caster.transform.position; direction.y = 0; direction.Normalize();
        Vector3 final = origin + direction * distance;
        bool wall = NavMesh.Raycast(origin, final, out NavMeshHit hit, NavMesh.AllAreas);
        if (wall) final = hit.position;
        else if (NavMesh.SamplePosition(final, out NavMeshHit sample, 1f, NavMesh.AllAreas)) final = sample.position;
        float allowed = Vector3.Distance(origin, final);
        var result = CalculateValidMovePos(origin, direction, allowed, target.gameObject, logCollisions: false);
        if (Vector3.Distance(origin, result.FinalPosition) >= allowed) result.FinalPosition = final;
        result.FinalPosition = new Vector3(result.FinalPosition.x, origin.y, result.FinalPosition.z);
        result.IsCollided |= wall;
        return result;
    }

    /// <summary>按现行距离与稳定注册顺序查询夹击候选；可传预计落点，不消费夹击资格或触发反应。</summary>
    public PieceController FindFlankPartner(PieceController attacker, PieceController target, Vector3 targetPosition)
    {
        if (!CanReact(attacker) || !CanReact(target) || !BattleRelations.CanAttack(attacker, target)) return null;
        PieceController closest = null; float minDistance = float.MaxValue;
        foreach (var partner in SpeedParticipants().Where(p => BattleRelations.CanSupport(attacker, p)))
        {
            if (partner == attacker || !CanReact(partner) || !BattleRelations.CanAttack(partner, target) ||
                partner.pieceData.MeleeAttack == null || !partner.ableStrick) continue;
            float distance = Vector3.Distance(partner.transform.position, targetPosition);
            if (distance <= partner.pieceData.MeleeAttack.rangeValue && distance < minDistance)
            { minDistance = distance; closest = partner; }
        }
        return closest;
    }
}
