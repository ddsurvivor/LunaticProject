using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private readonly SpeedTurnQueue<PieceController> speedQueue = new(); // 保存普通行动条及满条顺序。
    private Coroutine speedRoutine; // 战斗期间唯一的速度回合调度协程。
    private bool finishPieceTurn; // 请求结束当前普通棋子的剩余行动。
    /// <summary>当前获得控制权的棋子；聚能期间临时指向正在额外行动的角色。</summary>
    public PieceController ActivePiece { get; private set; }
    /// <summary>速度回合调度是否已启动，供输入和速度条 UI 判断生命周期。</summary>
    public bool SpeedTurnsActive { get; private set; }
    /// <summary>当前行动者和聚能参与者显示100，其他角色读取普通队列进度；不改写队列。</summary>
    public float ActionProgress(PieceController piece) => piece == ActivePiece || IsBurstActor(piece) ? 100 : (float)speedQueue.Progress(piece);
    /// <summary>速度条只读排序：当前行动者、聚能主角、其余普通队列；不消费或推进普通进度。</summary>
    public int CompareActionOrder(PieceController a, PieceController b)
    {
        if (a == b) return 0;
        if (a == ActivePiece) return -1;
        if (b == ActivePiece) return 1;
        bool leftBurst = IsBurstActor(a), rightBurst = IsBurstActor(b);
        if (leftBurst != rightBurst) return leftBurst ? -1 : 1;
        if (leftBurst) return selectedBurstActors.IndexOf(a).CompareTo(selectedBurstActors.IndexOf(b));
        return speedQueue.CompareDisplayOrder(a, b);
    }
    /// <summary>统一校验玩家能否操作该棋子，包含回合归属、AP、控制状态及动作等待限制。</summary>
    public bool CanControlPiece(PieceController piece) => SpeedTurnsActive && inBattle && !BattleScene.Ins.TM.IsPaused &&
        (Training == null || Training.AllowsActor(piece)) && AppleTraining?.AlarmPending != true && !BurstInputPending && PlayerController.isInTurn && piece != null && piece == ActivePiece &&
        IsSpeedParticipant(piece) && !piece.cantControl && !BattleRelations.IsBehaviorControlled(piece) && !piece.isIdle && piece.unitAttrCenter.CurMovePoint > 0 &&
        !HasCombatPresentation && !moveManager.IsMoving;

    /// <summary>受击表现结束后，仅在当前棋子仍可继续行动时恢复其镜头；交棒由调度器聚焦下一位。</summary>
    public void RestoreCameraAfterHit()
    {
        if (!SpeedTurnsActive || !inBattle || finishPieceTurn || BurstInputPending ||
            (PlayerController.isBursting && cancelBurst) || !IsSpeedParticipant(ActivePiece) ||
            ActivePiece.isIdle || ActivePiece.cantControl || ActivePiece.unitAttrCenter.CurMovePoint <= 0) return;
        cameraController.SetFollow(ActivePiece.transform);
    }

    /// <summary>判断棋子能否参与速度调度；排除死亡、未激活及尚未唤醒的敌人。</summary>
    public static bool IsSpeedParticipant(PieceController piece) => piece != null &&
        piece.gameObject.activeInHierarchy && !(piece is SceneBattleDevice) && piece.unitAttrCenter != null && !piece.isDead &&
        (!(piece is EnemyController enemy) || enemy.isActived);

    /// <summary>收集当前有效参战者；按双方交错顺序注册，作为同刻满条时的稳定排序依据。</summary>
    public List<PieceController> SpeedParticipants()
    {
        // 同速时交错注册双方；后续同刻到达按稳定顺序处理。
        var result = new List<PieceController>();
        for (int i = 0; i < Mathf.Max(PlayerController.pieces.Count, AIController.pieces.Count); i++)
        {
            if (i < PlayerController.pieces.Count) result.Add(PlayerController.pieces[i]);
            if (i < AIController.pieces.Count) result.Add(AIController.pieces[i]);
        }
        result.AddRange(summonPieces);
        return result.Where(IsSpeedParticipant).Distinct().ToList();
    }

    private void StartSpeedTurns()
    {
        if (SpeedTurnsActive || !inBattle || startupPhase != StartupPhase.Active) return;
        speedQueue.Clear();
        SpeedTurnsActive = true;
        GM.Ins.CaptureDeviceAlarmDeployment(this);
        BattleScene.Ins.UM.EnableSpeedTimeline(this);
        PlayerController.RefreshBurstUI();
        speedRoutine = StartCoroutine(SpeedTurnRoutine());
    }

    private void StopSpeedTurns()
    {
        ClearMoveUndo();
        // 先撤销输入和活动者，再恢复额外 AP；清理回调不能在停止过程中重新发出合法命令。
        SpeedTurnsActive = false;
        var routine = speedRoutine;
        speedRoutine = null;
        ActivePiece = null;
        if (PlayerController != null) PlayerController.isInTurn = false;
        if (AIController != null) AIController.isInTurn = false;
        ResetBurstInterruption();
        if (routine != null) StopCoroutine(routine);
    }

    private IEnumerator SpeedTurnRoutine()
    {
        yield return null;
        while (SpeedTurnsActive && inBattle)
        {
            if (BattleScene.Ins.TM.IsPaused || HasCombatPresentation || moveManager.IsMoving || Training?.BlocksTurnAdvance == true || AppleTraining?.AlarmPending == true) { yield return null; continue; }
            AIController.RefreshSpeedTurnWorld();
            speedQueue.Sync(SpeedParticipants());
            // 优先处理已满条的棋子，队列无就绪单位时才推进速度时钟。
            var next = rollbackActor;
            rollbackActor = null;
            next ??= Training != null ? Training.TakeOpeningActor() : null;
            next ??= speedQueue.TakeReady();
            if (next == null)
            {
                PlayerCheckWin();
                if (!inBattle) yield break;
                speedQueue.Tick(p => p.TurnSpeed);
                // 关卡对话/增援/坚持回合沿用速度10的标准时间轮，独立于个人回合。
                int round = (speedQueue.Ticks - 1) / 10 + 1;
                if (round != _turnNumber)
                {
                    _turnNumber = round;
                    SceneBattleDevice.OnWorldRound(this);
                    battleDialogueManager?.TriggerTurnNumStart(round);
                    BattleScene.Ins.UM.turnNumberText.text = round.ToString();
                    if (round >= winTurnCondition) { PlayerWin(); yield break; }
                }
                yield return new WaitForSeconds(0.12f);
                continue;
            }

            CaptureNormalTurnCheckpoint(next);
            ActivePiece = next;
            finishPieceTurn = false;
            burstUsedThisTurn = false;
            orderManager?.ClearForPiece(next);
            bool manual = PlayerController.pieces.Contains(next) && !(next is EnemyController);
            // 每个普通棋子回合只触发自身开始事件和 Buff 结算。
            // 新状态在事件前快照，保护TurnStart内施加或刷新的持续时间。
            timedMemeStatuses.BeginNormalTurn(next);
            next.TurnStart();
            SceneBattleDevice.OnNormalTurnStart(next, this);
            if (!inBattle || !SpeedTurnsActive) yield break;
            buffManager.ProcessBuffs(next.unitAttrCenter);
            timedMemeStatuses.ResolveTurnStart(next);
            if (!inBattle) yield break;
            // 回合开始的持续伤害也可能触发受击；结束后再聚焦行动者并开放控制。
            while (inBattle && HasCombatPresentation) yield return null;
            if (!inBattle || !SpeedTurnsActive) yield break;
            if (IsSpeedParticipant(next))
            {
                cameraController.SetFollow(next.transform);
                if (manual)
                {
                    PlayerController.BeginPieceTurn(next);
                    Training?.OnPieceTurnStarted(next);
                    BattleScene.Ins.CM.SelectActivePiece(next);
                }
                else if (next is EnemyController enemy) AIController.BeginEnemyPieceTurn(enemy);
                else if (!next.cantControl && !BattleRelations.IsBehaviorControlled(next) && next.unitAttrCenter.CurMovePoint > 0)
                {
                    var summon = next.GetComponent<SelfExploreSummonPiece>();
                    if (summon != null) summon.ExecuteAI();
                    finishPieceTurn = true;
                }
            }

            // 至少等待一帧，让行动、Buff伤害、死亡事件及动画锁全部生效。
            yield return null;
            while (inBattle && IsSpeedParticipant(next) && !finishPieceTurn &&
                   !next.isIdle && !next.cantControl && next.unitAttrCenter.CurMovePoint > 0)
            {
                if (BattleScene.Ins.TM.IsPaused || Training?.BlocksTurnAdvance == true || AppleTraining?.AlarmPending == true) { yield return null; continue; }
                if (burstRequested) yield return BurstInterruption(next);
                else yield return null;
            }
            // 最后一次行动中提出的请求也必须在结束当前棋子前被处理。
            if (burstRequested && inBattle) yield return BurstInterruption(next);
            PlayerController.isInTurn = false;
            AIController.isInTurn = false;
            while (inBattle && (BattleScene.Ins.TM.IsPaused || HasCombatPresentation || moveManager.IsMoving || Training?.BlocksTurnAdvance == true || AppleTraining?.AlarmPending == true)) yield return null;
            if (!inBattle) yield break;
            BattleScene.Ins.CM.ClearTurnSelection();
            if (_delaySkillCaster == next) HandleDelaySkill();
            while (inBattle && (BattleScene.Ins.TM.IsPaused || HasCombatPresentation || moveManager.IsMoving)) yield return null;
            if (!inBattle) yield break;
            if (next != null)
            {
                next.TurnEnd();
                timedMemeStatuses.EndNormalTurn(next);
                AppleTraining?.OnNormalTurnEnded();
            }
            if (manual) PlayerController.EndPieceTurn();
            else if (next is EnemyController) AIController.EndPieceTurn();
            // 聚能额外行动及刚被插入聚能的普通回合均不抵扣新冷却。
            if (!burstUsedThisTurn) PlayerController.CompleteNormalTurnForBurst();
            ActivePiece = null;
            PlayerCheckWin();
            yield return null;
        }
    }
}
