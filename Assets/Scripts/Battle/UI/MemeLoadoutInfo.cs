using System.Collections.Generic;
using System.Text;

/// <summary>模因装配界面的只读说明；展示来源与门槛，不施加效果、不判定概率。</summary>
public static class MemeLoadoutInfo
{
    public static string Name(MemeCatalogSO catalog, int id) => id == 0 ? "空槽" : catalog?.GetDefinition(id)?.displayName ?? "已失效词条";

    /// <summary>分别展示目标、识别、施加概率和抗性门槛，不把施加率称为最终成功率。</summary>
    public static string Describe(MemeDefinition meme)
    {
        if (meme == null) return "选择一个词条，查看效果和触发条件。";
        string body = meme.targetBodies == MemeBodyType.Biological ? "生物" : meme.targetBodies == MemeBodyType.Mechanical ? "机械" : "不限身体类型";
        var text = new StringBuilder(meme.displayName).Append("\n\n").Append(meme.description).Append("\n\n适用目标：").Append(body);
        if (meme.requiredTargetTags != null && meme.requiredTargetTags.Count > 0) text.Append("\n目标标签：").Append(string.Join("、", meme.requiredTargetTags));
        text.Append("\n命中后的施加概率：").Append(meme.chancePercent).Append('%');
        if (meme.requiresRecognitionSuccess) text.Append("\n需要本次技能通过模式识别；普通攻击不会自动追加识别。");
        text.Append("\n目标抗性另行判定。");
        if (!meme.allowReactions) text.Append("\n夹击、警戒反应不触发。");
        return text.ToString();
    }

    /// <summary>列出实际已装配及固有来源；技能和插件不占自由槽，也不会因此解锁。</summary>
    public static string Sources(Player player, PieceData data, PieceController actor, MemeCatalogSO catalog)
    {
        var text = new StringBuilder("装配与固有来源");
        var lines = new HashSet<string>();
        void Add(int id, string source)
        {
            if (id > 0) lines.Add(Name(catalog, id) + " · " + source);
        }
        for (int i = 0; i < MemeEquipment.SlotCount; i++)
            if (player?.memeSlots != null && player.memeSlots.Length > i && GM.Ins?.PLAYERPROFILE?.memeProgress?.IsUnlocked(player.memeSlots[i]) == true)
                Add(player.memeSlots[i], "自由槽 " + (i + 1));
        foreach (int id in data?.innateMemeIds ?? new List<int>()) Add(id, "角色固有");
        var skills = new List<SkillPack>();
        if (actor != null) skills.AddRange(actor.availableSkills);
        else if (player?.skills?.initialized == true) skills.AddRange(player.skills.Resolve(data?.skillLibrary));
        else if (data != null) skills.AddRange(data.DefaultSkills);
        if (data?.MeleeAttack != null) skills.Add(data.MeleeAttack);
        if (data?.RangedAttack != null) skills.Add(data.RangedAttack);
        foreach (var skill in skills)
            foreach (int id in skill?.memeIds ?? new List<int>()) Add(id, "技能 · " + skill.skillName);
        foreach (var component in ComponentEquipment.Equipped(player))
            foreach (int id in component.memeIds ?? new List<int>()) Add(id, "插件 · " + component.itemName);
        var sorted = new List<string>(lines);
        sorted.Sort(System.StringComparer.Ordinal);
        foreach (var line in sorted) text.Append("\n").Append(line);
        if (lines.Count == 0) text.Append("\n暂无；技能和插件固有词条会单独列出。");
        return text.ToString();
    }
}
