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

## 后续修复：行动面板仅显示“移动”

用户提供实际战斗截图，一级菜单仅有“移动”。检查 Editor.log 确认 `PieceActionListPanel.Init` 原第 41 行空引用：运行时为旧按钮添加 ActionBtn 后，旧按钮没有 costText，初始化给消耗列赋值时异常。此时字典仅登记第一个动作，而 menuReady 已提前置为 true，后续 ShowPanel 跳过初始化，因此攻击、技能、待机均无法登记。

修复：ActionBtn.Configure 为旧按钮补齐独立消耗 Text；布局完成后才设置 menuReady；新增 actionsInitialized，在全部动作绑定完成后才允许跳过初始化；重试前清理监听器，并防御缺失消耗引用。未修改角色技能配置、可用动作筛选或费用规则。

验证：使用 Unity 2021 项目引用离线编译通过，git diff --check 通过。异常摘录与编译结果在 tmp/BattleUIAcceptance/ActionMenuBug/。修复后的真实 Play 效果仍未验证；需重新进入 Play 确认移动、攻击、技能、待机按角色配置出现，并检查技能二级菜单。本次没有操作或保存用户当前运行场景。

## Action Panel 整体优化（2026-10-24 手动执行）

本节覆盖前文旧批次的行动菜单实现状态，执行规格为 ActionPanelOptimizationPrompt.md。

| 需求 | 本次落地 | 验收状态 |
|---|---|---|
| 等间隔逐行缓动 | ActionMenuTiming + ActionBtn.Reveal；单行 0.2s，间隔 min(0.2, 0.6/(n-1))，独立 Visual 子节点淡入/位移，unscaled time；关闭/复用取消序列 | 算法回归通过；实际视觉未验证 |
| 分类箭头/费用互斥 | 分类使用 Selector 三角 Image，隐藏 AP；展开固定 -90°，执行条目显示真实 AP | 资源与代码检查通过；实际视觉未验证 |
| 子菜单居中 | 可见二级面板中心对齐父行中心，整组约束安全区，空间不足翻转/滚动 | 代码检查；三分辨率未运行 |
| 父项选中锁定 | ExpandedLocked 独立状态；重复点击无操作，切分类/关面板复位 | 代码检查；实际输入未验证 |
| 统一按钮状态 | Normal/Hover/Pressed/ExpandedLocked/Disabled 由 ActionBtn 集中管理；禁用不触发悬停/费用预览，AP 灰色；分类不受子项费用不足影响 | 编译通过；实际输入未验证 |
| 按钮预制体 | 新增 Assets/Prefab/Battle/ActionButton.prefab；完整绑定 Button、Text、三角、边框、Visual、CanvasGroup、LayoutElement，无运行时补齐按钮结构；显式目录与对象池代替枚举/数组顺序绑定 | 本地 fileID、父子关系及字段检查通过 |
| AP/MP/弹药悬停预览 | ActionResourceCost 只读查询；ActionCostPreviewUI 接入实际跟随角色的 PieceStatePanel 和左下 PieceInfo；可用 AP 右侧点阵平滑呼吸，能量/弹药消耗区间叠层，所有退出路径清理 | 编译/接入引用检查；真实角色效果未验证 |
| 攻击二级分类 | 有能力的近战/远程攻击归入“攻击”；父级不扣费；具体条目保持原 StartNormalAttack 入口 | 编译/代码检查；实际攻击未验证 |
| 枚举双语与兼容 | ActionType 英文标识、原整数值、Odin LabelText；EnumExtensions.ToChinese/ToEnglish/ToDisplayName；读取现有设置实时刷新标签；EnumMember 及 JSON converter 兼容旧中文名称和字典键 | 133 条回归断言通过，包含动画计时 |

主要新增文件：ActionBtn 完整绑定预制体、ActionResourceCost.cs、ActionCostPreviewUI.cs、ActionMenuTiming.cs、ActionTypeLabels.cs（均有 meta）。PieceActionListPanel 两个文件重构，PieceInfoPanel/PieceStatePanel 接入预览；其他业务/战斗测试脚本的 ActionType 引用仅随枚举改名更新。BattleManager 的动作名称入口改为统一双语显示。GameConst/角色/技能数据值未改动。

角色身上状态的实际入口是 `Assets/Prefab/UI/PieceStatePanel.prefab`（UM.pieceStatePanel，跟随当前所选棋子）。原 PlayerHealthBar 处于停用状态，本次没有把停用资源当作验收入口。实际 PieceStatePanel 新增 AP 点阵、弹药条和资源预览组件；左下信息面板只追加预览组件及透明叠层，原几何与 SceneBackdrop 不变。

证据均在 `tmp/ActionPanelRework/`（未入库）：

