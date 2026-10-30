using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum MemeCureTag { None = 0, Vaccine = 1, Medical = 2, Update = 4, TrueShadow = 8 }

/// <summary>精确模因的运行状态；持续、强度和施加来源独立，不复用旧Buff层数。</summary>
public sealed class TimedMemeStatus
{
    public int MemeId { get; internal set; }
    public string HandlerId { get; internal set; }
    public int RemainingTurns { get; internal set; }
    public float Magnitude { get; internal set; }
    public PieceController Actor { get; internal set; }
    public MemeSourceKind Sources { get; internal set; }
    public BattleActionContext ApplicationAction { get; internal set; }
    internal int Revision; // 刷新使旧回合快照失效，避免当回合立即减时或追加DOT。
}

/// <summary>本场精确状态：普通回合快照计时、付费主动行动伤害及指定标签解除。</summary>
public sealed class TimedMemeStatusService
{
    public const string CellHandler = "cellular-self-destruction";
    public const string HeatHandler = "heat-malfunction";
    public const string ParalysisHandler = "paralysis";
    public const string ViolenceHandler = "violence";
    public const string InversionHandler = "inverted-allegiance";
    public const string DragonHandler = "dragon-slaying";
    private readonly Dictionary<PieceController, List<TimedMemeStatus>> states = new();
    private readonly Dictionary<PieceController, List<TurnEntry>> turns = new(); // 仅保存正在进行的普通回合快照。

    private sealed class TurnEntry
    {
        internal TimedMemeStatus Status;
        internal int Revision;
        internal bool Ticked;
    }

    /// <summary>只读查询当前状态；返回列表副本，调用者不能修改服务持有的集合。</summary>
    public IReadOnlyList<TimedMemeStatus> GetStatuses(PieceController target) => target != null && states.TryGetValue(target, out var list)
        ? list.ToArray() : Array.Empty<TimedMemeStatus>();

    internal IMemeEffectHandler CreateHandler(string id) => new Handler(this, id);

    private sealed class Handler : IMemeEffectHandler
    {
        private readonly TimedMemeStatusService owner;
        private readonly string id;
        internal Handler(TimedMemeStatusService owner, string id) { this.owner = owner; this.id = id; }
        public bool CanApply(MemeEffectContext context, MemeEffectDefinition effect) => context.Target != null &&
            !context.Target.isDead && context.Target.unitAttrCenter != null && effect.handlerId == id &&
            effect.durationTurns == Duration(id) && Mathf.Approximately(effect.magnitude, Magnitude(id));
        public void Apply(MemeEffectContext context, MemeEffectDefinition effect) => owner.Apply(context, effect);
    }

    private static int Duration(string id) => id == CellHandler ? 5 : id == HeatHandler || id == InversionHandler ? 3 : 4;
    private static float Magnitude(string id) => id == CellHandler || id == HeatHandler ? .1f : id == ParalysisHandler ? .3f : 0f;

    private void Apply(MemeEffectContext context, MemeEffectDefinition effect)
    {
        var target = context.Target;
        if (!states.TryGetValue(target, out var list)) states.Add(target, list = new List<TimedMemeStatus>());
        var status = list.Find(s => s.HandlerId == effect.handlerId);
        if (status == null) list.Add(status = new TimedMemeStatus { HandlerId = effect.handlerId });
        BattleScene.Ins?.BM?.ClearMoveUndo();
        status.Revision++;
        status.MemeId = context.Meme.id;
        status.RemainingTurns = effect.durationTurns;
        status.Magnitude = effect.magnitude;
        status.Actor = context.Actor;
        status.Sources = context.Sources;
        status.ApplicationAction = context.Action ?? BattleActionContext.Current ?? new BattleActionContext(context.Actor);
        RefreshState(target, list);
    }

    /// <summary>在目标普通TurnStart事件前快照；当回合事件中新施加或刷新的状态不进入本轮计时。</summary>
    public void BeginNormalTurn(PieceController target)
    {
        if (target == null || turns.ContainsKey(target)) return;
        var snapshot = new List<TurnEntry>();
        if (states.TryGetValue(target, out var list))
            foreach (var status in list) snapshot.Add(new TurnEntry { Status = status, Revision = status.Revision });
        turns.Add(target, snapshot);
    }

