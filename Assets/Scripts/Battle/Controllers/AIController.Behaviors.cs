using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public partial class AIController
{
    private readonly HashSet<EnemyController> woundedAttackDone = new(); // 个人回合内残血撤离前最多主动攻击一次。
    private readonly HashSet<EnemyController> healedThisTurn = new(); // 避免延迟治疗在同一回合重复施放。

    private bool TryDesignAttack(EnemyController piece, PieceController target, EnemyStrategy strategy, bool allowMove)
    {
        var mode = strategy.Action;
        float distance = Vector3.Distance(piece.transform.position, target.transform.position);
        bool far = distance > EnemyBehaviorRules.MaximumRange(piece) * 0.5f;
        if (mode == EnemyAIType.WeakestDefenseShooter && allowMove && retreatAfterShot.Remove(piece) && TryShooterEvade(piece)) return true;
        if (TryCreateExplosivePlan(piece, out var explosive) && TryAttackPlan(piece, explosive, false)) return true;
        if (allowMove && piece.navigate && Mathf.Abs(target.transform.position.y - piece.transform.position.y) > 1f)
            return EnemyMoveToLadder(piece);

        if (mode == EnemyAIType.WeakestDefenseShooter)
        {
            if (TryAttackPlan(piece, NormalPlan(piece, target, true), false) || actionAttempted) return true;
            if (allowMove && TryShooterEvade(piece)) return true;
            if (!allowMove) return false;
            return TryAttackPlan(piece, NormalPlan(piece, target, true)) || actionAttempted ||
                TryApproach(piece, target.transform.position, piece.pieceData.rangedAtk?.rangeValue ?? 0f);
        }

        bool crowd = (mode == EnemyAIType.DamageHunterAOE || mode == EnemyAIType.DamageHunterRangedSkill) && HasMultiTargetAttack(piece, target);
        bool lowTarget = mode == EnemyAIType.DamageHunterRangedSkill && piece.availableSkills != null &&
            BattleScene.Ins.BM.PlayerController.pieces.Any(p => IsValidTarget(piece, p) &&
            EnemyBehaviorRules.BelowHealth(p, piece.executeHealthRatio) &&
            piece.availableSkills.Any(s => IsOffensiveSkill(s) && CanExecuteAttack(piece, p, s)));
        if (mode == EnemyAIType.DamageHunterAOE && crowd && TryModeSkill(piece, target, null, true, allowMove)) return true;
        if (mode == EnemyAIType.DamageHunterRangedSkill)
        {
            if ((crowd || lowTarget) && TryModeSkill(piece, lowTarget ? FindLowHealthSkillTarget(piece) ?? target : target,
                null, crowd && !lowTarget, false)) return true;
            if (far && UnityEngine.Random.value < 1f / 3f && TryModeSkill(piece, target, EnemySkillRole.RangedAttack, false, allowMove)) return true;
        }
        if (mode == EnemyAIType.DamageHunterMeleeSkill && distance < EnemyBehaviorRules.MaximumRange(piece) * 0.5f &&
            UnityEngine.Random.value < 1f / 3f &&
            TryModeSkill(piece, target, EnemySkillRole.MeleeAttack, false, allowMove)) return true;
        if ((mode == EnemyAIType.SkillUser || mode == EnemyAIType.HealingSupport) &&
            TryModeSkill(piece, target, null, false, allowMove)) return true;

        bool ranged = EnemyBehaviorRules.PreferRanged(piece, mode, distance);
        if (EnemyBehaviorRules.IsMeleeOnly(mode)) ranged = false;
        if (mode == EnemyAIType.Shoot) ranged = true;
        if (mode == EnemyAIType.DamageHunterAOE && crowd)
        {
            foreach (bool useRanged in new[] { ranged, !ranged })
            {
                var plan = NormalPlan(piece, target, useRanged);
                if (EnemyAttackPosition.IsAOE(plan.Skill) && CountSkillHits(piece, target, plan.Skill) > 1 &&
                    (TryAttackPlan(piece, plan, false) || actionAttempted)) return true;
            }
        }
        if (TryAttackPlan(piece, NormalPlan(piece, target, ranged), allowMove) || actionAttempted) return true;
        if (!allowMove) return false;
        // 模式1无弹药或处于半射程内时坚持近战，不通过失败回退偷偷改成射击。
        float range = (ranged ? piece.pieceData.rangedAtk : piece.pieceData.meleeAtk)?.rangeValue ?? 0f;
        return TryApproach(piece, target.transform.position, range);
    }

    private bool TryModeSkill(EnemyController piece, PieceController target, EnemySkillRole? role, bool multiOnly, bool allowMove)
    {
        if (piece.availableSkills == null) return false;
        // 优先执行当前能命中的技能，再考虑移动后施放，避免忽略眼前可用的群攻。
        for (int pass = 0; pass < (allowMove ? 2 : 1); pass++)
            foreach (var skill in piece.availableSkills)
            {
                if (!IsOffensiveSkill(skill) || EnemyBehaviorRules.SkillRole(piece, skill) == EnemySkillRole.Healing ||
                    role.HasValue && EnemyBehaviorRules.SkillRole(piece, skill) != role.Value ||
                    !piece.unitAttrCenter.HasMana(skill.mpCost)) continue;
                if (multiOnly && CountSkillHits(piece, target, skill) < 2) continue;
                if (pass == 0 && !CanExecuteAttack(piece, target, skill)) continue;
                var plan = new AttackPlan { Target = target, Skill = skill, Action = ActionType.技能, Strategy = piece.ResolveStrategy() };
                if (TryAttackPlan(piece, plan, pass != 0) || actionAttempted) return true;
            }
        return false;
    }

    private bool HasMultiTargetAttack(EnemyController piece, PieceController target)
    {
        if (CountSkillHits(piece, target, piece.pieceData.meleeAtk) > 1 || CountSkillHits(piece, target, piece.pieceData.rangedAtk) > 1) return true;
        return piece.availableSkills != null && piece.availableSkills.Any(s => IsOffensiveSkill(s) &&
            piece.unitAttrCenter.HasMana(s.mpCost) && CountSkillHits(piece, target, s) > 1);
    }

    private static int CountSkillHits(EnemyController piece, PieceController target, SkillPack skill)
    {
        if (!EnemyAttackPosition.IsAOE(skill)) return 0;
        if (!piece.maximizeAOETargets) return SkillTargeting.Query(piece, skill, target.transform.position)
            .Count(t => t.isPlayerPiece != piece.isPlayerPiece);
        return EnemyAttackPosition.IsAOE(skill) && EnemyAttackPosition.TryFind(piece, target, skill, out var aim)
            ? SkillTargeting.Query(piece, skill, aim).Count(t => t.isPlayerPiece != piece.isPlayerPiece) : 0;
    }

    private PieceController FindLowHealthSkillTarget(EnemyController piece)
    {
        return BattleScene.Ins.BM.PlayerController.pieces.Where(p => IsValidTarget(piece, p) &&
            EnemyBehaviorRules.BelowHealth(p, piece.executeHealthRatio) &&
            piece.availableSkills.Any(s => IsOffensiveSkill(s) && CanExecuteAttack(piece, p, s)))
            .OrderBy(p => p.unitAttrCenter.CurHealth).FirstOrDefault();
    }

    private bool TryHealingPriority(EnemyController piece, EnemyStrategy strategy)
    {
        if ((!piece.prioritizeHealing && strategy.Action != EnemyAIType.HealingSupport) || healedThisTurn.Contains(piece) ||
            piece.availableSkills == null || !CanAfford(piece, ActionType.技能)) return false;
        if (pendingAttacks.TryGetValue(piece, out var pending) && pending.Support)
        {
            pendingAttacks.Remove(piece);
            if (IsSupportTarget(piece, pending.Target, pending.Skill) && TryAttackPlan(piece, pending)) return true;
        }
        var allies = pieces.Concat(new[] { piece }).Distinct().Where(p => p != null && !p.isDead &&
            p.gameObject.activeInHierarchy && (p == piece || p is EnemyController e && e.isActived) &&
            EnemyBehaviorRules.BelowHealth(p, piece.healHealthRatio) &&
            Vector3.Distance(piece.transform.position, p.transform.position) <= piece.unitAttrCenter.MoveRange)
            .OrderBy(p => p.unitAttrCenter.CurHealth / (float)p.unitAttrCenter.MaxHealth);
        foreach (var ally in allies)
            foreach (var skill in piece.availableSkills)
            {
                if (EnemyBehaviorRules.SkillRole(piece, skill) != EnemySkillRole.Healing ||
                    !piece.unitAttrCenter.HasMana(skill.mpCost) || !IsSupportTarget(piece, ally, skill)) continue;
                // 自动治疗尚在生效时不重复覆盖，优先把回复机会让给其他伤员。
                if (skill.buffPacks != null && skill.buffPacks.Any(b => b != null && b.buffType == BuffType.AutoHeal) &&
                    ally.unitAttrCenter.GetBuffStacks(BuffType.AutoHeal) != 0) continue;
                var plan = new AttackPlan { Target = ally, Skill = skill, Action = ActionType.技能,
                    Strategy = strategy, Support = true };
                if (TryAttackPlan(piece, plan) || actionAttempted) return true;
            }
        return false;
    }

    private bool TryWoundedAttack(EnemyController piece, PieceController preferred, EnemyStrategy strategy)
    {
        if (IsValidTarget(piece, preferred) && TryDesignAttack(piece, preferred, strategy, false)) return true;
        // 撤离前可攻击其他在范围内的目标，不为追逐原仇恨目标浪费逃生机会。
        foreach (var target in BattleScene.Ins.BM.PlayerController.pieces)
            if (target != preferred && IsValidTarget(piece, target) && TryDesignAttack(piece, target, strategy, false)) return true;
        return false;
    }

    private static bool IsSupportTarget(EnemyController piece, PieceController target, SkillPack skill)
    {
        if (skill == null || target == null || target.isDead || !target.gameObject.activeInHierarchy ||
            target.isPlayerPiece != piece.isPlayerPiece || !EnemyBehaviorRules.BelowHealth(target, piece.healHealthRatio)) return false;
        return skill.target == SkillTarget.Area || SkillTargeting.IsValid(piece, target, skill);
    }

    private static bool CanExecuteSupport(EnemyController piece, PieceController target, SkillPack skill)
    {
        if (!IsSupportTarget(piece, target, skill)) return false;
        if (skill.layerSkill && Mathf.Abs(piece.transform.position.y - target.transform.position.y) > 0.1f) return false;
        if (skill.target == SkillTarget.Area) return Vector3.Distance(piece.transform.position, target.transform.position) <= skill.rangeValue;
        return SkillTargeting.Query(piece, skill, target.transform.position).Contains(target);
    }

    private static bool NeedsClearShot(EnemyController piece) => piece.ResolveStrategy().Action == EnemyAIType.WeakestDefenseShooter;

    private static bool CoverObstructsFrom(Vector3 origin, PieceController target)
    {
        if (target == null || target.CurCaverSlot == null) return false;
        Vector3 direction = target.transform.position - origin;
        foreach (var hit in Physics.RaycastAll(origin + Vector3.up * 1.2f, direction.normalized, direction.magnitude))
            if (hit.collider.gameObject == target.CurCaverSlot.gameObject) return true;
        return false;
    }

    private static bool CanExecutePlan(EnemyController piece, AttackPlan plan) =>
        (!NeedsClearShot(piece) || !CoverObstructsFrom(piece.transform.position, plan.Target)) &&
        CanExecuteAttack(piece, plan.Target, plan.Skill);

    private bool TryShooterEvade(EnemyController piece)
    {
        float safeRange = (piece.pieceData.rangedAtk?.rangeValue ?? 0f) * 0.5f;
        if (safeRange <= 0f || !CanMove(piece)) return false;
        var threats = BattleScene.Ins.BM.PlayerController.pieces.Where(p => IsValidTarget(piece, p)).ToList();
        if (threats.Count == 0) return false;
        float initial = threats.Min(p => Vector3.Distance(piece.transform.position, p.transform.position));
        if (initial >= safeRange) return false;
        var candidates = new List<MoveCandidate>();
        for (int i = 0; i < PositionSamples; i++)
        {
            Vector3 point = piece.transform.position + Quaternion.Euler(0, i * 360f / PositionSamples, 0) *
                Vector3.forward * piece.unitAttrCenter.MoveRange;
            if (!BattleScene.Ins.BM.moveManager.TryGetAIMoveDestination(piece.gameObject, point,
                piece.unitAttrCenter.MoveRange, out var destination, out float length, false) || IsOccupied(piece, destination)) continue;
            float safety = threats.Min(p => Vector3.Distance(destination, p.transform.position));
            if (safety <= initial + 0.1f) continue;
            candidates.Add(new MoveCandidate { Position = destination, PathLength = length, Score = safety });
        }
        foreach (var candidate in candidates.OrderByDescending(c => c.Score))
            if (TryMoveTo(piece, candidate.Position)) return true;
        return false;
    }

    private bool TryFleeToEdge(EnemyController piece)
    {
        if (!CanMove(piece)) return false;
        var agent = piece.GetComponent<NavMeshAgent>();
        var vertices = NavMesh.CalculateTriangulation().vertices;
        if (agent == null || vertices.Length == 0) return false;
        float minX = vertices.Min(v => v.x), maxX = vertices.Max(v => v.x);
        float minZ = vertices.Min(v => v.z), maxZ = vertices.Max(v => v.z);
        Vector3 origin = piece.transform.position;
        float margin = Mathf.Max(0.5f, agent.radius + 0.1f); // 到达场地边缘后不继续奔向另一个边缘。
        if (Mathf.Min(Mathf.Min(origin.x - minX, maxX - origin.x), Mathf.Min(origin.z - minZ, maxZ - origin.z)) <= margin) return false;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, origin.y, (minZ + maxZ) * 0.5f);
        var candidates = new List<MoveCandidate>();
        void Consider(Vector3 goal)
        {
            if (BattleScene.Ins.BM.moveManager.TryGetAIMoveDestination(piece.gameObject, goal, float.PositiveInfinity,
                out var destination, out float length, false) && !IsOccupied(piece, destination))
                candidates.Add(new MoveCandidate { Position = destination, PathLength = length });
        }
        Consider(new Vector3(minX + margin, origin.y, origin.z));
        Consider(new Vector3(maxX - margin, origin.y, origin.z));
        Consider(new Vector3(origin.x, origin.y, minZ + margin));
        Consider(new Vector3(origin.x, origin.y, maxZ - margin));
        // 只取导航网格外侧极值点，避免把场地中间的掩体边缘当作撤离终点。
        int stride = Mathf.Max(1, (vertices.Length + 127) / 128); // 限制单次撤离规划的导航路径查询数量。
        for (int i = 0; i < vertices.Length; i += stride)
        {
            Vector3 vertex = vertices[i];
            if (vertex.x > minX + 0.1f && vertex.x < maxX - 0.1f && vertex.z > minZ + 0.1f && vertex.z < maxZ - 0.1f) continue;
            Vector3 goal = vertex + (center - vertex).normalized * margin;
            Consider(goal);
        }
        foreach (var candidate in candidates.OrderBy(c => c.PathLength))
            if (TryMoveTo(piece, candidate.Position)) return true;
        return false;
    }
}
