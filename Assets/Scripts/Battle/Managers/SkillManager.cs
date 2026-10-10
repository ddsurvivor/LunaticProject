using System.Collections.Generic;
using UnityEngine;

/// <summary>技能范围查询，不修改高亮或保留上一次查询结果。</summary>
public class SkillManager : MonoBehaviour
{
    public PieceController casterPc;
    [SerializeField] private RangeUI rangeUIPrefab;

    /// <summary>旧敌人预制体没有 RangeUI 时，复用场上玩家的范围组件作为模板。</summary>
    public RangeUI EnsureRangeUI(PieceController caster)
    {
        if (caster == null) return null;
        if (caster.rangeUI == null)
        {
            RangeUI template = rangeUIPrefab;
            var players = BattleScene.Ins?.BM?.PlayerController?.pieces;
            if (template == null && players != null)
                foreach (var player in players)
                    if (player != null && player.rangeUI != null) { template = player.rangeUI; break; }
            if (template == null)
            {
                Debug.LogWarning($"{caster.name} 缺少范围 UI 模板，请配置 SkillManager.rangeUIPrefab。", caster);
                return null;
            }
            caster.rangeUI = Instantiate(template, caster.transform);
            caster.rangeUI.transform.localPosition = template.transform.localPosition;
            caster.rangeUI.transform.localRotation = template.transform.localRotation;
            caster.rangeUI.gameObject.SetActive(true);
            caster.rangeUI.CloseRange();
        }
        caster.rangeUI.Bind(caster);
        return caster.rangeUI;
    }

    public List<PieceController> GetTargets(PieceController caster, Transform target, SkillPack skill)
    {
        casterPc = caster;
        return target == null ? new List<PieceController>() : GetTargets(caster, target.position, skill);
    }

    public List<PieceController> GetTargets(PieceController caster, Vector3 targetPosition, SkillPack skill)
    {
        casterPc = caster;
        return SkillTargeting.Query(caster, skill, targetPosition);
    }
}

/// <summary>玩家预览、AI 查询和最终结算共享的范围和目标规则。</summary>
public static class SkillTargeting
{
    public const float SingleTargetRadius = 1f;

    /// <summary>输入世界坐标即可查询，不依赖 RangeUI 或鼠标输入。</summary>
    public static List<PieceController> Query(PieceController caster, SkillPack skill, Vector3 targetPosition)
    {
        if (caster == null || skill == null) return new List<PieceController>();
        if (skill.target == SkillTarget.Self)
            return SkillTargeting.Filter(caster, new[] { caster }, skill, caster.transform.position);
        var candidates = new List<PieceController>();
        Vector3 origin = caster.transform.position;
        Vector3 selection = ClampPosition(origin, targetPosition, skill.rangeValue);
        Vector3 center = selection;
        Vector3 forward = targetPosition - origin;
        forward.y = 0;
        if (forward.sqrMagnitude < 0.0001f) forward = caster.transform.forward;
        forward.Normalize();
        float arcRadius = 0f;
        float radius;
        switch (skill.rangeType)
        {
            case RangeType.Circle: radius = SingleTargetRadius; break;
            case RangeType.Grenade: radius = skill.explodeRadius; break;
            case RangeType.Fan: center = origin; radius = skill.rangeValue; break;
            case RangeType.Nova:
                center = origin;
                radius = skill.explodeRadius > 0 ? skill.explodeRadius : skill.rangeValue;
                break;
            case RangeType.Arc:
                arcRadius = GetArcRadius(skill);
                center = GetArcCenter(origin, forward, skill);
                radius = arcRadius + Mathf.Max(0f, skill.arcWeight) / 2f;
                break;
            default: return candidates;
        }
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
            else if (skill.rangeType == RangeType.Arc)
            {
                Vector3 dir = piece.transform.position - center;
                dir.y = 0;
                float innerRadius = Mathf.Max(0f, arcRadius - skill.arcWeight / 2f);
                float halfAngle = GetArcHalfAngle(skill);
                Vector3 arcForward = Vector3.Cross(Vector3.up, forward);
                if (dir.sqrMagnitude < innerRadius * innerRadius || dir.sqrMagnitude > radius * radius ||
                    Vector3.Angle(arcForward, dir) > halfAngle) continue;
            }
            candidates.Add(piece);
        }
        return Filter(caster, candidates, skill, selection);
    }

    public static Vector3 ClampPosition(Vector3 origin, Vector3 position, float range)
    {
        Vector3 direction = position - origin;
        direction.y = 0;
        return origin + Vector3.ClampMagnitude(direction, Mathf.Max(0f, range));
    }

    public static float GetArcRadius(SkillPack skill) =>
        Mathf.Sqrt(skill.arcCenterDis * skill.arcCenterDis + skill.rangeValue * skill.rangeValue / 4f);

    public static float GetArcHalfAngle(SkillPack skill)
    {
        float radius = GetArcRadius(skill);
        return radius > 0f ? Mathf.Asin(Mathf.Clamp01(skill.rangeValue / (2f * radius))) * Mathf.Rad2Deg : 0f;
    }

    public static Vector3 GetArcCenter(Vector3 origin, Vector3 forward, SkillPack skill) =>
        origin - Vector3.Cross(Vector3.up, forward.normalized) * skill.arcCenterDis +
        forward.normalized * skill.rangeValue / 2f;

    /// <summary>按阵营选择存活队友；includeSelf 控制是否包含施法者。</summary>
    public static List<PieceController> SelectAllies(PieceController caster,
        IEnumerable<PieceController> candidates, bool includeSelf = true)
    {
        var result = new List<PieceController>();
        var seen = new HashSet<PieceController>();
        if (caster == null || candidates == null) return result;
        foreach (var candidate in candidates)
            if (IsValid(caster, candidate, SkillTarget.Ally) && (includeSelf || candidate != caster) && seen.Add(candidate))
                result.Add(candidate);
        return result;
    }

    public static bool IsValid(PieceController caster, PieceController target, SkillPack skill)
    {
        return skill != null && IsValid(caster, target, skill.target) &&
            (!skill.layerSkill || Mathf.Abs(caster.transform.position.y - target.transform.position.y) <= 0.1f);
    }

    public static bool IsValid(PieceController caster, PieceController target, SkillTarget targetType)
    {
        if (caster == null || caster.isDead || target == null ||
            !target.gameObject.activeInHierarchy || !BuffManager.CanTarget(caster, target)) return false;
        bool ally = caster.isPlayerPiece == target.isPlayerPiece;
        if (targetType == SkillTarget.AllyBody) return ally && target.isDead;
        if (target.isDead) return false;
        switch (targetType)
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
