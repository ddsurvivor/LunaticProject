# 当前伤害系统：检定与计算步骤

核对日期：2026-09-22。本文记录当前代码的实际行为；字段默认值与场景中的实际配置可能不同。

## 1. 入口、范围与取整约定

- 战斗入口：`BattleManager.PieceSkill`。玩家和敌人调用此入口时共用相同规则。
- 单目标结算：`DamageManager.ResolveSkillTarget`。
- 预览入口：`DamageManager.PreviewSkill`，由 `HitInfoPanel` 请求。
- 共用公式：`DamageManager.CalculateDamage` → `DamageCalculator.CalculateDamage`。
- 最终扣血及反馈：`UnitAttrCenter.TakeDamage`。
- `attackPacks` 的每个元素是一段伤害。每个目标判定一次命中、一次显式暴击；命中后逐段计算暗骰和伤害。
- `PieceSkill` 先剔除空目标并去重，再按目标列表顺序结算。管理器自身不按 `atkTimes` 再循环；额外攻击次数取决于调用方。

本文用 `trunc(x)` 表示 C# `(int)x`（向零截断），`floor(x)` 表示向下取整。对非负数两者相同，但不能把各步骤的取整合并到最后一次。

## 2. 目标合法性与楼层检定

结算、预览都调用 `DamageManager.CanAffect`，满足以下条件才继续：

1. 攻击者存在且未死亡。
2. 目标、技能存在。
3. `BuffManager.CanTarget` 允许选中：友方可选；敌方若具有 `CognitiveProtection`（认知防护），不可选。
4. 通过技能的单层限制：

```text
layerSkill == false：不限制双方高度差
layerSkill == true ：abs(攻击者.position.y - 目标.position.y) <= 0.1
```

**楼层检定已经纳入当前结算和预览。** 这是世界坐标 Y 值比较，不读取楼层编号。高度差恰好为 0.1 时允许（边界以浮点运算结果为准），大于 0.1 时不允许。

- `SkillPack.layerSkill` 默认是 `false`，需在具体技能中开启。
- 不通过时：本目标不掷命中或伤害骰，不扣血、不触发针对该目标的附加效果；预览命中率及伤害为 0。
- 聚能、必中、必暴击不能绕过楼层限制。
- 当前没有“高打低增伤”“低打高减命中”等高度倍率。
- 攻击距离、范围形状和目标阵营筛选主要由调用方及 `SkillManager` 负责，`CanAffect` 不再检查射程。
- `CanAffect` 本身不排除死亡目标；最终 `TakeDamage` 会忽略已死亡单位。不要把这个入口当成完整的技能选目标校验。

## 3. 被攻击触发与屏障快照

通过目标校验后、命中判定前：

1. 敌对攻击调用 `BuffManager.OnAttacked`。
2. 目标有 `OffensiveCognitiveProtection` 时，对攻击者施加或刷新 `MemeticRetaliation`，因此未命中也可能招致反噬。
3. 记录目标此时是否具有 `SlowingField`（阻速场），作为本次攻击的屏障状态。

屏障只阻挡敌对攻击。此时只记录状态，实际耐久扣减在伤害计算完成后执行。

## 4. 掩体检定

`BattleManager.CheckCoverObstruction`：

1. 目标必须有 `CurCaverSlot`。
2. 从攻击者位置向目标位置发射射线，起终点均上移 1.2。
3. 用 `Physics.RaycastAll` 获取沿途碰撞体。
4. 命中目标关联掩体的 GameObject 时，该掩体生效。

生效掩体提供：

| 字段 | 用途 |
| --- | --- |
| `evadeChance` | 从攻击命中率中扣除的百分点 |
| `damageReduction` | 在护甲、暗骰前削减基础伤害的百分比 |

这不是通用“障碍物完全挡住攻击”的检测；不会因为射线碰到任意墙体就直接判定攻击失败。

## 5. 随机命中判定

满足任一条件，命中率直接为 100%：

- 攻击者所属 `player` 存在且 `isBursting == true`。
- 技能 `target` 是 `EnemyAll / All / Self / AllyBody / Ally`。

其余情况：

```text
H = clamp(floor(攻击者.HitRate - 目标.EvasionRate - 掩体.evadeChance), 0, 100)
命中骰 U = 随机整数 1～100
U <= H：命中
U > H ：未命中
```

- 无有效掩体时，掩体命中惩罚为 0。
- H = 100 时不掷命中骰；H = 0 时必定未命中。
- 必中免除命中率惩罚，但仍可能受到掩体减伤。
- 未命中播放 Dodge 和 Miss，结束本目标的伤害及附加效果；其他目标继续。
- 命中之后即使算出 0，也属于有效命中，不是 Miss。

## 6. 模式识别检定

技能 `isRecognitionCheck` 开启时，伤害管理器使用调用方传入的 `CheckResult`。

