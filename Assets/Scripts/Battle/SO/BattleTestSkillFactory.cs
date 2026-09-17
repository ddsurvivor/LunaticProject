using System.Collections.Generic;

/// <summary>战斗状态测试技能模板。</summary>
public static class BattleTestSkillFactory
{
    public const string CognitiveProtection = "测试·认知防护";
    public const string SlowingField = "测试·阻速场";
    public const string OffensiveProtection = "测试·进攻性认知防护";
    public const string SpontaneousAttack = "测试·自发模因攻击";

    public static readonly string[] SkillNames =
    {
        CognitiveProtection, SlowingField, OffensiveProtection, SpontaneousAttack
    };

    public static SkillPack Create(string skillName)
    {
        BuffType buffType;
        SkillTarget buffTarget;
        int stacks = 2;
        switch (skillName)
        {
            case CognitiveProtection:
                buffType = BuffType.CognitiveProtection;
                buffTarget = SkillTarget.Self;
                break;
            case SlowingField:
                buffType = BuffType.SlowingField;
                buffTarget = SkillTarget.Self;
                stacks = 1;
                break;
            case OffensiveProtection:
                buffType = BuffType.OffensiveCognitiveProtection;
                buffTarget = SkillTarget.Self;
                break;
            case SpontaneousAttack:
                buffType = BuffType.SpontaneousMemeticAttack;
                buffTarget = SkillTarget.Enemy;
                break;
            default:
                return null;
        }

        return new SkillPack
        {
            skillName = skillName,
            description = buffTarget == SkillTarget.Self
                ? $"战斗测试：造成1点固定伤害，给自己100%施加{buffType.ToChinese()}。"
                : $"战斗测试：造成1点固定伤害，给敌人100%施加{buffType.ToChinese()}。",
            target = SkillTarget.Enemy,
            rangeType = RangeType.Circle,
            rangeValue = 20f,
            mpCost = 0,
            atkTimes = 1,
            attackPacks = new List<AttackPack> { new AttackPack(1, DamageType.Electric) },
            buffPacks = new List<BuffPack>
            {
                new BuffPack
                {
                    buffType = buffType,
                    target = buffTarget,
                    stacks = stacks,
                    rate = 100,
                    barrierHealth = buffType == BuffType.SlowingField ? 100 : 0,
                    retaliationTurns = buffType == BuffType.OffensiveCognitiveProtection ? 3 : 0
                }
            }
        };
    }
}
