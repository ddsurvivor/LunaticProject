using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class SkillPack
{
    public string skillName;
    public string description;// 技能描述
    [LabelText("能量消耗")]public int mpCost;
    //public int cooldown;// 冷却时间
    public SkillTarget target;// 作用目标
    public RangeType rangeType;// 作用范围类型
    public float rangeValue;// 范围半径
    [ShowIf("@this.rangeType == RangeType.Fan")]public float rangeAgle;// 范围角度（仅扇形范围有效）
    [ShowIf("@this.rangeType == RangeType.Grenade")]public float explodeRadius;// 爆炸半径（仅爆炸范围有效）
    [ShowIf("@this.rangeType == RangeType.Arc")] public float arcWeight;// 弧线宽度
    //[ShowIf("@this.rangeType == RangeType.Arc")] public float arcRadius;// 弧线半径
    [ShowIf("@this.rangeType == RangeType.Arc")] public float arcCenterDis;// 弧线中心距离（仅弧线范围有效）
    
    public ItemType skillVFXType;// 技能特效类型
    //[ShowIf("@this.skillVFXType != ItemType.NONE")]  
    //public bool isBullet = false; // 是否为子弹技能
    [ShowIf("@this.skillVFXType != ItemType.NONE")] 
    public bool isRotate = false; // 是否旋转子弹特效
    public ItemType bulletVFXType;// 子弹特效类型
    public List<AttackPack> attackPacks = new();// 伤害列表
    public int atkTimes = 1; // 攻击次数
    public List<BuffPack> buffPacks = new();    // 附加效果列表
    public List<SkillEffect> skillEffects = new(); // 技能特殊效果列表
    public AudioClip skillSound;// 施放音效
    public List<ItemPack> consumeItems = new(); // 施放消耗道具列表
    public int animationIndex = 0; // 技能动画索引
    public bool isDelaySkill = false; // 是否为延迟技能
    public bool layerSkill = false; // 是否为单层技能
    [LabelText("模式识别技能")]public bool isRecognitionCheck = false; // 是否进行模式识别检定
    public List<SkillEffectBase> additionalEffects = new(); // 额外效果列表
}

public static class SkillPackExtension
{
    public static string GetSkillDesc(this SkillPack skill)
    {

        // 2. 拼接技能详细信息（消耗、范围、描述等）
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"能量消耗: {skill.mpCost}");
        sb.AppendLine($"作用目标: {(skill.target.ToChinese())}");
        sb.Append($"作用范围: {skill.rangeValue}");

        // 根据技能范围类型，动态追加特有属性
        if (skill.rangeType == RangeType.Fan)
        {
            sb.Append($" (角度: {skill.rangeAgle}°)");
        }
        else if (skill.rangeType == RangeType.Grenade)
        {
            sb.Append($" (爆炸半径: {skill.explodeRadius})");
        }

        sb.AppendLine(); // 换行

        //sb.AppendLine("---------------------------");
        sb.AppendLine($"技能描述: {skill.description}");

        // 3. 新增：在描述末尾动态追加技能伤害信息
        if (skill.attackPacks != null && skill.attackPacks.Count > 0)
        {
            //sb.AppendLine(); // 与描述隔开一行
            //sb.AppendLine("【技能伤害】");

            // 如果只有一段伤害，直接整行输出
            if (skill.attackPacks.Count == 1)
            {
                AttackPack atk = skill.attackPacks[0];
                string critLabel = atk.isCritical ? " (必定暴击)" : "";
                sb.AppendLine(
                    $"造成 <b>{atk.damage}</b> 点 {(atk.damageType.ToChinese())}伤害{critLabel}");
            }
            // 如果有多段伤害（比如复合属性或连击），循环输出每段细节
            else
            {
                for (int i = 0; i < skill.attackPacks.Count; i++)
                {
                    AttackPack atk = skill.attackPacks[i];
                    string critLabel = atk.isCritical ? " (必定暴击)" : "";
                    sb.AppendLine(
                        $"  • 第 {i + 1} 段: <b>{atk.damage}</b> 点 {(atk.damageType.ToChinese())}伤害{critLabel}");
                }
            }

            // 如果攻击次数大于 1，可以额外提示总连击数
            if (skill.atkTimes > 1)
            {
                sb.AppendLine($"总计攻击次数: {skill.atkTimes} 次");
            }
        }

        // 3. 赋值给详细信息 Text
        return sb.ToString();
    }

}

