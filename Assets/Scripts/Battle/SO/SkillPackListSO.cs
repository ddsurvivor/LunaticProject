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
        return skillPacks?.Find(sp => sp != null && sp.skillName == skillName);
    }

    /// <summary>按稳定 ID 查询技能；空 ID 或不存在的配置返回 null。</summary>
    public SkillPack GetById(string id) => string.IsNullOrEmpty(id) ? null : skillPacks?.Find(s => s != null && s.skillId == id);

    /// <summary>为新技能分配稳定 ID，并导入尚未收录的技能对象；不按名称合并不同配置。</summary>
    public string Register(SkillPack skill)
    {
        if (skill == null) return "";
        skillPacks ??= new List<SkillPack>();
        // 重复 ID 归列表中首个定义所有，仅为后续副本分配新 ID。
        if (string.IsNullOrEmpty(skill.skillId) || skillPacks.Find(s => s != null && s.skillId == skill.skillId) is SkillPack owner && owner != skill)
            skill.skillId = Guid.NewGuid().ToString("N");
        if (!skillPacks.Contains(skill)) skillPacks.Add(skill);
        return skill.skillId;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skillPacks == null) return;
        foreach (var skill in skillPacks.ToArray()) Register(skill);
    }
#endif
}
