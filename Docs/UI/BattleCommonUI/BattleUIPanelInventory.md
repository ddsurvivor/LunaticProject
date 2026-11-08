# 战斗 UI 面板清单与复用矩阵（B35）

更新：2026-11-05。用户最新要求跳过剩余面板，后续迁移停止；已完成的 U14/U15 保留，其他项目不计完成。分支：`dev_feature`。B35 第一阶段完成入口盘点、共享模块现状、迁移矩阵及执行顺序；第二步将 U14 暂停窗口、第三步将 U15 结果窗口接入真实嵌套 AVG 模块模板。B35 整体保持进行中，尚未批量替换窗口或宣称全量视觉统一。

## 核对范围与判定

核对 `UIManager` 及其 Windows/Messages 部分、`Assets/Scripts/Battle/UI/` 的 56 个 C# 文件、战斗部署、剧情检定、加载转场、设置及队伍入口；扫描正式 `Assets/` 下 322 个 prefab/unity 文件的脚本和模块 GUID 直接引用。扫描不包含 `tmp/`。

GUID 直接引用不等于运行时实例数量，也不展开 prefab 继承。无直接引用的类仍可能通过 `AddComponent` / `Resources.Load` 创建。以下把场景内嵌、独立 prefab、运行时生成分开说明；不因同名旧资产或零引用而直接删除。

`Assets/Prefab/Battle/BattleScene.prefab` 是战斗 UI 主要承载体，但仍有场景直接内嵌 UIManager/InfoBox/TurnPanel 等组件。后续只改一个 prefab 不能覆盖所有关卡。

## 面板清单

路径未写目录时，脚本位于 `Assets/Scripts/Battle/UI/`；“BattleScene 内嵌”指 `Assets/Prefab/Battle/BattleScene.prefab`。表中复用是目标方案，只有明确写“已接入”的部分代表现状。

