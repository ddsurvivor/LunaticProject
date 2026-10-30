using System.Collections.Generic;
using UnityEngine;

/// <summary>头顶与详情共用的只读状态条目；不把旧Buff层数当作精确模因回合。</summary>
public sealed class BattleStatusEntry
{
    public string Name { get; }
    public string Value { get; }
    public string Detail { get; }
    public Sprite Icon { get; }
    public BattleStatusEntry(string name, string value, string detail, Sprite icon = null)
    { Name = name; Value = value; Detail = detail; Icon = icon; }
}

public static class BattleStatusReadModel
{
    /// <summary>读取旧Buff、六种精确模因、网络暴露与本场抗性；不触发随机、解除或计时。</summary>
    public static List<BattleStatusEntry> Read(PieceController piece, BattleManager battle)
    {
        var result = new List<BattleStatusEntry>();
        if (piece == null || piece.unitAttrCenter == null || battle == null) return result;
        var buffs = piece.unitAttrCenter.buffStates.FindAll(buff => buff != null);
        buffs.Sort((a, b) => ((int)a.buffType).CompareTo((int)b.buffType));
        foreach (var buff in buffs)
        {
            var name = buff.buffType.ToChinese();
            string value = buff.buffType == BuffType.SlowingField ? $"{buff.barrierHealth}盾"
                : buff.stacks < 0 ? "永久" : $"{buff.stacks}层";
            Sprite icon = null;
            battle.spriteManager?.buffIconDic?.TryGetValue(buff.buffType, out icon);
            result.Add(new BattleStatusEntry(name, value, buff.stacks < 0 ? "永久状态不会随普通回合自然减少。" : "层数按此状态的规则结算。", icon));
        }
        foreach (var status in battle.timedMemeStatuses.GetStatuses(piece))
        {
            var definition = battle.memeService.Catalog?.GetDefinition(status.MemeId);
            string name = definition?.displayName ?? HandlerName(status.HandlerId);
            string actor = status.Actor != null ? status.Actor.pieceData?.pieceName ?? status.Actor.name : "来源已离场";
            result.Add(new BattleStatusEntry(name, $"{status.RemainingTurns}回", $"目标普通回合；聚能不减时。施加者：{actor}。"));
        }
        if (piece.MemeState.NetworkLinked)
            result.Add(new BattleStatusEntry("网络暴露", $"{piece.MemeState.NetworkSourceCount}源", "仅放宽允许跨类型的词条，不修改身体、剧情标签或抗性。"));
        var catalog = battle.memeService.Catalog;
        if (catalog != null)
            foreach (var definition in catalog.memes)
            {
                if (definition == null) continue;
                int resistance = piece.MemeState.GetResistance(definition.id);
                if (resistance > 0)
                    result.Add(new BattleStatusEntry(definition.displayName + "抗性", $"{resistance}%", resistance >= 100 ? "本场同词条免疫。" : "本场同词条抗性，不叠加强度。"));
            }
        return result;
    }

    private static string HandlerName(string id) => id == TimedMemeStatusService.CellHandler ? "细胞自毁"
        : id == TimedMemeStatusService.HeatHandler ? "散热故障" : id == TimedMemeStatusService.ParalysisHandler ? "麻痹"
        : id == TimedMemeStatusService.ViolenceHandler ? "暴力" : id == TimedMemeStatusService.InversionHandler ? "逆转敌我"
        : id == TimedMemeStatusService.DragonHandler ? "屠龙术" : "模因状态";
}
