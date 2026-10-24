using System;
using System.Collections.Generic;

/// <summary>角色的技能学习与配置存档，只保存 ID，不修改共享技能 SO。</summary>
[Serializable]
public sealed class SkillLoadoutState
{
    public bool initialized; // 区分旧存档和玩家主动清空的技能配置。
    public List<string> learned = new();
    public List<string> equipped = new();

    /// <summary>首次初始化继承角色初始技能；已有存档保留学习和配置结果。</summary>
    public void Initialize(PieceData data)
    {
        learned ??= new List<string>();
        equipped ??= new List<string>();
        if (initialized || data?.skillLibrary == null) return;
        foreach (string id in data.initialSkillIds ?? new List<string>())
            if (data.skillLibrary.GetById(id) != null && !learned.Contains(id)) { learned.Add(id); equipped.Add(id); }
        initialized = true;
    }

    /// <summary>学习技能库中的技能；可选择立即加入配置，不重复学习或装配。</summary>
    public bool Learn(string id, SkillPackListSO library, bool equip = false)
    {
        if (library == null || library.GetById(id) == null) return false;
        learned ??= new List<string>();
        equipped ??= new List<string>();
        if (!learned.Contains(id)) learned.Add(id);
        if (equip && !equipped.Contains(id)) equipped.Add(id);
        return true;
    }

    /// <summary>整组替换已配置技能；未学习、失效或重复 ID 会使整次变更失败，允许空配置。</summary>
    public bool Configure(IEnumerable<string> ids, SkillPackListSO library)
    {
        if (ids == null || library == null) return false;
        var next = new List<string>();
        foreach (string id in ids)
        {
            if (library.GetById(id) == null || learned == null || !learned.Contains(id) || next.Contains(id)) return false;
            next.Add(id);
        }
        equipped = next;
        return true;
    }

    /// <summary>解析当前配置的有效技能；缺失的技能配置不会污染存档或共享资源。</summary>
    public List<SkillPack> Resolve(SkillPackListSO library)
    {
        var result = new List<SkillPack>();
        if (library == null || equipped == null) return result;
        foreach (string id in equipped)
        {
            var skill = library.GetById(id);
            if (skill != null && learned != null && learned.Contains(id) && !result.Contains(skill)) result.Add(skill);
        }
        return result;
    }
}