| ID | 面板 / 用途 | 实际入口、脚本与载体 | 保留 / 合并 / 退役及共享目标 | 必须保留的行为 / 当前缺口 |
| --- | --- | --- | --- | --- |
| U01 | 左下角色信息 | `PieceInfoPanel`、`ActionCostPreviewUI`；BattleScene 内嵌，另被 `Assets/Prefab/UI/TeamPanel.prefab` 使用 | 保留排版与绑定；面板/头像/资源轨道采用共享 AVG 样式 | 预览费用、HP/AP/能量/弹药、拖影、角色切换；不可把队伍页用途误删 |
| U02 | 一级行动菜单 | `PieceActionListPanel`；BattleScene 及部分棋子 prefab 直接挂载；`Assets/Prefab/UI/PieceActionList.prefab` 另存 | 合并到同一布局与外观配置，继续由正式命令入口驱动 | 展开不扣费、边界翻转、当前行动者、输入锁；旧副本需逐场景核对 |
| U03 | 技能 / 道具 / 警戒二级列表 | `PieceActionListPanel.Layout.cs` 运行时创建 `SecondaryMenu` 等列表，`ActionBtn` 复用行实例 | 与 U02 共用行/选中态/滚动及空列表组件 | 超过十条、空列表、确认/取消、跨菜单关闭；不另建独立技能窗口 |
| U04 | 行动行 / 页签按钮 | `ActionBtn`、`CustomAdvancedButton`；`Assets/Prefab/Battle/ActionButton.prefab`、`Assets/Prefab/UI/ActionButton2.prefab` | 合并视觉到 AVG Button 状态，保留两种业务组件的事件适配 | 当前费用、禁用原因、悬停缩放；不以纯 Button 替换而丢失原事件 |
| U05 | 技能 / 道具详情 | `SkillTooltipUI` + `.Layout.cs`；BattleScene 内嵌，运行时追加 `DetailsOutline` 和滚动视口 | 保留一个详情入口；改用 AVG Panel/Header/Scrollbar | 悬停到可滚动详情的转移、长文、实际角色费用；当前仍是单独背景/描边配置 |
| U06 | 棋子状态面板 | `PieceStatePanel`；BattleScene 内嵌及 `Assets/Prefab/UI/PieceStatePanel.prefab` | 保留详细数据功能；外框/标题统一；旧 `upRoot` 头顶区域由 U07 接管 | 选中目标与参数绑定；不能停用整个脚本来隐藏旧头顶 |
| U07 | 敌我头顶血条 / 状态 | `BattleOverheadStatusLayer.Ensure` → `PieceOverheadStatusUI`，在原 Canvas 创建 | 已接正式 `Resources/UI/PieceOverheadStatus.prefab` 和共享样式；敌我同一模板与 AVG PanelCut，保留共享 AP/生命动画；头顶头像已按用户要求移除 | 满血隐藏、红黄预览、六格及分页；本轮顶部避让及死亡显示/Init已补验；全镜头/墙体/完整死亡演出仍待复验 |
| U08 | 旧玩家血条 / EnemyCanvas | `PlayerHealthBarUI`、`HpBarUI`、`EnemyCanvas`；六个玩家 prefab、`Assets/Prefab/UI/EnemyCanvas.prefab` 及敌人 prefab | 视觉由 U07 合并接管；旧接口保留，待完成引用迁移再讨论删除 | `HpBarUI.HasDamagePreview` 仍是正式预览快照来源；旧视觉用 CanvasGroup 隐藏，不可连同业务禁用 |
| U09 | 头顶完整状态详情 | `BattleOverheadStatusLayer.BuildDetails` 运行时生成 `StateDetails` | 与 U07 数据模型共用；外观改为 AVG Panel/Header/Button | 分页、关闭、所属棋子离场清理；本轮未替换目前硬编码底色及按钮 |
| U10 | 顶部速度条 / 头像 | `SpeedTimelineUI`、`SpeedTimelineMarkerUI`；`Assets/Resources/UI/SpeedTimeline.prefab` | 保留正式队列与同源 AVG 素材；抽出样式引用，不新增另一套速度条 | 一行混排、密集缩放、超额进度、高亮；B31 规则与字体取景保持 |
| U11 | 旧回合 / 结束回合 / 重开按钮 | `TurnPanel`、UIManager 的 `turnImage/endTurnButton/restartButton`，多个场景内嵌 | 旧回合图/结束按钮是视觉退役候选，重开入口收敛到 U14/U15；本轮不删除 | 当前速度模式已隐藏部分入口，但场景引用仍存在，须逐场景验证后迁移 |
| U12 | 聚能按钮 / 闪图 / 通知 | `PlayerController.RefreshBurstUI`，`UIManager.Messages.ShowBurstNotice` | 按钮接共享状态；提示合并 U17；旧 `burstStart/burstEnd` 闪图保持停用 | 选人数/费用/冷却/结束文本；不恢复旧全屏闪图流程 |
| U13 | 模因双槽换装 | `MemeLoadoutPanel.OpenBattle/OpenCharacter` + `.Layout.cs` 运行时生成；角色页/战斗菜单接入，脚本无 prefab/unity 直接挂载 | 保留独立窗口和草稿；面板/双槽按钮/确认/取消/列表外观复用 AVG | 战内2AP、战外免费、双槽草稿、取消不提交、来源和失败原因；不与状态详情合并业务 |
| U14 | 暂停窗口 / Esc 面板 | `UIManager.InitializePauseWindow` 实例化 `Resources/UI/BattlePauseWindow.prefab`；`keyPanelDic` 适配旧窗口 | 已接入嵌套 AVG Panel/Header/Button；支持 Prefab Variant 与 Inspector 模板覆盖，见[记录](暂停窗口模板与B35记录.md) | `ActivePause` 多持有者、Esc层级、继续/重开/退出；不能因样式替换重建事件或暂停持有 |
| U15 | 胜负 / 奖励 / 重开 | `BattleFinishPanel` + `UIManager.InitializeResultWindow`，统一加载 `Resources/UI/BattleResultWindow.prefab`；旧内嵌实例初始化时停用 | 已接入 AVG Panel/Header/Button 嵌套模板，支持 Variant、滚动奖励；有/无旧面板引用共用同一入口；见[记录](结果窗口模板与B35记录.md) | 奖励唯一提交、继续/重试、结果不可留空遮罩、返回场景；不能新建第二个胜败控制器 |
| U16 | 右上快进 / 设置 | `BattleTimeControls.Create` 运行时生成，优先场景内 SettingPanel，再实例化指定 prefab；`Assets/Prefab/UI/SettingPanel.prefab` | 设置整窗已复用 AVG，保留；快进/齿轮外壳改为共享 Button/Surface，图标保持原绘制 | 短按/长按、1x/3x、失焦恢复、设置暂停；只改战斗实例，不改 AVG 原场景 |
| U17 | 顺序通知 / 操作日志 | `UIManager.Messages` 创建消息 Canvas，重挂 `skillNameDisplay/PlayerLogPanel` | 共用通知容器、正文/强调色及滚动组件；保持不同消息区用途 | 有界队列、真实时间、独立滚动、安全区；不可把伤害跳字也并入同一队列 |
| U18 | 物品 / 经验通知 | `NotificationManager` 的普通/经验池；`Assets/Prefab/UI/ItemGetTipPanel.prefab`、`LevelUpTipPanel.prefab`；`ItemGetPanel/NotificationItem` | 两类数据保留，共用外框/图标/文字槽及入退场表现 | 队列、池复用、关闭及奖励显示；`Market/ItemGetPanel` 跨模块用途需保留 |
| U19 | 教学 / 开场提示 | `TutorialUI`、`TutorialManager`；BattleScene 内嵌、`Assets/Prefab/UI/TutorialPanel.prefab`，AVG/Playing 也有直接引用 | 面板/标题/确认复用；数据和教学顺序保留 | 已读标记、首触发、完成后交还控制；不能把普通提示都判为教学关 |
| U20 | 战斗开场 / 胜利演出 | `BattleStartUIPanel`；BattleScene、`Assets/Prefab/UI/BattleStartPanel.prefab`，AVG/Playing 也有直接引用 | 演出容器保留，文字/按钮统一；不把它替换为暂停或结果控制器 | 演出完成回调、跳过、输入占用；注意 UIManager 对其有胜利提示兼容用法 |
| U21 | 检定面板 / 成败标识 | `Assets/Scripts/剧本/CheckDicePanel.cs`；BattleScene、`Assets/Prefab/UI/DicePanel.prefab` 等 | 保留剧情与战斗共用检定控制；外框/按钮/字体统一，骰子专用表现保留 | 检定等待和最终结算次序，不因换窗跳过确认回调 |
| U22 | 部署面板 / 确认按钮 | `Assets/Scripts/Battle/Authoring/BattleDeploymentController.cs` 创建独立 Canvas、`DeploymentPanel/ConfirmDeployment` | 独立 Canvas 生命周期保留，面板/确认/说明复用 AVG | 部署拖放、确认、退出、无部署区回退；不能迁移到会被战斗输入锁提前隐藏的 HUD |
| U23 | 加载 / 转场 / tips | `Assets/Scripts/UI/BattleTransitionPanel.cs`，由常驻 GM 使用；支持引用及回退 loadingPanel，`Resources/UI/LoadingTipCatalog` | 转场遮罩保持专用；tips/标题/进度条外壳统一 AVG，保留回退路径 | 异步加载完成、黑屏保护、tips冷却/分页、切换后清理；不修改实际进度与等待条件 |
| U24 | 伤害 / 命中 / 浮动文字 | `HitInfoPanel`、`TipText`、`Assets/Prefab/UI/TipText 1.prefab`，棋子/对象池入口 | 保留专用表现、阵营和伤害语义；仅统一可读字体及色板 | 不套 Panel 边框、不拦截射线，不改变时间倍率适配或对象池租期 |
| U25 | 移动 / 技能范围 / 目标指示 | `RangeUI` + `.Presentation.cs`、`EnemyAttackIndicator`；`Assets/Prefab/UI/RangeUI.prefab` 等 | 保留世界空间几何与范围素材；可复用色板，不能套窗口模块 | 遮挡/跨层/准星/合法性；B21/B18规则不重写，这是有理由的结构例外 |
| U26 | InfoBox / 消息确认 | `InfoBox` 场景内嵌；`MessagePanel` 在 AVG/Playing 与 `Assets/Prefab/UI/MessagePanel.prefab` | 视觉适配共享确认/取消模块，先核对调用来源，不能仅凭相似外观合并回调 | 动态文本、等待确认/取消及剧情异步续行；MessagePanel 不等同于战斗操作日志 |
| U27 | 队伍 / 角色 / 技能 / 武器页 | `EscPanels/TeamPanel`、`CharactorPanel`、`CharactorSkillPanel`、`CharacterUIPage`、`WeaponPanel`；`Assets/Prefab/UI/TeamPanel.prefab` | 功能页保留，统一页签/正文/按钮外观；角色页的模因入口继续调用 U13 | 队员选择、技能装配及跨地图用途；不能按“旧战斗面板”整体删除 |
| U28 | 保存 / 剧情正文风格基准 | `Assets/Prefab/UI/SavePanel.prefab`、`SaveCell.prefab`、`Assets/Prefab/剧本/AVG正文.prefab` | 作为面板/标题/按钮/文本来源；本阶段不把其业务实例挂到战斗中 | AVG 及存档业务不随本项重写；共享的是视觉资产和样式约定 |