- `enum-timing-tests.log`：133 条通过，执行正式 GameEnum、ActionTypeLabels/JSON converter、ActionMenuTiming；宿主语言/设置使用临时替身，JSON 使用项目实际 Newtonsoft 库。覆盖 15 个枚举整数与中文/英文名称、整数输出、中文/英文字典键读取、运行时中英标签及 0/1/2/4/10/25/100 行计时。不代表 Unity UI 运行测试。
- `compiler.log`：Unity 2021 Roslyn 使用真实项目引用与宏编译通过（退出码 0，保留现有其他业务警告）。
- `asset-validation.json`：新按钮全部 28 个对象/组件唯一 fileID；BattleScene 及 PieceStatePanel 新增组件本地引用/父子关系通过；本次启动前的 PR2BATTLE 场景文件逐字节保留。BattleScene 原对象仅四处变化：菜单预制体引用、PieceInfo 组件表、两条资源条的子节点表。
- `before/`：本次启动时的工作区差异、BattleScene、PR2BATTLE、PieceStatePanel 及枚举引用迁移前脚本备份。临时生成器、兼容测试与验证脚本也只在 tmp。

真实场景控制仍受阻：本次能列出 PR2BATTLE Unity 编辑器窗口，但首次截图返回 `FrameArrived timed out`；重新选择窗口恢复时 `activate_window` 再次超时。CLI 返回 STATUS_NO_INSTANCES。未继续盲发输入、未保存/切换/丢弃场景、未安装 Pipeline 或升级 Unity。没有本轮 Play 帧号、改造后截图或合法技能施放证据；不能宣称真实运行通过。

待实测重点：首次重新进入 Play 后确认新资源导入；0/1/2/4/10 行展开与中途关闭；攻击/技能/道具/警戒反复切换及已展开按钮重复点击；全部技能费用不足时父分类仍可展开；6 容量/4 剩余/2 AP 消耗只闪第 3、4 个点；MP 修正费、弹药消耗、资源为零和角色切换时无残留；三种分辨率及四角位置、长列表滚动；中英切换；完整攻击/技能合法执行与费用刷新。

补充：`Tools/Art/generate_battle_ui.py` 已同步新的“攻击”分类、分类三角箭头（选中旋转 90°）、费用列互斥和二级菜单居中示例，语法检查通过。没有重生成或覆盖现有设计 PNG。

### 回归修复：只有背板，没有按钮（2026-10-24）

用户真实截图显示菜单深色背板存在，但所有按钮不可见。Editor.log 在 `BindRows/Instantiate<ActionBtn>` 处反复报告 ActionButton 上有 missing script。定位为新 ActionButton.prefab 的 LayoutElement 脚本 GUID 写错：原为不存在的 `30649d3a9faa99c48a074b0f25955a527`，已改为项目实际 uGUI 包中的 `306cc8c2b49d7114eaa3623786fc2126`。缺失的布局组件未贡献 44px 行高，导致视觉子节点被压成零高度。

本次仅修正正式按钮资源的脚本引用；临时资源生成器同步改为读取实际 LayoutElement.cs.meta，避免再硬编码错误值。新增临时验证逐个解析按钮的 Image/Text/ActionBtn/Button/LayoutElement GUID，并确认根 LayoutElement 的 preferredHeight=44、ignoreLayout=false。修复前资源被该检查明确拒绝，修复后通过；原 fileID/父子关系检查仍通过。这也补上了上一轮只检查本地 fileID 而没有解析外部脚本 GUID 的漏检。

证据：`tmp/ActionPanelRework/ButtonVisibility/` 下的修复前 prefab、before-validation.log、after-validation.log 及验证脚本。未再次执行无关编译；本次未操作或保存用户场景。修复后的真实 Play 效果仍未验证；需要重新进入 Play，让池内按钮从修复后的预制体重新实例化。

### 菜单底板与悬停调整（2026-10-24）

按用户最新要求，一级/二级菜单不再绘制整块背景：ScrollRect 根只保留透明命中区域，按钮自身底色与描边保留。详情悬停和可操作反馈分离：ActionBtn 允许不可用条目进入详情悬停；PieceActionListPanel 在不可用技能/道具上仍显示说明，但不显示 AP/MP/弹药费用呼吸。资源变化不再误关掉仍在悬停中的说明；移出/关闭/切换仍统一清理。

可用按钮新增 1.06 倍、0.12 秒的 unscaled 缓动缩放，锚点为 Visual 子节点左侧中心。布局根维持 44px 行高，行距改为至少 6px（根据倍率上调）；透明视口内为缩放与进入位移预留横向余量及上下边距，避免放大裁切或相邻按钮重叠。不可用按钮及已展开锁定的父项不放大。复用/隐藏时取消缩放 Tween 并归一。

修改：ActionBtn.cs、PieceActionListPanel.cs、PieceActionListPanel.Layout.cs、ActionButton.prefab；执行提示词和设计生成源同步。Unity 项目引用编译、按钮脚本 GUID/44px 布局引用检查、生成器语法与差异检查通过；未进行真实 Play 视觉验收。待回归：两级菜单无整块底色、不可用技能/道具详情、资源不足/恢复时说明生命周期、首末行及相邻行缩放无重叠裁切、进入/移出/关闭后的缩放复位。

### Action Panel 综合审查（2026-10-24）

结论：静态检查部分通过，尚不能整体验收通过。本次为审查，仅更新本记录，未修改业务代码或保存场景。

确认的问题（均由当前源码/资源推导，未声称已在 Play 中复现）：

