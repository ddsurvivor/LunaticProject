using System.Linq;
using UnityEngine;

public partial class ClickManager
{
    private BattleOutcomePreviewUI outcomePreview;
    private PieceController outcomeTarget;
    private float nextOutcomeRefresh; // 限制只读几何和伤害查询频率，目标取消时立即隐藏。

    private void UpdateOutcomePreview()
    {
        var battle = BattleScene.Ins.BM;
        var actor = battle.ActivePiece;
        var range = actor != null ? actor.rangeUI : null;
        if (range == null || range.PreviewSkill == null || lastHoveredPiece == null ||
            !range.PreviewTargets.Contains(lastHoveredPiece) || battle.HasCombatPresentation || battle.BurstInputPending)
        { if (outcomePreview != null) outcomePreview.gameObject.SetActive(false); return; }
        if (outcomePreview == null)
        {
            var prefab = Resources.Load<BattleOutcomePreviewUI>("UI/BattleOutcomePreview");
            var canvas = BattleScene.Ins.UM.pieceInfoPanel.GetComponentInParent<Canvas>();
            if (prefab == null || !prefab.HasTemplateBindings || canvas == null) return;
            outcomePreview = Instantiate(prefab, canvas.rootCanvas.transform, false);
        }
        if (outcomePreview.gameObject.activeSelf && outcomeTarget == lastHoveredPiece && Time.unscaledTime < nextOutcomeRefresh) return;
        outcomeTarget = lastHoveredPiece;
        nextOutcomeRefresh = Time.unscaledTime + .15f;
        outcomePreview.transform.SetAsLastSibling();
        outcomePreview.Show(battle, actor, lastHoveredPiece, range.PreviewSkill);
    }
}