玩家技能调用方使用 `DiceCheckManager.ModeRecognitionCheck` 时，先对选中列表首个目标进行 3D6 检定，再把结果传入本次 `PieceSkill`，由各目标共用：

```text
R = 3D6（3～18）
C = 目标.CON
```

| 条件／传入结果 | 识别伤害倍率 M | 强制暴击 |
| --- | --- | --- |
| R < C → DamageReduced | 0.4 | 否 |
| C <= R < 1.5 × C → DamageIncreased | 1.3 | 否 |
| R >= 1.5 × C → MustCrit | 1.3 | 是 |
| None | 1.0 | 否 |
| 技能未开启识别检定 | 1.0 | 否 |

当前识别检定虽然读取攻击者的 `RECOGNITION` 属性，但未把它加进骰点总和。伤害管理器不会自行掷识别骰；调用方未传入时默认 `None`。当前敌人 `CastSkillOnTarget` 走的是该默认值。

## 7. 显式暴击判定

每个命中目标独立判定：

```text
若识别结果强制暴击：isCritical = true
否则：随机整数 1～100 <= 攻击者.critRate 时暴击
```

- 本目标各段共用该显式暴击结果，不传递到后续目标。
- 暴击倍率使用 `critDamageRate / 100`；字段为 0 时按 100% 处理。
- 显式暴击不再掷该段暗骰，直接走暴击公式。
- 当前计算不读取输入 `AttackPack.isCritical` 来决定是否强制暴击。

## 8. 每段基础伤害

以下每一步对每个 `attackPack` 单独执行。

```text
A0 = attackPack.damage
   + 攻击者.attr.GetAtk(attackPack.damageType)
   + 攻击者.ATK
   + 攻击者.attr.GetAddDamage(目标.elementType)
```

四项依次为：技能威力、对应伤害类型攻击力、通用攻击力、对目标元素的克制加伤。

| DamageType | 当前中文名称 |
| --- | --- |
| Melee | 动能 |
| Ranged | 热能 |
| Electric | 火种 |

伤害类型与行动类型不是同一个概念；护甲和分类型攻击力都按该段 `damageType` 查询。

## 9. 掩体减伤与护甲修正

有效掩体的 `damageReduction > 0` 时：

```text
A = trunc(A0 × (100 - 掩体.damageReduction) / 100)
```

否则 `A = A0`。

读取该段对应护甲：

```text
D0 = 目标.attr.GetArmor(attackPack.damageType)
```

仅 `damageType == Melee` 时修正护甲：

```text
D = trunc(D0 × (1 - 目标.MeleeArmorPercent / 100))
```

热能、火种直接使用 `D = D0`。正的 `MeleeArmorPercent` 在当前公式中降低动能有效护甲。

## 10. 暗骰与基础伤害结果

非显式暴击时，每段独立掷 4D6：

```text
S = 4D6（4～24；是四颗六面骰之和，不是均匀随机 4～24）
Q = A + S - D
K = critDamageRate == 0 ? 1 : critDamageRate / 100
```

按以下优先级取得伤害 B：

| 条件 | B |
| --- | --- |
| 显式暴击，或 Q > 24（暗骰大成功） | floor(A × K)，无视护甲 |
| 非显式暴击且 Q < 6（暗骰失败） | floor(A × 0.2) |
| 非显式暴击且 6 <= Q <= 24 | max(0, A - D) |

注意：

- 暗骰影响的是伤害分支，不是把骰点直接加进最终伤害。
- 暗骰失败仍是“已经命中”的一次攻击，不变成 Miss。
- 高护甲可能让普通成功为 0，而暗骰失败反而造成 20% 伤害；这是当前公式的实际结果。
- 暗骰大成功使用暴击倍率，但不会把结算的 `IsCritical` 标记改成 true。因此暴击标记／镜头效果与高倍率伤害并不完全等价。

## 11. 增伤、减伤与识别倍率

```text
B1 = trunc(
    B
    × (100 + 攻击者.DamageIncrease) / 100
    × (100 - 目标.DamageReduction) / 100
    × M
)
```

此步骤在三个倍率相乘后取整一次。掩体减伤已在第 9 步取整，不能与此处合并计算。百分比参数在本步骤没有额外 clamp，最终伤害才保底为 0。

## 12. 聚能追加伤害与累计

仅攻击者所属 `player.isBursting` 时执行。累计状态存放在 `BattleManager.PlayerController`：

- `burstTarget`：当前连续攻击的目标。
- `totalDamage`：该目标当前聚能累计值 T。

换目标或首次攻击：

```text
B2 = B1
burstTarget = 当前目标
T = B1
```

继续攻击相同目标：

```text
B2 = trunc(1.2 × B1) + trunc(0.2 × T)
T = T + B2
```

