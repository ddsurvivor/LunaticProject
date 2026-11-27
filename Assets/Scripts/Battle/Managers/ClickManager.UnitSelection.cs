using UnityEngine;

public partial class ClickManager
{
    private bool inspectingUnit; // 临时查看不改变回合归属；结束后恢复当前行动者。

    private bool CanChangeUnitSelection()
    {
        var scene = BattleScene.Ins;
        var battle = scene != null ? scene.BM : null;
        return isActiveAndEnabled && battle != null && battle.SpeedTurnsActive && battle.IsBattleRunning &&
            battle.Training?.RestrictsCommands != true && !scene.TM.IsPaused && !battle.HasExternalPlayerControl && !battle.BurstInputPending &&
            !battle.HasCombatPresentation && !battle.moveManager.IsMoving && !scene.UM.BlocksWorldInput &&
            !scene.UM.HasActionFlow && !MemeLoadoutPanel.IsOpen && !GM.Ins.IsTransitioning && !_isDragging &&
            (battle.ActivePiece == null || !battle.ActivePiece.IsUsingSkill && !battle.ActivePiece.isUsingOrder);
    }

    /// <summary>头像点击统一入口；普通回合只查看非行动者，聚能队友沿正式请求切换，不授予额外行动权。</summary>
    public bool TrySelectPortraitUnit(PieceController piece)
    {
        if (!CanChangeUnitSelection() || !BattleManager.IsSpeedParticipant(piece)) return false;
        var scene = BattleScene.Ins;
        var battle = scene.BM;
        if (battle.PlayerController.isBursting && piece != battle.ActivePiece && battle.TrySelectBurstActor(piece))
        {
            battle.cameraController.SetFollow(piece.transform);
            return true;
        }
        if (battle.CanControlPiece(piece)) SelectActivePiece(piece);
        else
        {
            ClearSelection(false);
            inspectingUnit = true;
            _selectedPiece = piece;
            piece.OnSelect();
            scene.UM.pieceInfoPanel.OnSelectPiece(piece);
            scene.UM.ShowPieceState(piece);
            if (piece is EnemyController enemy) PinEnemyTargetLine(enemy);
        }
        battle.cameraController.SetFollow(piece.transform);
        return true;
    }

    private void RestoreActiveSelection(bool focusCamera)
    {
        if (!CanChangeUnitSelection()) return;
        var battle = BattleScene.Ins.BM;
        if (!battle.CanControlPiece(battle.ActivePiece)) return;
        SelectActivePiece(battle.ActivePiece);
        if (focusCamera) battle.cameraController.SetFollow(battle.ActivePiece.transform);
    }

    private void RefreshDefaultSelection()
    {
        // 只在选择丢失或临时查看结束时恢复，不逐帧抢回用户平移后的镜头。
        bool inspectionEnded = inspectingUnit && (!BattleManager.IsSpeedParticipant(_selectedPiece) ||
            !BattleScene.Ins.UM.pieceStatePanel.gameObject.activeInHierarchy);
        if (_selectedPiece == null || inspectionEnded) RestoreActiveSelection(inspectionEnded);
    }
}
