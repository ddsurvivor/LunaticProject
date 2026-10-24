using System.Collections.Generic;
using UnityEngine;

/// <summary>策划行为模式的只读判定；不消费资源、不掷骰，也不修改棋子位置。</summary>
public static class EnemyBehaviorRules
{
    public static bool IsDesignMode(EnemyAIType action) => action >= EnemyAIType.NearestAdaptive && action <= EnemyAIType.WoundedRetreat;
    public static bool IsMeleeOnly(EnemyAIType action) => action == EnemyAIType.DamageHunterMeleeOnly || action == EnemyAIType.FragileMeleeOnly;
    public static bool IsShooter(EnemyAIType action) => action == EnemyAIType.Shoot || action == EnemyAIType.WeakestDefenseShooter;

    /// <summary>模式内置索敌规则优先于基础索敌配置；旧模式仍使用独立索敌策略。</summary>
    public static EnemyTargetStrategy TargetStrategy(EnemyController enemy, EnemyStrategy strategy)
    {
        switch (strategy.Action)
        {
            case EnemyAIType.NearestAdaptive: return EnemyTargetStrategy.Nearest;
            case EnemyAIType.WeakestDefenseShooter: return EnemyTargetStrategy.LowestDefense;
            case EnemyAIType.DamageHunterAOE:
            case EnemyAIType.DamageHunterRangedSkill:
            case EnemyAIType.DamageHunterMeleeSkill:
            case EnemyAIType.DamageHunterMeleeOnly: return EnemyTargetStrategy.HighestDamage;
            case EnemyAIType.FragileMeleeOnly: return enemy.fragilePriority == EnemyFragilePriority.Defense
                ? EnemyTargetStrategy.LowestDefense : EnemyTargetStrategy.LowestMaxHealth;
            default: return strategy.Target;
        }
    }

    /// <summary>普通攻击的远近分界为远程射程的一半；模式1无弹药时强制接近近战。</summary>
    public static bool PreferRanged(EnemyController enemy, EnemyAIType action, float distance)
    {
        float ranged = enemy.pieceData?.RangedAttack?.rangeValue ?? 0f;
        if (ranged <= 0f || IsMeleeOnly(action)) return false;
        if (action == EnemyAIType.NearestAdaptive)
            return enemy.unitAttrCenter.AmmoCount > 0 && distance > ranged * 0.5f && distance <= ranged;
        float farthest = MaximumRange(enemy);
        return distance > farthest * 0.5f;
    }

    /// <summary>返回普攻和攻击技能的最远射程，不把回复技能计入远近分界。</summary>
    public static float MaximumRange(EnemyController enemy)
    {
        float range = Mathf.Max(enemy.pieceData?.MeleeAttack?.rangeValue ?? 0f, enemy.pieceData?.RangedAttack?.rangeValue ?? 0f);
        if (enemy.availableSkills != null)
            foreach (var skill in enemy.availableSkills)
                if (IsOffensive(skill)) range = Mathf.Max(range, skill.rangeValue);
        return range;
    }

    /// <summary>判断可参与进攻规划的目标类型，排除显式或自动识别的回复技能。</summary>
    public static bool IsOffensive(SkillPack skill) => skill != null && !IsHealingSkill(skill) &&
        skill.rangeValue > 0f && (skill.target == SkillTarget.Enemy || skill.target == SkillTarget.EnemyAll ||
        skill.target == SkillTarget.All || skill.target == SkillTarget.FarthestEnemy);

    private static bool IsHealingSkill(SkillPack skill)
    {
        if (skill.enemySkillRole != EnemySkillRole.Auto) return skill.enemySkillRole == EnemySkillRole.Healing;
        if (skill.skillEffects != null && skill.skillEffects.Contains(SkillEffect.HealArea)) return true;
        if (skill.buffPacks != null)
            foreach (var buff in skill.buffPacks)
                if (buff != null && buff.buffType == BuffType.AutoHeal) return true;
        return false;
    }

    /// <summary>未显式标注用途的攻击技能按相对近战射程识别；回复区域和自动治疗可自动识别。</summary>
    public static EnemySkillRole SkillRole(EnemyController enemy, SkillPack skill)
    {
        if (skill == null) return EnemySkillRole.Auto;
        if (skill.enemySkillRole != EnemySkillRole.Auto) return skill.enemySkillRole;
        if (IsHealingSkill(skill)) return EnemySkillRole.Healing;
        return skill.rangeValue <= (enemy.pieceData?.MeleeAttack?.rangeValue ?? 0f)
            ? EnemySkillRole.MeleeAttack : EnemySkillRole.RangedAttack;
    }

    /// <summary>按当前生命比例严格比较阈值，死亡单位和恰好等于阈值的单位不触发。</summary>
    public static bool BelowHealth(PieceController piece, float ratio) => piece != null && !piece.isDead &&
        piece.unitAttrCenter.MaxHealth > 0 && piece.unitAttrCenter.CurHealth / (float)piece.unitAttrCenter.MaxHealth < ratio;

    /// <summary>通用残血撤离遵守独立开关和 Boss 豁免；特殊关卡行为由调用方保留。</summary>
    public static bool ShouldFlee(EnemyController enemy) => !enemy.isBoss && enemy.retreatWhenLowHealth && BelowHealth(enemy, enemy.retreatHealthRatio);

    public static float HealthValue(EnemyController enemy, PieceController target) => enemy.healthPriority == EnemyHealthPriority.MaximumHealth
        ? target.unitAttrCenter.MaxHealth : target.unitAttrCenter.CurHealth;

    /// <summary>仅在两个以上可立即普攻的目标之间比较血量，不以尚需移动的目标替换当前机会。</summary>
    public static PieceController ChooseInRangeWeakTarget(EnemyController enemy, IEnumerable<PieceController> candidates,
        PieceController nearest, PieceController previewPiece = null, Vector3? previewPosition = null)
    {
        PieceController weakest = null;
        int count = 0;
        foreach (var target in candidates)
        {
            if (!SkillTargeting.IsValid(enemy, target, SkillTarget.Enemy)) continue;
            Vector3 position = target == previewPiece && previewPosition.HasValue ? previewPosition.Value : target.transform.position;
            float distance = Vector3.Distance(enemy.transform.position, position);
            bool ranged = PreferRanged(enemy, EnemyAIType.NearestAdaptive, distance);
            var attack = ranged ? enemy.pieceData?.RangedAttack : enemy.pieceData?.MeleeAttack;
            if (attack == null || distance > attack.rangeValue || attack.layerSkill && Mathf.Abs(position.y - enemy.transform.position.y) > 0.1f) continue;
            count++;
            if (weakest == null || HealthValue(enemy, target) < HealthValue(enemy, weakest)) weakest = target;
        }
        return count > 1 ? weakest : nearest;
    }
}