1. **P2：长详情无法通过鼠标正常滚动。** `ActionBtn.OnPointerExit → ExitHover → PieceActionListPanel.ClearHover → HideTooltip` 在离开按钮时立即隐藏详情。`SkillTooltipUI.Layout` 虽创建了 ScrollRect，但详情固定在左上，鼠标移过去必先离开动作按钮；按钮上的滚轮则发送给行动列表的 ScrollRect。长技能/道具说明因此无法访问被裁掉的部分。建议资源预览在离开按钮时立即停止，详情独立维护按钮/详情区域之间的进入宽限与关闭条件。
2. **P2：材料不足的技能会逐帧刷警告，并产生重复分配。** `PieceActionListPanel.Layout.LateUpdate` 刷新全部激活行（包括滚动裁剪之外的行），技能行的 `CanExecute → SkillAvailable → HasItem` 每次创建 Dictionary；材料不足时 `UnitAttrCenter.HasItemCosts` 每次执行 `Debug.LogWarning`。保持技能目录打开就会持续刷屏，悬停还额外查询一次。建议提供无日志的只读可用性查询，并在资源/装备/状态变化时刷新缓存，点击时再次校验。
3. **P2：菜单缩窄时分类箭头不再处于消耗列。** `ActionButton.prefab` 的 Arrow 使用中心锚点加固定 X=92，Cost 则使用右侧锚点。默认按钮宽230时看似正常，但布局允许动态缩窄；宽180时箭头中心为182，已超出按钮右边界。建议 Arrow 与 Cost 共用右侧列或统一右锚点，验证收窄和悬停放大后的边界。

架构评估：统一 ActionBtn 预制体、独立 Visual 动画节点、按钮池、显式目录顺序、只读费用快照的方向合理。默认44高、6间距、1.06倍缩放时，相邻按钮即使同时处于放大过渡，仍有3.36单位间隔；首末行垂直留白4也大于单侧放大量1.32。无整块背景和禁用条目的详情/操作反馈分离已体现在实现中。需清理的低优先级遗留：旧 actionButtons/skillButtons 数组及禁用层级仍保留；menuRowFill/menuRowOutline 已不参与新菜单绘制；尺寸常量仍同时存在于代码和预制体，后续改尺寸容易失配。可用性规则目前分散在菜单和业务入口，建议统一无副作用查询，但保持实际执行入口的再次校验。

本次重新执行：Unity Roslyn 项目引用编译通过（既有警告）；ActionButton 全部 MonoScript GUID 可解析、LayoutElement preferredHeight=44；三个修改预制体内部引用检查通过；PR2BATTLE 与本轮实现开始前的保护副本一致。证据位于 `tmp/BattleUIAcceptance/Compile/compiler.log`、`tmp/ActionPanelRework/asset-validation.json`，检查脚本位于 `tmp/ActionPanelRework/ButtonVisibility/validate_button.py`。既有枚举/时序测试日志为133断言通过，本次未重跑。

运行验收未完成：本次 Unity CLI 仍返回 STATUS_NO_INSTANCES（无可连接的 Pipeline 实例，不能据此断言 Editor 未运行）。未进入或控制真实战斗，未生成验收截图。1920×1080、1280×720、1920×1200下的实际布局、动画和鼠标命中，二级菜单四角/长列表/滚动定位，以及完整选择—取消—重开—执行流程均仍待真实 Play 回归。独立预览与离线编译不作为这些项目的通过证据。

### 审查问题修复（2026-10-24）

按用户指示修复上述三个 P2：

- 详情生命周期：按钮退出时立即清除费用预览，详情独立保留可配置的1.25秒（非缩放时间）供鼠标跨越至固定详情区。鼠标位于详情内持续保留，离开后超时关闭；切换按钮/分类、执行、关闭菜单、切换角色等路径仍立即关闭旧详情。`SkillTooltipUI.ContainsPointer` 使用对应 Canvas 摄像机判断详情矩形命中，原 ScrollRect 接收详情区域内的滚轮。修改 PieceActionListPanel.cs/Layout.cs 和 SkillTooltipUI.Layout.cs。
- 材料查询：UnitAttrCenter.HasItem 改为无日志、无临时集合/闭包的只读查询，保留重复材料合计、负数/空条目拒绝、整数溢出拒绝和库存首条匹配语义。按通常较短的技能材料列表直接遍历；保留实时刷新，使库存改变能立即反映，无需新增易失同步的缓存。实际扣费和提交入口继续校验，未调整扣费规则。
- 箭头定位：ActionButton.prefab 的 Arrow 改为右侧中心锚点，X=-55，与 Cost 的中心列一致。默认宽230和缩窄宽180/80均在按钮边界内，不随宽度变化向右逸出。

验证：Unity Roslyn 项目引用编译通过（既有警告）；按钮 MonoScript、44px LayoutElement 和三个预制体内部引用检查通过；场景保护副本仍一致；目标文件差异检查通过。临时测试直接提取本次 HasItem、ClearHover、RefreshDetails 的正式方法体，10009断言通过，包含10000组材料随机等价验证、10000次查询零托管分配、数据不变、预览立即停止、详情宽限/区域内保留/超时/强制关闭。箭头定位另按资源数据检查宽80/180/230。测试及日志均在 `tmp/ActionPanelRework/ReviewFixes/`，入口 verify.py、结果 results.log；测试使用临时 Unity/UI 替身，不能替代真实事件系统及 Play 视觉验收。

