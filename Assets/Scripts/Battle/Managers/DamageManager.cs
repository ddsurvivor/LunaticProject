using System.Collections.Generic;
using UnityEngine;

/// <summary>由 BattleManager 持有，统一技能伤害结算与只读预览。</summary>
public sealed class DamageManager
{
    private readonly BattleManager battle;
    public DamageManager(BattleManager battle) => this.battle = battle;

    public sealed class Settlement
    {
        public bool CanApplyEffects;
        public bool HitAttack;
        public bool IsCritical;
        public readonly List<DamageInfo> DamageInfos = new();
        public readonly List<BattleDamageResult> DamageResults = new(); // 只记录实际进入生命提交的段，不包含屏障吸收。
    }

    public sealed class Preview
    {
        public int HitRate;
        public DamagePreviewResult Total;
        public readonly Dictionary<DamageType, DamagePreviewResult> ByType = new();
    }

    private struct Context
    {
        public PieceController Attacker, Target;
        public CaverSlot Cover;
        public int CoverReduction; // 本次目标结算共用的配置减伤，预览使用同一公式。
        public int HitRate;
        public bool BarrierBlocked, IsBursting, IsFlank, MustCrit;
        public float RecognitionModifier, PassiveMultiplier;
    }

    private struct BurstState
    {
        public PieceController Target;
        public int Total;
    }

    /// <summary>结算单个目标；显式行动上下文可供多目标共享，返回请求伤害展示及各段实际生命损失。</summary>
    public Settlement ResolveSkillTarget(PieceController attacker, PieceController target,
        SkillPack skill, CheckResult checkResult = CheckResult.None, bool isFlank = false, float passiveMultiplier = 1f, BattleActionContext action = null)
    {
        var result = new Settlement();
        // 1. 校验目标，读取掩体、屏障与命中规则。
        if (!SkillTargeting.IsValid(attacker, target, skill)) return result;
        if (battle.PlayerController.isBursting && !battle.IsBurstActor(attacker) && skill.attackPacks.Count > 0 &&
            battle.TryDeferBurstEffect(attacker, () => ResolveSkillTarget(attacker, target, skill, checkResult, isFlank, passiveMultiplier, action))) return result;
        action ??= BattleActionContext.Current ?? new BattleActionContext(attacker, skill, isFlank);
        using var actionScope = action.Enter();
        if (skill.attackPacks.Count > 0 && BattleRelations.CanAttack(attacker, target))
            battle.buffManager.OnAttacked(attacker, target);
        Context context = CreateContext(attacker, target, skill, checkResult, isFlank);
        context.PassiveMultiplier = passiveMultiplier;
        if (context.Cover != null)
            ObjectPool.Ins.GenerateObject(ItemType.SHIELD, target.transform.position, Quaternion.identity);

        // 2. 仅实战掷骰；每个目标独立暴击，避免暴击传给后续目标。
        if (context.HitRate < 100 && BattleRandom.Range(1, 101) > context.HitRate)
        {
            target.pieceDisplay.ChangeDisplayState(PieceDisplayState.Dodge, true, -1f);
            battle.tipTextManager.ShowMiss(target.transform);
            return result;
        }
        result.IsCritical = context.MustCrit || BattleRandom.Range(1, 101) <= attacker.unitAttrCenter.critRate;

        // 3. 各段使用统一公式；实战才提交聚能累计、扣血和受击事件。
        bool reachedTarget = skill.attackPacks.Count == 0;
        foreach (var pack in skill.attackPacks)
        {
            // 致死后不再结算后续段，也不重复发送受击或击杀事件。
            if (target.isDead || (attacker.isDead && !skill.AllowDeadCaster)) break;
            BurstState burst = CaptureBurst();
            int dice = result.IsCritical ? DamageCalculator.MinDice : DamageCalculator.Roll4D6();
            int damage = CalculateDamage(context, pack, result.IsCritical, dice, ref burst);
            if (context.IsBursting) CommitBurst(burst);
            int incoming = damage;
            damage = battle.AbsorbRangeDamage(attacker, target, damage);
            if (incoming > 0 && damage == 0) continue;
            reachedTarget = true;
            if (context.BarrierBlocked)
            {
                battle.buffManager.AbsorbAttack(target.unitAttrCenter, damage);
                continue;
            }
            result.DamageResults.Add(target.unitAttrCenter.TakeDamage(new AttackPack(damage, pack.damageType, result.IsCritical),
                new BattleDamageSource(BattleDamageKind.Skill, attacker, skill, action)));
            bool killed = target.isDead;
            System.Action notify = () =>
            {
                if (attacker == null || target == null || target is RangeBarrier) return;
                battle.characterSkillManager.NotifyTakeDamage(target.gameObject, attacker.gameObject);
                if (killed && attacker.isPlayerPiece != target.isPlayerPiece)
                    battle.characterSkillManager.NotifyKillEnemy(attacker.gameObject, target.gameObject);
            };
            if (!battle.PlayerController.isBursting || !battle.TryDeferBurstEffect(target, notify, action)) notify();
            if (target is EnemyController enemy) enemy.AddDamageRecord(attacker, damage);
            if (damage > 0) result.DamageInfos.Add(new DamageInfo(damage, pack.damageType.ToChinese(), result.IsCritical));
        }

        // 减速场仍阻挡整次攻击；范围护盾仅在全部伤害段被吸收时阻止附加效果。
        result.CanApplyEffects = !context.BarrierBlocked && reachedTarget && !(target is RangeBarrier);
        result.HitAttack = !context.BarrierBlocked && reachedTarget && skill.attackPacks.Count > 0;
        return result;
    }

