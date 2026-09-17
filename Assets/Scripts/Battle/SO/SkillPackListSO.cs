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

    public SkillPack GetSkillPack(string skillName)
    {
        return skillPacks.Find(sp => sp.skillName == skillName) ?? BattleTestSkillFactory.Create(skillName);
    }

    [Button("加入/更新 Buff 测试技能")]
    public void InstallBuffTestSkills()
    {
        foreach (var name in BattleTestSkillFactory.SkillNames)
        {
            var testSkill = BattleTestSkillFactory.Create(name);
            int index = skillPacks.FindIndex(sp => sp.skillName == name);
            if (index < 0) skillPacks.Add(testSkill);
            else skillPacks[index] = testSkill;
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}