未完成：真实战斗中的鼠标转移和滚动、详情遮挡/射线、三种分辨率及悬停缩放视觉回归仍未执行。低优先级的旧序列化数组/禁用层级清理未纳入本次三个问题修复。

### 范围选择预览、AP 消耗动画与行动后返回（2026-10-24）

按用户新要求，费用呼吸不再以关闭菜单为终点：PieceActionListPanel 在移交具体动作时调用 UIManager.BeginActionSelection，由新 partial 文件 UIManager.ActionFlow.cs 持有只读费用查询和预览视图。攻击、技能、道具攻击、警戒或移动范围选择期间持续刷新 AP/MP/弹药费用；角色正式 CostMP 两个入口通知 NotifyActionCommitted，立即停止待消耗预览。取消选择、换人、交棒、角色禁用/销毁和外部控制接管清理旧状态。点击动作仍由原业务入口执行与扣费。

AP 点阵 SetPoints 检测同源同容量的真实下降，为区间 [current, previousCurrent) 中每个已消耗点建立独立实心视觉图层，用同一 DOTween Sequence 并行执行 DOScale(2,1秒) 与 DOFade(0,1秒)，使用非缩放时间。原点阵立即更新为真实剩余值，呼吸刷新不会覆盖消耗图层。恢复/重复刷新不重播；ActionCostPreviewUI 换源时重置快照，初始化、换人及容量变化不误播；禁用或换源清理未完成效果，完成后回收临时图层。

行动展示协调器等待真实 HasCombatPresentation、MoveManager.IsMoving、角色 IsPerformingAction 全部解除后，只返回一次镜头和菜单；使用 CanControlPiece 再检查是否还能操作，耗尽 AP/待机不会重开旧菜单，正常交棒沿用调度器选择下一行动者。移动完成入口不再直接激活已清空绑定的旧菜单。当前角色已变化时直接放弃旧返回，外部自动控制不接管镜头。

修改文件：UIManager.cs、UIManager.ActionFlow.cs/.meta、PieceActionListPanel.cs、ClickManager.cs、UnitAttrCenter.cs、PieceController.cs、ActionCostPreviewUI.cs、ActionPointPipsUI.cs，及本验收记录/README/执行提示词。无需修改或保存场景。

验证：Unity Roslyn 项目引用编译通过（既有警告），预制体引用及场景保护检查通过。直接编译正式 UIManager.ActionFlow.cs 的临时测试15断言通过，覆盖120帧选择预览、费用刷新、确认立即清理、战斗/反应/移动等待、取消、只返回一次、无 AP、交棒与外部控制；直接编译正式 ActionPointPipsUI.cs 的临时测试14断言通过，覆盖消耗区间、并行2倍/1秒淡出 Tween 参数、重复刷新、恢复、耗尽、换源和容量变化。两组测试使用 Unity/DOTween 记录替身，不能证明真实动画或 Cinemachine 画面效果。入口与结果在 `tmp/ActionPanelRework/ActionFlow/`、`tmp/ActionPanelRework/Pips/`；编译日志在 `tmp/BattleUIAcceptance/Compile/compiler.log`。

未完成：真实 Play 中的范围选择持续闪烁、实际1秒消耗动画（含最后 AP 交棒清理）、镜头回归与菜单重开仍待回归；本批未生成运行截图。需覆盖近/远攻、技能、移动、道具、警戒、待机、取消、Buff中断和回合切换，确认无重复扣费、旧预览或镜头抢回。

### 用户截图补充验收

用户提供四张真实战斗画面，原样归档至 `tmp/BattleUIAcceptance/ImageReview/01-primary-menu.png` 至 `04-range-selection.png`，逐项报告在同目录 `review.md`。此证据来自用户，不是代理现场运行；不能从截图确认当前代码版本或动画时序。

静态部分通过：六个一级入口出现，主/子菜单无整块底板，道具二级整体中心与父按钮对齐，展开态及灰色AP可辨，灰色能量包条目同时显示详情；左下已认可风格保持，范围选择画面中菜单隐藏。

整体尚不通过：四图顶部资源文字被行动序列下缘遮挡；图02详情小卡片外又有带角标的大块空背景；图03/04攻击范围红边和填充视觉权重过高。前两项需核对实际层级/锚点，第三项调整表现但不据截图修改技能判定。另需在目标分辨率原尺寸下复核行动序列的小字可读性。

动态验收仍未完成：静态图的AP明暗差异仅能提示预览状态，不能证明呼吸时长、实际扣费、1秒消耗动画、镜头返回和菜单重开。未凭这四张图将动态项目标为通过。本次只补充验收记录，无代码或场景修改。

### 用户截图问题修复

按用户要求修改图像验收指出的三处表现问题：

