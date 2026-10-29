using UnityEngine;

public partial class AIController
{
    /// <summary>当前可执行的环境攻击机会，不为引爆绕路；爆炸命中人数优先，保留行动类型偏好。</summary>
    private bool TryCreateExplosivePlan(EnemyController piece, out AttackPlan plan)
    {
        plan = null;
        if (piece == null || !piece.attackExplosives || piece.pieceData == null ||
            piece.IsPerformingAction || piece.ResolveStrategy().Action == EnemyAIType.Special) return false;
        var strategy = piece.ResolveStrategy();
        AttackPlan best = null;
        int bestCount = 0;
        float bestDistance = float.PositiveInfinity;
        foreach (var explosive in EnemyExplosiveTargets.Active)
        {
            if (explosive == null || !explosive.CanBeTriggered || explosive.Piece == null) continue;
            int count = explosive.CountThreatenedPlayers();
            if (count <= 0) continue;
            float distance = Vector3.Distance(piece.transform.position, explosive.Piece.transform.position);
            if (count < bestCount || count == bestCount && distance >= bestDistance) continue;
            AttackPlan candidate = null;
            void Consider(SkillPack skill, ActionType action)
            {
                if (candidate != null || !CanAfford(piece, action)) return;
                if (action == ActionType.Skill && (skill == null || !piece.unitAttrCenter.HasMana(skill.mpCost))) return;
                if (action == ActionType.RangedAttack && piece.unitAttrCenter.AmmoCount <= 0 &&
                    (piece.unitAttrCenter.MaxAmmoCount <= 0 || piece.unitAttrCenter.CurMovePoint <
                        ActionCost(action) + ActionCost(ActionType.Reload))) return;
                if (!EnemyExplosiveTargets.CanAttack(piece, explosive, skill)) return;
                candidate = new AttackPlan { Target = explosive.Piece, Skill = skill, Action = action,
                    Strategy = strategy, Explosive = explosive };
            }
            if ((strategy.Action == EnemyAIType.SkillUser || strategy.Action == EnemyAIType.HealingSupport) && piece.availableSkills != null)
                foreach (var skill in piece.availableSkills)
                    if (IsOffensiveSkill(skill)) Consider(skill, ActionType.Skill);
            bool preferRanged = EnemyBehaviorRules.PreferRanged(piece, strategy.Action, distance);
            if (!EnemyBehaviorRules.IsShooter(strategy.Action) &&
                (strategy.Action != EnemyAIType.NearestAdaptive || !preferRanged)) Consider(piece.pieceData.MeleeAttack, ActionType.MeleeAttack);
            if (!EnemyBehaviorRules.IsMeleeOnly(strategy.Action) &&
                (strategy.Action != EnemyAIType.NearestAdaptive || preferRanged)) Consider(piece.pieceData.RangedAttack, ActionType.RangedAttack);
            if (candidate == null) continue;
            best = candidate;
            bestCount = count;
            bestDistance = distance;
        }
        plan = best;
        return plan != null;
    }
}
