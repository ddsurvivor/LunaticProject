using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class SkillPack
{
    public string skillName;
    public string description;// 技能描述
    public int mpCost;
    public int cooldown;
    public SkillTarget target;// 作用目标
    public RangeType rangeType;// 作用范围类型
    public float rangeValue;// 范围半径
    [ShowIf("@this.rangeType == RangeType.Fan")]public float rangeAgle;// 范围角度（仅扇形范围有效）
    [ShowIf("@this.rangeType == RangeType.Grenade")]public float explodeRadius;// 爆炸半径（仅爆炸范围有效）
    public ItemType skillVFXType;// 技能特效类型
    public List<AttackPack> attackPacks = new();// 伤害列表
    public List<BuffPack> buffPacks = new();    // 附加效果列表
    public List<SkillEffect> skillEffects = new(); // 技能特殊效果列表
    public AudioClip skillSound;// 施放音效
    
}