1. PieceStatePanel 原 upRoot 使用序列化的 -109.5 基础值与3887.8/镜头尺寸偏移，资源组容易抬到行动序列下。改为以当前角色 SpriteRenderer 世界包围盒顶部投影定位（缺少立绘时回退角色位置上方），资源图形按实际屏幕边界避让安全区和当前可见行动序列。UIManager 新增只读 ActionTimelineRect 引用入口。旧偏移字段保留序列化兼容并隐藏，已不驱动新定位。左下 PieceInfo 及 SceneBackdrop 不变。
2. SkillTooltipUI.Layout 先将姓名、正文、图标迁出旧容器，停用残留旧背景/角标节点，再由当前根背景和 DetailsOutline 统一绘制。正文卡片按实际文字高度加88单位留白计算，最低高度从180改为120，长内容仍由原 ScrollRect 裁剪滚动，详情转移宽限继续保留。
3. RangeUI.ShowFan 的两条直边仅厚度轴缩至原30%，长度和扇形半径保持；直边 alpha=0.65，扇形贴图 alpha=0.22，保留原色相并关闭装饰图形射线命中。圆弧边缘与填充共用原扇形贴图，圆弧随贴图一同降低透明度，本次未重新绘制贴图。技能射程、角度、位置及 SkillTargeting 判定未改。

修改：PieceStatePanel.cs、SkillTooltipUI.Layout.cs、RangeUI.cs、UIManager.cs；README及验收记录同步。Unity Roslyn 项目引用编译通过（既有警告），目标差异检查、预制体引用与场景保护检查通过；未保存编辑器未提交场景。未生成修改后的运行截图，因此三项状态为“修复实现完成，视觉复核待完成”，不能用修复前四张图作为修复后通过证据。需复核不同缩放/顶部角色状态、短/长详情的单一边界，以及扇形在明暗地图上的可读性；行动序列小字仍需原尺寸分辨率截图评估。

### 左下外框拉伸清晰度修复（2026-10-25）

用户截图指出左下信息面板外框拉伸后显得分辨率低。核查：面板外框使用128×96 PanelOutline，头像外框使用96×96 PortraitOutline，均为九宫格、无压缩；本次不再依赖这些栅格描边的放大采样，新增 BattlePanelFrame（继承 Image），按当前 RectTransform 生成固定2单位线宽、右上/左下切角的描边网格，并增加约0.75屏幕像素的透明羽化环。主体透明、不绘制填充，使用白色纹理及原 Image 颜色，不修改共用贴图或其他面板。

BattleScene.prefab 仅替换原 bgLight 与头像 outline 两个现有 Image 的脚本引用，保留对象/component ID、所有 RectTransform、颜色及原资源引用；分别保持16/12单位切角。左下半身像、姓名、AP、加粗血条、背景及 SceneBackdrop 未改动。未保存或改动 PR2BATTLE 场景文件。

验证：Unity Roslyn 项目引用编译通过（既有警告），目标差异检查通过；与本次修改前保护副本比较，严格只有两个外框组件变化，脚本 GUID 可解析。保护副本、验证入口及日志在 `tmp/BattleUIAcceptance/ImageReview/BeforeFrameFix.prefab`、`validate_frame.py`、`frame-validation.log`。尚无修改后的真实运行截图，状态为实现完成、实际画面清晰度待复核。

### 速度条上半身头像与蒙版（2026-10-25）

实现已接入原 `Resources/UI/SpeedTimeline` 战斗加载入口：

- PortraitViewport 使用60×60 RectMask2D，头像等比放大后截取有效立绘顶部42%的区域；专用 `turnPortrait` 默认完整适配，配置为全身立绘时使用相同裁切。无图片时显示问号，装饰与头像不拦截战场射线。
- 取景先从 Sprite 顶点、pixelsPerUnit、pivot 还原图内有效区域，然后在居中的 UI 矩形内定位，不修改原资源 pivot。已核对打印机 E01idle 的 pivot.y=0.3507803、无人机 E04idle 的 pivot.y=0.32897145，均通过同一规则补偿。Tight 网格范围按 Sprite 缓存，不读取纹理像素；Full Rect 或特殊体型可显式配置有效区域。
- PieceData 新增“行动条头像为全身立绘”和“行动条构图”中文配置。开启“覆盖默认构图”后，有效图像区域使用 Sprite 矩形内的归一化 Rect（左下原点）；缩放默认1；位移按蒙版宽高计，正值右移/上移；上半身高度占比默认0.42。独立头像的占比不生效，缩放和位移仍生效。绑定、窗口尺寸或构图配置变化时更新布局。
- 模板64×64，高亮外框68×68，当前行动缩放1.15；高亮复用 BattlePanelFrame 仅绘制金色边界，置于蒙版之外，避免背景中心覆盖新放大的头像。阵营色条也位于蒙版之外。
- 速度条高度172→240，双方中心间隔80；小字11–13→16。轨道内双向挤压处理进度相近头像，并为高亮预留端点空间。正常尺寸下同阵营最多8个头像可分离；更多参战者会压缩间距，密集阵容的实际可读性仍待场景评估。真实进度和调度逻辑未改。
- SpeedTimelineUI 负责来源与进度，SpeedTimelineMarkerUI 负责取景与状态；移除速度条自身的每帧 LINQ 临时数组和避让字典，资源回退图片按角色编号缓存。BattleManager 原参战者查询仍沿用现有实现。

