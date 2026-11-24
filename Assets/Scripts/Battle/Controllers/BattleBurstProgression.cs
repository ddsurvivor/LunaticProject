using System;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>随玩家档案保存的动力脊进度；设施费用与解锁条件由升级调用方先行校验。</summary>
[Serializable]
public sealed class PowerSpineProgress
{
    public int level; // 永久设施等级，兼容缺字段及越界旧数据。
    /// <summary>供战斗读取的有效设施等级，始终限制在 0–3。</summary>
    [JsonIgnore] public int Level => Math.Max(0, Math.Min(3, level));

    /// <summary>提升一级，满级返回 false；不扣费用、不写磁盘，由外部设施流程提交存档。</summary>
    public bool TryUpgrade()
    {
        if (Level >= 3) return false;
        level = Level + 1;
        return true;
    }
}

public enum BattleBurstStageKind { Unspecified, Normal, Prologue, Tutorial }

[Serializable]
public sealed class BattleBurstStageEntry
{
    public string sceneName;
    public BattleBurstStageKind stageKind;
}

/// <summary>以场景标识绑定聚能关卡规则，不根据玩家章节进度推断。</summary>
[Serializable]
public sealed class BattleBurstStageCatalog
{
    public BattleBurstStageEntry[] entries;

    /// <summary>按精确场景标识查找规则，缺失或无效条目按普通关处理。</summary>
    public BattleBurstStageKind Resolve(string sceneName)
    {
        if (entries != null)
            foreach (var entry in entries)
                if (entry != null && string.Equals(entry.sceneName, sceneName, StringComparison.Ordinal) &&
                    entry.stageKind >= BattleBurstStageKind.Normal && entry.stageKind <= BattleBurstStageKind.Tutorial)
                    return entry.stageKind;
        return BattleBurstStageKind.Normal;
    }
}

public static class BattleBurstProgression
{
    private static BattleBurstStageCatalog catalog; // 只读资源目录，场景重开不改变绑定。

    /// <summary>从设施进度初始化并清空本场能量/冷却；显式关卡类型优先于目录，旧强制标志始终使用基础数值。</summary>
    public static int Initialize(BurstGauge gauge, PowerSpineProgress progress, string sceneName,
        BattleBurstStageKind stageKind = BattleBurstStageKind.Unspecified, bool forceBase = false)
    {
        if (catalog == null)
        {
            var resource = Resources.Load<TextAsset>("Battle/BurstStages");
            catalog = resource != null ? JsonUtility.FromJson<BattleBurstStageCatalog>(resource.text) : null;
            catalog ??= new BattleBurstStageCatalog();
        }
        if (stageKind < BattleBurstStageKind.Normal || stageKind > BattleBurstStageKind.Tutorial)
            stageKind = catalog.Resolve(sceneName);
        int level = progress?.Level ?? 0; // 缺失设施数据回退未升级，不沿用场景里残留的调试等级。
        gauge.Reset(level, forceBase || stageKind == BattleBurstStageKind.Prologue || stageKind == BattleBurstStageKind.Tutorial);
        return level;
    }
}
