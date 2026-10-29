/// <summary>行为视角的目标关系；不改写原阵营、队伍列表或胜负归属，关系可因观察者状态而不对称。</summary>
public static class BattleRelations
{
    /// <summary>含直接伤害或敌对目标的技能属于进攻，禁止攻击状态也拦截无伤害控制。</summary>
    public static bool IsOffensive(SkillPack skill) => skill != null && (skill.attackPacks != null && skill.attackPacks.Count > 0 ||
        skill.target == SkillTarget.Enemy || skill.target == SkillTarget.EnemyAll || skill.target == SkillTarget.FarthestEnemy ||
        skill.target == SkillTarget.All && EnemyBehaviorRules.IsOffensive(skill) ||
        skill.skillEffects?.Contains(SkillEffect.SpaceBomb) == true || skill.additionalEffects?.Exists(e => e is HitBackEffect || e is SelfExplosionEffect) == true);

    /// <summary>决定当前行动是否由AI代管；麻痹只在付费后取消，不转移控制权。</summary>
    public static bool IsBehaviorControlled(PieceController actor) => actor != null &&
        (actor.MemeState.Violent || actor.MemeState.Inverted || actor.MemeState.AttackForbidden);

    /// <summary>进攻限制覆盖主动攻击及免费反应；支援不据此禁止。</summary>
    public static bool CanInitiateAttack(PieceController actor) => actor != null && !actor.MemeState.AttackForbidden;

    /// <summary>逆转优先于暴力；暴力可攻击双方但不可攻击自身。</summary>
    public static bool CanAttack(PieceController actor, PieceController target)
    {
        if (!CanInitiateAttack(actor) || target == null || actor == target) return false;
        bool originalAlly = actor.isPlayerPiece == target.isPlayerPiece;
        if (actor.MemeState.Inverted) return originalAlly;
        return actor.MemeState.Violent || !originalAlly;
    }

    /// <summary>逆转交换对其他单位的支援关系；自身支援和胜负阵营保持原义。</summary>
    public static bool CanSupport(PieceController actor, PieceController target) => actor != null && target != null &&
        (actor == target || (actor.isPlayerPiece == target.isPlayerPiece) != actor.MemeState.Inverted);
}
