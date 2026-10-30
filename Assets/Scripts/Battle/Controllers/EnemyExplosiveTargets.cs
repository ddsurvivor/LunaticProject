using System.Collections.Generic;
using UnityEngine;

public interface IEnemyExplosiveTarget
{
    PieceController Piece { get; }
    bool CanBeTriggered { get; }
    int CountThreatenedPlayers();
}

/// <summary>爆炸物自行注册，敌人无需每次行动扫描场景；预判与爆炸共用碰撞层和半径。</summary>
public static class EnemyExplosiveTargets
{
    public static readonly HashSet<IEnemyExplosiveTarget> Active = new();

    public static List<PieceController> CollectTargets(PieceController source, Vector3 position,
        float radius, LayerMask mask)
    {
        var targets = new List<PieceController>();
        foreach (var hit in Physics.OverlapSphere(position, Mathf.Max(0f, radius), mask))
        {
            var piece = hit.GetComponentInParent<PieceController>();
            if (piece != null && piece != source && !targets.Contains(piece)) targets.Add(piece);
        }
        return targets;
    }

    public static int CountPlayers(PieceController source, Vector3 position, float radius,
        LayerMask mask, SkillPack skill)
    {
        if (source == null || skill == null) return 0;
        int count = 0;
        foreach (var piece in SkillTargeting.Filter(source, CollectTargets(source, position, radius, mask), skill, position))
        {
            if (!piece.isPlayerPiece || piece.isDead || !piece.gameObject.activeInHierarchy) continue;
            var damage = BattleScene.Ins?.BM?.damageManager?.PreviewSkill(source, piece, skill);
            if (damage != null && damage.HitRate > 0 && damage.Total.MaxDamage > 0) count++;
        }
        return count;
    }

    public static bool CanAttack(EnemyController enemy, IEnemyExplosiveTarget explosive, SkillPack skill)
    {
        if (enemy == null || !enemy.attackExplosives || enemy.isDead || explosive == null ||
            !explosive.CanBeTriggered || skill == null || skill.attackPacks == null || skill.attackPacks.Count == 0)
            return false;
        var barrel = explosive.Piece;
        if (barrel == null || barrel == enemy || !barrel.gameObject.activeInHierarchy || barrel.isDead ||
            explosive.CountThreatenedPlayers() == 0) return false;
        // 环境目标通常与敌人同阵营，仅本次攻击允许命中它，不改变技能资产。
        var attack = skill.WithTarget(SkillTarget.All);
        if (!SkillTargeting.IsValid(enemy, barrel, attack) ||
            !BattleRangeRules.CanReach(enemy, skill, enemy.transform.position, barrel.transform.position)) return false;
        if (!SkillTargeting.Query(enemy, attack, barrel.transform.position).Contains(barrel)) return false;
        var damage = BattleScene.Ins?.BM?.damageManager?.PreviewSkill(enemy, barrel, attack);
        return damage != null && damage.HitRate > 0 && damage.Total.MaxDamage > 0;
    }
}
