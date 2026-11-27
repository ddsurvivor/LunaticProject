using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private sealed class BurstActorState
    {
        internal int AP; // 入场前的普通AP，只在退出时归还。
        internal int Life; // 快照对应生命，Init后不得写回。
        internal bool Idle; // 入场前普通回合的待机状态。
    }

    private sealed class InterruptedDelaySkill
    {
        internal PieceController Caster;
        internal int Life;
        internal SkillPack Skill;
        internal BattleActionContext Action;
        internal Vector3 Position;
        internal GameObject Effect;
    }

    private bool burstRequested; // 等待在途动作完成后统一支付并接管。
    private bool cancelBurst; // 结束请求等待在途动作安全结算。
    private bool burstUsedThisTurn; // 被插入聚能的普通回合不抵扣本次冷却。
    private int burstInterruptionVersion; // 旧协程不能恢复新战斗或新生命的状态。
    private PieceController burstActor; // 当前聚能操作者，切人不重置任何角色的额外AP。
    private PieceController requestedBurstActor; // 等待调度器安全交接，期间禁止再次提交行动。
    private InterruptedDelaySkill interruptedDelaySkill; // 普通回合的延时技能不能被聚能角色覆盖。
    private readonly Dictionary<PieceController, BurstActorState> burstActorStates = new();
    private readonly List<PieceController> selectedBurstActors = new(); // 发动时自动纳入合格主角，顺序仅用于默认切换与显示。
    /// <summary>兼容旧选人查询；聚能直接发动，不再进入预选流程。</summary>
    public bool IsChoosingBurst => false;
    /// <summary>等待聚能进入、切人或退出时禁止提交新的普通行动。</summary>
    public bool BurstInputPending => burstRequested || requestedBurstActor != null || PlayerController != null && PlayerController.isBursting && cancelBurst;
    public int SelectedBurstCount => selectedBurstActors.Count;
    /// <summary>判断当前生命是否参与聚能；初始化复用后的同一对象不继承旧资格。</summary>
    public bool IsBurstActor(PieceController piece) => PlayerController.isBursting && piece != null &&
        burstActorStates.TryGetValue(piece, out var state) && state.Life == piece.BattleLifeVersion;

    /// <summary>统一请求发动或提前结束；发动一次扣50，不预选人数，在途动作完成后接管。</summary>
    public void OnBurstButton()
    {
        if (Training != null && Training.RestrictsCommands) return;
        if (BattleScene.Ins.TM.IsPaused || !SpeedTurnsActive || !inBattle) return;
        if (PlayerController.isBursting) { cancelBurst = true; return; }
        if (burstRequested) { burstRequested = false; PlayerController.RefreshBurstUI(); return; }
        if (ActivePiece == null || !PlayerController.ableBurst || !PlayerController.pieces.Any(CanJoinBurst)) return;
        burstRequested = true;
        PlayerController.RefreshBurstUI();
    }

    private bool CanJoinBurst(PieceController piece) => IsSpeedParticipant(piece) && piece.isPlayerPiece &&
        PlayerController.pieces.Contains(piece) && !piece.cantControl && !BattleRelations.IsBehaviorControlled(piece) &&
        piece.unitAttrCenter.GetBuffStacks(BuffType.Stun) == 0;

    private bool CanActInBurst(PieceController piece) => IsBurstActor(piece) && CanJoinBurst(piece) &&
        !piece.isIdle && piece.unitAttrCenter.CurMovePoint > 0;

    /// <summary>请求切换聚能角色；不扣费、不补AP，动作/移动/暂停期间拒绝交接。</summary>
    public bool TrySelectBurstActor(PieceController piece)
    {
        if (Training != null && Training.RestrictsCommands) return false;
        if (!SpeedTurnsActive || !inBattle || !PlayerController.isBursting || !PlayerController.isInTurn ||
            BattleScene.Ins.TM.IsPaused || cancelBurst || HasCombatPresentation || moveManager.IsMoving || !CanActInBurst(piece)) return false;
        requestedBurstActor = piece != burstActor ? piece : null;
        return true;
    }

    /// <summary>兼容旧选人调用，现在只请求切换已入场的聚能角色。</summary>
    public void ToggleBurstActor(PieceController piece) => TrySelectBurstActor(piece);

    /// <summary>取消尚未支付的发动请求，不扣费或进入冷却。</summary>
    public void CancelBurstSelection()
    {
        burstRequested = false;
        PlayerController.RefreshBurstUI();
    }

    private IEnumerator BurstInterruption(PieceController interrupted)
    {
        int version = burstInterruptionVersion;
        var routine = BurstInterruptionRoutine(interrupted, version);
        bool finished = false;
        try
        {
            while (routine.MoveNext()) yield return routine.Current;
            finished = true;
        }
        finally
        {
            (routine as System.IDisposable)?.Dispose();
            if (version == burstInterruptionVersion && (!finished || !inBattle || !SpeedTurnsActive)) StopSpeedTurns();
        }
    }

    private bool IsBurstInterruptionCurrent(int version) =>
        version == burstInterruptionVersion && SpeedTurnsActive && inBattle;

    private IEnumerator BurstInterruptionRoutine(PieceController interrupted, int version)
    {
        // 等待在途动作，不能为了切换控制权取消已经发出的子弹或到达结算。
        while (IsBurstInterruptionCurrent(version) && (BattleScene.Ins.TM.IsPaused || HasCombatPresentation || moveManager.IsMoving)) yield return null;
        if (!IsBurstInterruptionCurrent(version)) yield break;
        bool requested = burstRequested;
        burstRequested = false;
        if (!requested || !PlayerController.ableBurst) { PlayerController.RefreshBurstUI(); yield break; }
        selectedBurstActors.AddRange(PlayerController.pieces.Where(CanJoinBurst).Distinct());
        if (selectedBurstActors.Count == 0 || !PlayerController.SpendBurst(selectedBurstActors.Count))
        {
            selectedBurstActors.Clear();
            PlayerController.RefreshBurstUI();
            yield break;
        }
        if (!IsBurstInterruptionCurrent(version)) yield break;

        bool playerTurn = PlayerController.isInTurn, enemyTurn = AIController.isInTurn;
        PlayerController.isInTurn = AIController.isInTurn = false;
        BattleScene.Ins.CM.ClearTurnSelection();
        if (!IsBurstInterruptionCurrent(version)) yield break;
        cancelBurst = false;
        burstUsedThisTurn = true;
        SuspendInterruptedDelaySkill();
        PlayerController.EnterBurstMode();
        if (!IsBurstInterruptionCurrent(version)) yield break;
        // 所有人只在入场时补一次AP；切换只改变控制权，保留各自已经消耗的额外AP。
        foreach (var actor in selectedBurstActors.ToArray())
        {
            if (!CanJoinBurst(actor)) continue;
            burstActorStates.Add(actor, new BurstActorState { AP = actor.unitAttrCenter.CurMovePoint, Idle = actor.isIdle, Life = actor.BattleLifeVersion });
        }
        foreach (var item in burstActorStates.ToArray())
        {
            var actor = item.Key;
            if (!CanJoinBurst(actor) || actor.BattleLifeVersion != item.Value.Life) continue;
            actor.isIdle = false;
            actor.unitAttrCenter.FullMovePoint();
            if (!IsBurstInterruptionCurrent(version)) yield break;
            orderManager?.ClearForPiece(actor);
            if (!IsBurstInterruptionCurrent(version)) yield break;
        }
        RefreshBurstVisuals();
        BattleScene.Ins.UM.logPanel.PlayerLog("聚能：本次统一消耗50。点击主角或按1/2/3自由切换，切换保留剩余AP；再次点击聚能结束。");
        SetActiveBurstActor(CanActInBurst(interrupted) ? interrupted : selectedBurstActors.FirstOrDefault(CanActInBurst));
        while (IsBurstInterruptionCurrent(version) && !cancelBurst)
        {
            if (BattleScene.Ins.TM.IsPaused || HasCombatPresentation || moveManager.IsMoving) { yield return null; continue; }
            if (requestedBurstActor != null || !CanActInBurst(burstActor))
            {
                var next = requestedBurstActor;
                requestedBurstActor = null;
                PlayerController.isInTurn = false;
                BattleScene.Ins.CM.ClearTurnSelection();
                ResolveBurstDelaySkill();
                while (IsBurstInterruptionCurrent(version) && (BattleScene.Ins.TM.IsPaused || HasCombatPresentation || moveManager.IsMoving)) yield return null;
                if (!IsBurstInterruptionCurrent(version)) yield break;
                if (cancelBurst) break;
                if (!CanActInBurst(next)) next = selectedBurstActors.FirstOrDefault(CanActInBurst);
                if (next == null) break;
                SetActiveBurstActor(next);
            }
            yield return null;
        }
        if (!IsBurstInterruptionCurrent(version)) yield break;
        PlayerController.isInTurn = false;
        while (IsBurstInterruptionCurrent(version) && (BattleScene.Ins.TM.IsPaused || HasCombatPresentation || moveManager.IsMoving)) yield return null;
        if (!IsBurstInterruptionCurrent(version)) yield break;
        ResolveBurstDelaySkill();
        while (IsBurstInterruptionCurrent(version) && (BattleScene.Ins.TM.IsPaused || HasCombatPresentation || moveManager.IsMoving)) yield return null;
        if (!IsBurstInterruptionCurrent(version)) yield break;
        RestoreBurstActors();
        RestoreInterruptedDelaySkill();
        RestoreBurstVisuals();
        ClearBurstSelection();
        if (!IsBurstInterruptionCurrent(version)) yield break;
        PlayerController.EndBurstMode();
        while (IsBurstInterruptionCurrent(version) && HasPendingBurstRelease) yield return null;
        if (!IsBurstInterruptionCurrent(version)) yield break;
        ActivePiece = interrupted;
        PlayerController.isInTurn = playerTurn;
        AIController.isInTurn = enemyTurn;
        if (interrupted is EnemyController)
        {
            finishPieceTurn = true;
            AIController.isInTurn = false;
        }
        else if (playerTurn && IsSpeedParticipant(interrupted))
        {
            Training?.OnPieceTurnStarted(interrupted);
            BattleScene.Ins.CM.SelectActivePiece(interrupted);
        }
        PlayerController.RefreshBurstUI();
    }

    private void SetActiveBurstActor(PieceController actor)
    {
        burstActor = ActivePiece = actor;
        if (actor == null) return;
        PlayerController.isInTurn = true;
        cameraController.SetFollow(actor.transform);
        BattleScene.Ins.CM.SelectActivePiece(actor);
    }

    private void ClearBurstSelection()
    {
        var previous = selectedBurstActors.ToArray();
        selectedBurstActors.Clear();
        foreach (var piece in previous) if (piece != null) piece.CancelSelect();
    }

    private void RestoreBurstActors()
    {
        ClearMoveUndo();
        var states = burstActorStates.ToArray();
        burstActor = requestedBurstActor = null;
        int version = burstInterruptionVersion;
        // 每份快照先移交再回调；重入清理仍可归还其余角色，且不能污染新生命。
        foreach (var item in states)
        {
            var actor = item.Key;
            var state = item.Value;
            if (version != burstInterruptionVersion) return;
            if (!burstActorStates.Remove(actor)) continue;
            if (actor == null || actor.BattleLifeVersion != state.Life) continue;
            actor.isIdle = state.Idle;
            if (!actor.isDead) actor.unitAttrCenter.SetMovePoint(state.AP);
            if (version != burstInterruptionVersion || actor == null || actor.BattleLifeVersion != state.Life) continue;
            actor.CancelSelect();
        }
    }

    private void SuspendInterruptedDelaySkill()
    {
        if (_delaySkillCaster == null) return;
        interruptedDelaySkill = new InterruptedDelaySkill { Caster = _delaySkillCaster, Life = _delaySkillCaster.BattleLifeVersion,
            Skill = _delaySkillPack, Action = delayAction, Position = _delaySkillTargetPos, Effect = _delaySkillEffectObj };
        ClearBurstDelaySkill(false);
    }

    private void ResolveBurstDelaySkill()
    {
        if (_delaySkillCaster == null) return;
        if (IsBurstActor(_delaySkillCaster) && !_delaySkillCaster.isDead && _delaySkillCaster.gameObject.activeInHierarchy) HandleDelaySkill();
        else ClearBurstDelaySkill(true);
    }

    private void ClearBurstDelaySkill(bool hideEffect)
    {
        if (hideEffect && _delaySkillEffectObj != null) _delaySkillEffectObj.SetActive(false);
        _delaySkillCaster = null;
        _delaySkillPack = null;
        delayAction = null;
        _delaySkillEffectObj = null;
    }

    private void RestoreInterruptedDelaySkill()
    {
        var saved = interruptedDelaySkill;
        interruptedDelaySkill = null;
        if (saved == null) return;
        if (saved.Caster == null || saved.Caster.BattleLifeVersion != saved.Life || saved.Caster.isDead || !saved.Caster.gameObject.activeInHierarchy)
        {
            if (saved.Effect != null) saved.Effect.SetActive(false);
            return;
        }
        _delaySkillCaster = saved.Caster;
        _delaySkillPack = saved.Skill;
        delayAction = saved.Action;
        _delaySkillTargetPos = saved.Position;
        _delaySkillEffectObj = saved.Effect;
    }

    private void ResetBurstInterruption()
    {
        ClearBurstEffects();
        burstInterruptionVersion++;
        burstRequested = false;
        cancelBurst = true;
        burstUsedThisTurn = false;
        if (burstActorStates.Count > 0 || interruptedDelaySkill != null) ClearBurstDelaySkill(true);
        if (interruptedDelaySkill?.Effect != null) interruptedDelaySkill.Effect.SetActive(false);
        interruptedDelaySkill = null;
        RestoreBurstActors();
        RestoreBurstVisuals();
        ClearBurstSelection();
        if (PlayerController != null)
        {
            PlayerController.isBursting = false;
            PlayerController.totalDamage = 0;
            PlayerController.burstTarget = null;
        }
        ShowBurstGray(false);
    }
}
