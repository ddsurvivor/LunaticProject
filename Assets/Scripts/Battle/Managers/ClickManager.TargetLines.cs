using System.Collections.Generic;
using UnityEngine;

public partial class ClickManager
{
    private EnemyController pinnedTargetLineEnemy;
    private EnemyController suppressedHoveredEnemy;
    private readonly HashSet<EnemyController> displayedTargetLineEnemies = new();
    private readonly HashSet<EnemyController> currentTargetLineEnemies = new();

    private void SetHoveredPiece(PieceController piece)
    {
        if (piece != suppressedHoveredEnemy) suppressedHoveredEnemy = null;
        if (lastHoveredPiece == piece) return;
        if (lastHoveredPiece != null) lastHoveredPiece.ShowOutline(false);
        lastHoveredPiece = piece;
        if (piece != null) piece.ShowOutline(true);
    }

    public void PinEnemyTargetLine(EnemyController enemy)
    {
        ClearEnemyTargetLines();
        if (enemy == null || enemy.isDead || !enemy.isActived || !enemy.gameObject.activeInHierarchy) return;
        pinnedTargetLineEnemy = enemy;
        suppressedHoveredEnemy = null;
        UpdateTargetLinePresentation();
    }

    public void ClearEnemyTargetLineInteraction()
    {
        CancelMovementPreview();
        ClearEnemyTargetLines(true);
    }

    public void ClearEnemyTargetLines(bool suppressCurrentHover = false)
    {
        if (suppressCurrentHover) suppressedHoveredEnemy = lastHoveredPiece as EnemyController;
        pinnedTargetLineEnemy = null;
        foreach (var enemy in displayedTargetLineEnemies)
            if (enemy != null) enemy.HideTargetLine();
        displayedTargetLineEnemies.Clear();
        var enemies = BattleScene.Ins?.BM?.AIController?.pieces;
        if (enemies != null)
            foreach (var piece in enemies)
                if (piece is EnemyController enemy) enemy.HideTargetLine();
    }

    private void UpdateTargetLinePresentation()
    {
        var battle = BattleScene.Ins?.BM;
        if (battle == null || !battle.CanInspectEnemyTargets)
        {
            ClearEnemyTargetLines();
            return;
        }
        PieceController previewPiece = null;
        Vector3? previewPosition = null;
        var movement = battle.moveManager;
        if (_isDragging && _selectedPiece != null && _selectedPiece.isPlayerPiece && !_selectedPiece.isDead)
        {
            previewPiece = _selectedPiece;
            if (dragMove) previewPosition = previewPiece.transform.position;
            else if (movement != null && movement.TryGetPreviewPosition(previewPiece.gameObject, out var destination))
                previewPosition = destination;
        }
        else if (movement != null && movement.MovingPiece != null && movement.MovingPiece.isPlayerPiece && !movement.MovingPiece.isDead)
        {
            previewPiece = movement.MovingPiece;
            previewPosition = previewPiece.transform.position;
        }
        bool preview = previewPiece != null && previewPosition.HasValue;
        currentTargetLineEnemies.Clear();
        if (battle.AIController != null)
        {
            foreach (var piece in battle.AIController.pieces)
            {
                if (!(piece is EnemyController enemy) || !enemy.isActived || enemy.isDead || !enemy.gameObject.activeInHierarchy) continue;
                bool hovered = lastHoveredPiece == enemy && suppressedHoveredEnemy != enemy;
                // 移动期间仅显示威胁移动角色的敌人，覆盖悬停/点击保持。
                // 不可达位置没有有效预测，不沿用上一落点或混入普通指示线。
                if (previewPiece != null && !preview) continue;
                if (!preview && !hovered && pinnedTargetLineEnemy != enemy) continue;
                if (enemy.RenderTargetLine(preview ? previewPiece : null, previewPosition, preview,
                    requirePreviewTarget: preview)) currentTargetLineEnemies.Add(enemy);
            }
        }
        foreach (var enemy in displayedTargetLineEnemies)
            if (enemy != null && !currentTargetLineEnemies.Contains(enemy)) enemy.HideTargetLine();
        displayedTargetLineEnemies.Clear();
        foreach (var enemy in currentTargetLineEnemies) displayedTargetLineEnemies.Add(enemy);
    }

    private void OnDisable()
    {
        CancelMovementPreview();
        SetHoveredPiece(null);
        ClearEnemyTargetLines();
    }
}
