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
    [ShowIf("@this.skillVFXType != ItemType.NONE")]  
    public bool isBullet = false; // 是否为子弹技能
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


/// <summary>
/// 技能特殊效果基类，所有技能特殊效果都继承自这个类
/// </summary>
public class SkillEffectBase
{
    
}

/// <summary>
/// 击退效果，造成伤害的同时将目标击退一定距离
/// </summary>
public class HitBackEffect : SkillEffectBase
{
    [LabelText("击退距离")]
    public float dis;// 击退距离
    [LabelText("碰撞伤害")]
    public int hitBackDamage;// 击退伤害
}

public class ShootFxEffect : SkillEffectBase
{
    public ItemType shootFxType;// 射击特效类型
    public bool isRotate;
    
    public void ApplyEffect(PieceController attacker, Vector3 targetPos)
    {
        // 在射击点生成特效
        GameObject fx = ObjectPool.Ins.GenerateObject(shootFxType, targetPos, Quaternion.identity);
        // 根据设置决定是否旋转特效
        
        
        if (isRotate)
        {
            //z轴旋转，从attacker 指向targetPos
            Vector3 direction = targetPos - attacker.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            fx.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            Debug.Log($"生成射击特效 {shootFxType} at {targetPos}, rotate: {angle}");
        }
        else
        {
            fx.transform.rotation = Quaternion.identity;
        }
        
    }
}

public class SelfExplosionEffect : SkillEffectBase
{
    public void ApplyEffect(PieceController attacker)
    {
        // 对自身造成伤害
        int selfDamage = 100; // 示例伤害值，可以根据需要调整
        attacker.unitAttrCenter.TakeDamage(new AttackPack(selfDamage,DamageType.Ranged));
    }
}