using UnityEngine;

/// <summary>方尖碑护盾技能的发射器；未破盾时拒绝重复施放，复用和离场不保留旧护盾。</summary>
[DisallowMultipleComponent]
public sealed class BarrierEmitter : MonoBehaviour
{
    [Min(0)] public float radius = 8;
    [Min(1)] public int durability = 40;
    [SerializeField] private RangeBarrier barrierPrefab;
    private RangeBarrier current;
    private int life = -1;
    private int shutdownLife = -1; // 破盾停机只属于当前生命，复用后不保留。
    private BuffState shutdownState; // 复用现有眩晕的行动限制和普通回合计时。
    private PieceController owner;
    public PieceController Owner => owner != null ? owner : owner = GetComponent<PieceController>();
    public bool HasBarrier => current != null && current.IsActive;
    public bool IsCurrentLife => Owner != null && life == Owner.BattleLifeVersion;
    /// <summary>破盾停机剩余的自身普通回合；已解除或复用时返回0。</summary>
    public int ShutdownTurns => Owner != null && shutdownLife == Owner.BattleLifeVersion && shutdownState != null &&
        Owner.unitAttrCenter.buffStates.Contains(shutdownState) ? Mathf.Max(0, shutdownState.stacks) : 0;
    private bool CanDeploy => isActiveAndEnabled && barrierPrefab != null && BattleManager.IsSpeedParticipant(Owner) &&
        Owner.unitAttrCenter.GetBuffStacks(BuffType.Stun) == 0 && !HasBarrier;

    /// <summary>仅当前生命的护盾被击破时停机两个自身普通回合；离场清理不触发。</summary>
    public void OnBarrierBroken(RangeBarrier barrier)
    {
        if (barrier != current || !IsCurrentLife || !BattleManager.IsSpeedParticipant(Owner)) return;
        current = null;
        var battle = BattleScene.Ins?.BM;
        if (battle == null) return;
        battle.buffManager.AddBuff(Owner.unitAttrCenter, BuffType.Stun, 2);
        shutdownLife = Owner.BattleLifeVersion;
        shutdownState = Owner.unitAttrCenter.buffStates.Find(state => state.buffType == BuffType.Stun);
        Owner.InterruptForShutdown();
    }

    /// <summary>技能菜单、扣费与AI共用施放门槛，不创建实体或扣资源。</summary>
    public static bool CanUse(PieceController caster, SkillPack skill) => skill?.skillEffects?.Contains(SkillEffect.RangeBarrier) != true ||
        caster != null && caster.TryGetComponent<BarrierEmitter>(out var emitter) && emitter.CanDeploy;

    /// <summary>正式技能释放点创建护盾；引用缺失时拒绝，不生成运行时UI或临时材质。</summary>
    public bool Deploy()
    {
        if (!CanDeploy) return false;
        if (current != null) current.Dismiss();
        life = Owner.BattleLifeVersion;
        current = Instantiate(barrierPrefab);
        current.Initialize(this);
        return true;
    }

    private void LateUpdate() { if (current != null && (!BattleManager.IsSpeedParticipant(Owner) || life != Owner.BattleLifeVersion)) Clear(); }
    private void Clear() { if (current != null) current.Dismiss(); current = null; }
    private void OnDisable() => Clear();
}
