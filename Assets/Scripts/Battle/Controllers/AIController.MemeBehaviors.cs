using System.Linq;
using UnityEngine;

public partial class AIController
{
    private bool UpdateBehaviorControl(BattleManager battle)
    {
        var actor = battle.ActivePiece;
        if (!BattleRelations.IsBehaviorControlled(actor)) return false;
        if (!battle.SpeedTurnsActive || battle.BurstInputPending && !battle.IsBurstActor(actor) || battle.IsChoosingBurst || battle.HasCombatPresentation || battle.moveManager.IsMoving) return true;
        _timer += Time.deltaTime;
        if (_timer < _actionInterval) return true;
        _timer = 0f;
        if (actor is EnemyController enemy) { pendingAttacks.Remove(enemy); retreatAfterShot.Remove(enemy); }
        if (!BattleManager.IsSpeedParticipant(actor) || actor.isIdle || actor.cantControl || actor.unitAttrCenter.CurMovePoint <= 0 ||
            !TryBehaviorTurn(actor))
        {
            actor.isIdle = true;
            if (!battle.IsBurstActor(actor)) battle.ChangeTurn();
        }
        return true;
    }

    private bool TryBehaviorTurn(PieceController actor)
    {
        if ((!actor.MemeState.Violent || actor.MemeState.Inverted || actor.MemeState.AttackForbidden) && TryBehaviorRecovery(actor)) return true;
        var battle = BattleScene.Ins.BM;
        var candidates = battle.SpeedParticipants().Where(t => BattleRelations.CanAttack(actor, t))
            .OrderBy(t => actor.MemeState.Violent && !actor.MemeState.Inverted && t.isPlayerPiece != actor.isPlayerPiece ? 1 : 0)
            .ThenBy(t => (t.transform.position - actor.transform.position).sqrMagnitude).ToList();
        if (actor.MemeState.AttackForbidden) return TryBehaviorFlee(actor);
        foreach (var target in candidates)
        {
            if (TryBehaviorCast(actor, target, actor.pieceData.MeleeAttack, PlayerBattleCommandKind.Melee) ||
                TryBehaviorCast(actor, target, actor.pieceData.RangedAttack, PlayerBattleCommandKind.Ranged)) return true;
            foreach (var skill in actor.availableSkills)
                if (BattleRelations.IsOffensive(skill) && TryBehaviorCast(actor, target, skill, PlayerBattleCommandKind.Skill)) return true;
            // 暴力优先追逐可达的原友军，不为近处的其他敌人放弃优先目标。
            var offset = actor.transform.position - target.transform.position;
            offset.y = 0;
            float clearance = (actor.GetComponent<UnityEngine.AI.NavMeshAgent>()?.radius ?? .5f) +
                (target.GetComponent<UnityEngine.AI.NavMeshAgent>()?.radius ?? .5f) + .1f;
            if (TryBehaviorMove(actor, target.transform.position + offset.normalized * clearance)) return true;
        }
        return false;
    }

    private bool TryBehaviorRecovery(PieceController actor)
    {
        var battle = BattleScene.Ins.BM;
        var targets = battle.SpeedParticipants().Where(t => BattleRelations.CanSupport(actor, t)).ToList();
        foreach (var target in targets.OrderByDescending(t => battle.timedMemeStatuses.NeededCures(t) != MemeCureTag.None)
                     .ThenBy(t => t.unitAttrCenter.CurHealth / (float)t.unitAttrCenter.MaxHealth))
            foreach (var skill in actor.availableSkills)
            {
                if (skill == null || BattleRelations.IsOffensive(skill)) continue;
                bool cure = (skill.memeCureTags & battle.timedMemeStatuses.NeededCures(target)) != 0;
                bool heal = target.unitAttrCenter.CurHealth < target.unitAttrCenter.MaxHealth &&
                    (skill.enemySkillRole == EnemySkillRole.Healing || skill.skillEffects?.Contains(SkillEffect.HealArea) == true ||
                     skill.buffPacks?.Exists(p => p != null && p.buffType == BuffType.AutoHeal) == true);
                if ((cure || heal) && TryBehaviorCast(actor, target, skill, PlayerBattleCommandKind.Skill)) return true;
            }
        if (!BattleRelations.CanInitiateAttack(actor)) return false;
        foreach (var target in targets)
            if (target != actor && target.isPlayerPiece == actor.isPlayerPiece && (target.MemeState.Violent || target.MemeState.Inverted) &&
                actor.TryBehaviorRecoveryHit(target) != BattleCommandResult.Rejected) return true;
        return false;
    }

    private static bool TryBehaviorCast(PieceController actor, PieceController target, SkillPack skill, PlayerBattleCommandKind kind)
    {
        if (skill == null || target == null || kind == PlayerBattleCommandKind.Skill && !actor.SkillAvailable(skill) ||
            !SkillTargeting.IsValid(actor, target, skill) || !BattleRangeRules.CanReach(actor, skill, actor.transform.position, target.transform.position) ||
            !SkillTargeting.Query(actor, skill, target.transform.position).Contains(target)) return false;
        return actor.ExecutePlayerCommand(new PlayerBattleCommand { kind = kind, skill = skill, position = target.transform.position }) != BattleCommandResult.Rejected;
    }

    private static bool TryBehaviorMove(PieceController actor, Vector3 position) =>
        actor.ExecutePlayerCommand(new PlayerBattleCommand { kind = PlayerBattleCommandKind.Move, position = position }) != BattleCommandResult.Rejected;

    private static bool TryBehaviorFlee(PieceController actor)
    {
        var battle = BattleScene.Ins.BM;
        var threats = battle.SpeedParticipants().Where(p => p != actor && p.isPlayerPiece).ToList();
        if (threats.Count == 0) return false;
        float Safety(Vector3 p) => threats.Min(t => (p - t.transform.position).sqrMagnitude);
        Vector3 best = actor.transform.position;
        float score = Safety(best); // 仅选择确实远离玩家威胁的可达落点。
        for (int i = 0; i < PositionSamples; i++)
        {
            float angle = i * Mathf.PI * 2 / PositionSamples;
            Vector3 point = actor.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * actor.unitAttrCenter.MoveRange;
            if (!battle.moveManager.TryGetAIMoveDestination(actor.gameObject, point, actor.unitAttrCenter.MoveRange, out var reachable, out _, true)) continue;
            float next = Safety(reachable);
            if (next > score + MinimumMove) { best = reachable; score = next; }
        }
        return (best - actor.transform.position).sqrMagnitude >= MinimumMove * MinimumMove && TryBehaviorMove(actor, best);
    }
}
