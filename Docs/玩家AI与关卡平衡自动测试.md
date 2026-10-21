# 玩家 AI 与关卡平衡自动测试

这套测试使用普通 C# 控制器，不调用 AI Agent、网络服务、鼠标模拟或大模型。Unity 运行时，玩家 AI 会通过正式战斗接口操控当前我方棋子，敌方仍运行关卡自身的 AI。

## 在 Unity 中使用

1. 打开要测试的战斗场景，进入 Play Mode。场景需具备可正常手动战斗的 GM、BattleScene、玩家/敌人、碰撞体及 NavMesh。
2. 点击 **Tools → 战斗自动测试 → 创建玩家 AI 测试控制器**。选中的 `BattleBalanceRunner` Inspector 中可以配置战术风格、技能、聚能、随机种子和连续局数。
3. 点击 Inspector 的 **开始关卡自动测试**，或菜单 **Tools → 战斗自动测试 → 开始当前关卡**。
4. 停止时点击 **停止并导出报告**。菜单 **打开最近报告** 可以定位导出文件。

也可以在编辑状态创建该对象、勾选“进入运行状态后自动开始”，再保存到测试场景。无需打开 Codex 即可运行；直接关闭或禁用组件也会释放输入并恢复测试前的玩家存档引用。

控制器会请求正常开战、确认已有合法部署、跳过战前巡视及说明教程。出生点没有合法可用站位时仍会失败并输出 `StartupTimeout`，不会把棋子强制放到非法位置。暂停游戏时不下指令，也不累计正式战斗的超时预算。

建议先从场景初始状态运行单局，检查玩家行动是否符合关卡预期，再把连续局数改成 20～100。运行期间不要同时操作战斗按钮；鼠标棋子操作由测试控制器独占接管。

## 玩家 AI 能做什么

- 近战、射击、主动技能、回复、空弹匣装填和结束行动。
- 使用正式 `SkillTargeting.Query` 判断命中，使用正式 `DamageManager.PreviewSkill` 比较预期伤害、击杀和友军误伤。
- 比较 AOE 中点、扇形/弧形朝向，优先选择能提高收益的合法落点。
- 使用正式 NavMesh 路径预算接近攻击位置；跨层时尝试现有梯子，不能到达时让出行动。
- 可选使用已有聚能接口；默认关闭，以便先建立不依赖聚能的关卡基线。
- 默认不使用会消耗背包道具的技能；需要评估道具资源时可单独开启。

三种战术风格：**均衡**、**优先击杀**、**节省技能资源**。它们是固定评分策略，不能代替熟练玩家操作。改变阵容、数值或关卡后，比较同一策略、同一组种子的结果更有意义。

“探索未唤醒敌人所在位置”默认开启。它仅用于规划探索移动，不允许直接攻击未激活单位；该选项利用场景中已配置的敌人位置，不是受战争迷雾限制的玩家视野。需要评估信息受限玩法时应关闭并另外实现探索策略。

回复技能通过“敌人 AI 技能用途”的回复标记、自动治疗 Buff 或治疗区效果识别；回复本身仍由正式技能效果执行。当前版本不主动使用普通背包道具、警戒指令、全部特殊场景交互或复杂任务目标，也不保证所有关卡都能通关。相关关卡可能得到超时/卡住结果，应先检查报告中的行动而非直接认定数值过难。

## 连续测试与存档隔离

每局完成后重载同一战斗场景，使用相同的初始玩家存档副本；种子依次为 `baseSeed + 局序号（从0开始）`。编辑器支持直接重载未加入 Build Settings 的场景；构建运行时需将被测场景加入 Build Settings。

测试过程中替换 `GM.PLAYERPROFILE` 为深拷贝，并让玩家棋子引用该副本；道具消耗、掉落和教程标记只作用于副本。测试结束恢复原存档引用。接管期间不执行正式胜负奖励、退出剧情场景或 `DataManager.SaveData` 写入。

停止测试不会回滚当前场景已经发生的移动、伤害或动画。若要继续手动调试，可在当前状态接管；若要重新测试同一起点，应重新载入场景。不要把正在运行的战斗状态当作已经还原的初始关卡。

固定种子控制测试开始后的 Unity 全局随机序列，退出时恢复原序列。物理、导航、帧时序及测试开始前的初始化随机数并非逐帧确定性回放；跨机器结果可能有差异。

## 报告

默认目录：`Application.persistentDataPath/BattleBalanceReports/<批次时间与ID>/`。Inspector 可指定其他报告根目录。

