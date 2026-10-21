using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
// 菜单创建CreateAssetMenu
[CreateAssetMenu(fileName = "SkillPackListSO", menuName = "BattleSO/SkillPackListSO", order = 2)]
public class SkillPackListSO : SerializedScriptableObject
{
    [OdinSerialize]
    public List<SkillPack> skillPacks = new();

    /// <summary>按名称查询正式技能配置，未配置时返回 null。</summary>
    public SkillPack GetSkillPack(string skillName)
    {
        return skillPacks.Find(sp => sp.skillName == skillName);
    }
}
