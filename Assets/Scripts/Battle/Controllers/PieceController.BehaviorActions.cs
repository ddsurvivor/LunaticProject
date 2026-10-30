using System.Collections.Generic;
using UnityEngine;

public partial class PieceController
{
    /// <summary>AI以正常攻击费用帮助原友军解除暴力/逆转；仅单体、预计非致死且有实际伤害的攻击可选。</summary>
    internal BattleCommandResult TryBehaviorRecoveryHit(PieceController target)
    {
        if (target == null || target == this || target.isPlayerPiece != isPlayerPiece || !BattleRelations.CanSupport(this, target) ||
            !BattleRelations.CanInitiateAttack(this) || !(target.MemeState.Violent || target.MemeState.Inverted)) return BattleCommandResult.Rejected;
        var battle = BattleScene.Ins.BM;
        foreach (bool ranged in new[] { false, true })
        {
            var normal = ranged ? pieceData.RangedAttack : pieceData.MeleeAttack;
            if (normal == null || normal.target != SkillTarget.Enemy || normal.rangeType != RangeType.Circle ||
                ranged && unitAttrCenter.AmmoCount <= 0 || !BattleRangeRules.CanReach(this, normal, transform.position, target.transform.position)) continue;
            var skill = normal.WithTarget(SkillTarget.Ally);
            if (!SkillTargeting.Query(this, skill, target.transform.position).Contains(target)) continue;
            var preview = battle.damageManager.PreviewSkill(this, target, skill);
            if (preview.Total.MinDamage <= 0 || preview.Total.MaxDamage >= target.unitAttrCenter.CurHealth) continue;
            int ap = unitAttrCenter.CurMovePoint;
            var action = ranged ? ActionType.RangedAttack : ActionType.MeleeAttack;
            if (!unitAttrCenter.CostMP(action)) return unitAttrCenter.CurMovePoint != ap || isDead ? BattleCommandResult.Interrupted : BattleCommandResult.Rejected;
            if (!BattleRelations.CanSupport(this, target)) return BattleCommandResult.Interrupted;
            if (ranged) unitAttrCenter.CostAmmo();
            BeginCombatAction(skill, new List<PieceController> { target }, target.transform.position, action,
                ranged ? PieceDisplayState.Shoot : PieceDisplayState.Attack);
            return BattleCommandResult.Executed;
        }
        return BattleCommandResult.Rejected;
    }
}
