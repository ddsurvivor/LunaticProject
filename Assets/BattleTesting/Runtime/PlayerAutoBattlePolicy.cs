using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Lunatic.BattleTesting
{
    public enum PlayerAutoBattleStyle
    {
        [LabelText("均衡")] Balanced,
        [LabelText("优先击杀")] FocusFire,
        [LabelText("节省技能资源")] ConserveResources
    }

    [Serializable]
    public sealed class PlayerAutoBattleSettings
    {
        [LabelText("战术风格")] public PlayerAutoBattleStyle style;
        [LabelText("使用技能")] public bool useSkills = true;
        [LabelText("使用消耗道具的技能")] public bool useConsumableSkills;
        [LabelText("使用聚能")] public bool useBurst;
        [LabelText("探索未唤醒敌人所在位置")] public bool exploreInactiveEnemies = true;
        [LabelText("回复血量阈值"), Range(0f, 1f)] public float healBelow = 0.5f;
        [LabelText("移动方向采样数"), Range(8, 32)] public int moveSamples = 12;
    }

    public sealed class PlayerAutoBattleDecision
    {
        public PlayerBattleCommand command;
        public string reason;
        public float score;
    }

    /// <summary>只读玩家决策器；查询正式目标、伤害和导航接口，不移动棋子或支付任何费用。</summary>
    public sealed class PlayerAutoBattlePolicy
    {
        private readonly PlayerAutoBattleSettings settings;
        public PlayerAutoBattlePolicy(PlayerAutoBattleSettings settings) { this.settings = settings ?? new PlayerAutoBattleSettings(); }

        /// <summary>为当前玩家棋子选择一条命令；没有合法机会时结束本次个人行动。</summary>
        public PlayerAutoBattleDecision Decide(BattleManager battle, PieceController actor, Vector3? previousMoveOrigin = null)
        {
            var enemies = battle.SpeedParticipants().Where(p => p.isPlayerPiece != actor.isPlayerPiece &&
                SkillTargeting.IsValid(actor, p, SkillTarget.Enemy)).ToList();
            PlayerAutoBattleDecision best = null;
            ConsiderAttack(battle, actor, actor.pieceData?.meleeAtk, PlayerBattleCommandKind.Melee, enemies, ref best);
            if (actor.unitAttrCenter.AmmoCount > 0)
                ConsiderAttack(battle, actor, actor.pieceData?.rangedAtk, PlayerBattleCommandKind.Ranged, enemies, ref best);
            if (settings.useSkills && actor.availableSkills != null)
                foreach (var skill in actor.availableSkills)
                {
                    if (skill == null || !CanPaySkill(actor, skill)) continue;
                    if (IsHealing(skill)) ConsiderHealing(battle, actor, skill, ref best);
                    else ConsiderAttack(battle, actor, skill, PlayerBattleCommandKind.Skill, enemies, ref best);
                }
            if (best != null) return best;
            if (enemies.Count > 0 && actor.unitAttrCenter.AmmoCount == 0 && actor.unitAttrCenter.MaxAmmoCount > 0 &&
                actor.pieceData?.rangedAtk != null && Allowed(actor, ActionType.远程攻击) && CanPay(actor, ActionType.重新装填))
                return Decision(PlayerBattleCommandKind.Reload, actor.transform.position, "空弹匣装填");
            if (Allowed(actor, ActionType.移动) && CanPay(actor, ActionType.移动) && actor.unitAttrCenter.GetBuffStacks(BuffType.Bind) == 0)
            {
                var movement = ChooseMove(battle, actor, enemies, previousMoveOrigin);
                if (movement != null) return movement;
            }
            return Decision(PlayerBattleCommandKind.EndTurn, actor.transform.position, "没有可支付且合法的行动");
        }

        private void ConsiderAttack(BattleManager battle, PieceController actor, SkillPack skill, PlayerBattleCommandKind kind,
            List<PieceController> enemies, ref PlayerAutoBattleDecision best)
        {
            var action = kind == PlayerBattleCommandKind.Skill ? ActionType.技能 : kind == PlayerBattleCommandKind.Ranged ? ActionType.远程攻击 : ActionType.近战攻击;
            if (skill == null || !EnemyBehaviorRules.IsOffensive(skill) || !Allowed(actor, action) || !CanPay(actor, action)) return;
            var aims = enemies.Select(p => p.transform.position).ToList();
            if (EnemyAttackPosition.IsAOE(skill))
            {
                for (int i = 0; i < enemies.Count; i++)
                    for (int j = i + 1; j < enemies.Count; j++) aims.Add((enemies[i].transform.position + enemies[j].transform.position) * 0.5f);
                if (skill.rangeType == RangeType.Fan || skill.rangeType == RangeType.Arc)
                    for (int i = 0; i < 24; i++) aims.Add(actor.transform.position + Quaternion.Euler(0, i * 15f, 0) * Vector3.forward * skill.rangeValue);
            }
            foreach (var aim in aims)
            {
                Vector3 position = SkillTargeting.ClampPosition(actor.transform.position, aim, skill.rangeValue);
                var hits = SkillTargeting.Query(actor, skill, position);
                float score = 0f;
                int hostileHits = 0, kills = 0;
                foreach (var target in hits)
                {
                    var preview = battle.damageManager.PreviewSkill(actor, target, skill);
                    float expected = (preview.Total.MinDamage + preview.Total.MaxDamage) * 0.5f * preview.HitRate / 100f;
                    if (target.isPlayerPiece == actor.isPlayerPiece) { score -= expected * 3f; continue; }
                    if (expected <= 0f) continue;
                    hostileHits++;
                    score += Mathf.Min(expected, target.unitAttrCenter.CurHealth);
                    if (expected >= target.unitAttrCenter.CurHealth) { kills++; score += settings.style == PlayerAutoBattleStyle.FocusFire ? 150f : 75f; }
                }
                if (hostileHits == 0) continue;
                if (kind == PlayerBattleCommandKind.Skill)
                {
                    if (settings.style == PlayerAutoBattleStyle.ConserveResources && hostileHits < 2 && kills == 0) continue;
                    score -= actor.unitAttrCenter.GetSkillManaCost(skill) * 0.5f;
                }
                score /= Mathf.Max(1, GM.Ins.DM.gameConstSO.GetActionPointCost(action));
                if (score <= 0f || best != null && best.score >= score) continue;
                best = Decision(kind, position, $"预估命中{hostileHits}个目标，预估击杀{kills}个", kind == PlayerBattleCommandKind.Skill ? skill : null, score);
            }
        }

        private void ConsiderHealing(BattleManager battle, PieceController actor, SkillPack skill, ref PlayerAutoBattleDecision best)
        {
            if (!Allowed(actor, ActionType.技能) || !CanPay(actor, ActionType.技能)) return;
            foreach (var ally in battle.PlayerController.pieces)
            {
                if (ally == null || !ally.gameObject.activeInHierarchy || !EnemyBehaviorRules.BelowHealth(ally, settings.healBelow)) continue;
                if (skill.buffPacks != null && skill.buffPacks.Any(b => b != null && b.buffType == BuffType.AutoHeal) &&
                    ally.unitAttrCenter.GetBuffStacks(BuffType.AutoHeal) != 0) continue;
                Vector3 position = SkillTargeting.ClampPosition(actor.transform.position, ally.transform.position, skill.rangeValue);
                if (skill.target == SkillTarget.Area ? Vector3.Distance(position, ally.transform.position) > Mathf.Max(0.1f, skill.explodeRadius) :
                    !SkillTargeting.Query(actor, skill, position).Contains(ally)) continue;
                float score = 150f * (1f - ally.unitAttrCenter.CurHealth / (float)ally.unitAttrCenter.MaxHealth);
                if (best == null || best.score < score) best = Decision(PlayerBattleCommandKind.Skill, position, "回复低血量友军", skill, score);
            }
        }

        private PlayerAutoBattleDecision ChooseMove(BattleManager battle, PieceController actor, List<PieceController> enemies, Vector3? previous)
        {
            var goals = new List<(Vector3 point, string reason)>();
            var map = battle.AIController.mapController;
            if (map != null && map.ladderAreas != null && enemies.Any(e => Mathf.Abs(e.transform.position.y - actor.transform.position.y) > 1f))
                foreach (var ladder in map.ladderAreas)
                {
                    if (ladder == null || ladder.upPos == null || ladder.downPos == null) continue;
                    Vector3 near = Vector3.Distance(actor.transform.position, ladder.upPos.position) < Vector3.Distance(actor.transform.position, ladder.downPos.position)
                        ? ladder.upPos.position : ladder.downPos.position;
                    if (Vector3.Distance(actor.transform.position, near) <= 2f && CanPay(actor, ActionType.攀爬))
                        return new PlayerAutoBattleDecision { command = new PlayerBattleCommand { kind = PlayerBattleCommandKind.Climb, ladder = ladder }, reason = "通过梯子接近其他层敌人" };
                    goals.Add((near, "接近梯子"));
                }
            var attacks = new List<SkillPack> { actor.pieceData?.meleeAtk, actor.pieceData?.rangedAtk };
            if (settings.useSkills && actor.availableSkills != null) attacks.AddRange(actor.availableSkills.Where(s => s != null &&
                EnemyBehaviorRules.IsOffensive(s) && CanPaySkill(actor, s)));
            foreach (var enemy in enemies)
                foreach (var attack in attacks)
                {
                    if (attack == null || attack.rangeValue <= 0f) continue;
                    for (int i = 0; i < Mathf.Clamp(settings.moveSamples, 8, 32); i++)
                    {
                        float angle = i * 360f / Mathf.Clamp(settings.moveSamples, 8, 32);
                        goals.Add((enemy.transform.position + Quaternion.Euler(0, angle, 0) * Vector3.forward * attack.rangeValue * 0.8f, "接近可攻击位置"));
                    }
                }
            if (enemies.Count == 0 && settings.exploreInactiveEnemies)
                foreach (var enemy in battle.AIController.pieces)
                    if (enemy != null && !enemy.isDead) goals.Add((enemy.transform.position + Vector3.forward, "探索未唤醒敌人附近"));
            PlayerAutoBattleDecision best = null;
            foreach (var goal in goals)
            {
                if (!battle.moveManager.TryGetAIMoveDestination(actor.gameObject, goal.point, float.PositiveInfinity, out _, out float total, false) ||
                    !battle.moveManager.TryGetAIMoveDestination(actor.gameObject, goal.point, actor.unitAttrCenter.MoveRange, out var destination, out _, true) ||
                    Vector3.Distance(destination, actor.transform.position) < 0.3f || Occupied(battle, actor, destination)) continue;
                float score = -total;
                if (previous.HasValue && Vector3.Distance(previous.Value, destination) < 0.5f) score -= 100f;
                if (best == null || score > best.score) best = Decision(PlayerBattleCommandKind.Move, destination, goal.reason, score: score);
            }
            return best;
        }

        private static bool Occupied(BattleManager battle, PieceController actor, Vector3 point)
        {
            float radius = actor.GetComponent<NavMeshAgent>()?.radius ?? 0.5f;
            return battle.SpeedParticipants().Any(p => p != actor && Mathf.Abs(point.y - p.transform.position.y) < 1f &&
                Vector3.Distance(point, p.transform.position) < radius + (p.GetComponent<NavMeshAgent>()?.radius ?? 0.5f));
        }

        private bool CanPaySkill(PieceController actor, SkillPack skill) => actor.SkillAvailable(skill) &&
            (settings.useConsumableSkills || skill.consumeItems == null || skill.consumeItems.Count == 0);
        private static bool CanPay(PieceController actor, ActionType action) => actor.unitAttrCenter.CurMovePoint >= GM.Ins.DM.gameConstSO.GetActionPointCost(action);
        private static bool Allowed(PieceController actor, ActionType action) => actor.availableActions != null && actor.availableActions.Contains(action);
        private static bool IsHealing(SkillPack skill) => skill.enemySkillRole == EnemySkillRole.Healing ||
            skill.enemySkillRole == EnemySkillRole.Auto && ((skill.buffPacks != null && skill.buffPacks.Any(b => b != null && b.buffType == BuffType.AutoHeal)) ||
            (skill.skillEffects != null && skill.skillEffects.Contains(SkillEffect.HealArea)));
        private static PlayerAutoBattleDecision Decision(PlayerBattleCommandKind kind, Vector3 position, string reason, SkillPack skill = null, float score = 0f) =>
            new PlayerAutoBattleDecision { command = new PlayerBattleCommand { kind = kind, position = position, skill = skill }, reason = reason, score = score };
    }
}
