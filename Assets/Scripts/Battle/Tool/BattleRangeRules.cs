using UnityEngine;

/// <summary>施放距离与层高共用规则；不扩大范围形状或改变护甲、掩体及阵营规则。</summary>
public static class BattleRangeRules
{
    public const float SameLayerTolerance = 0.1f; // 沿用既有 layerSkill 同层判定。

    /// <summary>显式远近程标记优先；旧技能按普攻引用、敌人用途和相对近战射程兼容识别。</summary>
    public static bool IsRanged(PieceController caster, SkillPack skill)
    {
        if (caster == null || skill == null || !BattleRelations.IsOffensive(skill) ||
            skill.target is SkillTarget.Self or SkillTarget.Ally or SkillTarget.AllyBody) return false;
        if (skill.reachKind != SkillReachKind.Auto) return skill.reachKind == SkillReachKind.Ranged;
        if (SameSkill(skill, caster.pieceData?.MeleeAttack)) return false;
        if (SameSkill(skill, caster.pieceData?.RangedAttack)) return true;
        if (skill.enemySkillRole != EnemySkillRole.Auto) return skill.enemySkillRole == EnemySkillRole.RangedAttack;
        return skill.rangeValue > (caster.pieceData?.MeleeAttack?.rangeValue ?? 0f);
    }

    private static bool SameSkill(SkillPack a, SkillPack b) => b != null &&
        (ReferenceEquals(a, b) || !string.IsNullOrEmpty(a.skillId) && a.skillId == b.skillId);

    /// <summary>战斗施放距离按水平面测量，高度另由高打低和 layerSkill 规则处理。</summary>
    public static float Distance(Vector3 a, Vector3 b)
    {
        Vector3 delta = b - a;
        delta.y = 0f;
        return delta.magnitude;
    }

    /// <summary>仅高打低的远程定点施放距离乘三；自身范围、扇形和弧形的尺寸保持原配置。</summary>
    public static float CastRange(PieceController caster, SkillPack skill, Vector3 targetPosition,
        Vector3? originOverride = null)
    {
        if (skill == null) return 0f;
        float range = Mathf.Max(0f, skill.rangeValue);
        if (caster == null || skill.layerSkill || !IsRanged(caster, skill) ||
            skill.rangeType != RangeType.Circle && skill.rangeType != RangeType.Grenade) return range;
        var origin = originOverride ?? caster.transform.position;
        return origin.y - targetPosition.y + 0.00001f >= GM.Ins.DM.gameConstSO.HighGroundHeightThreshold
            ? range * 3f : range;
    }

    /// <summary>按共享水平施放距离和 layerSkill 检查目标点；AOE 的受击者仍由正式形状查询决定。</summary>
    public static bool CanReach(PieceController caster, SkillPack skill, Vector3 origin, Vector3 target)
    {
        if (caster == null || skill == null ||
            skill.layerSkill && Mathf.Abs(origin.y - target.y) > SameLayerTolerance) return false;
        Vector3 direction = target - origin;
        direction.y = 0f;
        float range = CastRange(caster, skill, target, origin);
        return direction.sqrMagnitude <= range * range + 0.00001f &&
            (skill.rangeType != RangeType.Fan || direction.sqrMagnitude >= 1f);
    }

    /// <summary>保留可跨层技能的目标高度，只限制水平施放距离；支援与近战不获得高打低加成。</summary>
    public static Vector3 ClampPosition(PieceController caster, SkillPack skill, Vector3 position)
    {
        var origin = caster.transform.position;
        var result = SkillTargeting.ClampPosition(origin, position, CastRange(caster, skill, position));
        if (!skill.layerSkill) result.y = position.y;
        return result;
    }
}
