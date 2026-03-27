using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum ComponentType { [LabelText("普通插件")]Normal, [LabelText("武器插件")]Weapon }

public enum ComponentEffect
{
    // 属性加成
    [LabelText("属性加成")]
    AttrBonus = 0,
    // 被动效果
    [LabelText("被动效果")]
    PassiveEffect = 100,
    // 主动技能
    [LabelText("主动技能")]
    ActiveSkill = 200
}

[System.Serializable]
public class ComponentData
{
    public int id; 
    public string itemName;
    public ComponentType type;
    public Sprite icon;
    [TextArea] public string description;
    
    public ComponentEffect effectType;
    [ShowIf("@this.effectType == ComponentEffect.AttrBonus")][LabelText("属性编号")]
    public int attrId;
    [ShowIf("@this.effectType == ComponentEffect.AttrBonus")][LabelText("属性加成值")]
    public int attrValue;
    [ShowIf("@this.effectType == ComponentEffect.PassiveEffect")][LabelText("被动技能id")]
    public int passiveEffectId; // 被动效果ID
    [ShowIf("@this.effectType == ComponentEffect.ActiveSkill")][LabelText("主动技能")]
    public SkillPack skillPack; // 主动技能数据包
}

[CreateAssetMenu(fileName = "ComponentConfig", menuName = "Game/ComponentConfig")]
public class ComponentConfig : ScriptableObject
{
    
    public List<ComponentData> componentList = new List<ComponentData>();

    public ComponentData GetData(int id)
    {
        // 如果 ID 为 0 或未找到，返回 null
        if (id < 0) return null;
        return componentList.Find(c => c.id == id);
    }
}