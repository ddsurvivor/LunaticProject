using System;
using System.Threading;

/// <summary>一次结算的身份与根行动；不改变费用、公式或反应顺序，不作为存档中的永久ID。</summary>
public sealed class BattleActionContext
{
    private static long nextId; // 当前进程单调递增，预览不分配行动ID。
    /// <summary>仅同步主线程结算有效；延迟回调必须显式捕获，不能跨yield保持作用域。</summary>
    public static BattleActionContext Current { get; private set; }
    public long ActionId { get; }
    /// <summary>本行动及其派生反应共用的根ID；用于追踪，不自动抑制反应或重复提交。</summary>
    public long RootActionId { get; }
    public PieceController Actor { get; }
    public SkillPack Skill { get; }
    public bool IsReaction { get; }

    public BattleActionContext(PieceController actor = null, SkillPack skill = null, bool isReaction = false,
        BattleActionContext parent = null)
    {
        ActionId = Interlocked.Increment(ref nextId);
        RootActionId = (parent ?? Current)?.RootActionId ?? ActionId;
        Actor = actor;
        Skill = skill;
        IsReaction = isReaction;
    }

    /// <summary>在同步结算范围内传播根行动；不能跨yield持有，退出时恢复父上下文。</summary>
    public IDisposable Enter() => new Scope(this);

    private sealed class Scope : IDisposable
    {
        private readonly BattleActionContext previous;
        private bool disposed;
        public Scope(BattleActionContext action) { previous = Current; Current = action; }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Current = previous;
        }
    }
}

public enum BattleDamageKind { Unknown, Skill, Periodic, ActionEffect, Collision, SelfDamage, Component }

/// <summary>一段伤害的来源；延迟伤害持有此快照，避免使用回调执行时其他行动的身份。</summary>
public sealed class BattleDamageSource
{
    public BattleDamageKind Kind { get; }
    public BattleActionContext Action { get; }
    public BattleDamageSource(BattleDamageKind kind, PieceController actor = null, SkillPack skill = null,
        BattleActionContext action = null)
    {
        Kind = kind;
        Action = action ?? BattleActionContext.Current ?? new BattleActionContext(actor, skill);
    }
}

/// <summary>一次实际提交的生命变化；零伤害仍可命中，HpLoss不包含过量伤害或屏障吸收。</summary>
public sealed class BattleDamageResult
{
    public PieceController Target { get; }
    public BattleDamageSource Source { get; }
    public int Requested { get; }
    public int HealthBefore { get; }
    public int HealthAfter { get; }
    public int HpLoss => Math.Max(0, HealthBefore - HealthAfter);
    public bool Killed => !Ignored && HealthBefore > 0 && HealthAfter == 0;
    public bool Ignored { get; }
    public BattleDamageResult(PieceController target, BattleDamageSource source, int requested,
        int healthBefore, int healthAfter, bool ignored = false)
    {
        Target = target; Source = source; Requested = requested;
        HealthBefore = healthBefore; HealthAfter = healthAfter; Ignored = ignored;
    }
}