## 共享模块与引用现状

六个 prefab 位于 `Assets/Prefab/UI/AvgCommon/`。第一阶段扫描时六者在其他正式 prefab/unity 文件均为零直接引用。第二步新增 BattlePauseWindow.prefab，真实嵌套 Panel 一次、Header 一次、Button 三次；第三步新增 BattleResultWindow.prefab，嵌套 Panel 一次、Header 一次、Button 两次；Separator/Track/Portrait 仍未接入，不能将部分接入误写为全量统一。现有速度条的同源 sprite 复用，以及代码生成的结构，需要单独迁移。

| 模块 | 现有可复用内容 | 当前引用 / 使用证据 | 后续消费者与约束 |
| --- | --- | --- | --- |
| Panel | `Panel.prefab`；`Assets/Art/UI/AvgCommon/PanelCut.png`，Border 28/28/28/28 | U14 暂停与 U15 结果模板嵌套 Panel；速度条与 B34 样式仍是 PanelCut 素材复用 | U01/U05/U07/U09/U13/U14/U15/U17/U18/U19/U22；小卡默认保留描边像素，避免乘倍率缩细 |
| Header | `Header.prefab`；`HeaderCut.png`，Border 26/26/26/26 | U14 暂停与 U15 结果模板嵌套 Header | 主要窗口标题；紧凑菜单行不直接套大标题尺寸 |
| Button | `Button.prefab`；`ButtonNormal/ButtonSelected`，Border 40/12/28/12 | 存在正常/高亮/按下 sprite 状态；U14 暂停模板嵌套三个 Button，U15 结果模板嵌套两个 | U02/U03/U04/U13/U14/U15/U16/U19/U22；保留 CustomAdvancedButton 与原监听器，仅适配外观 |
| Separator | `Separator.prefab` | 无直接引用；不能假定所有分隔线来自该模块 | U05/U09/U13/U17/U27；分隔线不能覆盖射线或成为焦点 |
| Track | `Track.prefab` | 无直接引用；现有资源条/AP仍由正式脚本生成/更新 | U01/U07/U10/U23；复用背景轨道，填充仍用 Filled，不能把带刻度速度轨道当普通血条 |
| Portrait | `Portrait.prefab` 与同源面板素材 | 无直接引用；速度条复用取景算法；B34 头顶头像已移除 | U01/U10/U27；保留透明留白、非人形取景和遮罩，禁止通过改 Sprite pivot 修复 UI |
| Close / Confirm | `Assets/Prefab/UI/Buttons/closeBtn.prefab` 等现有控件 + AVG 原窗口事件样例 | 尚无统一关闭/确认适配层，现有接口有 Button 和 CustomAdvancedButton 两类 | 先统一视觉/焦点/禁用状态，再接调用者回调；不清空已有监听器来套统一模板 |
| Scrollbar / Viewport | AVG 对话滚动条素材 + 各窗口现有 ScrollRect | U03/U05/U13/U17 各自拥有视口和布局 | 保留各自内容和滚动位置语义，可抽视口/滚动条视觉；避免 LayoutGroup/ContentSizeFitter 冲突 |

