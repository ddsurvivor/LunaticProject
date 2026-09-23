# 伤害系统

核对日期：2026-09-22。本文说明当前代码规则，实际参数以场景和 SO 配置为准。

## 1. 入口与计算单位

- 结算：BattleManager.PieceSkill → DamageManager.ResolveSkillTarget。
- 预览：HitInfoPanel → DamageManager.PreviewSkill。
- 共用公式：DamageManager.CalculateDamage → DamageCalculator.CalculateDamage。
- 扣血与反馈：UnitAttrCenter.TakeDamage。

目标去空、去重后，按列表顺序结算。每个目标判定一次命中、一次显式暴击；每个 attackPack 是一段伤害，独立计算暗骰。管理器不按 atkTimes 额外循环。

下文 trunc 表示舍弃小数，floor 表示向下取整。各步骤分别取整，不能合并到最后。

## 2. 目标与楼层检定

结算和预览共用 CanAffect：

1. 攻击者存在且存活，目标和技能存在。
2. CanTarget 允许选中；敌方有认知防护（CognitiveProtection）时不可选。
3. 满足楼层限制：

```text
layerSkill = false：不限制高度
layerSkill = true ：abs(攻击者.y - 目标.y) <= 0.1
```

**楼层检定已接入结算和预览。** 它比较世界坐标 Y，不读取楼层编号。layerSkill 默认关闭，没有高低差增伤或命中修正。

不通过时，本目标不掷骰、不扣血、不触发附加效果，预览为 0。聚能、必中和必暴击不能绕过限制。

射程、范围和阵营主要由调用方筛选。CanAffect 不检查射程，也不排除死亡目标；TakeDamage 会忽略已死亡单位。

## 3. 命中前触发与掩体

敌对攻击先触发 OnAttacked。目标有攻击性认知防护时，对攻击者施加或刷新认知反噬；未命中也可能触发。

同时记录阻速场（SlowingField）状态，供本次攻击各段共用。

掩体检查：从攻击者向目标发射射线，两端上移 1.2。RaycastAll 命中目标 CurCaverSlot 关联的掩体 GameObject 时，掩体生效。

- evadeChance：降低命中率，单位为百分点。
- damageReduction：在护甲、暗骰前降低基础伤害。

这不是通用墙体阻挡检查，碰到其他墙体不会直接判攻击失败。

## 4. 命中判定

以下情况必中：

- 攻击者所属 player 正在聚能。
- 技能目标类型为 EnemyAll、All、Self、AllyBody 或 Ally。

其余情况：

```text
命中率 H = clamp(floor(攻击者.HitRate - 目标.EvasionRate - 掩体.evadeChance), 0, 100)
随机整数 1～100 <= H：命中，否则未命中
```

无掩体时惩罚为 0。H 为 100 时不掷骰，为 0 时必定未命中。必中仍受掩体减伤。

未命中播放 Dodge 和 Miss，结束本目标结算。**命中后算出 0 伤害仍算命中。**

## 5. 模式识别与显式暴击

开启 isRecognitionCheck 时，使用调用方传入的 CheckResult。玩家对首个目标掷 3D6，所有目标共用结果。

设 R 为 3D6 总和（3～18），C 为目标 CON：

| 条件／结果 | 伤害倍率 M | 强制暴击 |
| --- | --- | --- |
| R < C：DamageReduced | 0.4 | 否 |
| C <= R < 1.5C：DamageIncreased | 1.3 | 否 |
| R >= 1.5C：MustCrit | 1.3 | 是 |
| None 或未开启识别 | 1 | 否 |

当前 RECOGNITION 不参与骰点计算。管理器不重掷识别骰；敌人 CastSkillOnTarget 默认传入 None。

每个命中目标再判定一次显式暴击：

```text
识别必暴击：直接暴击
否则：随机整数 1～100 <= critRate 时暴击
暴击倍率 K = critDamageRate == 0 ? 1 : critDamageRate / 100
```

各段共用显式暴击结果。显式暴击跳过暗骰；输入 AttackPack.isCritical 不作为强制暴击依据。

## 6. 每段基础伤害与护甲