    /// <summary>
    /// 当前状态下单次 PieceSkill 命中后的直接伤害区间；未命中概率仅由 HitRate 表示。
    /// 识别结果未指定时涵盖所有结果；不预测被动、击退碰撞或协同攻击。
    /// </summary>
    public Preview PreviewSkill(PieceController attacker, PieceController target, SkillPack skill,
        CheckResult? checkResult = null, bool isFlank = false, IReadOnlyList<PieceController> orderedTargets = null)
    {
        var preview = new Preview();
        if (skill == null) return preview;
        foreach (var pack in skill.attackPacks) preview.ByType[pack.damageType] = default;
        // 1. 和实战共用目标与命中规则；屏障不会产生生命伤害。
        if (!SkillTargeting.IsValid(attacker, target, skill)) return preview;
        Context context = CreateContext(attacker, target, skill, checkResult ?? CheckResult.None, isFlank);
        context.PassiveMultiplier = battle.characterSkillManager.PreviewDamageMultiplier(attacker.gameObject, target.gameObject);
        preview.HitRate = context.HitRate;
        if (context.BarrierBlocked) return preview;

        // 2. 遍历识别和暴击分支，不掷骰、不触发受击事件。
        // 模式识别检定
        CheckResult[] checks = skill.isRecognitionCheck && !checkResult.HasValue
            ? new[] { CheckResult.None, CheckResult.DamageReduced, CheckResult.DamageIncreased, CheckResult.MustCrit }
            : new[] { checkResult ?? CheckResult.None };
        bool first = true;
        foreach (var check in checks)
        {
            SetRecognition(ref context, skill, check);
            for (int critical = 0; critical <= 1; critical++)
            {
                bool isCrit = critical == 1;
                if (!isCrit && (context.MustCrit || attacker.unitAttrCenter.critRate >= 100)) continue;
                if (isCrit && !context.MustCrit && attacker.unitAttrCenter.critRate <= 0) continue;

                // 3. 逐段穷举 4D6，在副本上累计聚能，再按伤害类型合并。
                BurstState minBurst = CaptureBurst();
                BurstState maxBurst = minBurst;
                var minShields = new Dictionary<RangeBarrier, int>();
                var maxShields = new Dictionary<RangeBarrier, int>();
                PrimeRangePreview(attacker, target, skill, check, isFlank, orderedTargets,
                    false, minShields, ref minBurst);
                PrimeRangePreview(attacker, target, skill, check, isFlank, orderedTargets,
                    true, maxShields, ref maxBurst);
                var byType = new Dictionary<DamageType, DamagePreviewResult>();
                int totalMin = 0, totalMax = 0;
                foreach (var pack in skill.attackPacks)
                {
                    int min = CalculateBound(context, pack, isCrit, false, ref minBurst);
                    int max = CalculateBound(context, pack, isCrit, true, ref maxBurst);
                    min = battle.AbsorbRangeDamage(attacker, target, min, minShields);
                    max = battle.AbsorbRangeDamage(attacker, target, max, maxShields);
                    byType.TryGetValue(pack.damageType, out var old);
                    byType[pack.damageType] = DamagePreviewResult.Create(old.MinDamage + min, old.MaxDamage + max);
                    totalMin += min;
                    totalMax += max;
                }
                foreach (var pair in byType)
                    preview.ByType[pair.Key] = Merge(preview.ByType[pair.Key], pair.Value, first);
                preview.Total = Merge(preview.Total, DamagePreviewResult.Create(totalMin, totalMax), first);
                first = false;
            }
        }

        // 4. 直接返回命中后的区间；护甲或屏障实际造成的零伤害仍保留。
        return preview;
    }

