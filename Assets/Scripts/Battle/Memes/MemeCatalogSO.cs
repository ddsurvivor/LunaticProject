using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MemeCatalog", menuName = "Game/MemeCatalog")]
public sealed class MemeCatalogSO : ScriptableObject
{
    public List<MemeDefinition> memes = new();

    /// <summary>重复编号视为无效，避免配置列表顺序改变实际使用的词条。</summary>
    public MemeDefinition GetDefinition(int id)
    {
        if (id <= 0 || memes == null) return null;
        MemeDefinition found = null;
        foreach (var meme in memes)
        {
            if (meme == null || meme.id != id) continue;
            if (found != null) return null;
            found = meme;
        }
        return found;
    }

    /// <summary>供配置工具及构建检查报告无效或重复词条；禁用占位词条可以没有效果。</summary>
    public List<string> ValidateConfiguration()
    {
        var issues = new List<string>();
        var ids = new HashSet<int>();
        if (memes == null) { issues.Add("模因目录为空"); return issues; }
        foreach (var meme in memes)
        {
            if (meme == null) { issues.Add("模因目录包含空条目"); continue; }
            if (meme.id <= 0 || !ids.Add(meme.id)) issues.Add($"模因编号无效或重复：{meme.id}");
            if (meme.enabled && !meme.IsValid()) issues.Add($"模因 {meme.id} 的目标、概率或效果配置无效");
        }
        return issues;
    }

    private void OnValidate()
    {
        foreach (string issue in ValidateConfiguration()) Debug.LogWarning(issue, this);
    }
}
