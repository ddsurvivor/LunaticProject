using System.Collections.Generic;
using UnityEngine;

/// <summary>技能范围查询，不修改高亮或保留上一次查询结果。</summary>
public class SkillManager : MonoBehaviour
{
    public PieceController casterPc;

    public List<PieceController> GetTargets(PieceController caster, Transform target, SkillPack skill)
    {
        casterPc = caster;
        if (caster == null || target == null || skill == null) return new List<PieceController>();
        if (skill.target == SkillTarget.Self)
            return SkillTargeting.Filter(caster, new[] { caster }, skill, caster.transform.position);
        var candidates = new List<PieceController>();
        Vector3 origin = caster.transform.position;
        Vector3 center = target.position;
        float radius;
        switch (skill.rangeType)
        {
            case RangeType.Circle: radius = 1f; break;
            case RangeType.Grenade: radius = skill.explodeRadius; break;
            case RangeType.Fan: center = origin; radius = skill.rangeValue; break;
            case RangeType.Nova:
                center = origin;
                radius = skill.explodeRadius > 0 ? skill.explodeRadius : skill.rangeValue;
                break;
            default: return candidates;
        }
        Vector3 forward = target.position - origin;
        forward.y = 0;
        foreach (var collider in Physics.OverlapSphere(center, Mathf.Max(0f, radius)))
        {
            var piece = collider.GetComponentInParent<PieceController>();
            if (piece == null) continue;
            if (skill.rangeType == RangeType.Fan)
            {
                Vector3 dir = piece.transform.position - origin;
                dir.y = 0;
                if (dir.sqrMagnitude < 1f || dir.sqrMagnitude > radius * radius ||
                    Vector3.Angle(forward, dir) > skill.rangeAgle / 2f) continue;
            }
            candidates.Add(piece);
        }
        return SkillTargeting.Filter(caster, candidates, skill, target.position);
    }
}

/// <summary>玩家预览、AI 查询和最终结算共享的目标规则；几何范围由调用方提供。</summary>
public static class SkillTargeting
{
    public static bool IsValid(PieceController caster, PieceController target, SkillPack skill)
    {
        if (caster == null || caster.isDead || target == null || skill == null ||
            !target.gameObject.activeInHierarchy || !BuffManager.CanTarget(caster, target)) return false;
        if (skill.layerSkill && Mathf.Abs(caster.transform.position.y - target.transform.position.y) > 0.1f) return false;
        bool ally = caster.isPlayerPiece == target.isPlayerPiece;
        if (skill.target == SkillTarget.AllyBody) return ally && target.isDead;
        if (target.isDead) return false;
        switch (skill.target)
        {
            case SkillTarget.Enemy:
            case SkillTarget.EnemyAll:
            case SkillTarget.FarthestEnemy: return !ally;
            case SkillTarget.Ally: return ally;
            case SkillTarget.Self: return caster == target;
            case SkillTarget.All: return true;
            // Area 只提供地面位置，效果由 ApplySkillEffectOnce 负责。
            default: return false;
        }
    }

    public static List<PieceController> Filter(PieceController caster, IEnumerable<PieceController> candidates,
        SkillPack skill, Vector3 selectionPosition)
    {
        var result = new List<PieceController>();
        if (caster == null || skill == null) return result;
        if (skill.target == SkillTarget.Self)
        {
            if (IsValid(caster, caster, skill)) result.Add(caster);
            return result;
        }
        if (candidates == null) return result;
        var seen = new HashSet<PieceController>();
        foreach (var candidate in candidates)
            if (IsValid(caster, candidate, skill) && seen.Add(candidate)) result.Add(candidate);
        bool farthest = skill.target == SkillTarget.FarthestEnemy;
        bool single = skill.target is SkillTarget.Enemy or SkillTarget.Ally or SkillTarget.AllyBody || farthest;
        if (single && result.Count > 1)
        {
            Vector3 origin = farthest ? caster.transform.position : selectionPosition;
            result.Sort((a, b) =>
            {
                int order = (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude);
                if (farthest) order = -order;
                return order != 0 ? order : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            result.RemoveRange(1, result.Count - 1);
        }
        return result;
    }
}