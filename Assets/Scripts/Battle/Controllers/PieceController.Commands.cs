using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

public enum PlayerBattleCommandKind
{
    [LabelText("移动")] Move,
    [LabelText("近战攻击")] Melee,
    [LabelText("远程攻击")] Ranged,
    [LabelText("施放技能")] Skill,
    [LabelText("装填")] Reload,
    [LabelText("攀爬")] Climb,
    [LabelText("结束行动")] EndTurn
}
public enum BattleCommandResult { Rejected, Executed, Interrupted }

public struct PlayerBattleCommand
{
    public PlayerBattleCommandKind kind;
    public Vector3 position;
    public SkillPack skill;
    public LadderArea ladder;
    public bool showRecognition;
}

public partial class PieceController
{
    /// <summary>供战斗管理器转发已授权命令；实际费用和动画共用玩家操作的正式流程。</summary>
    internal BattleCommandResult ExecutePlayerCommand(PlayerBattleCommand command)
    {
        var battle = BattleScene.Ins.BM;
        ActionType requested = command.kind == PlayerBattleCommandKind.Move ? ActionType.Move : command.kind == PlayerBattleCommandKind.Melee ? ActionType.MeleeAttack :
            command.kind == PlayerBattleCommandKind.Ranged ? ActionType.RangedAttack : command.kind == PlayerBattleCommandKind.Skill ? ActionType.Skill :
            command.kind == PlayerBattleCommandKind.Reload ? ActionType.Reload : command.kind == PlayerBattleCommandKind.Climb ? ActionType.Climb : ActionType.Wait;
        bool unlocked = availableActions != null && (availableActions.Contains(requested) ||
            command.kind == PlayerBattleCommandKind.Reload && availableActions.Contains(ActionType.RangedAttack) ||
            command.kind == PlayerBattleCommandKind.Climb && availableActions.Contains(ActionType.Move));
        if (command.kind != PlayerBattleCommandKind.EndTurn && !unlocked) return BattleCommandResult.Rejected;
        if (float.IsNaN(command.position.x) || float.IsInfinity(command.position.x) || float.IsNaN(command.position.y) || float.IsInfinity(command.position.y) ||
            float.IsNaN(command.position.z) || float.IsInfinity(command.position.z)) return BattleCommandResult.Rejected;
        int beforeAP = unitAttrCenter.CurMovePoint;
        BattleCommandResult PaidFailure() => unitAttrCenter.CurMovePoint != beforeAP || isDead
            ? BattleCommandResult.Interrupted : BattleCommandResult.Rejected;
        if (command.kind == PlayerBattleCommandKind.EndTurn)
        {
            battle.ClearMoveUndo();
            unitAttrCenter.SetMovePoint(0);
            isIdle = true;
            if (!battle.IsBurstActor(this)) battle.ChangeTurn();
            return BattleCommandResult.Executed;
        }
        if (command.kind == PlayerBattleCommandKind.Reload)
        {
            if (unitAttrCenter.MaxAmmoCount <= 0 || unitAttrCenter.AmmoCount >= unitAttrCenter.MaxAmmoCount) return BattleCommandResult.Rejected;
            if (!unitAttrCenter.CostMP(ActionType.Reload)) return PaidFailure();
            ReloadAmmo();
            return BattleCommandResult.Executed;
        }
        if (command.kind == PlayerBattleCommandKind.Climb)
        {
            var ladder = command.ladder;
            if (ladder == null || !ladder.gameObject.activeInHierarchy || ladder.upPos == null || ladder.downPos == null ||
                Mathf.Min(Vector3.Distance(transform.position, ladder.upPos.position), Vector3.Distance(transform.position, ladder.downPos.position)) > 2f)
                return BattleCommandResult.Rejected;
            if (!unitAttrCenter.CostMP(ActionType.Climb)) return PaidFailure();
            ladder.TriggerAction(this);
            return BattleCommandResult.Executed;
        }
        if (command.kind == PlayerBattleCommandKind.Move)
        {
            if (!battle.moveManager.TryGetAIMoveDestination(gameObject, command.position, unitAttrCenter.MoveRange,
                out var destination, out _, true) || Vector3.Distance(transform.position, destination) < 0.2f) return BattleCommandResult.Rejected;
            float radius = GetComponent<NavMeshAgent>()?.radius ?? 0.5f;
            foreach (var other in battle.SpeedParticipants())
            {
                if (other == this || Mathf.Abs(other.transform.position.y - destination.y) > 1f) continue;
                float clearance = radius + (other.GetComponent<NavMeshAgent>()?.radius ?? 0.5f);
                if (Vector3.Distance(other.transform.position, destination) < clearance) return BattleCommandResult.Rejected;
            }
            if (!unitAttrCenter.CostMP(ActionType.Move)) return PaidFailure();
            StartDrag();
            CheckFace(destination - transform.position);
            StartMove();
            if (battle.moveManager.ExecuteMove(gameObject, StopMove, destination) <= 0f) { StopMove(); return BattleCommandResult.Interrupted; }
            return BattleCommandResult.Executed;
        }
        bool special = command.kind == PlayerBattleCommandKind.Skill;
        bool ranged = command.kind == PlayerBattleCommandKind.Ranged;
        if (!special && !ranged && command.kind != PlayerBattleCommandKind.Melee) return BattleCommandResult.Rejected;
        var skill = special ? command.skill : ranged ? pieceData?.RangedAttack : pieceData?.MeleeAttack;
        if (skill == null || BattleRelations.IsOffensive(skill) && !BattleRelations.CanInitiateAttack(this) || special && !availableSkills.Contains(skill) || ranged && unitAttrCenter.AmmoCount <= 0) return BattleCommandResult.Rejected;
        Vector3 position = BattleRangeRules.ClampPosition(this, skill, command.position);
        var targets = SkillTargeting.Query(this, skill, position);
        if (targets.Count == 0 && skill.target != SkillTarget.Area && skill.target != SkillTarget.Self) return BattleCommandResult.Rejected;
        var action = special ? ActionType.Skill : ranged ? ActionType.RangedAttack : ActionType.MeleeAttack;
        if (!(special ? unitAttrCenter.TryCostSkill(skill) : unitAttrCenter.CostMP(action))) return PaidFailure();
        // 付费自伤可能解除行为状态，提交后再次校验当前关系，避免按旧敌我关系出手。
        if (targets.Count > 0 && !targets.Exists(t => SkillTargeting.IsValid(this, t, skill))) return BattleCommandResult.Interrupted;
        if (ranged) unitAttrCenter.CostAmmo();
        _isAttacking = _isUsingSkill = isUsingOrder = false;
        rangeUI?.CloseRange();
        if (special && skill.isDelaySkill)
        {
            battle.RestoreDelaySkill(this, skill, position);
            NotifySkillUsed(skill);
            return BattleCommandResult.Executed;
        }
        bool recognition = special && skill.isRecognitionCheck && targets.Count > 0;
        CheckResult check = recognition ? battle.diceCheckManager.ModeRecognitionCheck(this, targets[0], command.showRecognition) : CheckResult.None;
        BeginCombatAction(skill, targets, position, action, special ? PieceDisplayState.Skill : ranged ? PieceDisplayState.Shoot : PieceDisplayState.Attack,
            check: check, waitForRecognition: recognition && command.showRecognition);
        if (special) NotifySkillUsed(skill);
        return BattleCommandResult.Executed;
    }
}
