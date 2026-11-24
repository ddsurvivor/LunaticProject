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
    private readonly Vector3[] actionAreaCorners = new Vector3[4]; // 复用 HUD 边界查询缓冲，避免逐帧分配。
    private readonly List<UnityEngine.UI.Graphic> actionHudGraphics = new();

    /// <summary>在安全区内为行动菜单与详情保留上下 HUD 的空间；边界随当前布局和 Canvas 缩放更新。</summary>
    public Rect GetActionPanelArea(RectTransform parent, Camera camera, float margin)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.min, camera, out var min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.max, camera, out var max);
        min += Vector2.one * margin;
        max -= Vector2.one * margin;
        float bottom = min.y, top = max.y;
        ReserveActionHud(ActionTimelineRect, parent, true, margin, ref bottom, ref top);
        ReserveActionHud(TimeControlsRect, parent, true, margin, ref bottom, ref top);
        ReserveActionHud(pieceInfoPanel != null ? (RectTransform)pieceInfoPanel.transform : null,
            parent, false, margin, ref bottom, ref top);
        // 极窄窗口先保留安全区，避免 HUD 挤占后产生负高度。
        if (top - bottom >= 120) { min.y = bottom; max.y = top; }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void ReserveActionHud(RectTransform hud, RectTransform parent, bool upper, float margin,
        ref float bottom, ref float top)
    {
        if (hud == null || !hud.gameObject.activeInHierarchy) return;
        hud.GetWorldCorners(actionAreaCorners);
        float low = float.PositiveInfinity, high = float.NegativeInfinity;
        foreach (var corner in actionAreaCorners)
        {
            float y = parent.InverseTransformPoint(corner).y;
            low = Mathf.Min(low, y);
            high = Mathf.Max(high, y);
        }
        if (!upper)
        {
            // 立绘和 AP 图形可能伸出信息面板根矩形，按实际可见图形预留顶部空间。
            hud.GetComponentsInChildren(false, actionHudGraphics);
            foreach (var graphic in actionHudGraphics)
            {
                if (!graphic.enabled) continue;
                graphic.rectTransform.GetWorldCorners(actionAreaCorners);
                foreach (var corner in actionAreaCorners) high = Mathf.Max(high, parent.InverseTransformPoint(corner).y);
            }
        }
        if (upper) top = Mathf.Min(top, low - margin);
        else bottom = Mathf.Max(bottom, high + margin);
    }

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
        UpdateMessageLanes();
        if (BattleScene.Ins != null && BattleScene.Ins.TM.IsPaused) return;
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

    private void OnDisable() { ClearTransientMessages(); ClearActionFlow(); }
}
