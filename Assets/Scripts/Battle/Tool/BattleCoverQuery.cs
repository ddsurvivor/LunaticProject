using UnityEngine;

/// <summary>预览、实战和 AI 共用的只读掩体查询；普通墙体不自动变成掩体。</summary>
public static class BattleCoverQuery
{
    public const float SightHeight = 1.2f; // 沿用现行攻击射线相对棋子脚点的高度。

    /// <summary>攻击线命中且目标靠近实体表面的有效掩体中，选最高等级；同级按实例 ID 稳定选择。</summary>
    public static CaverSlot Find(Vector3 attackerPosition, PieceController target)
    {
        if (target == null) return null;
        Vector3 start = attackerPosition + Vector3.up * SightHeight;
        Vector3 end = target.transform.position + Vector3.up * SightHeight;
        Vector3 direction = end - start;
        if (direction.sqrMagnitude < 0.000001f) return null;
        float protection = GM.Ins.DM.gameConstSO.CoverProtectionDistance;
        CaverSlot best = null;
        foreach (var hit in Physics.RaycastAll(start, direction.normalized, direction.magnitude,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var cover = hit.collider.GetComponentInParent<CaverSlot>();
            if (cover == null || !cover.ProvidesCover ||
                hit.collider.GetComponentInParent<PieceController>() != null) continue;
            Vector3 closest = hit.collider.ClosestPoint(target.transform.position);
            if ((closest - target.transform.position).sqrMagnitude > protection * protection + 0.00001f) continue;
            if (best == null || cover.Level > best.Level ||
                cover.Level == best.Level && cover.GetInstanceID() < best.GetInstanceID()) best = cover;
        }
        return best;
    }
}
