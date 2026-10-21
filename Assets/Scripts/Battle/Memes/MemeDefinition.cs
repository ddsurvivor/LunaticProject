using System;
using System.Collections.Generic;

[Flags]
public enum MemeBodyType { None = 0, Biological = 1, Mechanical = 2, All = Biological | Mechanical }
[Flags]
public enum MemeTrigger { None = 0, AttackHit = 1, SupportResolved = 2 }
[Flags]
public enum MemeSourceKind { None = 0, Equipped = 1, Skill = 2, Component = 4, Innate = 8 }
public enum MemeTargetRelation { Hostile, Friendly, Any }
public enum MemeEffectKind { Buff, Custom }
public enum MemeOutcome
{
    Eligible, Applied, InvalidDefinition, TriggerMismatch, ReactionBlocked, RelationMismatch,
    BodyMismatch, TagMismatch, RecognitionRequired, Immune, UnsupportedEffect, AlreadyAttempted, Resisted, ChanceFailed
}

[Serializable]
public sealed class MemeDefinition
{
    public int id; // 稳定配置编号，发布后不随名称变更。
    public string displayName;
    public string description;
    public bool enabled = true;
    public MemeBodyType targetBodies = MemeBodyType.All;
    public MemeTargetRelation targetRelation = MemeTargetRelation.Hostile;
    public List<string> requiredTargetTags = new();
    public bool allowNetworkCrossType = true;
    public MemeTrigger triggers = MemeTrigger.AttackHit;
    public bool requiresRecognitionSuccess;
    public bool allowReactions;
    public int chancePercent = 100;
    public List<MemeEffectDefinition> effects = new();

    /// <summary>校验会影响运行安全的配置；空效果和未实现词条不能参与触发。</summary>
    public bool IsValid()
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(displayName) || chancePercent < 0 || chancePercent > 100 ||
            targetBodies == MemeBodyType.None || (targetBodies & ~MemeBodyType.All) != 0 ||
            triggers == MemeTrigger.None || (triggers & ~(MemeTrigger.AttackHit | MemeTrigger.SupportResolved)) != 0 ||
            !Enum.IsDefined(typeof(MemeTargetRelation), targetRelation) || effects == null || effects.Count == 0) return false;
        if (requiredTargetTags != null)
            foreach (string tag in requiredTargetTags)
                if (string.IsNullOrWhiteSpace(tag)) return false;
        foreach (var effect in effects)
            if (effect == null || !effect.IsValid()) return false;
        return true;
    }
}

[Serializable]
public sealed class MemeEffectDefinition
{
    public MemeEffectKind kind;
    public BuffType buffType;
    public int stacks = 1; // 沿用现有 Buff 的层数约定，-1 为永久。
    public int barrierHealth;
    public int retaliationTurns;
    public string handlerId; // 自定义效果由本场战斗注册的处理器执行。
    public int durationTurns;
    public float magnitude;

    public bool IsValid()
    {
        if (kind == MemeEffectKind.Custom)
            return !string.IsNullOrWhiteSpace(handlerId) && durationTurns >= 0 && !float.IsNaN(magnitude) && !float.IsInfinity(magnitude);
        return kind == MemeEffectKind.Buff && Enum.IsDefined(typeof(BuffType), buffType) &&
            (stacks > 0 || stacks == -1) && barrierHealth >= 0 && retaliationTurns >= 0;
    }
}

public readonly struct MemeSource
{
    public int Id { get; }
    public MemeSourceKind Kind { get; }
    public MemeSource(int id, MemeSourceKind kind) { Id = id; Kind = kind; }
}

public readonly struct MemeAttempt
{
    public int MemeId { get; }
    public MemeSourceKind Sources { get; }
    public MemeOutcome Outcome { get; }
    public float ChancePercent { get; }
    public MemeAttempt(int id, MemeSourceKind sources, MemeOutcome outcome, float chance)
    { MemeId = id; Sources = sources; Outcome = outcome; ChancePercent = chance; }
}

public sealed class MemeTargetContext
{
    public int targetId;
    public MemeBodyType bodyType;
    public IReadOnlyCollection<string> tags;
    public bool networkLinked;
    public bool friendly;
    public bool recognitionPassed;
    public MemeTrigger trigger;
    public int resistancePercent; // 每个词条独立查询，100 表示完全免疫。
}
