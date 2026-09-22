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
        public bool IsCritical;
        public readonly List<DamageInfo> DamageInfos = new();
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
        public int HitRate;
        public bool BarrierBlocked, IsBursting, IsFlank, MustCrit;
        public float RecognitionModifier;
    }

    private struct BurstState
    {
        public PieceController Target;
        public int Total;
    }

    public Settlement ResolveSkillTarget(PieceController attacker, PieceController target,
        SkillPack skill, CheckResult checkResult = CheckResult.None, bool isFlank = false)
    {
        var result = new Settlement();
        // 1. 校验目标，读取掩体、屏障与命中规则。
        if (!CanAffect(attacker, target, skill)) return result;
        if (attacker.isPlayerPiece != target.isPlayerPiece) battle.buffManager.OnAttacked(attacker, target);
        Context context = CreateContext(attacker, target, skill, checkResult, isFlank);
        if (context.Cover != null)
            ObjectPool.Ins.GenerateObject(ItemType.SHIELD, target.transform.position, Quaternion.identity);

        // 2. 仅实战掷骰；每个目标独立暴击，避免暴击传给后续目标。
        if (context.HitRate < 100 && UnityEngine.Random.Range(1, 101) > context.HitRate)
        {
            target.pieceDisplay.ChangeDisplayState(PieceDisplayState.Dodge, false, 0.5f);
            battle.tipTextManager.ShowMiss(target.transform);
            return result;
        }
        result.IsCritical = context.MustCrit || UnityEngine.Random.Range(1, 101) <= attacker.unitAttrCenter.critRate;

        // 3. 各段使用统一公式；实战才提交聚能累计、扣血和受击事件。
        foreach (var pack in skill.attackPacks)
        {
            BurstState burst = CaptureBurst();
            int dice = result.IsCritical ? DamageCalculator.MinDice : DamageCalculator.Roll4D6();
            int damage = CalculateDamage(context, pack, result.IsCritical, dice, ref burst);
            if (context.IsBursting) CommitBurst(burst);
            if (context.BarrierBlocked)
            {
                battle.buffManager.AbsorbAttack(target.unitAttrCenter, damage);
                continue;
            }
            target.unitAttrCenter.TakeDamage(new AttackPack(damage, pack.damageType, result.IsCritical));
            battle.characterSkillManager.NotifyTakeDamage(target.gameObject, attacker.gameObject);
            if (target.unitAttrCenter.CurHealth <= 0)
                battle.characterSkillManager.NotifyKillEnemy(attacker.gameObject, target.gameObject);
            if (target is EnemyController enemy) enemy.AddDamageRecord(attacker, damage);
            if (damage > 0) result.DamageInfos.Add(new DamageInfo(damage, pack.damageType.ToChinese(), result.IsCritical));
        }

        // 4. 屏障阻挡整次攻击，破盾后本次剩余伤害和附加效果仍不穿透。
        result.CanApplyEffects = !context.BarrierBlocked;
        return result;
    }

    /// <summary>
    /// 当前状态下单次 PieceSkill 命中后的直接伤害区间；未命中概率仅由 HitRate 表示。
    /// 识别结果未指定时涵盖所有结果；不预测被动、击退碰撞或协同攻击。
    /// </summary>
    public Preview PreviewSkill(PieceController attacker, PieceController target, SkillPack skill,
        CheckResult? checkResult = null, bool isFlank = false)
    {
        var preview = new Preview();
        if (skill == null) return preview;
        foreach (var pack in skill.attackPacks) preview.ByType[pack.damageType] = default;
        // 1. 和实战共用目标与命中规则；屏障不会产生生命伤害。
        if (!CanAffect(attacker, target, skill)) return preview;
        Context context = CreateContext(attacker, target, skill, checkResult ?? CheckResult.None, isFlank);
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
                var byType = new Dictionary<DamageType, DamagePreviewResult>();
                int totalMin = 0, totalMax = 0;
                foreach (var pack in skill.attackPacks)
                {
                    int min = CalculateBound(context, pack, isCrit, false, ref minBurst);
                    int max = CalculateBound(context, pack, isCrit, true, ref maxBurst);
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

    private static bool CanAffect(PieceController attacker, PieceController target, SkillPack skill)
    {
        return attacker != null && !attacker.isDead && target != null && skill != null
            && BuffManager.CanTarget(attacker, target)
            && (!skill.layerSkill || Mathf.Abs(attacker.transform.position.y - target.transform.position.y) <= 0.1f);
    }

    private Context CreateContext(PieceController attacker, PieceController target, SkillPack skill,
        CheckResult check, bool isFlank)
    {
        var context = new Context
        {
            Attacker = attacker, Target = target, Cover = battle.CheckCoverObstruction(attacker, target),
            IsBursting = attacker.player != null && attacker.player.isBursting, IsFlank = isFlank,
            BarrierBlocked = attacker.isPlayerPiece != target.isPlayerPiece
                && target.unitAttrCenter.GetBuffStacks(BuffType.SlowingField) != 0
        };
        bool guaranteed = context.IsBursting || skill.target is SkillTarget.EnemyAll
            or SkillTarget.All or SkillTarget.Self or SkillTarget.AllyBody or SkillTarget.Ally;
        context.HitRate = guaranteed ? 100 : Mathf.Clamp(Mathf.FloorToInt(
            attacker.unitAttrCenter.buffAttrDic[BuffAttrType.HitRate]
            - target.unitAttrCenter.buffAttrDic[BuffAttrType.EvasionRate]
            - (context.Cover != null ? context.Cover.evadeChance : 0)), 0, 100);
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
        if (context.Cover != null && context.Cover.damageReduction > 0)
            damage = (int)(damage * (100 - context.Cover.damageReduction) / 100f);
        int armor = target.attr.GetArmor(pack.damageType);
        if (pack.damageType == DamageType.Melee)
            armor = (int)(armor * (1f - target.buffAttrDic[BuffAttrType.MeleeArmorPercent] / 100f));

        // 2. 统一暗骰/暴击公式，按原顺序应用增减伤、识别修正并取整。
        damage = DamageCalculator.CalculateDamage(damage, armor, isCrit, attacker.critDamageRate, dice);
        damage = (int)(damage * (100 + attacker.buffAttrDic[BuffAttrType.DamageIncrease]) / 100f
            * (100 - target.buffAttrDic[BuffAttrType.DamageReduction]) / 100f * context.RecognitionModifier);

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