- 实际使用 `GameConst.burstDamageRate = 1.2`、`burstAddDamageRate = 0.2`；并非 GameConstSO 中的同名属性。
- 两项分别取整后相加。
- 多段攻击逐段更新累计，所以第二段即可吃到第一段累计。
- 累计属于玩家控制器，共享于该控制器的棋子，不是每个攻击者一份。
- 切换目标就重置，不为每个敌人分别保存累计；范围攻击的目标顺序会影响聚能结果。
- 累计先于夹击、屏障和实际扣血；它不是目标实际损失的 HP，包含被屏障挡住的计算伤害。
- 开始、结束聚能时清空目标及累计。

## 13. 夹击倍率与最终保底

调用方明确传入 `isFlank == true` 时：

```text
B3 = trunc(B2 × GM.Ins.DM.gameConstSO.FlankDamageRate)
```

否则 `B3 = B2`（未聚能时用 B1）。

```text
最终计算伤害 F = max(0, B3)
```

`FlankDamageRate` 的代码默认值为 0.7；运行时以配置资产为准。是否应用倍率取决于传参，不是只要旁边有队友就自动乘倍率。玩家协同普攻使用 `isFlank: !isOrder`；当前敌人 `CastAttackOnTarget` 未传该参数，走默认 false。

## 14. 屏障、扣血与零伤害反馈

若第 3 步记录到敌对目标有屏障：

1. 用 F 扣减屏障耐久，耐久 <= 0 时移除屏障。
2. 整次攻击的各段都不进入 `TakeDamage`，不把破盾溢出传给生命值。
3. 即使第一段破盾，后续段仍被本次屏障快照阻挡。
4. 不执行本目标的 Buff、击退等附加效果。
5. 预览直接显示生命伤害 0，而不是屏障耐久损失。

无屏障时调用 `TakeDamage(new AttackPack(F, 类型, 显式暴击标记))`：

- 已死亡单位忽略；负数伤害忽略。
- 生命值减 F，最低为 0；刷新血条。
- 所有有效命中生成跳字，包括显示数字 0。
- 存活目标调用 `Hurt()`，播放受击及对应伤害特效；致死时走死亡流程。
- F = 0 时不发送 `NotifyHpChanged`，但仍会调用 `Hurt()` 和伤害管理器的 `NotifyTakeDamage`。
- `Hurt()` 含 OnHurt、停顿和玩家受击聚能充能，因此零伤害也会进入这些受击流程。
- 管理器发送受击通知、生命 <= 0 时发送击杀通知，并为敌人登记伤害记录。
- 当前战斗日志的 `DamageInfos` 只加入 F > 0 的条目；零伤害有跳字，但该列表不增加零伤害条目。

## 15. 附加效果、夹击触发与充能

本目标合法且命中、未被屏障阻挡时，由 BattleManager 继续处理：

1. 按 `buffPack.target` 检查作用对象。
2. 每次 Buff 概率检测独立掷 1～100，骰点 <= `buffPack.rate` 时添加。
3. 执行 `skillEffects` 和 `additionalEffects`。
4. 聚能时处理假死清除；若技能没有自带 HitBackEffect，追加默认击退：

```text
X = 本目标本次 DamageInfos 的伤害合计
默认击退距离 = 1 + X / 10 × 0.5
默认碰撞伤害 = trunc(X × 0.4)
```

5. 检查协同夹击：目标活着且敌对、没有夹击重入锁；从可夹击且近战范围能触及目标的队友中，选择离目标最近者。敌方队友还需已激活。触发后消耗其 `ableStrick`，协同普攻另走一次攻击流程。

整次 `PieceSkill` 结束后：

- 玩家棋子且未聚能：攻击充能 +10（`GameConst.attackBurstCharge`），多段、多目标在此入口只充一次；不要求产生正伤害。
- 玩家 `Hurt()`：受击充能 +5（`GameConst.hurtBurstCharge`）；已聚能或能量满时 `ChargeBurst` 不再增加。
- 执行 `ApplySKillEffectOnce`；这类一次性效果不受单目标的 `CanApplyEffects` 分支控制。

## 16. 预览如何与实战统一

预览和实战共用目标校验、上下文、护甲和完整伤害公式。区别只在输入的随机结果和是否提交副作用：

1. 用同一个楼层／认知防护检查和命中率公式。
2. 显示的是“假设命中后的直接伤害”，未命中概率只出现在 HitRate；即使命中率为 0，也计算条件性的命中伤害。
3. 根据暴击率遍历可能的显式暴击分支；0% 排除随机暴击、100% 排除普通分支，识别必暴击优先。
4. 开启识别而未提供结果时，覆盖 None、减伤、增伤、必暴击四个分支；不根据目标 CON 收窄识别结果。
5. 对每段遍历所有 4～24 暗骰总和，找伤害上下界，不仅取最小／最大骰点两个端点。
6. 在聚能状态副本上按顺序累计；夹击取整相同时，保留累计值的极值用于后续段。
7. 合并同类型各段伤害，生成 `ByType`；生成整次直接伤害合计 `Total`。总区间按同一识别／暴击分支计算，不能随意将各类型跨分支的上界再次相加。
8. UI 固定值显示一个数字，不固定值显示区间；血条用同一 Total 除以最大生命值。黄色层未绑定时只显示现有红色预览层。
9. 不掷随机数、不扣血、不提交聚能、不触发受击或 Buff。

