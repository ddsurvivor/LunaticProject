using System;
using System.Collections.Generic;

[Serializable]
public sealed class MemeProgress
{
    public List<int> unlockedIds = new();

    public bool IsUnlocked(int id) => id > 0 && unlockedIds != null && unlockedIds.Contains(id);

    /// <summary>由成长、剧情或配方奖励永久解锁；重复解锁不重复写入。</summary>
    public bool TryUnlock(MemeCatalogSO catalog, int id)
    {
        var definition = catalog != null ? catalog.GetDefinition(id) : null;
        if (definition == null || !definition.enabled || !definition.IsValid() || IsUnlocked(id)) return false;
        unlockedIds ??= new List<int>();
        unlockedIds.Add(id);
        return true;
    }
}

public static class MemeEquipment
{
    public const int SlotCount = 2;
    public const int BattleEquipCost = 2; // 一次确认替换整套自由槽位的 AP 费用。

    /// <summary>只读验证整套槽位；0 为空槽，不会修正或写回旧存档。</summary>
    public static bool Validate(MemeCatalogSO catalog, MemeProgress progress, IReadOnlyList<int> slots, out string error)
    {
        error = null;
        if (slots == null || slots.Count != SlotCount) { error = "必须提交两个模因槽位"; return false; }
        var seen = new HashSet<int>();
        foreach (int id in slots)
        {
            if (id == 0) continue;
            var definition = catalog != null ? catalog.GetDefinition(id) : null;
            if (definition == null || !definition.enabled || !definition.IsValid()) { error = $"模因 {id} 不可用"; return false; }
            if (progress == null || !progress.IsUnlocked(id)) { error = $"模因 {id} 尚未解锁"; return false; }
            if (!seen.Add(id)) { error = "同一模因不能重复装配"; return false; }
        }
        return true;
    }

    /// <summary>队伍菜单的免费装配入口；战斗进行中必须改用 MemeService.TryEquipInBattle。</summary>
    public static bool TryEquipOutsideBattle(Player player, MemeProgress progress, MemeCatalogSO catalog,
        IReadOnlyList<int> slots, out string error)
    {
        error = null;
        if (BattleScene.Ins != null && BattleScene.Ins.BM != null && BattleScene.Ins.BM.IsBattleRunning)
        { error = "战斗中配置模因需要消耗行动点"; return false; }
        if (player == null) { error = "角色不存在"; return false; }
        if (!Validate(catalog, progress, slots, out error)) return false;
        player.memeSlots = CopySlots(slots);
        return true;
    }

    internal static int[] CopySlots(IReadOnlyList<int> slots) => new[] { slots[0], slots[1] };

    internal static bool SameSlots(int[] current, IReadOnlyList<int> slots)
    {
        for (int i = 0; i < SlotCount; i++)
            if ((current != null && current.Length > i ? current[i] : 0) != slots[i]) return false;
        return true;
    }
}
