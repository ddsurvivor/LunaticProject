using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct MemeEffectContext
{
    public PieceController Actor { get; }
    public PieceController Target { get; }
    public MemeDefinition Meme { get; }
    public MemeSourceKind Sources { get; }
    public MemeEffectContext(PieceController actor, PieceController target, MemeDefinition meme, MemeSourceKind sources)
    { Actor = actor; Target = target; Meme = meme; Sources = sources; }
}

/// <summary>自定义模因效果入口；CanApply 必须只读，Apply 不得重新判定词条概率。</summary>
public interface IMemeEffectHandler
{
    bool CanApply(MemeEffectContext context, MemeEffectDefinition effect);
    void Apply(MemeEffectContext context, MemeEffectDefinition effect);
}

/// <summary>由 BattleManager 持有，连接存档装配、纯规则判定和实际 Buff 效果。</summary>
public sealed class MemeService
{
    private readonly BattleManager battle;
    private readonly MemeResolver resolver;
    private readonly Dictionary<string, IMemeEffectHandler> handlers = new(StringComparer.Ordinal);
    public MemeCatalogSO Catalog => battle.memeCatalog != null ? battle.memeCatalog : GM.Ins?.DM?.memeCatalog;

    /// <summary>完成单个词条判定和效果提交后通知；预览不发送该事件。</summary>
    public event Action<MemeEffectContext, MemeAttempt> AttemptResolved;

    public MemeService(BattleManager battle, Func<int, bool> rollPercent = null)
    {
        this.battle = battle ?? throw new ArgumentNullException(nameof(battle));
        resolver = new MemeResolver(rollPercent ?? (chance => UnityEngine.Random.Range(0, 100) < chance));
    }

    /// <summary>注册复杂行为处理器；重复编号会拒绝，防止悄悄替换本场规则。</summary>
    public bool RegisterEffectHandler(string id, IMemeEffectHandler handler)
    {
        if (string.IsNullOrWhiteSpace(id) || handler == null || handlers.ContainsKey(id)) return false;
        handlers.Add(id, handler);
        return true;
    }

    /// <summary>读取当前装配与固有来源，合并重复词条；不解锁、不占用额外自由槽位。</summary>
    public MemeActionContext BeginAction(PieceController actor, SkillPack skill, bool isReaction = false)
    {
        var sources = new List<MemeSource>();
        if (actor == null || skill == null) return new MemeActionContext(sources, isReaction);
        var progress = GM.Ins?.PLAYERPROFILE?.memeProgress;
        var slots = actor.playerData?.memeSlots;
        if (slots != null)
            for (int i = 0; i < Math.Min(slots.Length, MemeEquipment.SlotCount); i++)
                if (progress != null && progress.IsUnlocked(slots[i])) sources.Add(new MemeSource(slots[i], MemeSourceKind.Equipped));
        AddSources(sources, actor.pieceData?.innateMemeIds, MemeSourceKind.Innate);
        AddSources(sources, skill.memeIds, MemeSourceKind.Skill);
        if (actor.playerData != null)
            foreach (var component in ComponentEquipment.Equipped(actor.playerData))
                AddSources(sources, component.memeIds, MemeSourceKind.Component);
        return new MemeActionContext(sources, isReaction);
    }

    /// <summary>在一次已命中或已完成支援的目标结算后调用；死亡目标不再接收新状态。</summary>
    public IReadOnlyList<MemeAttempt> ResolveTarget(PieceController actor, PieceController target, SkillPack skill,
        CheckResult check, MemeActionContext action)
    {
        return ProcessTarget(actor, target, skill, check, action, false);
    }

    /// <summary>只读列出目标门槛、免疫和触发概率；概率以技能成功命中或支援完成为前提。</summary>
    public IReadOnlyList<MemeAttempt> PreviewTarget(PieceController actor, PieceController target, SkillPack skill,
        CheckResult check = CheckResult.None, bool isReaction = false)
    {
        return ProcessTarget(actor, target, skill, check, BeginAction(actor, skill, isReaction), true);
    }