```text
A0 = 技能威力 + 对应类型攻击力 + 通用 ATK + 对目标元素的克制加伤
```

类型按 attackPack.damageType 查询：Melee 为动能，Ranged 为热能，Electric 为火种。它与行动类型不同。

有效掩体的 damageReduction > 0 时：

```text
A = trunc(A0 × (100 - 掩体.damageReduction) / 100)
```

否则 A = A0。读取对应类型护甲 D0，再修正：

```text
动能：D = trunc(D0 × (1 - 目标.MeleeArmorPercent / 100))
热能、火种：D = D0
```

正的 MeleeArmorPercent 会降低动能有效护甲。

## 7. 暗骰伤害分支

非显式暴击时，每段独立掷四颗六面骰：

```text
S = 4D6 总和（4～24，不是均匀随机整数）
Q = A + S - D
```

| 条件 | 基础伤害 B |
| --- | --- |
| 显式暴击或 Q > 24（大成功） | floor(A × K)，无视护甲 |
| Q < 6（失败） | floor(A × 0.2) |
| 6 <= Q <= 24（普通成功） | max(0, A - D) |

暗骰只决定分支，不直接加到伤害。暗骰失败仍算命中。高护甲下，普通成功可能为 0，失败反而造成 20% 伤害。

暗骰大成功使用暴击倍率，但不会把 IsCritical 标记改为 true。

## 8. 增减伤、聚能与夹击

先计算增伤、减伤和识别倍率：

```text
B1 = trunc(B × (100 + 攻击者.DamageIncrease) / 100
             × (100 - 目标.DamageReduction) / 100 × M)
```

三个倍率相乘后取整一次。此处不限制百分比范围，最终伤害保底为 0。

聚能时，使用 PlayerController 的目标和累计值 T：

```text
首次攻击或换目标：B2 = B1，T = B1
继续攻击同一目标：B2 = trunc(1.2 × B1) + trunc(0.2 × T)，T += B2
未聚能：B2 = B1
```

- 1.2 和 0.2 来自 GameConst，不是 GameConstSO 的同名字段。
- 两项分别取整。多段逐段累计，同一玩家控制器的棋子共享累计。
- 换目标、开始或结束聚能时重置；目标结算顺序会影响结果。
- 累计发生在夹击、屏障和扣血之前，包含被挡住或超出剩余 HP 的伤害。

最后处理夹击：

```text
isFlank 为 true：B3 = trunc(B2 × SO.FlankDamageRate)
否则：B3 = B2
最终伤害 F = max(0, B3)
```

夹击倍率默认 0.7，以 SO 为准。需调用方传入 isFlank；玩家协同普攻传入 !isOrder，敌人 CastAttackOnTarget 默认 false。

## 9. 屏障、扣血与零伤害

若命中前记录到敌对目标有阻速场：

- 用 F 扣耐久，耐久 <= 0 时移除屏障。
- 本次所有伤害段均不扣 HP；首段破盾后，后续段仍被阻挡。
- 无破盾溢出，不执行该目标的 Buff、击退等附加效果。
- 预览显示 HP 伤害 0，不显示耐久损失。

无屏障时进入 TakeDamage：

- 忽略已死亡目标和负数伤害；HP 减 F，最低为 0。
- **0 伤害也跳字显示 0，并对存活目标播放受击和伤害特效。** 致死时走死亡流程。
- 0 伤害不发 NotifyHpChanged，但仍有 Hurt、OnHurt、停顿、受击充能及管理器受击通知。
- 管理器处理击杀通知和敌人伤害记录。DamageInfos 只记录正伤害，0 不入该列表。

## 10. 附加效果与充能

目标合法、命中且未被屏障阻挡后：

1. 检查 Buff 作用对象；每次独立掷 1～100，<= buffPack.rate 时添加。
2. 执行 skillEffects、additionalEffects。
3. 聚能时清除假死。技能未自带 HitBackEffect 时，追加默认击退。
4. 检查协同夹击，另走一次攻击流程。

```text
X = 本目标本次 DamageInfos 的伤害合计
默认击退距离 = 1 + X / 10 × 0.5
默认碰撞伤害 = trunc(X × 0.4)
```

