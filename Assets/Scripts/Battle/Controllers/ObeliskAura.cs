using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>方尖碑动态球形光环；不向目标堆叠旧AutoHeal或残留来源状态。</summary>
[DisallowMultipleComponent]
public sealed class ObeliskAura : MonoBehaviour
{
    [Min(0)] public float radius = 8;
    [Range(0, 1)] public float healFraction = .35f;
    private PieceController owner;
    private PieceController Owner => owner != null ? owner : owner = GetComponent<PieceController>();
    private static bool BlackSuit(PieceController piece) => piece != null && piece.pieceData?.memeTargetTags?.Contains("黑衣人") == true;
    private bool InRange(PieceController target) => target != null && !float.IsNaN(radius) && !float.IsInfinity(radius) &&
        radius >= 0 && (target.transform.position - transform.position).sqrMagnitude <= radius * radius;

    /// <summary>只读选择覆盖目标的最强友方光环；破盾停机期间无效，同强度按参战注册顺序取首个。</summary>
    public static ObeliskAura HealingSource(PieceController target, BattleManager battle)
    {
        if (battle == null || !BattleManager.IsSpeedParticipant(target) || !BlackSuit(target)) return null;
        ObeliskAura selected = null;
        foreach (var piece in battle.SpeedParticipants())
        {
            var aura = piece.GetComponent<ObeliskAura>();
            if (aura == null || !aura.isActiveAndEnabled || piece.GetComponent<BarrierEmitter>()?.ShutdownTurns > 0 || !aura.InRange(target) ||
                !BattleRelations.CanSupport(piece, target) || float.IsNaN(aura.healFraction) ||
                float.IsInfinity(aura.healFraction) || aura.healFraction <= 0) continue;
            if (selected == null || aura.healFraction > selected.healFraction) selected = aura;
        }
        return selected;
    }

    /// <summary>普通回合入口调用一次；向上取整治疗，复用现有Heal表现和生命上限。</summary>
    public static void HealAtTurnStart(PieceController target, BattleManager battle)
    {
        var source = HealingSource(target, battle);
        if (source == null || target.unitAttrCenter.CurHealth >= target.unitAttrCenter.MaxHealth) return;
        target.unitAttrCenter.Heal(Mathf.CeilToInt(target.unitAttrCenter.MaxHealth * Mathf.Clamp01(source.healFraction)));
    }

    /// <summary>成功屠龙术先快照范围内黑衣人，再秒杀自身，按参战注册顺序传播一次。</summary>
    public void ApplyDragon(MemeEffectContext context, Action<PieceController> propagate)
    {
        var battle = BattleScene.Ins?.BM;
        if (battle == null || Owner == null || Owner.isDead) return;
        var snapshot = new List<(PieceController Piece, int Life)>();
        foreach (var target in battle.SpeedParticipants())
            if (target != Owner && BlackSuit(target) && InRange(target)) snapshot.Add((target, target.BattleLifeVersion));
        KillByDragon(context);
        // 死亡事件可能移动、销毁或复用对象；范围保留命中快照，旧生命不承接传播。
        foreach (var entry in snapshot)
            if (entry.Piece != null && !entry.Piece.isDead && entry.Piece.gameObject.activeInHierarchy &&
                entry.Piece.BattleLifeVersion == entry.Life) propagate(entry.Piece);
    }

    /// <summary>传播命中的其他方尖碑也秒杀，但不再次扩散；死亡表现沿现有聚能队列处理。</summary>
    public void KillByDragon(MemeEffectContext context)
    {
        if (Owner == null || Owner.isDead) return;
        Owner.unitAttrCenter.TakeDamage(new AttackPack(Owner.unitAttrCenter.CurHealth, DamageType.Electric),
            new BattleDamageSource(BattleDamageKind.ActionEffect, context.Actor, action: context.Action));
    }

    private void OnDrawGizmosSelected() { Gizmos.color = new Color(.49f, .77f, .62f, .4f); Gizmos.DrawWireSphere(transform.position, Mathf.Max(0, radius)); }
}
