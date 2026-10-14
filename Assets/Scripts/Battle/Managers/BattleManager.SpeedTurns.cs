using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private readonly SpeedTurnQueue<PieceController> speedQueue = new();
    private Coroutine speedRoutine;
    private bool finishPieceTurn;
    public PieceController ActivePiece { get; private set; }
    public bool SpeedTurnsActive { get; private set; }
    public float ActionProgress(PieceController piece) => piece == ActivePiece ? 100 : (float)speedQueue.Progress(piece);
    public bool CanControlPiece(PieceController piece) => SpeedTurnsActive && inBattle &&
        PlayerController.isInTurn && piece != null && piece == ActivePiece &&
        IsSpeedParticipant(piece) && !piece.cantControl && !piece.isIdle && piece.unitAttrCenter.CurMovePoint > 0 &&
        !HasCombatPresentation && !moveManager.IsMoving;

    public static bool IsSpeedParticipant(PieceController piece) => piece != null &&
        piece.gameObject.activeInHierarchy && piece.unitAttrCenter != null && !piece.isDead &&
        (!(piece is EnemyController enemy) || enemy.isActived);

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
        BattleScene.Ins.UM.EnableSpeedTimeline(this);
        speedRoutine = StartCoroutine(SpeedTurnRoutine());
    }

    private void StopSpeedTurns()
    {
        SpeedTurnsActive = false;
        if (speedRoutine != null) StopCoroutine(speedRoutine);
        speedRoutine = null;
        ActivePiece = null;
        if (PlayerController != null) PlayerController.isInTurn = false;
        if (AIController != null) AIController.isInTurn = false;
    }

    private IEnumerator SpeedTurnRoutine()
    {
        yield return null;
        while (SpeedTurnsActive && inBattle)
        {
            if (HasCombatPresentation || moveManager.IsMoving) { yield return null; continue; }
            AIController.RefreshSpeedTurnWorld();
            speedQueue.Sync(SpeedParticipants());
            var next = speedQueue.TakeReady();
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
                    battleDialogueManager?.TriggerTurnNumStart(round);
                    BattleScene.Ins.UM.turnNumberText.text = round.ToString();
                    if (round >= winTurnCondition) { PlayerWin(); yield break; }
                }
                yield return new WaitForSeconds(0.12f);
                continue;
            }

            ActivePiece = next;
            finishPieceTurn = false;
            orderManager?.ClearForPiece(next);
            bool manual = PlayerController.pieces.Contains(next) && !(next is EnemyController);
            next.TurnStart();
            if (!inBattle || !SpeedTurnsActive) yield break;
            buffManager.ProcessBuffs(next.unitAttrCenter);
            if (!inBattle) yield break;
            if (IsSpeedParticipant(next))
            {
                cameraController.SetFollow(next.transform);
                if (manual)
                {
                    PlayerController.BeginPieceTurn(next);
                    BattleScene.Ins.CM.SelectActivePiece(next);
                }
                else if (next is EnemyController enemy) AIController.BeginEnemyPieceTurn(enemy);
                else if (!next.cantControl && next.unitAttrCenter.CurMovePoint > 0)
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
                yield return null;
            PlayerController.isInTurn = false;
            AIController.isInTurn = false;
            while (inBattle && (HasCombatPresentation || moveManager.IsMoving)) yield return null;
            if (!inBattle) yield break;
            BattleScene.Ins.CM.ClearTurnSelection();
            if (_delaySkillCaster == next) HandleDelaySkill();
            while (inBattle && (HasCombatPresentation || moveManager.IsMoving)) yield return null;
            if (!inBattle) yield break;
            if (next != null) next.TurnEnd();
            if (manual) PlayerController.EndPieceTurn();
            else if (next is EnemyController) AIController.EndPieceTurn();
            ActivePiece = null;
            PlayerCheckWin();
            yield return null;
        }
    }
}