夹击要求目标存活且敌对、没有重入锁。选择最近的存活队友，且其 ableStrick 可用、近战范围能触及目标；敌方队友还需已激活。触发后消耗 ableStrick。

整次 PieceSkill 结束时，未聚能的玩家攻击充能 +10，多段、多目标只加一次，不要求正伤害。玩家 Hurt 充能 +5；已聚能或充满时不再增加。

ApplySKillEffectOnce 在整次技能结束时执行，不受单目标附加效果分支限制。

## 11. 伤害预览

预览共用上述校验和公式，枚举随机分支，不掷骰或修改战斗状态。

1. 显示**命中后的伤害**。未命中的 0 只体现为命中率；命中率为 0 时也计算条件伤害。
2. 按暴击率保留可能分支：0% 无随机暴击，100% 无普通分支，识别必暴击优先。
3. 开启识别但未传结果时，遍历 None、减伤、增伤、必暴击，不按 CON 收窄。
4. 遍历暗骰总和 4～24 的全部可能值；在聚能副本中逐段累计，保留后续计算需要的累计极值。
5. 同类型伤害合并为 ByType，总伤害为 Total。总区间按同一识别／暴击分支计算，不能直接相加各类型跨分支的上界。
6. 固定伤害显示单值，否则显示区间。血条使用 Total / 最大生命；未绑定黄色层时仅显示红色预览层。

护甲或屏障造成的真实 0 仍保留。伤害不按剩余 HP 截断，血条比例会限制范围。

预览只反映请求时的状态，不预测消耗、行动触发、被动属性变化或其他目标先结算的聚能变化；不包含后续夹击、碰撞和持续伤害。

## 12. 绕过通用公式的伤害

以下直接调用 TakeDamage，不经过管理器的楼层、命中、护甲、暴击、聚能或屏障检定：

| 来源 | 规则 |
| --- | --- |
| 击退碰撞 | hitBackDamage 点动能伤害 |
| 自发模因攻击 | 每次行动 ceil(最大生命 × 15%)；另有 30% 概率随机干扰、束缚或眩晕 |
| 认知反噬 | 自身回合开始 ceil(最大生命 × 20%) |
| 燃烧 | 当前层数点火种伤害，然后减少 3 层 |
| SelfExplosionEffect.ApplyEffect | 调用时自伤 100 点热能 |

这些伤害仍有扣血、跳字和受击反馈，但不会自动触发管理器的通知、日志和仇恨登记。详见 [Buff 系统](BuffSystem.md)。

## 13. 数值示例

热能攻击 13、技能威力 10、目标热能护甲 15、暴击倍率 150%。通用 ATK 和克制加伤为 0，无其他修正。

```text
A = 10 + 13 = 23
Q = 23 + 4D6 - 15 = 8 + 4D6
普通成功（4D6 <= 16）：23 - 15 = 8
暗骰大成功（4D6 >= 17）或显式暴击：floor(23 × 1.5) = 34
```

命中后区间为 **8～34**。100% 显式暴击时为 34；识别必暴击还需乘 1.3。此例没有暗骰失败分支，也不计未命中的 0。

聚能例：同目标连续两段 B1 均为 10，初始无累计。

```text
第一段：10，T = 10
第二段：trunc(1.2 × 10) + trunc(0.2 × 10) = 14，T = 24
合计：24（尚未处理夹击、屏障和扣血）
```

## 14. 主要代码

| 文件 | 用途 |
| --- | --- |
| DamageManager.cs / DamageCalculator.cs | 共用公式、检定、聚能、预览、4D6 |
| BattleManager.cs / DiceCheckManager.cs | 目标流程、掩体、附加效果、夹击、3D6 |
| BuffManager.cs / UnitAttrCenter.cs | 防护、屏障、持续伤害、扣血反馈 |
| PieceController.cs / EnemyController.cs / PlayerController.cs | 调用参数、受击、聚能状态 |
| AttrCenter.cs / SkillPack.cs | 属性、伤害类型、技能开关 |
| GameConst.cs / GameConstSO.cs | 常量与配置 |
| HitInfoPanel.cs / HpBarUI.cs | 数字与血条预览 |