    /// <summary>旧回合开始Buff结算后调用；仅快照内的细胞自毁扣血，每轮至多一次，死亡不继续结算。</summary>
    public void ResolveTurnStart(PieceController target)
    {
        if (target == null || target.isDead || !turns.TryGetValue(target, out var snapshot)) return;
        foreach (var entry in snapshot.ToArray())
        {
            if (target.isDead) break;
            if (entry.Ticked || !IsCurrent(target, entry)) continue;
            entry.Ticked = true;
            if (entry.Status.HandlerId == CellHandler) Damage(target, entry.Status, BattleDamageKind.Periodic);
        }
    }

    /// <summary>普通TurnEnd事件后减时；聚能额外行动不调用，刷新或重新施加不消费旧快照。</summary>
    public void EndNormalTurn(PieceController target)
    {
        if (target == null || !turns.TryGetValue(target, out var snapshot)) return;
        turns.Remove(target);
        if (target.isDead) { ClearForPiece(target); return; }
        foreach (var entry in snapshot)
            if (IsCurrent(target, entry) && --entry.Status.RemainingTurns <= 0) Remove(target, entry.Status, true);
    }

    /// <summary>成功扣除正AP后、执行动作前调用；结束回合和免费反应不调用，先散热后麻痹，致死或麻痹取消时返回false且不退AP。</summary>
    public bool BeforePaidAction(PieceController target)
    {
        if (target == null || target.isDead) return false;
        if (states.TryGetValue(target, out var list))
            foreach (var status in list.ToArray())
            {
                if (target.isDead) break;
                if (status.HandlerId == HeatHandler && states.TryGetValue(target, out var current) &&
                    ReferenceEquals(current, list) && list.Contains(status)) Damage(target, status, BattleDamageKind.ActionEffect);
            }
        if (target.isDead) return false;
        return !target.MemeState.Paralyzed || BattleRandom.Range(0, 100) >= 30;
    }

    /// <summary>生命提交后、伤害观察者及本次新状态施加前解除旧暴力/逆转；纯自伤与无生命损失不解除。</summary>
    public void OnDamageCommitted(BattleDamageResult result)
    {
        if (result == null || result.Ignored || result.HpLoss <= 0 || result.Source.Kind == BattleDamageKind.SelfDamage ||
            result.Source.Action.Actor == result.Target || !states.TryGetValue(result.Target, out var list)) return;
        foreach (var status in list.ToArray())
            if (status.HandlerId == ViolenceHandler || status.HandlerId == InversionHandler) Remove(result.Target, status, false);
    }

    /// <summary>AI只读查询指定解除标签；已解除和没有对应状态返回None。</summary>
    public MemeCureTag NeededCures(PieceController target)
    {
        var tags = MemeCureTag.None;
        if (target != null && states.TryGetValue(target, out var list))
            foreach (var status in list) tags |= CureTags(status.HandlerId);
        return tags;
    }

    private static MemeCureTag CureTags(string id) => id == HeatHandler || id == InversionHandler ? MemeCureTag.Update :
        id == DragonHandler ? MemeCureTag.TrueShadow : MemeCureTag.Vaccine | MemeCureTag.Medical;

    /// <summary>只解除标签匹配的新状态；普通回血、无标签净化与重复解除不产生恢复抗性或额外回血。</summary>
    public int TryCure(PieceController target, MemeCureTag tags)
    {
        if (target == null || target.isDead || !states.TryGetValue(target, out var list)) return 0;
        int removed = 0;
        foreach (var status in list.ToArray())
        {
            var allowed = CureTags(status.HandlerId);
            if ((tags & allowed) == 0) continue;
            Remove(target, status, true);
            removed++;
        }
        return removed;
    }

    /// <summary>初始化、死亡和销毁只清理状态及快照，不登记恢复抗性，也不改变其他模因抗性。</summary>
    public void ClearForPiece(PieceController target)
    {
        if (ReferenceEquals(target, null)) return;
        states.Remove(target);
        turns.Remove(target);
        if (target != null) RefreshState(target, new List<TimedMemeStatus>());
    }

    /// <summary>战斗禁用或结束时释放本场状态；不将清理视为自然恢复。</summary>
    public void ClearAll()
    {
        foreach (var target in new List<PieceController>(states.Keys)) ClearForPiece(target);
        turns.Clear();
    }