验证结果：Unity Roslyn 项目引用编译通过（既有警告）；`tmp/SpeedTimelinePortrait/Tests.csproj` 直接编译实际两个 UI 脚本，使用 Unity 替身通过5376项断言，覆盖底部 pivot 不改变裁切、专用头像、缺图、构图配置变化、窗口尺寸变化及200组进度/端点/多人排布。`validate_assets.py` 检查原对象保留、引用与双向层级一致、蒙版/缺图提示绑定、蒙版外高亮、双方轨道与标题/页脚间距、原战斗 Resources 加载入口，全部通过。日志位于 `tmp/SpeedTimelinePortrait/tests.log`、`assets.log` 与 `tmp/BattleUIAcceptance/Compile/compiler.log`。这些检查不代表渲染或实际战斗通过。

实际场景验收未完成：开始时没有 Unity 窗口和进程；尝试启动现有编辑器后，许可证客户端连接等待60秒超时，编辑器以199退出，日志 `tmp/SpeedTimelinePortrait/Editor.log`。未进入 Play、未保存场景、未产生运行截图。仍需在 PR2BATTLE 实测友方/敌方头部构图、当前行动高亮、多人同进度、顶部棋子资源避让及不同窗口尺寸；特殊非人形和透明留白资源可能需要按角色微调。保留用户现有场景和其他未提交改动。

### 速度条独立复验：尚未通过完整验收（2026-10-25）

本轮为验收，未修改正式业务代码、预制体行为或场景。编译、原5376项断言、预制体引用与静态几何检查再次通过。进一步以实际 SpeedTimelineUI、SpeedTimelineMarkerUI 和 SpeedTurnQueue 源码配合 Unity 替身复现以下问题：

1. **P2，同刻满条展示顺序与真实队列不一致。** `SpeedTimelineUI.CompareProgress` 按进度再按 InstanceID 排序；真实队列按 Arrival 和首次注册 Order 选择。复现：先创建/注册 A，再创建/注册 B，速度分别100、110；一次 Tick 后真实队列先选 B，当前角色进度展示固定100，A也为100，UI却把 A 排在 B 左侧。若待行动者保留超过100的进度，还可能排到当前行动者之前。建议展示层使用调度提供的顺序，并明确当前行动者优先，不能以 InstanceID 代表行动顺序。
2. **P2，首次显示缓动期间头像重叠。** `LayoutLane` 对每个新头像从模板位置0分别 Lerp；60fps 首帧，相邻目标间距79.6，实际中心间距仅22.56，低于60的头像宽度。原测试将 deltaTime 设为10，只验证收敛后的布局，未覆盖此阶段。建议首次布局直接放到安全目标位置，之后的移动再保持间距约束或设计明确的切换动画。
3. **配置体验问题，默认覆盖不是保持当前构图。** `RefreshFraming` 在 useOverride=true 时直接以 contentRect 替换自动 Tight 网格边界。复现有效纵向范围5%–95%的立绘，只开启覆盖、保持zoom=1及offset=0，图片高度便由158.73变为142.86。建议将有效区域覆盖单独开关，或初始化为当前自动区域，避免用户仅想调整偏移却先改变默认取景。

证据：`tmp/SpeedTimelinePortrait/Acceptance/probes.log`（输出以上复现值，包含5377项断言）、`Probe.csproj`、`Probe.cs`；原回归 `tests.log`，资源检查 `assets.log`。替身用于算法与状态复现，不验证 Unity 实际蒙版、字体、纹理采样或画面观感。

再次尝试启动现有 Unity 2021.3.23f1 后，许可证客户端 IPC 连接被拒绝，等待60.01秒后以199退出，详见 `tmp/SpeedTimelinePortrait/Acceptance/Editor.log`。未取得实际战斗截图，不将源素材检查或任何独立预览计为 PR2BATTLE 通过。需修复上述展示问题后补充真实场景、多分辨率、敌人特殊体型和动画过程中验收。

## B02 行动菜单、详情和费用链（2026-10-27）

结论：**修复道具费用缺陷，离线定向验证通过；真实战斗验收受阻，B02未完成。** 对应[开发计划](../../战斗系统开发计划与验收.md)与[B01基线](../../战斗系统当前基线.md)。

### 本轮修复

- 攻击型道具原在 `UseItem` 进入瞄准时扣库存，取消后不返还。当前仅保存待确认道具；`CastAttack`确认合法目标后，经 `TryCostBattleItem` 支付一件库存与 `ActionType.Item` 配置AP。菜单预览和确认使用同一费用类型，保留UseItem作为既有行动标记，不改枚举或序列化配置。
- 立即生效道具原在扣AP前就减少库存并产生回复效果；当前付款成功后才生效。行动钩子中断时沿用只消耗AP的现行行为，不额外扣库存或施加道具效果；钩子使库存失效时不扣成负数、不继续攻击。行动力回复道具也按先付费、后回复处理，达到上限时最终AP可能与旧顺序不同。
- 攻击型道具技能不存在时直接拒绝，不减库存、不落入未处理道具分支。取消选择、右键取消、生命周期清理和切换普通攻击/技能时清空待确认引用。确认后清空攻击选择，重复确认不会重复扣费。
- 只修改 `Assets/Scripts/Battle/Controllers/PieceController.cs` 与 `UnitAttrCenter.cs`；保留用户已有改动，场景/预制体和正式配置不修改。

