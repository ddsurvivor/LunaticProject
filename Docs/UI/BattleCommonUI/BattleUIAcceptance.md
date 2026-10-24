# 战斗 UI 优化验收记录

执行时间：2026-10-24 07:30–07:42（北京时间，单次自动化 ui）。

结论：**部分实现，实际战斗验收受阻，整体未通过验收。** 本文所有“静态通过”仅指源码/资源检查，不代表游戏运行通过。

## 环境与保护

- 已完整阅读 BattleUIOptimizationPrompt.md、根 AGENTS.md、代码注释规范、设计 README，以及菜单、详情、UIManager、ClickManager 和头顶/范围入口代码。
- 磁盘 `Library/LastSceneManagerSetup.txt` 记录活动场景 `Assets/Scenes/PR2BATTLE.unity`。尚无法从编辑器确认当前加载状态或是否 dirty。
- Unity 2021.3.23f1 进程 PID 27516 存在，但 Windows Computer Use `sky.list_windows()` 没有返回 Unity 编辑器窗口（只有 Unity Hub 等窗口）。没有可用窗口可观察或输入，未盲发按键或重复启动项目。
- `unity status --format json` 返回 `STATUS_NO_INSTANCES`；manifest 未配置 Pipeline。更新检查因 127.0.0.1:9 拒绝连接失败，同时提示 CLI beta.13 有 beta.14 可用。未升级 CLI/Unity、未安装包、未结束进程。
- 保留任务开始时的所有角色、技能、AI、GameConst、PieceInfoPanel 等改动；未保存、切换或丢弃任何场景，未改角色配置，未提交/推送 Git，未创建聊天或追加定时任务。
- 开始状态及共用 prefab 备份位于 `tmp/BattleUIAcceptance/`，该目录由 Git 忽略。

## 本批实现与场景接入

| 项目 | 修改文件 | 已实现 | 验收状态 |
|---|---|---|---|
| P0 一级行动列表 | PieceActionListPanel.cs、PieceActionListPanel.Layout.cs、ActionBtn.cs、HoverScale.cs、UIManager.cs | 移除硬编码 1920 判定；棋子投影到当前 Canvas 父坐标；安全区内整组翻转、底部上移；纵向滚动背板；名称/AP/箭头分列；正常/悬停/禁用/当前分类；待机显示真实剩余 AP；重开重新绑定环境交互 | 编译通过，运行未验证 |
| P0 二级菜单 | PieceActionListPanel.cs / .Layout.cs | 沿触发行展开；切换时清理悬停；超出初始 10 个按钮后扩容并复用；隐藏条目不参与布局；空列表保持最小面板；道具过滤非消耗品及零数量；使用 interactable；技能/道具/警戒点击前重新校验；关闭时清理提示 | 编译通过，运行未验证 |
| P0 左上详情 | SkillTooltipUI.cs、SkillTooltipUI.Layout.cs | 固定安全区左上，顶部预留 160 Canvas 单位；深色底、铜金边、标题/正文分层；按实际宽度测量正文；最大高度受视口限制；长正文 ScrollRect；空数据关闭提示；保留实际配置内容和原显示生命周期 | 编译通过，运行未验证 |
| 共用 prefab 接入 | Assets/Prefab/Battle/BattleScene.prefab | 为既有 PieceActionListPanel 绑定 MenuRowFill/Outline，为既有 SkillTooltipUI 绑定 PanelFill/Outline；通过原脚本入口装配，不另建预览场景 | 静态通过，运行未验证 |
| 已认可左下信息面板 | 未修改其布局或脚本 | 备份对比证明本批 prefab 仅新增四个 Sprite 引用；出框立绘、姓名下置、菱形 AP、12px 血条、SceneBackdrop 原样保留 | 静态保护通过，实际场景回归未验证 |
| 设计生成源 | Tools/Art/generate_battle_ui.py | 同步菜单 230px 宽、44px 行高、46px 步进及背板；示意文字去除虚构预计伤害 | 源码语法检查；未重生成素材/设计 PNG |

以上脚本位于 `Assets/Scripts/Battle/UI/`，HoverScale 位于 `Assets/Scripts/Battle/Tool/`，UIManager 位于 `Assets/Scripts/Battle/Managers/`。两个新增 partial 文件带 meta，保留原组件 GUID 与序列化字段。

静态检查确认 PR2BATTLE 引用共用 BattleScene prefab。针对菜单根、菜单组件、二级根和详情组件检索场景 override，仅发现 Odin 的 `serializationData.Prefab` 来源记录，未发现这些目标上的视觉字段覆盖。其他场景与编辑器未保存 overrides 未验证。

## 验收证据（均未入库）

