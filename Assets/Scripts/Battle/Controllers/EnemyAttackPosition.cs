using System.Collections.Generic;
using UnityEngine;

/// <summary>以正式范围查询评分 AOE 落点，不移动棋子、不触发命中或高亮。</summary>
public static class EnemyAttackPosition
{
    public static bool IsAOE(SkillPack skill) => skill != null &&
        (skill.target == SkillTarget.EnemyAll || skill.target == SkillTarget.All);

    public static bool TryFind(EnemyController caster, PieceController preferredTarget, SkillPack skill,
        out Vector3 position)
    {
        position = caster != null ? caster.transform.position : Vector3.zero;
        if (caster == null || caster.isDead || !IsAOE(skill)) return false;
        var players = BattleScene.Ins?.BM?.PlayerController?.pieces;
        if (players == null) return false;
        Vector3 origin = caster.transform.position;
        var targets = new List<PieceController>();
        var samples = new List<Vector3>();
        if (preferredTarget != null) samples.Add(preferredTarget.transform.position);
        Vector3 sum = Vector3.zero;
        foreach (var target in players)
        {
            if (!SkillTargeting.IsValid(caster, target, skill)) continue;
            targets.Add(target);
            samples.Add(target.transform.position);
            sum += target.transform.position;
        }
        if (targets.Count == 0) return false;
        samples.Add(sum / targets.Count);
        for (int i = 0; i < targets.Count; i++)
            for (int j = i + 1; j < targets.Count; j++)
            {
                Vector3 a = targets[i].transform.position, b = targets[j].transform.position;
                samples.Add((a + b) * 0.5f);
                // 圆形爆炸的两目标覆盖圆交点，可找到兼顾第三个目标的偏移落点。
                if (skill.rangeType == RangeType.Grenade || skill.rangeType == RangeType.Circle)
                {
                    float radius = skill.rangeType == RangeType.Grenade ? skill.explodeRadius : SkillTargeting.SingleTargetRadius;
                    Vector3 delta = b - a;
                    delta.y = 0;
                    float distance = delta.magnitude;
                    if (distance <= 0.001f || distance > 2f * radius) continue;
                    float offset = Mathf.Sqrt(Mathf.Max(0f, radius * radius - distance * distance / 4f));
                    Vector3 side = Vector3.Cross(Vector3.up, delta.normalized) * offset;
                    samples.Add((a + b) * 0.5f + side);
                    samples.Add((a + b) * 0.5f - side);
                }
            }
        if (skill.rangeType == RangeType.Fan || skill.rangeType == RangeType.Arc)
        {
            // 扇形/弧形还采样朝向，允许技能朝向落在两个棋子之间。
            for (int i = 0; i < 72; i++)
            {
                float angle = i * 5f * Mathf.Deg2Rad;
                samples.Add(origin + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * skill.rangeValue);
            }
        }
        int bestCount = 0, bestFriendlyCount = int.MaxValue;
        bool bestIncludesPreferred = false;
        foreach (var sample in samples)
        {
            Vector3 aim = BattleRangeRules.ClampPosition(caster, skill, sample);
            int count = 0, friendlyCount = 0;
            bool includesPreferred = false;
            foreach (var hit in SkillTargeting.Query(caster, skill, aim))
            {
                if (hit.isPlayerPiece == caster.isPlayerPiece) { friendlyCount++; continue; }
                count++;
                if (hit == preferredTarget) includesPreferred = true;
            }
            if (count == 0 || count < bestCount) continue;
            if (count == bestCount && (bestIncludesPreferred && !includesPreferred ||
                bestIncludesPreferred == includesPreferred && friendlyCount >= bestFriendlyCount)) continue;
            bestCount = count;
            bestIncludesPreferred = includesPreferred;
            bestFriendlyCount = friendlyCount;
            position = aim;
            // Nova 以自身为中心，改变选点不会增加命中数。
            if (skill.rangeType == RangeType.Nova) break;
        }
        return bestCount > 0;
    }
}
