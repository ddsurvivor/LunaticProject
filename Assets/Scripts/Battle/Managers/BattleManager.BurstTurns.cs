using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private bool burstRequested; // 等待当前动作完成后进入选人。
    private bool burstConfirmed; // 玩家已确认本次角色名单。
    private bool cancelBurst; // 取消选人或在当前动作完成后提前退出。
    private bool burstUsedThisTurn; // 被插入聚能的普通回合不抵扣本次冷却。
    /// <summary>是否正在选择聚能角色；此时世界点击仅处理选人。</summary>
    public bool IsChoosingBurst { get; private set; }
    /// <summary>是否已请求聚能或正在选人；为 true 时暂停普通玩家输入和 AI 决策。</summary>
    public bool BurstInputPending => burstRequested || IsChoosingBurst;
    private readonly List<PieceController> selectedBurstActors = new(); // 选择顺序即额外行动顺序。
    private readonly Dictionary<SpriteRenderer, Material> burstOriginalMaterials = new(); // 退出时恢复的共享材质。
    private readonly List<Material> burstMaterials = new(); // 本次聚能创建并负责销毁的彩色材质副本。
    public int SelectedBurstCount => selectedBurstActors.Count;
    /// <summary>判断角色是否参与当前聚能，供命中、伤害和附加效果结算使用。</summary>
    public bool IsBurstActor(PieceController piece) => PlayerController.isBursting && selectedBurstActors.Contains(piece);

    /// <summary>聚能按钮入口：依当前状态请求发动、确认选人、撤销请求或提前结束。</summary>
    public void OnBurstButton()
    {
        if (!SpeedTurnsActive || !inBattle || ActivePiece == null) return;
        if (PlayerController.isBursting) { cancelBurst = true; return; }
        if (burstRequested) { burstRequested = false; PlayerController.RefreshBurstUI(); return; }
        if (IsChoosingBurst)
        {
            selectedBurstActors.RemoveAll(p => !CanJoinBurst(p));
            burstConfirmed = selectedBurstActors.Count > 0 && selectedBurstActors.Count <= PlayerController.AffordableBurstActors;
            PlayerController.RefreshBurstUI();
            return;
        }
        if (!PlayerController.ableBurst) return;
        burstRequested = true; // 已在飞行/移动的动作完成后再接管输入，不取消动画与伤害回调。
        PlayerController.RefreshBurstUI();
    }

    private bool CanJoinBurst(PieceController piece) => IsSpeedParticipant(piece) &&
        PlayerController.pieces.Contains(piece) && !piece.cantControl &&
        piece.unitAttrCenter.GetBuffStacks(BuffType.Stun) == 0;

    /// <summary>选人期间切换一名合格我方角色；新增人数不能超过当前能量可支付人数。</summary>
    public void ToggleBurstActor(PieceController piece)
    {
        if (!IsChoosingBurst || !CanJoinBurst(piece)) return;
        if (selectedBurstActors.Remove(piece)) piece.CancelSelect();
        else if (selectedBurstActors.Count < PlayerController.AffordableBurstActors)
        {
            selectedBurstActors.Add(piece);
            piece.OnSelect();
        }
        PlayerController.RefreshBurstUI();
    }

    /// <summary>取消尚未支付的选人流程，恢复原回合且不进入冷却。</summary>
    public void CancelBurstSelection()
    {
        if (!IsChoosingBurst) return;
        cancelBurst = true;
    }

    private IEnumerator BurstInterruption(PieceController interrupted)
    {
        // 等待在途攻击与移动结算，避免切换控制权时丢失伤害回调。
        while (inBattle && (HasCombatPresentation || moveManager.IsMoving)) yield return null;
        if (!inBattle || !burstRequested || !PlayerController.ableBurst) yield break;
        burstRequested = false;
        IsChoosingBurst = true;
        burstConfirmed = cancelBurst = false;
        bool playerTurn = PlayerController.isInTurn, enemyTurn = AIController.isInTurn; // 退出选人或聚能时恢复的输入归属。
        PlayerController.isInTurn = AIController.isInTurn = false;
        BattleScene.Ins.CM.ClearTurnSelection();
        selectedBurstActors.Clear();
        BattleScene.Ins.UM.logPanel.PlayerLog("聚能：点击我方棋子选择/取消，可选多名，每名消耗50。再次点击聚能确认，右键取消。");
        PlayerController.RefreshBurstUI();
        while (inBattle && !cancelBurst && !burstConfirmed) yield return null;
        // 提交前重新校验名单，支付成功后才开启额外行动。
        selectedBurstActors.RemoveAll(p => !CanJoinBurst(p));
        IsChoosingBurst = false;
        if (!inBattle) yield break;
        if (cancelBurst || !PlayerController.SpendBurst(selectedBurstActors.Count))
        {
            ClearBurstSelection();
            PlayerController.isInTurn = playerTurn;
            AIController.isInTurn = enemyTurn;
            if (playerTurn) BattleScene.Ins.CM.SelectActivePiece(interrupted);
            PlayerController.RefreshBurstUI();
            yield break;
        }

        burstUsedThisTurn = true;
        PlayerController.EnterBurstMode();
        SetBurstActorColors();
        // 额外行动段不消耗行动条，也不重复触发该棋子的普通回合/Buff持续时间。
        foreach (var actor in selectedBurstActors.ToArray())
        {
            if (cancelBurst || !inBattle) break;
            if (!CanJoinBurst(actor)) continue;
            int savedAP = actor.unitAttrCenter.CurMovePoint; // 退出额外行动后恢复的普通 AP。
            bool savedIdle = actor.isIdle; // 额外行动不改变普通回合的待机状态。
            ActivePiece = actor;
            actor.isIdle = false;
            actor.unitAttrCenter.FullMovePoint();
            orderManager?.ClearForPiece(actor);
            PlayerController.isInTurn = true;
            cameraController.SetFollow(actor.transform);
            BattleScene.Ins.CM.SelectActivePiece(actor);
            try
            {
                yield return null;
                while (inBattle && !cancelBurst && CanJoinBurst(actor) && !actor.isIdle && actor.unitAttrCenter.CurMovePoint > 0)
                    yield return null;
                PlayerController.isInTurn = false;
                while (inBattle && (HasCombatPresentation || moveManager.IsMoving)) yield return null;
                if (inBattle && _delaySkillCaster == actor) HandleDelaySkill();
                while (inBattle && (HasCombatPresentation || moveManager.IsMoving)) yield return null;
            }
            finally
            {
                // 离开该角色的额外行动段时归还普通行动状态，避免额外 AP 泄漏。
                if (actor != null)
                {
                    actor.CancelSelect();
                    if (!actor.isDead) actor.unitAttrCenter.SetMovePoint(savedAP);
                    actor.isIdle = savedIdle;
                }
            }
        }
        RestoreBurstActorColors();
        ClearBurstSelection();
        if (!inBattle) yield break;
        PlayerController.EndBurstMode();
        ActivePiece = interrupted;
        PlayerController.isInTurn = playerTurn;
        AIController.isInTurn = enemyTurn;
        if (interrupted is EnemyController)
        {
            // 结束被打断敌人的剩余AP；下一位仍由速度队列决定，不跳过我方满条单位。
            finishPieceTurn = true;
            AIController.isInTurn = false;
        }
        else if (playerTurn && IsSpeedParticipant(interrupted)) BattleScene.Ins.CM.SelectActivePiece(interrupted);
        PlayerController.RefreshBurstUI();
    }

    private void ClearBurstSelection()
    {
        foreach (var piece in selectedBurstActors) if (piece != null) piece.CancelSelect();
        selectedBurstActors.Clear();
    }

    private void SetBurstActorColors()
    {
        // 使用独立副本取消灰度，避免修改共享材质影响未选中的角色。
        foreach (var actor in selectedBurstActors)
        {
            var renderer = actor.pieceDisplay?.pieceSpriteRenderer;
            if (renderer == null || renderer.sharedMaterial == null) continue;
            burstOriginalMaterials[renderer] = renderer.sharedMaterial;
            var material = new Material(renderer.sharedMaterial);
            material.DisableKeyword("GREYSCALE_ON");
            renderer.sharedMaterial = material;
            burstMaterials.Add(material);
        }
    }

    private void RestoreBurstActorColors()
    {
        foreach (var pair in burstOriginalMaterials) if (pair.Key != null) pair.Key.sharedMaterial = pair.Value;
        burstOriginalMaterials.Clear();
        foreach (var material in burstMaterials) if (material != null) Destroy(material);
        burstMaterials.Clear();
    }

    private void ResetBurstInterruption()
    {
        burstRequested = IsChoosingBurst = burstConfirmed = false;
        cancelBurst = true;
        burstUsedThisTurn = false;
        RestoreBurstActorColors();
        ClearBurstSelection();
        if (PlayerController != null) PlayerController.isBursting = false;
        ShowBurstGray(false);
    }
}
