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
        public Image damageIcon;         // 伤害类型图标组件
        public Text damageText;          // 伤害数值/百分比文本组件
    }

    [Header("UI Components - Hit Rate")]
    [SerializeField] private Image hitRateIcon;   // 命中率图标（可在编辑器中固定图片）
    [SerializeField] private Text hitRateText;    // 命中率文本（仅显示百分比数字）

    [Header("UI Components - Damage Slots (Configure 3 types in Editor)")]
    [SerializeField] private List<DamageDisplaySlot> damageSlots; // 在编辑器中配置的3种伤害UI槽位

    [Header("UI Components - Crit Rate & HP")]
    [SerializeField] private Text critRateText;   // 暴击率文本
    public HpBarUI hpBarUI;                       // 生命值显示组件

    /// <summary>
    /// 更新面板信息的公共接口
    /// </summary>
    public void UpdateDisplay(PieceController attacker, SkillPack skillPack, PieceController target)
    {
        if (attacker == null || target == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        Debug.Log($"更新命中信息面板: 攻击者={attacker.name}, 目标={target.name}, 技能={skillPack.skillName}");

        // 1. 计算并刷新命中率
        float hitRate = CalculateHitRate(attacker, skillPack, target);
        hitRateText.text = $"{hitRate:F0}%"; // 移除原本的"命中率:"前缀，只留数字，因为前面已有Icon

        // 2. 计算并刷新各类伤害显示
        List<AttackPack> damagePacks = CalculateDamage(attacker, skillPack, target);
        UpdateDamageSlots(damagePacks);

        // // 3. 计算并刷新暴击率
        // float critRate = CalculateCritRate(attacker, skillPack, target);
        // critRateText.text = $"暴击率: {critRate:F0}%";
        
        // 4. 显示掉血预期结果
        UpdateHpBarPrediction(damagePacks, target);
    }

    /// <summary>
    /// 核心重构：根据计算出的伤害数据包，动态刷新对应的Icon和文本
    /// </summary>
    private void UpdateDamageSlots(List<AttackPack> damagePacks)
    {
        // 步骤 A: 先隐藏所有伤害槽位，避免残留上次的数据显示
        foreach (var slot in damageSlots)
        {
            if (slot.slotRoot != null)
            {
                slot.slotRoot.SetActive(false);
            }
        }

        // 步骤 B: 遍历当前技能产生的伤害包，激活并刷新对应槽位
        foreach (var damagePack in damagePacks)
        {
            // 通过 Linq 或 Find 匹配对应的伤害类型槽位
            DamageDisplaySlot targetSlot = damageSlots.Find(s => s.damageType == damagePack.damageType);
            
            if (targetSlot != null)
            {
                if (targetSlot.slotRoot != null)
                {
                    targetSlot.slotRoot.SetActive(true);
                }

                // 刷新伤害文本。由于你提到需要“百分比数字的形式”：
                targetSlot.damageText.text = $"{damagePack.damage}";
                
                // 如果这里原本是固定伤害数值，误写成了百分比描述，可改回：
                // targetSlot.damageText.text = $"{damagePack.damage:F0}";
                
                // 注：targetSlot.damageIcon 的 Sprite 建议直接在 Hierarchy 对应的物体上挂好，
                // 这样不需要在代码里动态加载图片资产，效率最高。
            }
        }
    }

    /// <summary>
    /// 刷新血条预扣血表现
    /// </summary>
    private void UpdateHpBarPrediction(List<AttackPack> damagePacks, PieceController target)
    {
        float damagePercent = 0f;
        if (damagePacks.Count > 0)
        {
            int totalDamage = 0;
            foreach (var damagePack in damagePacks)
            {
                totalDamage += damagePack.damage;
            }
            damagePercent = (float)totalDamage / target.unitAttrCenter.MaxHealth;
        }
        hpBarUI.ShowPreDamage(damagePercent);
    }

    // --- 战斗计算逻辑保持原样 ---

    private float CalculateHitRate(PieceController a, SkillPack skillPack, PieceController t)
    {
        int result = 100;
        if (a.player.isBursting) result = 100;
        else if (skillPack.target is SkillTarget.EnemyAll or SkillTarget.All or SkillTarget.Self) result = 100;
        else
        {
            float hitRate = a.unitAttrCenter.buffAttrDic[BuffAttrType.HitRate];
            float evade = t.unitAttrCenter.buffAttrDic[BuffAttrType.EvasionRate];
            result = (int)(hitRate - evade);
        }
        return result;
    }

    private List<AttackPack> CalculateDamage(PieceController attacker, SkillPack skillPack, PieceController target)
    {
        List<AttackPack> result = new List<AttackPack>();
        foreach (var attackPack in skillPack.attackPacks)
        {
            int realDamage = attackPack.damage + attacker.unitAttrCenter.ATK;
            int armor = target.unitAttrCenter.attr.GetArmor(attackPack.damageType);
            if (attackPack.damageType == DamageType.Melee)
            {
                armor = (int)(armor * (1f - target.unitAttrCenter.buffAttrDic[BuffAttrType.MeleeArmorPercent] / 100f));
            }

            realDamage -= armor;
            realDamage = (int)(realDamage
                * (100 + attacker.unitAttrCenter.buffAttrDic[BuffAttrType.DamageIncrease]) / 100f 
                * (100 - target.unitAttrCenter.buffAttrDic[BuffAttrType.DamageReduction]) / 100f);

            if (realDamage < 0) realDamage = 0;
            
            result.Add(new AttackPack(realDamage, attackPack.damageType));
        }
        return result;
    }

    private float CalculateCritRate(PieceController a, SkillPack skillPack, PieceController t)
    {
        return a.unitAttrCenter.critRate;
    }

    private void OnDisable()
    {
        if (hpBarUI != null) hpBarUI.ClosePreDamage();
    }
}