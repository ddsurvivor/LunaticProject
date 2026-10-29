using System;
using System.Collections.Generic;
using UnityEngine;

public partial class UIManager
{
    private PieceController actionActor;
    private Func<ActionResourceCost> selectionCost;
    private readonly List<ActionCostPreviewUI> selectionViews = new();
    private bool actionCommitted; // 确认扣费后只等待执行结束，不再展示待消耗费用。
    private int actionStartFrame;

    /// <summary>由行动菜单移交选择范围的费用预览；菜单隐藏后仍保持，直到确认执行或取消。</summary>
    public void BeginActionSelection(PieceController actor, Func<ActionResourceCost> cost)
    {
        ClearActionFlow();
        if (actor == null) return;
        actionActor = actor;
        selectionCost = cost;
        actionStartFrame = Time.frameCount;
        selectionViews.AddRange(actor.GetComponentsInChildren<ActionCostPreviewUI>(false));
        AddSelectionView(pieceStatePanel.GetComponent<ActionCostPreviewUI>());
        AddSelectionView(pieceInfoPanel.GetComponent<ActionCostPreviewUI>());
    }

    private void AddSelectionView(ActionCostPreviewUI view)
    {
        if (view != null && !selectionViews.Contains(view)) selectionViews.Add(view);
    }

    /// <summary>正式 AP 扣费时结束选择预览，安排在移动和战斗表现全部完成后恢复当前行动者 UI。</summary>
    public void NotifyActionCommitted(PieceController actor)
    {
        var battle = BattleScene.Ins.BM;
        if (actor == null || !actor.isPlayerPiece || battle.ActivePiece != actor || battle.HasExternalPlayerControl) return;
        if (actionActor != actor) BeginActionSelection(actor, null);
        actionCommitted = true;
        actionStartFrame = Time.frameCount;
        ClearSelectionPreview();
    }

    private void ClearSelectionPreview()
    {
        foreach (var view in selectionViews) if (view != null) view.Clear();
        selectionCost = null;
    }

    /// <summary>交棒、重新选择或关闭场景时放弃旧行动的预览和自动返回，避免抢回新角色的镜头。</summary>
    public void ClearActionFlow()
    {
        ClearSelectionPreview();
        selectionViews.Clear();
        actionActor = null;
        actionCommitted = false;
    }

    private void LateUpdate()
    {
        if (MemeLoadoutPanel.IsOpen) return;
        if (actionActor == null)
        {
            if (selectionViews.Count > 0) ClearActionFlow();
            return;
        }
        var scene = BattleScene.Ins;
        var battle = scene != null ? scene.BM : null;
        if (battle == null || !battle.SpeedTurnsActive || battle.ActivePiece != actionActor ||
            battle.HasExternalPlayerControl || actionActor.isDead || !actionActor.isActiveAndEnabled || !actionActor.gameObject.activeInHierarchy)
        {
            ClearActionFlow();
            return;
        }
        // 输入和扣费可能发生于同一帧；等待所有 Update 入口完成，再判断是否已经取消/结束。
        if (Time.frameCount <= actionStartFrame) return;
        bool selecting = actionActor.IsUsingSkill || actionActor.isUsingOrder || scene.CM.IsChoosingMove(actionActor);
        if (!actionCommitted && selecting)
        {
            if (selectionCost != null)
            {
                var cost = selectionCost();
                foreach (var view in selectionViews) if (view != null) view.Show(actionActor.unitAttrCenter, cost);
            }
            return;
        }
        ClearSelectionPreview();
        if (battle.HasCombatPresentation || battle.moveManager.IsMoving || actionActor.IsPerformingAction) return;
        var actor = actionActor;
        ClearActionFlow();
        battle.cameraController.SetFollow(actor.transform);
        if (battle.CanControlPiece(actor)) scene.CM.SelectActivePiece(actor);
    }

    private void OnDisable() => ClearActionFlow();
}