| 文件 | 内容 |
| --- | --- |
| `run-001-seed-1000.json` | 每局结果、策略、种子、Unity 版本、标准回合数、实际战斗秒数、初始/结束单位血量和位置，以及逐条命令的 AP、类型、落点、技能、原因和执行结果 |
| `summary.csv` | 每局一行，可直接用 Excel 分析胜负、耗时、剩余血量、阵亡和拒绝命令数 |
| `suite-summary.json` | 已运行局数、胜利、失败、未完成、平均回合/耗时，以及两种分母的胜率 |

结果区分 `Victory`、`Defeat`、`Timeout`、`Stalled`、`ActionLimit`、`Stopped`、`SceneChanged`、`StartupTimeout`、`Error`。只有正式 `PlayerWin/PlayerLoss` 回调才记录胜负，敌人暂时未激活或等待增援不算通关。

`rounds` 是游戏已有的标准时间轮，不是双方各行动一次。单位血量和位置来自运行时观察；未在观察前生成或已经销毁的对象不会被猜测为已击杀。报告不声称逐段累计伤害、治疗或人类胜率。

每局默认最多 600 秒、300 个标准时间轮、5000 条命令；90 秒没有血量/位置/新增单位变化则标记卡住。参数均可配置。行动日志最多保存 20000 条，命令总数仍继续统计。

## 不依赖 Agent 的命令行运行

构建程序可直接使用：

```powershell
& '.\Game.exe' -battle-balance -battle-balance-runs 20 -battle-balance-seed 1000 -battle-balance-report 'D:\BattleReports' -battle-balance-quit
```

Unity 编辑器可以通过静态批处理入口打开关卡并进入 Play Mode：

```powershell
& '<Unity安装路径>\Editor\Unity.exe' -batchmode -projectPath 'D:\GitHub\LunaticProject' -executeMethod Lunatic.BattleTesting.Editor.BattleBalanceMenu.RunFromCommandLine -battle-balance -battle-balance-scene 'Assets/Scenes/你的战斗场景.unity' -battle-balance-runs 20 -battle-balance-seed 1000 -battle-balance-report 'D:\BattleReports' -battle-balance-quit -logFile 'D:\BattleReports\unity.log'
```

不要额外传 `-quit`，否则编辑器可能在测试完成前退出。编辑器实例不能与另一个打开同一项目的实例并行运行。正式胜利/失败都是有效样本，退出码为 0；超时、卡住、取消或基础设施失败返回 2。运行构建需要在启动场景中正常初始化战斗。

## 代码隔离与扩展

自动测试代码全部位于 `Assets/BattleTesting`：`Runtime` 包含策略、宿主和报告，`Editor` 仅提供菜单。主战斗系统不引用测试命名空间，也不会主动创建测试组件；只有命令行明确传入 `-battle-balance` 或手动创建/启用控制器时才接管。

主系统新增的是可复用的通用接口：

- `BattleManager.TryAcquirePlayerControl/ReleasePlayerControl`：独占外部输入。
- `BattleManager.TryIssuePlayerCommand` 与 `PlayerBattleCommand`：经过当前角色、行动解锁、费用、命中范围和移动预算校验的正式命令。
- `BattleManager.BattleCompleted/LastOutcome`：只读观察正式胜负；重复结算被去重。
- `DiceCheckManager.ModeRecognitionCheck(..., showPresentation)`：保留正式掷骰和成功事件，可关闭交互展示。

增加新的玩家策略时替换或扩展 `PlayerAutoBattlePolicy`；增加特殊交互时先复用其已有公开接口，再按需扩展命令类型。不要在测试策略中直接扣敌人血量、填满资源、改变回合归属或把玩家临时改成敌人。

## 验证范围

独立回归脚本仅保留在本地 `tmp/Tests/`，不进入 Git；此处介绍的 Play Mode 自动移动棋子模块 `Assets/BattleTesting/` 是测试存放规则的保留例外。

`tmp/Tests/BattleRegression/Run.ps1` 可直接通过 PowerShell + .NET 8 执行，不需要 Agent 或 Unity 编辑器。它链接正式策略、命令、目标查询、扣费及伤害代码，测试独占控制、非法/锁定命令、单次扣费、异常中断、聚能结束隔离、装填/移动/回复/AOE 选择、正式伤害击杀和 JSON/CSV 报告。导航和动画使用替身。

完整宿主、Unity 场景重载、真实动画/物理和存档生命周期仍需按上述 Play Mode 流程验收；这些不能仅凭 .NET 回归认定已经通过。