    /// <summary>当前可控角色确认换装时调用；无变化、无效装配或状态中断均不替换槽位。</summary>
    public bool TryEquipInBattle(PieceController actor, IReadOnlyList<int> slots, out string error)
    {
        error = null;
        if (actor == null || actor.playerData == null || !actor.isPlayerPiece || !battle.CanControlPiece(actor) ||
            battle.PlayerController == null || !battle.PlayerController.pieces.Contains(actor) || actor.IsUsingSkill ||
            battle.HasExternalPlayerControl)
        { error = "当前角色不能配置模因"; return false; }
        var progress = GM.Ins?.PLAYERPROFILE?.memeProgress;
        if (!MemeEquipment.Validate(Catalog, progress, slots, out error)) return false;
        if (MemeEquipment.SameSlots(actor.playerData.memeSlots, slots)) { error = "模因配置没有变化"; return false; }
        // 先快照整套配置，再调用已有费用入口；行动中断保留其正常扣费语义。
        var replacement = MemeEquipment.CopySlots(slots);
        if (!actor.unitAttrCenter.CostMP(MemeEquipment.BattleEquipCost))
        { error = "行动点不足或配置行动被状态中断"; return false; }
        actor.playerData.memeSlots = replacement;
        return true;
    }

    private IReadOnlyList<MemeAttempt> ProcessTarget(PieceController actor, PieceController target, SkillPack skill,
        CheckResult check, MemeActionContext action, bool preview)
    {
        var attempts = new List<MemeAttempt>();
        if (actor == null || actor.isDead || target == null || target.isDead || skill == null || action == null) return attempts;
        var catalog = Catalog;
        foreach (var source in action.Sources)
        {
            var meme = catalog != null ? catalog.GetDefinition(source.Id) : null;
            var context = new MemeEffectContext(actor, target, meme, source.Kind);
            var targetContext = BuildTarget(actor, target, skill, check, source.Id);
            MemeOutcome outcome = resolver.Preview(meme, targetContext, action);
            if (outcome == MemeOutcome.Eligible && !CanApply(context)) outcome = MemeOutcome.UnsupportedEffect;
            if (!preview && outcome == MemeOutcome.Eligible)
            {
                outcome = resolver.Resolve(meme, targetContext, action);
                if (outcome == MemeOutcome.Applied) ApplyEffects(context);
            }
            int chance = meme == null ? 0 : Math.Max(0, Math.Min(100, meme.chancePercent));
            int resistance = Math.Max(0, Math.Min(100, targetContext.resistancePercent));
            var attempt = new MemeAttempt(source.Id, source.Kind, outcome, chance * (100 - resistance) / 100f);
            attempts.Add(attempt);
            if (!preview) AttemptResolved?.Invoke(context, attempt);
            if (actor.isDead || target.isDead) break;
        }
        return attempts.AsReadOnly();
    }

    private static MemeTargetContext BuildTarget(PieceController actor, PieceController target, SkillPack skill, CheckResult check, int id)
    {
        return new MemeTargetContext
        {
            targetId = target.GetInstanceID(), bodyType = target.pieceData?.memeBodyType ?? MemeBodyType.None,
            tags = target.pieceData?.memeTargetTags, networkLinked = target.MemeState.NetworkLinked,
            friendly = actor.isPlayerPiece == target.isPlayerPiece,
            trigger = skill.attackPacks != null && skill.attackPacks.Count > 0 ? MemeTrigger.AttackHit : MemeTrigger.SupportResolved,
            recognitionPassed = skill.isRecognitionCheck && (check == CheckResult.DamageIncreased || check == CheckResult.MustCrit),
            resistancePercent = target.MemeState.GetResistance(id)
        };
    }

    private bool CanApply(MemeEffectContext context)
    {
        foreach (var effect in context.Meme.effects)
        {
            if (effect.kind == MemeEffectKind.Buff)
            {
                if (battle.buffManager == null || context.Target.unitAttrCenter == null) return false;
            }
            else if (!handlers.TryGetValue(effect.handlerId, out var handler) || !handler.CanApply(context, effect)) return false;
        }
        return true;
    }

    private void ApplyEffects(MemeEffectContext context)
    {
        foreach (var effect in context.Meme.effects)
        {
            if (context.Target.isDead) break;
            if (effect.kind == MemeEffectKind.Buff)
                battle.buffManager.AddBuff(context.Target.unitAttrCenter, effect.buffType, effect.stacks, effect.barrierHealth, effect.retaliationTurns);
            else handlers[effect.handlerId].Apply(context, effect);
        }
    }

    private static void AddSources(List<MemeSource> sources, IEnumerable<int> ids, MemeSourceKind kind)
    {
        if (ids == null) return;
        foreach (int id in ids) sources.Add(new MemeSource(id, kind));
    }
}