## 色板、字体与素材迁移

复用 `GameConst.UIColors` 现有 token，不再新增相近的硬编码蓝灰/金色。常用映射：Background `#11181E`，Surface `#243042`，SurfaceInset `#202C3D`，Border `#A59177`，Accent `#E7A533`，Text `#EEE6CF`，TextMuted `#A3ABAF`；Health `#7DC59E`、Ally `#4AC3D5`、Enemy/DamagePreview `#EA6963` 是已有战斗扩展色，不能称为 AVG 原图提取色。

AVG SettingPanel 的正文引用 `Assets/Fonts/Font/SourceHanSansSC/SourceHanSansSC-Regular.otf`，GUID `43c823e76308de643987ef1596ceaac3`；B34 样式沿用该字体。保留现有 UnityEngine.UI.Text 绑定，不把全量 TMP 迁移夹带进本次视觉统一。

| 现有外观分支 | 迁移处理 |
| --- | --- |
| `Assets/A美术/BattleCommonUI/` 中 PanelOutline、PortraitOutline 及同类窗口/头像边框 | 列为待替换素材；不作为 AVG 基准，也不因纹理暂未引用就立刻删除整目录 |
| 行动行 MenuRowFill/MenuRowOutline、技能详情 panelFill/panelOutline | 对照 U02–U05 逐实例迁移 AVG Button/Panel；保留紧凑布局、悬停/滚动行为 |
| `BattlePanelFrame` 自绘边框 | 它不是 AVG sprite 模块；U14/U15 已改为嵌套 AVG 模板，U01 等其余消费者仍待迁移。保留其他调用者与组件 |
| 暂停、结果回退、状态详情、模因卡片、部署、快进、tips 的硬编码底色与按钮 | 分批替换为共享 token 和模块；动态生成路径与已绑定 prefab 路径均须覆盖 |
| 世界范围、骰子、浮动伤害、快进/齿轮图标 | 保留专用形状与交互，统一允许复用的色板/字体；不把它们列成“无理由不统一”的例外 |

