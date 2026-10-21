using System;
using System.Collections.Generic;

/// <summary>一次技能结算内的模因来源和判定记录；不能跨行动复用。</summary>
public sealed class MemeActionContext
{
    private readonly Dictionary<int, MemeSourceKind> sources = new();
    private readonly HashSet<(int target, int meme)> attempted = new(); // 每个目标每种模因只判定一次。
    public bool IsReaction { get; }
    public IReadOnlyList<MemeSource> Sources { get; }

    public MemeActionContext(IEnumerable<MemeSource> entries, bool isReaction = false)
    {
        IsReaction = isReaction;
        if (entries != null)
            foreach (var source in entries)
            {
                if (source.Id <= 0) continue;
                sources.TryGetValue(source.Id, out var previous);
                sources[source.Id] = previous | source.Kind;
            }
        var ids = new List<int>(sources.Keys);
        ids.Sort();
        var snapshot = new List<MemeSource>();
        foreach (int id in ids) snapshot.Add(new MemeSource(id, sources[id]));
        Sources = snapshot.AsReadOnly();
    }

    internal bool TryMark(int target, int meme) => attempted.Add((target, meme));
    internal bool HasAttempted(int target, int meme) => attempted.Contains((target, meme));
}

/// <summary>仅执行模因规则，不访问场景、扣资源或施加效果；随机源由调用方注入。</summary>
public sealed class MemeResolver
{
    private readonly Func<int, bool> rollPercent;
    public MemeResolver(Func<int, bool> rollPercent)
    { this.rollPercent = rollPercent ?? throw new ArgumentNullException(nameof(rollPercent)); }

    /// <summary>只读评估目标门槛，不消费随机，也不记录已经尝试。</summary>
    public MemeOutcome Evaluate(MemeDefinition meme, MemeTargetContext target, bool isReaction)
    {
        if (meme == null || !meme.enabled || !meme.IsValid() || target == null) return MemeOutcome.InvalidDefinition;
        if ((meme.triggers & target.trigger) == 0) return MemeOutcome.TriggerMismatch;
        if (isReaction && !meme.allowReactions) return MemeOutcome.ReactionBlocked;
        if (meme.targetRelation == MemeTargetRelation.Hostile && target.friendly ||
            meme.targetRelation == MemeTargetRelation.Friendly && !target.friendly) return MemeOutcome.RelationMismatch;
        if (meme.targetBodies != MemeBodyType.All && (meme.targetBodies & target.bodyType) == 0 &&
            !(meme.allowNetworkCrossType && target.networkLinked && (target.bodyType & MemeBodyType.All) != 0))
            return MemeOutcome.BodyMismatch;
        if (meme.requiredTargetTags != null)
            foreach (string required in meme.requiredTargetTags)
            {
                bool found = false;
                if (target.tags != null)
                    foreach (string tag in target.tags)
                        if (string.Equals(tag, required, StringComparison.Ordinal)) { found = true; break; }
                if (!found) return MemeOutcome.TagMismatch;
            }
        if (meme.requiresRecognitionSuccess && !target.recognitionPassed) return MemeOutcome.RecognitionRequired;
        return target.resistancePercent >= 100 ? MemeOutcome.Immune : MemeOutcome.Eligible;
    }

    /// <summary>门槛通过后先判抗性，再判触发概率；同一行动同一目标不能重复掷骰。</summary>
    public MemeOutcome Resolve(MemeDefinition meme, MemeTargetContext target, MemeActionContext action)
    {
        if (action == null) return MemeOutcome.InvalidDefinition;
        var outcome = Evaluate(meme, target, action.IsReaction);
        if (outcome != MemeOutcome.Eligible) return outcome;
        if (!action.TryMark(target.targetId, meme.id)) return MemeOutcome.AlreadyAttempted;
        int resistance = Math.Max(0, Math.Min(100, target.resistancePercent));
        if (Roll(resistance)) return MemeOutcome.Resisted;
        return Roll(meme.chancePercent) ? MemeOutcome.Applied : MemeOutcome.ChanceFailed;
    }

    internal MemeOutcome Preview(MemeDefinition meme, MemeTargetContext target, MemeActionContext action)
    {
        if (action == null) return MemeOutcome.InvalidDefinition;
        var result = Evaluate(meme, target, action.IsReaction);
        return result == MemeOutcome.Eligible && action.HasAttempted(target.targetId, meme.id)
            ? MemeOutcome.AlreadyAttempted : result;
    }

    private bool Roll(int chance) => chance >= 100 || chance > 0 && rollPercent(chance);
}