    private bool IsCurrent(PieceController target, TurnEntry entry) => entry.Revision == entry.Status.Revision &&
        states.TryGetValue(target, out var list) && list.Contains(entry.Status);

    private void Remove(PieceController target, TimedMemeStatus status, bool recovered)
    {
        if (!states.TryGetValue(target, out var list) || !list.Remove(status)) return;
        BattleScene.Ins?.BM?.ClearMoveUndo();
        if (recovered && (status.HandlerId == HeatHandler || status.HandlerId == DragonHandler))
            target.MemeState.SetResistance(status.MemeId, Math.Max(50, target.MemeState.GetResistance(status.MemeId)));
        RefreshState(target, list);
        if (list.Count == 0) states.Remove(target);
    }

    private static void RefreshState(PieceController target, List<TimedMemeStatus> list)
    {
        var state = target.MemeState;
        state.TimedMoveMultiplier = list.Exists(s => s.HandlerId == CellHandler) ? .8f : 1f;
        state.Paralyzed = list.Exists(s => s.HandlerId == ParalysisHandler);
        state.Violent = list.Exists(s => s.HandlerId == ViolenceHandler);
        state.Inverted = list.Exists(s => s.HandlerId == InversionHandler);
        state.AttackForbidden = list.Exists(s => s.HandlerId == DragonHandler);
    }

    private static void Damage(PieceController target, TimedMemeStatus status, BattleDamageKind kind)
    {
        // 沿用施加者、原技能和根行动；不伪造一次普通命中或触发旧攻击被动。
        var source = new BattleDamageSource(kind, status.Actor, status.ApplicationAction.Skill, status.ApplicationAction);
        target.unitAttrCenter.TakeDamage(new AttackPack(Mathf.CeilToInt(target.unitAttrCenter.MaxHealth * status.Magnitude), DamageType.Electric), source);
    }
}

/// <summary>已确认B10/B11配置模板；编号由正式目录分配，不自动赋予角色或解锁。</summary>
public static class TimedMemeDefinitions
{
    /// <summary>创建已确认的细胞自毁模板；调用者须提供目录中未占用的稳定编号。</summary>
    public static MemeDefinition Cell(int id) => Create(id, "细胞自毁", MemeBodyType.Biological,
        TimedMemeStatusService.CellHandler, 5, 15, false);
    /// <summary>创建已确认的散热故障模板；沿用现行显式模式识别成功门槛。</summary>
    public static MemeDefinition Heat(int id) => Create(id, "散热故障", MemeBodyType.Mechanical,
        TimedMemeStatusService.HeatHandler, 3, 100, true);
    /// <summary>创建麻痹模板：30%施加，4普通回合，每次付费主动行动30%取消。</summary>
    public static MemeDefinition Paralysis(int id) => Create(id, "麻痹", MemeBodyType.Biological, TimedMemeStatusService.ParalysisHandler, 4, 30, false, .3f);
    /// <summary>创建暴力模板：生物15%施加，4回合，实际外来掉血或指定治疗解除。</summary>
    public static MemeDefinition Violence(int id) => Create(id, "暴力", MemeBodyType.Biological, TimedMemeStatusService.ViolenceHandler, 4, 15, false, 0f);
    /// <summary>创建逆转敌我模板：机械显式识别成功，3回合，不更改原阵营。</summary>
    public static MemeDefinition Inversion(int id) => Create(id, "逆转敌我", MemeBodyType.Mechanical, TimedMemeStatusService.InversionHandler, 3, 100, true, 0f);
    /// <summary>创建屠龙术模板；黑衣人标签由正式配置传入，空标签将使定义无效。</summary>
    public static MemeDefinition Dragon(int id, string blackSuitTag)
    {
        var meme = Create(id, "屠龙术1.0", MemeBodyType.All, TimedMemeStatusService.DragonHandler, 4, 15, false, 0f);
        meme.requiredTargetTags.Add(blackSuitTag);
        return meme;
    }
    private static MemeDefinition Create(int id, string name, MemeBodyType body, string handler, int turns, int chance, bool recognition, float magnitude = .1f) =>
        new MemeDefinition { id = id, displayName = name, targetBodies = body, chancePercent = chance,
            requiresRecognitionSuccess = recognition, effects = new List<MemeEffectDefinition> {
                new MemeEffectDefinition { kind = MemeEffectKind.Custom, handlerId = handler, durationTurns = turns, magnitude = magnitude } } };
}
