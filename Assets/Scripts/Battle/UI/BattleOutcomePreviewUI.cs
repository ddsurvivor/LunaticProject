using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>只展示当前悬停目标的组合提示；附带结果均为有条件预测，不并入直接伤害或头顶血条。</summary>
public sealed class BattleOutcomePreviewUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text summary;
    private LineRenderer landingLine;
    private Material landingMaterial;
    public bool HasTemplateBindings => summary != null;

    /// <summary>模板创建入口；运行时保留预制体布局。</summary>
    public void Configure(UnityEngine.UI.Text text) => summary = text;

    /// <summary>在当前场景快照上读取直接伤害、掩体、击退及夹击信息；不投骰、不付款、不移动棋子。</summary>
    public void Show(BattleManager battle, PieceController attacker, PieceController target, SkillPack skill)
    {
        if (!HasTemplateBindings || !SkillTargeting.IsValid(attacker, target, skill)) { gameObject.SetActive(false); return; }
        var preview = battle.damageManager.PreviewSkill(attacker, target, skill, orderedTargets: attacker.rangeUI?.PreviewTargets);
        var text = new StringBuilder();
        text.AppendLine($"{target.pieceData.pieceName} · 战果预览");
        text.AppendLine($"命中 {preview.HitRate}%　直接伤害 {preview.Total.MinDamage}–{preview.Total.MaxDamage}");
        var cover = skill.attackPacks.Count > 0 ? battle.CheckCoverObstruction(attacker, target) : null;
        if (cover != null) text.AppendLine($"掩体 {cover.Level} 级：减伤 {GM.Ins.DM.gameConstSO.GetCoverDamageReduction(cover.Level)}%（已计入）");
        var effects = skill.additionalEffects?.OfType<HitBackEffect>().ToArray();
        Vector3 landing = target.transform.position;
        if (landingLine != null) landingLine.gameObject.SetActive(false);
        if (effects != null && effects.Length > 1) text.AppendLine("多次击退：落点与连锁不预测");
        else if (target.ableMove && (effects?.Length == 1 || battle.IsBurstActor(attacker)))
        {
            float distance = effects?.Length == 1 ? effects[0].dis : 1f + preview.Total.MaxDamage * .05f;
            var move = battle.QueryKnockback(attacker, target, distance); landing = move.FinalPosition;
            text.AppendLine($"条件击退：预计 {Vector3.Distance(target.transform.position, landing):0.##} 米" + (effects?.Length == 1 ? "" : "（高伤害分支）"));
            DrawLanding(target.transform.position, landing);
            if (move.IsCollided)
            {
                var obstacle = move.HitPieces.FirstOrDefault(p => p != null && p != target);
                text.AppendLine($"潜在碰撞：{(obstacle != null ? obstacle.pieceData.pieceName : "墙体/地形边界")}");
                text.AppendLine(obstacle != null ? "碰撞基数为实际掉血×40%，双方分别结算" : "碰撞基数为实际掉血×40%，仅移动者结算");
            }
        }
        var partner = battle.FindFlankPartner(attacker, target, landing);
        if (partner != null && BuffManager.CanTarget(partner, target) && partner.unitAttrCenter.GetBuffStacks(BuffType.Stun) == 0)
            text.AppendLine($"夹击候选：{partner.pieceData.pieceName}（落点复核）");
        text.Append("附带结果需命中、目标存活且效果未被拦截；不含完整连锁总伤害。");
        summary.text = text.ToString(); gameObject.SetActive(true);
    }

    private void DrawLanding(Vector3 start, Vector3 end)
    {
        if (landingLine == null)
        {
            var node = new GameObject("ConditionalLanding"); node.transform.SetParent(transform, false);
            landingLine = node.AddComponent<LineRenderer>(); landingLine.useWorldSpace = true;
            landingMaterial = new Material(Shader.Find("Sprites/Default")); landingLine.sharedMaterial = landingMaterial;
            landingLine.startWidth = landingLine.endWidth = .04f;
            landingLine.startColor = landingLine.endColor = new Color(.25f, .85f, 1f, .8f);
            landingLine.sortingOrder = 5;
        }
        landingLine.gameObject.SetActive(true);
        Vector3 lift = Vector3.up * .15f;
        landingLine.positionCount = 6;
        landingLine.SetPositions(new[] { start + lift, end + lift, end + lift + Vector3.right * .15f,
            end + lift - Vector3.right * .15f, end + lift + Vector3.forward * .15f, end + lift - Vector3.forward * .15f });
    }

    private void OnDestroy() { if (landingMaterial != null) Destroy(landingMaterial); }
}
