using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
// 菜单创建CreateAssetMenu
[CreateAssetMenu(fileName = "SkillPackListSO", menuName = "BattleSO/SkillPackListSO", order = 2)]
public class SkillPackListSO : SerializedScriptableObject
{
    public List<SkillPack> skillPacks = new();

    public SkillPack GetSkillPack(string skillName)
    {
        return skillPacks.Find(sp => sp.skillName == skillName) ?? throw new InvalidOperationException($"No SkillPack found with name: {name}");
    }
}