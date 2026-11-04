using System.Collections.Generic;

/// <summary>保存一次击退的碰撞伤害快照；只提交首次碰撞，双方各一次，不派生新的击退。</summary>
internal sealed class BattleCollisionImpact
{
    private readonly BattleDamageSource source;
    private bool applied; // 在生命回调前消费，防止重复完成或重入结算。
    public int Damage { get; }

    public BattleCollisionImpact(int actualHealthLoss, BattleDamageSource source)
    {
        Damage = (int)(System.Math.Max(0, actualHealthLoss) * 2L / 5L);
        this.source = source;
    }

    /// <summary>撞墙时 obstacle 可为空；死亡、失效或重复的参与者不再扣血，零基数不发送伤害事件。</summary>
    public List<BattleDamageResult> Apply(PieceController mover, PieceController obstacle)
    {
        var results = new List<BattleDamageResult>(2);
        if (applied) return results;
        applied = true;
        if (Damage == 0) return results;
        ApplyTo(mover, results);
        if (obstacle != mover) ApplyTo(obstacle, results);
        return results;
    }

    private void ApplyTo(PieceController piece, List<BattleDamageResult> results)
    {
        if (piece == null || !piece.isActiveAndEnabled || piece.isDead || piece.unitAttrCenter == null) return;
        results.Add(piece.unitAttrCenter.TakeDamage(new AttackPack(Damage, DamageType.Melee), source));
    }
}
