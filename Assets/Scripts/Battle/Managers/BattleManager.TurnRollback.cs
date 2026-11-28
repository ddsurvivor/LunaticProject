using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public partial class BattleManager
{
    private sealed class TurnCheckpoint
    {
        internal PieceController Actor;
        internal Action Restore;
    }

    private readonly List<TurnCheckpoint> turnCheckpoints = new(); // 仅苹果树副本持有；恢复后丢弃未来分支。
    private PieceController rollbackActor; // 队列已消费本次满条，恢复后直接重入该普通回合。

    private void CaptureNormalTurnCheckpoint(PieceController actor)
    {
        if (AppleTraining == null) return;
        var restores = new List<Action>();
        var pieces = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PieceController>(true)).Distinct().ToArray();
        var players = PlayerController.pieces.ToArray();
        var enemies = AIController.pieces.ToArray();
        var summons = summonPieces.ToArray();
        foreach (var piece in pieces)
        {
            if (piece.unitAttrCenter == null) continue;
            restores.Add(piece.CaptureTurnState());
            if (piece is SceneBattleDevice device) restores.Add(device.CaptureDeviceTurnState());
        }
        restores.Add(speedQueue.CaptureTurnState());
        restores.Add(timedMemeStatuses.CaptureTurnState());
        restores.Add(characterSkillManager.CaptureTurnState());
        restores.Add(orderManager.CaptureTurnState());
        restores.Add(AIController.CaptureTurnState());
        restores.Add(PlayerController.CaptureBurstTurnState());
        restores.Add(battleDialogueManager.CaptureTurnState());
        restores.Add(GM.Ins.CaptureBattleTurnProgress());
        foreach (var cover in FindObjectsOfType<CaverSlot>()) restores.Add(cover.CaptureTurnState());
        foreach (var node in networkNodes) if (node != null) restores.Add(node.CaptureTurnState());
        int round = _turnNumber, currency = pendingCurrencyReward;
        var skill = _delaySkillPack; var caster = _delaySkillCaster; var position = _delaySkillTargetPos; var action = delayAction;
        var profile = GM.Ins.PLAYERPROFILE;
        var profileData = JObject.Parse(JsonConvert.SerializeObject(profile));
        profileData.Remove("player"); // 保留玩家对象引用，使被动临时加值及窗口仍指向本会话对象。
        var random = BattleRandom.CaptureState();
        BurstEvent CopyTimer(BurstEvent e) => new BurstEvent { Owner = e.Owner, Piece = e.Piece, Life = e.Life,
            Remaining = e.Remaining, Resolve = e.Resolve, Action = e.Action, Lifetime = e.Lifetime, Lease = e.Lease };
        var timers = worldTimers.Where(e => e.IsValid).Select(CopyTimer).ToList();
        var delayEffect = _delaySkillEffectObj;
        var nodes = networkNodes.ToArray();
        turnCheckpoints.Add(new TurnCheckpoint { Actor = actor, Restore = () =>
        {
            foreach (var extra in gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PieceController>(true)).Except(pieces).ToArray())
                if (extra != null) extra.gameObject.SetActive(false);
            JsonConvert.PopulateObject(profileData.ToString(), profile, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
            foreach (var restore in restores) restore();
            PlayerController.pieces = players.ToList(); AIController.pieces = enemies.ToList(); summonPieces = summons.ToList();
            networkNodes.Clear(); foreach (var node in nodes) if (node != null) networkNodes.Add(node);
            _turnNumber = round; pendingCurrencyReward = currency;
            _delaySkillPack = skill; _delaySkillCaster = caster; _delaySkillTargetPos = position; delayAction = action;
            _delaySkillEffectObj = delayEffect; if (delayEffect != null) delayEffect.SetActive(skill != null);
            foreach (var timer in timers)
            {
                var restored = CopyTimer(timer);
                restored.Lease = restored.Lifetime != null ? restored.Lifetime.Version : restored.Lease;
                restored.Life = restored.Piece != null ? restored.Piece.BattleLifeVersion : restored.Life;
                worldTimers.Add(restored);
            }
            BattleRandom.RestoreState(random);
        } });
    }

    /// <summary>报警确认后恢复上一名普通行动者的回合开始；首回合恢复自身，不发奖、不写档。</summary>
    internal bool RestorePreviousNormalTurn()
    {
        if (AppleTraining == null || !AppleTraining.AlarmPending || !SpeedTurnsActive || turnCheckpoints.Count == 0 ||
            GM.Ins.IsTransitioning || HasCombatPresentation || moveManager.IsMoving) return false;
        int index = Mathf.Max(0, turnCheckpoints.Count - 2);
        var checkpoint = turnCheckpoints[index];
        // 先撤销旧调度及聚能生命周期，再还原数值；旧回调不能消耗恢复后的新回合。
        StopSpeedTurns(); CancelBattleReactions(); ClearBurstEffects(); ClearNetworkConnections();
        BattleScene.Ins.CM.ClearTurnSelection();
        BattleScene.Ins.UM.ClearRollbackPresentation();
        checkpoint.Restore();
        AppleTraining.ResetTreeDeath();
        turnCheckpoints.RemoveRange(index, turnCheckpoints.Count - index);
        rollbackActor = checkpoint.Actor;
        finishPieceTurn = false; burstUsedThisTurn = false; inBattle = true;
        LastOutcome = BattleOutcome.None;
        PlayerController.isInTurn = AIController.isInTurn = false;
        BattleScene.Ins.UM.turnNumberText.text = _turnNumber.ToString();
        SpeedTurnsActive = true;
        speedRoutine = StartCoroutine(SpeedTurnRoutine());
        return true;
    }
}