### 本轮证据

证据根目录：`tmp/BattleUIAcceptance/B02-20261027/`，全部忽略Git。

| 检查 | 结果与证据 | 范围 |
| --- | --- | --- |
| 修复前复现 | `items-before.log`包含瞄准/取消预扣、费用不一致等失败断言；缺技能样本抛异常后停止 | 临时脚本提取当时实际方法，外部Unity/表现/背包宿主使用替身；不是Play截图或完整执行计数。 |
| 修复后道具规则 | `verify_items.py`、`Items/Items.csproj`，`items-after.log`：18/18通过 | 提取当前ItemAvailable/UseItem/StartItemAttack/CastAttack/CancelSelect及真实费用方法；覆盖取消、重复确认、库存/AP变动、眩晕、异常中断、缺技能/无目标/空库存、立即生效和AP回复时点。 |
| 行动生命周期 | `action-flow.log`：15项断言通过 | 链接实际UIManager.ActionFlow.cs，替身验证选择、取消、战斗/反应/移动等待、单次返回、AP耗尽、交棒及外部接管。 |
| 枚举/时序 | `enum-timing.log`：133项断言通过 | 链接实际GameEnum/ActionTypeLabels/ActionMenuTiming，验证旧序列化兼容、双语、0/1/2/4/10/25/100行时序；不证明菜单可见或可点击。 |
| 序列化引用 | `verify_bindings.py`、`bindings.json`：通过 | ActionButton八个本地组件引用、菜单绑定按钮预制体、详情标题/正文/图标，以及PR2BATTLE引用共用BattleScene；不证明运行override、射线或渲染。 |
| 离线编译 | `compile.log`：Unity2021 Roslyn项目引用编译退出码0，有既有警告 | 复用 `tmp/BattleUIAcceptance/Compile.ps1` 编译Assembly-CSharp，不是Unity完整导入/编辑器程序集/构建。 |

### 真实场景验收矩阵

| 必验项 | 当前结果 | 恢复后的操作 |
| --- | --- | --- |
| 普攻/技能：悬停→瞄准→取消→重开→执行 | 未执行Play；费用与入口已静态核对 | 隔离存档进入PR2BATTLE，记录角色/技能/真实AP/能量/弹药与前后值，等待完整弹道及受击。 |
| 道具：瞄准取消与确认、即时回复、待机 | 道具仅有18项离线规则证据；真实道具/待机未执行 | 用正式库存与配置复验取消不扣费、确认一次支付、回复发生在支付后、待机消耗剩余AP；不临时造正式角色资源。 |
| 空列表、超过10条、资源不足 | 仅有源码/资源与时序核对，真实点击/滚动未执行 | 记录实际配置，检查末行可达、不可用项仍能读说明且不能执行；不足以组成样本时记录前置条件，不标通过。 |
| 详情阅读/滚动、取消和角色切换清理 | 仅有静态检查与行动生命周期替身证据 | 实际悬停、跨到固定详情区、滚动长文、取消并交棒，确认无残留费用预览或抢回镜头。 |
| 1280×720、1920×1080 | 两种均未执行 | 各走一次以上流程，检查安全区、菜单翻转、详情/速度条避让、范围射线与Console，保存实际战斗截图。 |

### 运行阻塞与恢复条件

本轮Unity CLI的更新检查因网络失败，status返回STATUS_NO_INSTANCES并警告无法完整检查运行编辑器；项目manifest无Pipeline，本项目仍为Unity2021，不安装要求Unity6的Pipeline。Windows窗口清单只返回另一个项目的Unity6编辑器；未操作或关闭该编辑器，也未将其画面视为本项目证据。

单独启动已安装的Unity2021.3.23f1，指定本项目与本轮独立日志；`Editor.log`显示LicenseClient连接被拒，等待60.18秒后以199退出。本轮未进入Play、未写正式存档、未保存场景、没有战斗截图，也未执行构建。恢复条件为Unity2021能正常取得许可证并显示本项目编辑器；之后先观察dirty状态、建立隔离存档，再执行上述矩阵。B02保持受阻，不以离线通过替代验收。

## B03 速度条定向修复（2026-10-29）

结论：**三项代码修复与定向检查通过；真实场景/视觉未验，B03保持待验收。** B02仍保留原未验矩阵，本轮不把它标完成。