    private void PrimeRangePreview(PieceController attacker, PieceController target, SkillPack skill,
        CheckResult check, bool flank, IReadOnlyList<PieceController> targets,
        bool maximum, Dictionary<RangeBarrier, int> shields, ref BurstState burst)
    {
        if (targets == null) return;
        // 范围攻击共享耐久；先在副本上结算排在当前目标之前的伤害。
        foreach (var prior in targets)
        {
            if (prior == target) break;
            if (!SkillTargeting.IsValid(attacker, prior, skill)) continue;
            Context context = CreateContext(attacker, prior, skill, check, flank);
            context.PassiveMultiplier = battle.characterSkillManager.PreviewDamageMultiplier(attacker.gameObject, prior.gameObject);
            if (!maximum && context.HitRate < 100) continue;
            int health = prior is RangeBarrier shield && shields.TryGetValue(shield, out int durability)
                ? durability : prior.unitAttrCenter.CurHealth;
            foreach (var pack in skill.attackPacks)
            {
                if (health <= 0) break;
                int value = PreviewPreviousBound(context, pack, maximum, ref burst);
                value = battle.AbsorbRangeDamage(attacker, prior, value, shields);
                if (!context.BarrierBlocked) health -= value;
                if (prior is RangeBarrier barrier) shields[barrier] = Mathf.Max(0, health);
            }
        }
    }

    private static int PreviewPreviousBound(Context context, AttackPack pack, bool maximum, ref BurstState burst)
    {
        bool mustCrit = context.MustCrit || context.Attacker.unitAttrCenter.critRate >= 100;
        if (mustCrit || context.Attacker.unitAttrCenter.critRate <= 0)
            return CalculateBound(context, pack, mustCrit, maximum, ref burst);
        var normal = burst;
        var critical = burst;
        int a = CalculateBound(context, pack, false, maximum, ref normal);
        int b = CalculateBound(context, pack, true, maximum, ref critical);
        bool useCritical = maximum ? b > a : b < a;
        burst = useCritical ? critical : normal;
        return useCritical ? b : a;
    }

    private Context CreateContext(PieceController attacker, PieceController target, SkillPack skill,
        CheckResult check, bool isFlank)
    {
        var context = new Context
        {
            Attacker = attacker, Target = target, PassiveMultiplier = 1f,
            Cover = skill.attackPacks.Count > 0 ? battle.CheckCoverObstruction(attacker, target) : null,
            IsBursting = battle.IsBurstActor(attacker), IsFlank = isFlank,
            BarrierBlocked = skill.attackPacks.Count > 0
                && target.unitAttrCenter.GetBuffStacks(BuffType.SlowingField) != 0
        };
        context.CoverReduction = context.Cover != null
            ? GM.Ins.DM.gameConstSO.GetCoverDamageReduction(context.Cover.Level) : 0;
        bool guaranteed = context.IsBursting || skill.target is SkillTarget.EnemyAll
            or SkillTarget.All or SkillTarget.Self or SkillTarget.AllyBody or SkillTarget.Ally;
        context.HitRate = guaranteed ? 100 : Mathf.Clamp(Mathf.FloorToInt(
            attacker.unitAttrCenter.buffAttrDic[BuffAttrType.HitRate]
            + (skill.target is SkillTarget.Enemy or SkillTarget.FarthestEnemy
                ? attacker.unitAttrCenter.buffAttrDic[BuffAttrType.SingleTargetHitRate] : 0f)
            - target.unitAttrCenter.buffAttrDic[BuffAttrType.EvasionRate]
            - (context.Cover != null ? GM.Ins.DM.gameConstSO.CoverEvasionBonus : 0)), 0, 100);
        SetRecognition(ref context, skill, check);
        return context;
    }

