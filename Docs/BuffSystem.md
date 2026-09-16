# Buff 系统与新增效果

现有流程：SkillPack.buffPacks 配置效果，BattleManager.PieceSkill 按概率和阵营调用 BuffManager.AddBuff。同类型叠加持续层数，首次添加/最终移除通过 ApplyBuff 增减属性。PlayerController.TurnStart 在恢复行动点后调用 ProcessBuffs，结算持续效果并减少一层；-1 为永续，RemoveBuff 的 stack=-1 表示彻底移除。

## 新增配置

| BuffType | 效果与参数 |
| --- | --- |
| CognitiveProtection（认知防护） | 敌方 AI、技能目标预览、技能目标筛选与最终攻击结算均跳过该单位，包括范围攻击。友方效果不受影响。stacks 为持续回合。 |
| SlowingField（阻速场） | barrierHealth 为承伤耐久，0 使用 BuffManager.defaultBarrierHealth（默认100）。再次施加累加耐久；不按回合消失。按减伤后的攻击伤害扣耐久，破盾的整次攻击仍完全阻挡，包括多段伤害、附带状态和击退。Buff 数字显示剩余耐久。 |
| OffensiveCognitiveProtection（进攻性认知防护） | 被敌方攻击时给攻击者施加 MemeticRetaliation（认知反噬），未命中、被屏障阻挡也触发。retaliationTurns 为持续回合，0 使用默认3回合；重复攻击刷新到较长剩余时间，不累加伤害倍率。 |
| SpontaneousMemeticAttack（自发模因攻击） | 每次确认行动立即受到最大生命15%伤害，再独立进行一次30%判定；成功后等概率施加1层干扰、束缚或眩晕。致死中断行动；眩晕清空行动点，束缚中断移动/攀爬。 |

认知反噬每次自身回合开始扣最大生命20%，持续指定次数；两种百分比伤害向上取整，直接扣血，不经过护甲、阻速场。眩晕在自身回合开始清空行动点后减少层数，因此1层会使下一个自身回合无法行动。

## 调用示例

```csharp
buffManager.AddBuff(unit, BuffType.CognitiveProtection, 2);
buffManager.AddBuff(unit, BuffType.SlowingField, barrierHealth: 150);
buffManager.AddBuff(unit, BuffType.OffensiveCognitiveProtection, 3, retaliationTurns: 3);
buffManager.AddBuff(unit, BuffType.SpontaneousMemeticAttack, 2);
```

SkillPack 的 BuffPack 可以直接选择以上枚举并填写参数。新枚举保留旧值，未改动已有技能资产或分配到具体角色；新图标需要在 SpriteManager.buffIconDic 中配置。

## 验证

- 使用 Unity 现有 Assembly-CSharp.rsp 全量编译游戏脚本。
- 对真实 BuffManager 源码配合轻量 Unity/单位替身执行12项逻辑断言：敌我选敌、永续叠加和移除、护盾消耗和溢出阻挡、三次20%结算、燃烧与其他效果共存、30%概率边界、眩晕与致死中断。
- 仍需在 Unity 场景中试玩确认动画、UI、范围技能、警戒和敌我回合交互；替身测试不覆盖场景依赖。
