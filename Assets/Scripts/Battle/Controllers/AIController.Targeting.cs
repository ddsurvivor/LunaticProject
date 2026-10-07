using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class AIController
{
    /// <summary>正常显示已承诺的攻击计划；移动规划则预览新位置下的威胁选择。</summary>
    public PieceController PreviewEnemyTarget(EnemyController enemy, PieceController previewPiece = null,
        Vector3? previewPosition = null)
    {
        if (enemy == null || enemy.isDead) return null;
        if (previewPiece == null && pendingAttacks.TryGetValue(enemy, out var plan) && IsValidTarget(enemy, plan.Target))
            return plan.Target;
        return CheckEnemyTarget(enemy, previewPiece, previewPosition);
    }

    public PieceController CheckEnemyTarget(EnemyController aiPiece, PieceController previewPiece = null,
        Vector3? previewPosition = null)
    {
        if (aiPiece == null || aiPiece.isDead) return null;
        var candidates = BattleScene.Ins?.BM?.PlayerController?.pieces;
        if (candidates == null) return null;
        PieceController nearest = null, highestDamage = null;
        float minDistance = float.MaxValue;
        int maxDamage = -1;
        // 两次只读遍历，保留原威胁规则和同分顺序；实时预览不逐帧分配字典。
        foreach (var candidate in candidates)
        {
            if (!IsValidTarget(aiPiece, candidate)) continue;
            Vector3 position = candidate == previewPiece && previewPosition.HasValue
                ? previewPosition.Value : candidate.transform.position;
            float distance = Vector3.Distance(position, aiPiece.transform.position);
            if (distance < minDistance) { minDistance = distance; nearest = candidate; }
            if (aiPiece.damageDic.TryGetValue(candidate, out int damage) && damage > maxDamage)
            {
                maxDamage = damage;
                highestDamage = candidate;
            }
        }
        PieceController target = null;
        int highestThreat = int.MinValue;
        foreach (var candidate in candidates)
        {
            if (!IsValidTarget(aiPiece, candidate)) continue;
            int threat = candidate.unitAttrCenter.TauntValue + (candidate == nearest ? 1 : 0) +
                (candidate == highestDamage ? 2 : 0);
            if (threat >= highestThreat) { highestThreat = threat; target = candidate; }
        }
        return target;
    }
}
