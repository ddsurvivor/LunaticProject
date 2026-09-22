using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 严格使用旧版 UI 系统

/// <summary>
/// 命中信息面板：负责在 UI 上显示攻击者与目标之间的预期战斗结果（图文混排版）
/// </summary>
public class HitInfoPanel : MonoBehaviour
{
    [System.Serializable]
    public class DamageDisplaySlot
    {
        public DamageType damageType;    // 伤害类型枚举
        public GameObject slotRoot;      // 槽位的根节点（用于控制整组Icon+Text的显示/隐藏）
        public UnityEngine.UI.Image damageIcon;         // 伤害类型图标组件
        public UnityEngine.UI.Text damageText;          // 伤害数值/百分比文本组件
    }

    [Header("UI Components - Hit Rate")]
    [SerializeField] private UnityEngine.UI.Image hitRateIcon;   // 命中率图标（可在编辑器中固定图片）
    [SerializeField] private UnityEngine.UI.Text hitRateText;    // 命中率文本（仅显示百分比数字）

    [Header("UI Components - Damage Slots (Configure 3 types in Editor)")]
    [SerializeField] private List<DamageDisplaySlot> damageSlots; // 在编辑器中配置的3种伤害UI槽位

    [Header("UI Components - Crit Rate & HP")]
    [SerializeField] private UnityEngine.UI.Text critRateText;   // 暴击率文本
    public HpBarUI hpBarUI;                       // 生命值显示组件

    /// <summary>
    /// 更新面板信息的公共接口
    /// </summary>
    public void UpdateDisplay(PieceController attacker, SkillPack skillPack, PieceController target,
        CheckResult? checkResult = null, bool isFlank = false)
    {
        if (attacker == null || target == null || skillPack == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        // 1. 只向伤害管理器请求预览，UI 不再维护另一套战斗公式。
        var preview = BattleScene.Ins.BM.damageManager.PreviewSkill(attacker, target, skillPack, checkResult, isFlank);
        hitRateText.text = $"{preview.HitRate}%";

        // 2. 同类型多段伤害已合并；固定伤害显示单值，随机伤害显示区间。
        foreach (var slot in damageSlots)
        {
            bool hasDamage = preview.ByType.TryGetValue(slot.damageType, out var range);
            if (slot.slotRoot != null) slot.slotRoot.SetActive(hasDamage);
            if (hasDamage && slot.damageText != null)
                slot.damageText.text = range.MinDamage == range.MaxDamage
                    ? $"{range.MinDamage}" : $"{range.MinDamage}–{range.MaxDamage}";
        }

        // 3. 血条使用同一份合计区间，避免文本与血条预测不一致。
        if (hpBarUI != null) hpBarUI.ShowPreDamage(preview.Total, target.unitAttrCenter.MaxHealth);
    }

    private void OnDisable()
    {
        if (hpBarUI != null) hpBarUI.ClosePreDamage();
    }
}