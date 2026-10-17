using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyTargetStrategy
{
    [Sirenix.OdinInspector.LabelText("标准仇恨")] StandardThreat,
    [Sirenix.OdinInspector.LabelText("最近目标")] Nearest,
    [Sirenix.OdinInspector.LabelText("最低当前血量")] LowestHealth,
    [Sirenix.OdinInspector.LabelText("最高历史伤害来源")] HighestDamage,
    [Sirenix.OdinInspector.LabelText("最低最大生命值")] LowestMaxHealth,
    [Sirenix.OdinInspector.LabelText("最低防御力")] LowestDefense
}

public enum EnemyStrategyConditionType
{
    [Sirenix.OdinInspector.LabelText("自身血量比例不高于")] HealthRatioAtMost,
    [Sirenix.OdinInspector.LabelText("可索敌目标数量不多于")] TargetCountAtMost,
    [Sirenix.OdinInspector.LabelText("存活且激活的友军数量不多于（不含自身）")] AllyCountAtMost
}

[Serializable]
public class EnemyStrategyCondition
{
    [Sirenix.OdinInspector.LabelText("条件类型")] public EnemyStrategyConditionType type;
    [Sirenix.OdinInspector.LabelText("血量比例阈值"), Range(0f, 1f)] public float healthRatio = 0.3f;
    [Sirenix.OdinInspector.LabelText("数量阈值"), Min(0)] public int count = 1;

    public bool Matches(EnemyController enemy, int targetCount, int allyCount)
    {
        switch (type)
        {
            case EnemyStrategyConditionType.HealthRatioAtMost:
                return enemy.unitAttrCenter.MaxHealth > 0 &&
                    enemy.unitAttrCenter.CurHealth / (float)enemy.unitAttrCenter.MaxHealth <= Mathf.Clamp01(healthRatio);
            case EnemyStrategyConditionType.TargetCountAtMost: return targetCount <= count;
            case EnemyStrategyConditionType.AllyCountAtMost: return allyCount <= count;
            default: return false;
        }
    }
}

[Serializable]
public class EnemyStrategyRule
{
    [Sirenix.OdinInspector.LabelText("启用")] public bool enabled = true;
    [Tooltip("规则从上到下检查；一条规则的所有条件都满足才切换。空条件不触发。")]
    [Sirenix.OdinInspector.LabelText("同时满足的条件")] public List<EnemyStrategyCondition> conditions = new();
    [Sirenix.OdinInspector.LabelText("切换后索敌策略")] public EnemyTargetStrategy targetStrategy;
    [Sirenix.OdinInspector.LabelText("切换后行动策略")] public EnemyAIType actionStrategy = EnemyAIType.Combine;
}

public readonly struct EnemyStrategy
{
    public readonly EnemyTargetStrategy Target;
    public readonly EnemyAIType Action;
    public EnemyStrategy(EnemyTargetStrategy target, EnemyAIType action) { Target = target; Action = action; }
    public bool Matches(EnemyStrategy other) => Target == other.Target && Action == other.Action;
}

public partial class EnemyController
{
    [Header("索敌策略")]
    [Sirenix.OdinInspector.LabelText("基础索敌策略")] public EnemyTargetStrategy targetStrategy = EnemyTargetStrategy.StandardThreat;
    [Header("条件策略切换（第一条满足条件的规则生效）")]
    [Sirenix.OdinInspector.LabelText("条件切换规则")] public List<EnemyStrategyRule> strategyRules = new();
    [Tooltip("AOE 选点优先命中更多敌对目标，命中数量相同时优先保留索敌目标。")]
    [Sirenix.OdinInspector.LabelText("AOE 优先命中更多目标")] public bool maximizeAOETargets = true;
    [Tooltip("允许攻击当前能波及玩家的爆炸桶/电浆桶；默认关闭，按敌人单独配置。")]
    [Sirenix.OdinInspector.LabelText("允许攻击爆炸物")] public bool attackExplosives = false;
    [Header("通用行为模式配置")]
    [Sirenix.OdinInspector.LabelText("模式1·范围内血量优先级")] public EnemyHealthPriority healthPriority;
    [Sirenix.OdinInspector.LabelText("模式7·弱者判定")] public EnemyFragilePriority fragilePriority;
    [Sirenix.OdinInspector.LabelText("防御力比较类型")] public DamageType defenseType = DamageType.Ranged;
    [Sirenix.OdinInspector.LabelText("优先回复低血量友军")] public bool prioritizeHealing = true;
    [Sirenix.OdinInspector.LabelText("Boss（不自动残血撤离）")] public bool isBoss;
    [Sirenix.OdinInspector.LabelText("残血时向场地边缘撤离")] public bool retreatWhenLowHealth = true;
    [Sirenix.OdinInspector.LabelText("残血撤离阈值"), Range(0f, 1f)] public float retreatHealthRatio = 0.3f; // 严格低于此比例时触发撤离。
    [Sirenix.OdinInspector.LabelText("回复优先阈值"), Range(0f, 1f)] public float healHealthRatio = 0.5f; // 严格低于此比例时成为回复目标。
    [Sirenix.OdinInspector.LabelText("模式4·低血量技能优先阈值"), Range(0f, 1f)] public float executeHealthRatio = 0.3f;

    /// <summary>只读评估，每次规划都根据当前状态重新选择；条件解除则恢复基础策略。</summary>
    public EnemyStrategy ResolveStrategy()
    {
        var battle = BattleScene.Ins?.BM;
        int targets = 0, allies = 0;
        if (battle?.PlayerController?.pieces != null)
            foreach (var piece in battle.PlayerController.pieces)
                if (piece != null && !piece.isDead && piece.gameObject.activeInHierarchy &&
                    BuffManager.CanTarget(this, piece)) targets++;
        if (battle?.AIController?.pieces != null)
            foreach (var piece in battle.AIController.pieces)
                if (piece is EnemyController ally && ally != this && !ally.isDead && ally.isActived &&
                    ally.gameObject.activeInHierarchy) allies++;
        if (strategyRules != null)
            foreach (var rule in strategyRules)
            {
                if (rule == null || !rule.enabled || rule.conditions == null || rule.conditions.Count == 0) continue;
                bool matches = true;
                foreach (var condition in rule.conditions)
                    if (condition == null || !condition.Matches(this, targets, allies)) { matches = false; break; }
                if (matches) return new EnemyStrategy(rule.targetStrategy, rule.actionStrategy);
            }
        return new EnemyStrategy(targetStrategy, enemyAIType);
    }
}
