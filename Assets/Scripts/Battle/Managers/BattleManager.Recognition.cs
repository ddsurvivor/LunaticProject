using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    private int pendingRecognitionSettlements; // 等待检定展示结束的伤害结算数。
    private int recognitionSettlementVersion; // 场景停用后使旧等待失效，防止重进时补发伤害。

    /// <summary>仍有伤害等待检定展示时保持动作与回合锁定。</summary>
    public bool HasPendingRecognitionSettlement => pendingRecognitionSettlements > 0;

    private IEnumerator WaitForRecognitionSettlement(PieceController attacker, List<PieceController> targets,
        SkillPack skill, Vector3 position, ActionType action, CheckResult check, bool isFlank,
        MemeActionContext memeAction, float passiveMultiplier)
    {
        int version = recognitionSettlementVersion;
        pendingRecognitionSettlements++;
        try
        {
            while (diceCheckManager != null && diceCheckManager.IsPresenting)
            {
                if (version != recognitionSettlementVersion || !isActiveAndEnabled || attacker == null ||
                    (!skill.AllowDeadCaster && (!attacker.isActiveAndEnabled || attacker.isDead))) yield break;
                yield return null;
            }
            if (version != recognitionSettlementVersion || !isActiveAndEnabled || attacker == null ||
                (!skill.AllowDeadCaster && (!attacker.isActiveAndEnabled || attacker.isDead))) yield break;
            // 复用已消费的攻击被动与检定结果；PieceSkill 会重新过滤失效目标。
            PieceSkill(attacker, targets, skill, position, action, check, isFlank, memeAction, passiveMultiplier);
        }
        finally
        {
            if (version == recognitionSettlementVersion) pendingRecognitionSettlements--;
        }
    }

    private void CancelRecognitionSettlements()
    {
        recognitionSettlementVersion++;
        pendingRecognitionSettlements = 0;
    }
}