预览以请求时的状态和目标为前提。不预测技能消耗／行动触发／被动导致的属性变化，不包含后续夹击、碰撞、持续伤害，也不模拟其他目标先结算后的聚能状态。最大伤害是计算伤害，不以目标当前剩余 HP 截断；血条显示会限制到合法比例。

## 17. 不经过以上公式的伤害

以下路径直接调用 `UnitAttrCenter.TakeDamage`，不经过 DamageManager，因此不执行它的楼层、命中、护甲、暴击、聚能或屏障检定：

| 来源 | 当前数值规则 |
| --- | --- |
| 击退碰撞 | 使用 HitBackEffect 的 hitBackDamage，按动能直接扣血 |
| 自发模因攻击 | 每次行动 ceil(最大生命 × 15%)；另有 30% 概率随机施加干扰／束缚／眩晕之一 |
| 认知反噬 | 自身回合开始 ceil(最大生命 × 20%) |
| 燃烧 | 当前燃烧层数点火种伤害，随后减少 3 层 |
| SelfExplosionEffect.ApplyEffect | 自伤 100 点热能（该效果方法被调用时） |

这些调用仍走 TakeDamage 的扣血、跳字和受击反馈，但不会自动经过 DamageManager 的伤害通知、伤害日志和敌人仇恨登记流程。Buff 细节见 [BuffSystem.md](BuffSystem.md)。

## 18. 数值核对示例

假设：热能攻击力 13，技能热能威力 10，通用 ATK 和克制加伤均为 0；目标热能护甲 15，无掩体、增减伤、识别、聚能、夹击；暴击倍率 150%。

```text
A = 10 + 13 = 23
D = 15
Q = 23 + 4D6 - 15 = 8 + 4D6，范围 12～32
```

- 非显式暴击、4D6 <= 16：普通成功，23 - 15 = 8。
- 非显式暴击、4D6 >= 17：暗骰大成功，floor(23 × 1.5) = 34。
- 显式暴击：34。
- 因此非强制显式暴击的完整命中后区间为 **8～34**；若显式暴击率 100% 或识别必暴击，需按对应分支重新计算（识别必暴击还乘 1.3）。
- 这里不存在暗骰失败分支，也不把未命中的 0 加入区间。

聚能追加例：同目标两段进入聚能步骤前均为 10，初始没有聚能目标：

```text
第一段：10，累计 T = 10
第二段：trunc(1.2 × 10) + trunc(0.2 × 10) = 14，累计 T = 24
合计：24（夹击、屏障、实际扣血尚未处理）
```

## 19. 维护时应检查的源码

| 文件 | 对应规则 |
| --- | --- |
| Assets/Scripts/Battle/Managers/DamageManager.cs | 目标／楼层／命中、通用公式、聚能、预览 |
| Assets/Scripts/Battle/Tool/DamageCalculator.cs | 4D6、阈值 6／24、暴击与取整 |
| Assets/Scripts/Battle/Managers/BattleManager.cs | 目标循环、掩体射线、附加效果、夹击、碰撞 |
| Assets/Scripts/Battle/Managers/DiceCheckManager.cs | 3D6 模式识别 |
| Assets/Scripts/Battle/Managers/BuffManager.cs | 认知防护、屏障、反噬、持续伤害 |
| Assets/Scripts/Battle/Controllers/UnitAttrCenter.cs | 属性来源、最终扣血、零伤害反馈 |
| Assets/Scripts/Battle/Controllers/PieceController.cs | 玩家攻击／技能传参、Hurt |
| Assets/Scripts/Battle/Controllers/EnemyController.cs | 敌人攻击／技能传参 |
| Assets/Scripts/Battle/Controllers/PlayerController.cs | 聚能状态、充能和累计重置 |
| Assets/Scripts/Battle/Data&Config/AttrCenter.cs | 对应类型攻击、护甲和克制加伤 |
| Assets/Scripts/Battle/Data&Config/SkillPack.cs | layerSkill、isRecognitionCheck、attackPacks |
| Assets/Scripts/Battle/Tool/GameConst.cs、SO/GameConstSO.cs | 常量与运行时配置来源 |
| Assets/Scripts/Battle/UI/HitInfoPanel.cs、HpBarUI.cs | 数值和血条预览 |