| 证据 | 内容与结论 |
|---|---|
| tmp/BattleUIAcceptance/git-before.txt | 开始时的未提交改动清单 |
| tmp/BattleUIAcceptance/BattleScene.before.prefab | 开始时 prefab 完整备份 |
| tmp/BattleUIAcceptance/PieceActionListPanel.before.cs、SkillTooltipUI.before.cs | 本批主要行为入口备份 |
| tmp/BattleUIAcceptance/static-report.json | 原 prefab 去掉四条新绑定后与备份逐字一致；引用 Sprite GUID 均存在；PR2BATTLE 引用与目标 override 记录 |
| tmp/BattleUIAcceptance/prefab-bindings.diff | 相对本次任务开始时的精确 prefab 差异 |
| tmp/BattleUIAcceptance/Compile/compiler.log | Unity 2021 自带 Roslyn，复用 Library/Bee 编译响应文件、实际引用和宏，输出 DLL 到 tmp；退出码 0，有其他业务文件警告，无错误 |
| tmp/BattleUIAcceptance/Compile.ps1、verify_static.py | 可复执行的临时检查入口，均在 tmp，无正式脚本依赖 |
| tmp/BattleUIAcceptance/environment.txt | 连接阻塞与桌面窗口观测记录 |

**没有获得任何改造前后真实战斗截图。** 未运行独立预览冒充实际战斗，也未将已有设计 PNG 列为运行证据。编译不证明 Console、射线、滚动、对象生命周期或视觉效果正确。

## 真实场景待执行步骤

所有下列项目均为**未验证**，需在可观察、可控制的 Unity 编辑器中继续执行。

1. 先观察用户当前场景与 dirty 状态；不自动保存。进入真实 PR2BATTLE Play，间隔观察帧号递增与角色状态更新。先回归左下信息面板，在亮地、暗处、复杂纹理与角色重叠处留原图。
2. 用 `ClickManager.SelectActivePiece` 或既有数字快捷键选择满足 `CanControlPiece` 的行动者。截图记录角色、当前/最大 AP、HP、能量、弹药；观察菜单是否确实靠近棋子。
3. 一级列表：移动/缩放镜头、四角位置、动作数量变化、AP 不足；背板应阻挡地图点击，装饰不拦射线；重复展开检查监听器；待机应显示并消耗当前全部 AP。
4. 二级列表：技能/道具/警戒连续切换，空/1/多于 10 条、长中文名称、禁用项、角色切换与关闭重开。不得为造状态修改角色技能配置；验证滚动末行可达、分类高亮和费用一致。
5. 详情：连续悬停、长描述、空字段，检查顶部速度条间距、正文滚动是否能与悬停退出生命周期协调、道具图标缺失回退、长标题。当前滚动可操作性仍须重点验收。
6. 完整走通“选棋子 → 一级 → 技能二级 → 左上详情 → 点击进入瞄准 → 取消 → 重开 → 合法施放 → AP/HP/头顶刷新 → 行动者切换”，检查无残留提示和重复扣费。
7. 分别在 1920×1080、1280×720、1920×1200 执行上述视觉/边界检查；三种分辨率本次均未实际运行。截图保存到 tmp/BattleUIAcceptance/，记录场景、分辨率、角色、真实数值和操作。
8. 检查实际 Console 新错误、空引用、缺失 Sprite、射线遮挡、持续运行对象数与分配。静态编译不能替代此项。

## 未完成实现与后续批次

遵循文档“小批修改后运行验证再推进”的顺序，本批运行关卡受阻，没有继续扩展以下区域：

| 优先级/项目 | 当前状态 | 必需覆盖 |
|---|---|---|
| P0 敌我共用头顶状态 | 未实现本轮统一结构 | PlayerHealthBarUI / EnemyCanvas / HpBarUI 职责整合；紧凑 AP、阵营标识、Buff 缺图/溢出/全量查看、有限避让；受伤/治疗/AP容量变化/死亡/离场/池复用 |
| P0 移动、攻击、技能范围及瞄准 | 未实现本轮视觉统一 | 保留 RangeUI / SkillTargeting 的世界投影和真实规则；青/琥珀/红及形状辅助、准星、深度闪烁；各实际形状、阻挡/高差/取消/切换/目标离场一致性 |
| P1 速度条 | 未改动 | 保留 100←0 方向；阵营/当前者、头像重叠与缺图；死亡/新参战/聚能队列 |
| P1 聚能、回合、日志、通知 | 未改动 | 状态分层、紧凑日志、消息避让；聚能充满/消耗/取消、连续消息、回合交接 |
| P2 开始、胜败、暂停、退出 | 未改动 | 统一层级与按钮状态；实际胜败/重试/退出、取消、输入/时间恢复与奖励/跳转 |

当前最优先的下一步是恢复可观察的 Unity 编辑器控制通道，完成本批真实场景回归；**本任务未宣称整体完成或通过验收**。