## 辅助组件归属

56 个战斗 UI 文件中包含 partial 布局文件及非面板辅助类，不能把每个文件都计成独立窗口。其余组件按以下归属维护：

| 组件 | 归属 / 处理 |
| --- | --- |
| `ActionResourceCost`、`ActionCostPreviewUI`、`ActionMenuTiming` | U01–U05 费用/展开规则；只读预览和扣费时点不随样式改动 |
| `ActionPointPipsUI`、`HealthBarFillAnimation` | U01/U07/U08/U10 公共数据表现；保留独立生命/AP变化和真实时间动画 |
| `BattleStatusReadModel`、`BuffCell`、`MemeLoadoutInfo` | U07/U09/U13 数据与状态单元；缺图回退、来源和分页不丢失 |
| `SpeedTimelineMarkerUI`、`BattleTimeControlIcon` | U10/U16 头像取景与专用图标；不单独创建业务面板 |
| `PieceHeadUI` | 旧头像/生命/选中高亮展示，加入 U08/U10 引用核对；不以它取代正式速度队列逻辑 |
| `ActivePause`、`UIPanel`、`UITabController`、`UIButtonList` | 暂停、窗口、页签及按钮布局辅助；保留行为，外观由消费者迁移 |
| `UIMovement`、`SlideImage` | 浮动/旋转/闪烁与图片翻页辅助；逐消费者查用途，不把工具脚本直接判为已退役窗口 |
| `BattlePanelFrame`、`BattleHealthBarStyle` | 前者为待迁移的程序描边，后者目前只服务 B34 头顶卡片；不是两套应继续扩散的主题基准 |

## 分批执行与验收

1. **共享基础**：建立可供 prefab 和动态代码同时消费的 Panel/Header/Button/Separator/Track/Portrait 样式绑定；列出每个消费者。B34 的 `BattleHealthBarStyle` 目前仅服务头顶卡片，不将其命名或能力夸大为全局主题系统。
2. **模态窗口**：U14/U15/U09/U13 先迁移外壳、标题、确认/关闭；保持暂停持有、结果提交和草稿生命周期。已有设置 U16 复用原 prefab，避免创建第二套设置窗口。
3. **菜单与 HUD**：U02–U07、U10、U12、U17/U18 迁移。先完成 B34 顶部 HUD 遮挡/画面边界及相机复验，再扩散其头顶尺寸方案。
4. **入口与跨场景**：U19–U23/U26/U27，同时覆盖场景内嵌、prefab 继承、运行时回退。AVG 原场景不改，战斗实例适配不得写回共享资产。
5. **清理旧分支**：旧回合 UI、旧失败窗口、旧头顶视觉、旧 BattleCommonUI 窗口边框，只有在引用核对和真实场景回归通过后才能退役；保留仍被战斗/剧情调用的接口。

每一批检查 1280×720 / 1920×1080、打开/关闭/取消、键盘焦点与世界输入、1x/3x/暂停、数据重新绑定、销毁/回收/重开，核对共享材质与 AVG 源资产没有被运行时写改。确认同一功能只有一个有效业务入口，不能仅以“画风接近”或“GUID 零直接引用”判定完成。

## 本轮验证与后续状态

- 静态核对 322 个 prefab/unity、56 个战斗 UI 脚本文件及跨模块入口，GUID 引用明细保存在忽略的 `tmp/B35Inventory/reference-report.json`。本文保留正式入口、结论、原因与迁移顺序，不依赖临时 JSON 才能理解。
- 已核对表中主要脚本、prefab 和素材路径；此阶段是文档及引用审计，不新增测试、运行时入口或自动删除资产。
- 上项 B34 在当前代码上的隔离 Unity 运行有 68 项通过、运行期错误0，并有双分辨率卡片和真实战场截图；不作为 B35 所有窗口的运行验收。
- B35 已迁移 U14/U15 模板并执行专项；其余窗口替换、共享基础完善、跨场景与双分辨率全矩阵尚未完成，剩余任务按用户最新要求跳过。


## 2026-11-05 模板验收补充

B34现已从运行时代码构造卡片改为正式共用prefab，支持真实Prefab Variant和UIManager覆盖模板；手动布局/字体/颜色保留、顶部避让及屏幕边界专项通过。最终87项/146帧、测试运行期错误0。新模板是头顶卡片的共享结构，不代表六个AvgCommon模块已全部接入其他窗口。手调入口和完整边界见[UI模板验收与手动调整](UI模板验收与手动调整.md)。