| 已报问题 | 当前源码复现 | 本轮修改与复验 |
| --- | --- | --- |
| 满条顺序与调度不同 | 当前者B被UI排在排名1；UI按进度及InstanceID，实际按Arrival/注册Order | SpeedTurnQueue的展示比较与TakeReady共用满条比较；BattleManager加当前者优先入口；UI稳定排序，未入队者保持来源次序。当前者复验排名0；覆盖超100、同刻/同速、注册顺序不同于创建顺序、移除再参战及只读查询。 |
| 首次缓动重叠 | 60fps首帧中心距22.56，低于60头像/64标记宽 | 每个新标记首次直接安全定位，之后先插值再双向约束，交棒/增员/窗口变窄也保持可用间距；首次复验79.60。空间不足时仍保留原有压缩间距策略，不能保证无限人数不重叠。 |
| 默认覆盖改变自动构图 | 仅开useOverride，高度158.73→142.86 | 默认contentRect全图值保留自动Tight区域；新增“使用整图区域（含留白）”显式选项；旧非全图区域继续生效，全部变化纳入布局缓存失效。默认覆盖后仍158.73，缩放/位移、旧区域、整图切换、缺图、pivot、独立头像和窗口变化通过定向检查。 |

### 验证范围和证据

证据根目录 `tmp/SpeedTimelinePortrait/B03-20261029/`，不入Git。四个实际业务源文件修改前副本以 `.before` 结尾，其他用户文件通过开始/结束哈希比较保护；不修改角色资产、预制体或场景。

- `probes-before.log`：当前源码重新运行旧Probe得到三个修复前数值；原5377项通过仅说明旧断言没有捕获问题，不能代表问题不存在。
- `generate_tests.py`、`B03.csproj`、`tests-after.log`：41,887项断言通过，链接实际SpeedTimelineUI/SpeedTimelineMarkerUI/SpeedTurnQueue，并提取当前BattleManager.CompareActionOrder；Unity与场景宿主使用替身。覆盖30/60/144fps、420/656轨道宽（仅Canvas单位，非实际分辨率）、0–20人混合阵营、逐帧交棒/增员/缩窄、移除清理、满条/溢出/同刻、构图与缓存。不是41,887次真实战斗或渲染验收。
- `layout-before.log`、`layout-after.log`：增量修复阶段日志，分别保留间距失败与仅剩构图失败；最终结果以tests-after.log为准。
- `speed-turns.log`：真实速度/聚能调度加替身的35项回归通过，普通回合和聚能衔接未改变。
- `compile.log`：Unity2021 Roslyn结合项目引用编译Assembly-CSharp成功，既有警告；不是完整构建或Play。
- `assets.log`：预制体原对象保留、引用/层级/蒙版/高亮/几何及正式Resources加载入口静态通过。

### 配置语义与旧数据

`useOverride=false`仍使用自动构图。开启后，zoom=1、offset=0、upperBodyFraction=0.42且contentRect=(0,0,1,1)不改变原取景；全图Rect现在是自动区域的默认占位。旧的非全图contentRect保持原样使用，无需资产迁移。开启useFullSpriteRect时显式使用完整Sprite矩形，包含透明边缘；此时不使用自定义有效区域。关闭整图选项恢复自动/旧自定义区域。独立头像仍忽略上半身占比、保留缩放与位移。

### 当天运行环境与剩余验收

沙箱内CLI更新检查网络失败，status返回无Pipeline实例并警告进程检查不完整。随后经沙箱外执行启动已安装Unity2021打开本项目，`Editor.log`确认成功连接许可证并完成项目加载；因此10-27的许可证失败只保留为历史记录。

Windows Computer Use窗口清单只返回另一项目Unity6，未返回本项目；直接请求启动Unity2021也报告“launched app did not expose a targetable window”，已启动项目进程的MainWindowHandle为0。没有针对不可见窗口盲发按键，也没有操作另一个项目。结束时仅终止本轮创建、路径已核对的Unity2021进程42836，未保存场景，释放项目占用。

本轮没有进入Play、未采集战斗画面或真实角色资源，未执行构建。还需在可定位的本项目编辑器中，以PR2BATTLE实际检查当前者/待行动者、高溢出/同刻、首次显示/动态增减、密集队列、友敌非人形与透明留白、构图开关及窗口调整，并在1280×720和1920×1080分别留实际截图。蒙版采样、字体、特殊材质和缓动观感不由替身断言证明。B02/B03仍需补验。


### 2026-10-29 B01–B08隔离副本真实验收

本轮成功使用Unity2021真实Play，已打开检查PR2BATTLE及行动子菜单的1280×720/1920×1080画面；当前菜单生成6项，展开/关闭不扣AP。速度条补验真实6友方+1敌方首帧，头像宽64、相邻中心距至少79.60，双分辨率无首帧重叠且非人形敌方可辨识，结合41,887项回归，B03标为当前预制体范围完成。超出可用容量仍按既有规则压缩，B31单行降高未实施。

B02继续待验收：道具18项回归通过，但真实UI的确认/取消/资源不足、空列表和超过10条滚动、详情悬停尚未覆盖完整。旧窗口/许可问题已通过隔离副本路径绕开，不能继续作为本轮未验原因。详见[联合验收](../../战斗系统B01-B08验收记录.md)。实际证据在 `tmp/Acceptance/B01-B08-20261029/`，本轮未修改正式UI、场景或资产。