    private static void SetRecognition(ref Context context, SkillPack skill, CheckResult check)
    {
        context.MustCrit = skill.isRecognitionCheck && check == CheckResult.MustCrit;
        context.RecognitionModifier = !skill.isRecognitionCheck ? 1f : check switch
        {
            CheckResult.DamageReduced => 0.4f,
            CheckResult.DamageIncreased or CheckResult.MustCrit => 1.3f,
            _ => 1f
        };
    }

    private static int CalculateDamage(Context context, AttackPack pack, bool isCrit, int dice, ref BurstState burst)
    {
        var attacker = context.Attacker.unitAttrCenter;
        var target = context.Target.unitAttrCenter;
        // 1. 技能威力 + 对应类型攻击力 + 通用攻击力 + 克制加伤，再应用掩体和护甲修正。
        int damage = pack.damage + attacker.attr.GetAtk(pack.damageType)
            + attacker.ATK + attacker.attr.GetAddDamage(target.elementType);
        if (context.CoverReduction > 0)
            damage = (int)(damage * (100 - context.CoverReduction) / 100f);
        int armor = target.attr.GetArmor(pack.damageType);
        if (pack.damageType == DamageType.Melee)
            armor = (int)(armor * Mathf.Max(0f, target.buffAttrDic[BuffAttrType.MeleeArmorPercent]) / 100f);

        // 2. 统一暗骰/暴击公式，按原顺序应用增减伤、识别修正并取整。
        damage = DamageCalculator.CalculateDamage(damage, armor, isCrit, attacker.critDamageRate, dice);
        damage = (int)(damage * (100 + attacker.buffAttrDic[BuffAttrType.DamageIncrease]) / 100f
            * (100 - target.buffAttrDic[BuffAttrType.DamageReduction]) / 100f * context.RecognitionModifier * context.PassiveMultiplier);

        // 3. 聚能先累计，再应用夹击倍率；预览只修改传入的状态副本。
        if (context.IsBursting) damage = ApplyBurst(context.Target, damage, ref burst);
        if (context.IsFlank) damage = (int)(damage * GM.Ins.DM.gameConstSO.FlankDamageRate);
        return Mathf.Max(0, damage);
    }

    private static int CalculateBound(Context context, AttackPack pack, bool isCrit, bool maximum, ref BurstState burst)
    {
        int bound = maximum ? int.MinValue : int.MaxValue;
        BurstState selected = burst;
        for (int dice = DamageCalculator.MinDice; dice <= DamageCalculator.MaxDice; dice++)
        {
            BurstState candidate = burst;
            int damage = CalculateDamage(context, pack, isCrit, dice, ref candidate);
            // 夹击取整可能让本段伤害相同；保留聚能累计的极值，供后续段使用。
            bool better = maximum ? damage > bound : damage < bound;
            bool betterBurst = damage == bound && (maximum ? candidate.Total > selected.Total : candidate.Total < selected.Total);
            if (better || betterBurst)
            {
                bound = damage;
                selected = candidate;
            }
        }
        burst = selected;
        return bound;
    }

    private static DamagePreviewResult Merge(DamagePreviewResult old, DamagePreviewResult next, bool first)
    {
        return first ? next : DamagePreviewResult.Create(
            Mathf.Min(old.MinDamage, next.MinDamage), Mathf.Max(old.MaxDamage, next.MaxDamage));
    }

    private BurstState CaptureBurst() => new BurstState
    {
        Target = battle.PlayerController.burstTarget, Total = battle.PlayerController.totalDamage
    };

    private void CommitBurst(BurstState state)
    {
        battle.PlayerController.burstTarget = state.Target;
        battle.PlayerController.totalDamage = state.Total;
    }

    private static int ApplyBurst(PieceController target, int damage, ref BurstState state)
    {
        if (state.Target == target)
        {
            damage = (int)(GameConst.burstDamageRate * damage) + (int)(state.Total * GameConst.burstAddDamageRate);
            state.Total += damage;
        }
        else
        {
            state.Target = target;
            state.Total = damage;
        }
        return damage;
    }

    // 保留原 PlayerController 接口，同样使用统一聚能公式。
    public int AddBurstDamage(PieceController target, int damage)
    {
        BurstState state = CaptureBurst();
        damage = ApplyBurst(target, damage, ref state);
        CommitBurst(state);
        return damage;
    }
}
