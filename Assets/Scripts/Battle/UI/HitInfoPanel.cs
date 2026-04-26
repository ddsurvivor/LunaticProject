using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 使用旧版 UI 系统

/// <summary>
/// 命中信息面板：负责在 UI 上显示攻击者与目标之间的预期战斗结果
/// </summary>
public class HitInfoPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Text hitRateText;   // 命中率文本
    [SerializeField] private Text damageText;    // 伤害数值文本
    [SerializeField] private Text critRateText;  // 暴击率文本

   
    

    /// <summary>
    /// 更新面板信息的公共接口
    /// </summary>
    /// <param name="attacker">攻击方控制器</param>
    /// <param name="target">防守方/目标控制器</param>
    public void UpdateDisplay(PieceController attacker, SkillPack skillPack, PieceController target)
    {
        if (attacker == null || target == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        // 显示面板

        // 计算战斗数据（逻辑建议放在单独的战斗计算类中，这里演示直接调用）
        float hitRate = CalculateHitRate(attacker,skillPack, target);
        List<AttackPack> damagePacks = CalculateDamage (attacker, skillPack, target);
        float critRate = CalculateCritRate(attacker,skillPack, target);

        // 刷新文本显示
        hitRateText.text = $"命中率: {hitRate:F0}%"; // 格式化为百分比
        damageText.text = "预计伤害: ";
        foreach (var damagePack in damagePacks)
        {
            damageText.text += $"{damagePack.damage:F0} ({damagePack.damageType.ToChinese()}) | ";
        }
        critRateText.text = $"暴击率: {critRate:F0}%";
    }

    // --- 模拟战斗计算逻辑 ---
    // 在实际项目中，这些算法应由独立的 BattleEngine 或从 ScriptableObject 配置中读取

    private float CalculateHitRate(PieceController a,SkillPack skillPack, PieceController t)
    {
        // 示例：攻击者命中 - 目标闪避
        int result = 100;
        if (a.player.isBursting)
        {
            result = 100; // 聚能状态下必中
        }
        else if (skillPack.target is SkillTarget.EnemyAll
                 or SkillTarget.All or SkillTarget.Self)
        {
            result = 100; // AOE必中
        }
        else
        {
            // 命中率计算公式，D100 <= (攻击方.命中率 - 防御方.闪避率)
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
                armor = (int)(armor * (1f -
                                       target.unitAttrCenter.buffAttrDic[
                                           BuffAttrType.MeleeArmorPercent] / 100f));
            }

            realDamage -= armor;
            // 减伤
            realDamage = (int)(realDamage
                * (100 + attacker.unitAttrCenter.buffAttrDic[
                    BuffAttrType.DamageIncrease]) / 100f * // 伤害增加
                (100 - target.unitAttrCenter.buffAttrDic[
                    BuffAttrType.DamageReduction]) / 100f); // 伤害减免
            /*// 聚能伤害
            if (attacker.player.isBursting)
            {
                realDamage = attacker.player.AddBurstDamage(target, realDamage);
            }*/

            if (realDamage < 0) realDamage = 0;
            
            result.Add(new AttackPack(realDamage, attackPack.damageType));
        }
        
        return result;
    }



    private float CalculateCritRate(PieceController a,SkillPack skillPack, PieceController t)
    {
        // 示例：暴击率
        return a.unitAttrCenter.critRate;
    }
}