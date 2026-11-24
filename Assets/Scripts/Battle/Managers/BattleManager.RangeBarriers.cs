using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    public readonly List<RangeBarrier> RangeBarriers = new(); // 本场注册顺序；护盾不进入速度、胜负或掉落列表。

    /// <summary>外到内伤害按球面进入距离逐层吸收；传入副本时只做预览，不改变真实耐久。</summary>
    public int AbsorbRangeDamage(PieceController attacker, PieceController target, int damage,
        Dictionary<RangeBarrier, int> preview = null)
    {
        if (attacker == null || target == null || damage <= 0) return damage;
        var candidates = new List<(RangeBarrier Barrier, float Entry)>();
        Vector3 origin = attacker.transform.position, end = target.transform.position;
        foreach (var barrier in RangeBarriers)
        {
            if (barrier == null || !barrier.IsActive || barrier == target || barrier.Contains(origin) || !barrier.Contains(end)) continue;
            Vector3 direction = (end - origin).normalized, offset = origin - barrier.Center;
            float b = Vector3.Dot(offset, direction);
            float distance = -b - Mathf.Sqrt(Mathf.Max(0, b * b - offset.sqrMagnitude + barrier.Radius * barrier.Radius));
            candidates.Add((barrier, distance));
        }
        candidates.Sort((a, b) => { int compare = a.Entry.CompareTo(b.Entry); return compare != 0 ? compare : RangeBarriers.IndexOf(a.Barrier).CompareTo(RangeBarriers.IndexOf(b.Barrier)); });
        foreach (var candidate in candidates)
        {
            var barrier = candidate.Barrier;
            int health = preview != null && preview.TryGetValue(barrier, out int value) ? value : barrier.unitAttrCenter.CurHealth;
            int absorbed = Mathf.Min(damage, Mathf.Max(0, health));
            if (preview != null) preview[barrier] = health - absorbed;
            else if (absorbed > 0) barrier.unitAttrCenter.TakeDamage(new AttackPack(absorbed, DamageType.Electric),
                new BattleDamageSource(BattleDamageKind.ActionEffect, attacker));
            damage -= absorbed;
            if (damage <= 0) break;
        }
        return damage;
    }
}
