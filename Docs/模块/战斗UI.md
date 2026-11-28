# 战斗 UI

B85第一部分（2026-11-28）：苹果树报警确认复用`Resources/UI/BattleResultWindow`及`BattleFinishPanel`，独立实例显示“触发警报”，隐藏重开按钮，仅保留“确认”。独立`ActivePause`暂停，世界输入及Esc菜单在报警期间阻塞；确认回调成功才关闭，连点只消费一次，普通胜败窗口保留原两按钮及业务绑定。报警窗口不触发胜负、奖励或自动整场重开；实际画面及边界见结果页。

B63：本文历史`Tools/Battle UI/`入口已撤下，模板生成/升级实现暂留待删除；正式预制体、Variant及运行组件保留，后文菜单路径不再是可用入口。

B68（2026-11-22）：头顶卡片固定投影到棋子头顶并应用模板屏幕偏移，不再避让顶部HUD或限制在安全区内；临近画面边缘时允许裁切。下文旧避让描述以此更新为准。

B69（2026-11-22）：速度条头像按实际行动顺序均匀排成一行，人数增多时按轨道宽度缩小；当前行动者保留放大及强调，排序仍由正式调度提供。下文旧进度坐标及避让描述以此更新为准。

B70（2026-11-22）：共享头顶血条每次刷新按棋子阵营使用Health绿色或Enemy红色；伤害预览仍由独立图层显示。下方B56待实现的历史记录以本更新为准。

B71（2026-11-22）：区域、扇形、弧形及手雷范围瞄准时按住左右Ctrl跳过棋子吸附，松开恢复；范围期间技能说明显示操作提示。尚需正式关卡验证输入、提示生命周期及预览命中一致性。

素材生成入口为 `python Tools/Art/generate_battle_ui.py`（依赖Pillow）。正式Sprite与导入配置写入 `Assets/A美术/BattleCommonUI/`，提色JSON和设计预览写入 `tmp/Reports/BattleUIArt-<日期>/`；这些设计图不能作为运行验收截图。

样式使用 `GameConst.UIColors`；保留现有业务组件、事件、暂停持有者及序列化绑定。B35 其余面板迁移继续跳过，不能按旧迁移顺序自动恢复开发。

最新覆盖：B47左下Buff格子26×26、间距2；B48头顶锚点及顶部HUD横向避让；B50教程使用独立暂停；B51仅BattleScene无CG背景为黑色alpha0.20；B52共享头顶及护盾变体宽120；B53技能瞄准线0.09，普通范围/移动落点0.045。

B54/B55（2026-11-27）：`DamagePreviewFillAnimation`共用于共享头顶（含护盾）和旧HpBarUI。确定扣除的区段使用DamagePreviewMinimum白色，额外可能扣除的区段使用DamagePreviewMaximum黄色，按当前生命截断；黄色沿已确认样式以1.8秒周期在0.18–1透明度间呼吸，持续刷新不重启相位，隐藏/生命复用时清除。真实血条保持阵营色及原伤害动画。

命中/伤害预览由正式`HitInfoPanel.UpdateDisplay`计算；头顶卡片复制并同步已有文本、图标，不重算伤害。原脚底图形通过CanvasGroup隐藏，保留目标高亮取消及血条快照生命周期；只读副本不持有旧血条清理权限。副本锚定卡片顶边上方6像素，最多两列（总宽160像素），图标14像素、文字有效10–13像素，并随Canvas比例补偿字形采样。卡片展开、移动、镜头和既有遮挡规则统一驱动；保持B68固定锚点规则，密集目标不自动避让，屏幕边缘允许裁切。没有修改实际受伤飘字。

B54/B55通过双程序集编译和51项隔离Unity检查，实际渲染截图与边界见[结果](../验收和结果.md)。尚未完成正式关卡手动操作及Player验收。下文“红黄预览”和旧Enemy/DamagePreview配色为历史记录，以本更新及B70为准；B56已由B70覆盖。

## 默认选择与头像聚焦（B82）

2026-11-27：`ClickManager.UnitSelection`统一普通默认选择与头像入口。速度条每个有效参战者头像根背景接收左键点击，头像裁切/装饰本身仍不拦截射线；`SpeedTimelineMarkerUI`绑定当前单位，回调由所属速度条确认同一BattleManager后交给`TrySelectPortraitUnit`。左下`PieceInfoPanel.head`首次接入按钮，重复刷新不叠加监听，保留原按钮事件；点击读取当前显示的角色。

当前可行动者显示行动菜单；普通非行动者（含敌人）仅选中、查看详情和聚焦，不改ActivePiece、不授予AP或行动权。敌人详情沿已有状态面板，左下信息仍遵守只显示玩家的原规则。聚能合格队友经正式TrySelectBurstActor请求切换，安全交接后才变更行动者，不额外扣费或补AP。死亡、停用、未唤醒及丢失目标不接受头像选中。

右键取消/空白点击回到当前可行动者；头像临时查看关闭或目标失效后恢复，选择丢失时补回。普通回合交棒沿原SelectActivePiece刷新；不逐帧重设镜头，手动平移保持。技能/指令瞄准、移动选点、HasActionFlow协调、移动执行/战斗表现、暂停/窗口、模因页、聚能交接、外部控制和转场期间头像及自动恢复让出控制。UI命中即停止世界左键处理，不依赖Selectable焦点，避免头像点击同时提交下方的移动。

头像聚焦调用原CameraController.SetFollow，沿现有Cinemachine优先级与跟随链路，不改镜头缩放/阻尼。金色行动强调仍只指示当前行动者，临时查看不改变队列顺序。60项隔离Play Mode检查及双程序集编译通过，含原生GraphicRaycaster、Cinemachine位置、正式聚能切换、临时查看/恢复、生命周期与HUD回调幂等；正式关卡、物理输入、完整瞄准和Player待用户验收，证据见结果页。

## 模板与手调

B77装置影响范围由`ClickManager.DeviceRange`接入既有悬停入口，独立复用`BattleRangeStyle`世界圆环，不占用`RangeUI`的技能/移动状态。显示规则、颜色及水平截面边界见[场景装置](场景装置.md)。

### 战前布置模板（B81 / U22）

2026-11-27用户单项授权U22，不恢复B35其余面板批量迁移。`BattleDeploymentController`保留独立Canvas（sortingOrder=150），加载`Assets/Resources/UI/BattleDeploymentPanel.prefab`；可在其`Deployment Panel Prefab`指定变体。该组件由BattleManager自动接入，已有场景无需逐个重存；要保存自定义引用，可在BattleManager宿主预挂同一组件并指定模板。

模板保留AVG Panel/Header/Button的真实嵌套连接及共享字体。顶部标题、当前棋子与操作反馈分开；宽度按屏幕锚点伸缩，1920×1080参考缩放宽高匹配0.5。背景只在面板范围内拦截射线，不遮挡整个战场，不添加EventSystem或暂停持有者。确认沿原`BattleManager.ConfirmDeployment → ValidateAll → End → StartBattle`；拖拽时按钮不可交互且alpha0.4，结束恢复1。导航采样、区域占地/碰撞、拖拽提交与取消回退方法未改。

可在Prefab Mode调整Panel/Header、Selection/Status文字、ConfirmDeployment位置/尺寸和字体，保留BattleDeploymentPanel的四个绑定。运行时只更新姓名、状态、按钮interactable和确认CanvasGroup alpha；不重建层级或改写字号/锚点。模板事件保留，Bind只移除本实例上一次绑定的监听。结束/禁用立即隐藏并释放实例，恢复原战斗按钮状态；无部署区与重开回退仍走原流程。

59项隔离Play Mode检查及双程序集编译通过，960×540、1280×720、1920×1080、1280×1024实际渲染复核；夹具使用真实部署控制器、区域和NavMesh及正式Deployment partial，外围角色/开战后流程替身。两轮截图、复现步骤、未覆盖边界见结果页；正式关卡/物理输入、原场景遮挡和Player待验收。

### 哪些已经是正式模板

| UI | 模板与接入方式 | 手动调整能力 |
| --- | --- | --- |
| 敌我头顶卡片 | `Assets/Resources/UI/PieceOverheadStatus.prefab`；`BattleOverheadStatusLayer.CreateView` 实例化，敌我共用同一模板 | 可编辑层级、RectTransform、文字字号及图形；可创建 Prefab Variant，在 UIManager 的 `Overhead Status Prefab` 中指定 |
| 头顶共享外观 | `Assets/Resources/UI/BattleHealthBarStyle.asset` | 调整 AVG 面板 sprite、字体、九宫格倍率；模板关闭 `Apply Shared Style` 后可独立调整外观 |
| 速度条 | `Assets/Resources/UI/SpeedTimeline.prefab`，由 `SpeedTimelineUI` 创建 | 可调整轨道、模板头像、字体及外观；角色顺序、头像位置/数量/密集缩放由运行数据控制，本轮未对其做完整变体调参矩阵 |
| 战前布置 U22 | `Assets/Resources/UI/BattleDeploymentPanel.prefab`，部署控制器实例化到独立Canvas | 嵌套AVG模块，可编辑层级/字体/锚点并创建Variant；动态内容及拖拽确认状态由组件控制 |
| 设置窗口 | `Assets/Prefab/UI/SettingPanel.prefab`；战斗复用窗口实例或指定 prefab | 使用现有 prefab；战斗会适配关闭按钮和暂停，不能把这部分业务行为当成纯静态布局 |
| B35 其余窗口 | 暂停、结果回退、模因换装、状态详情等仍含运行时代码生成结构 | 尚未全部改为共享 prefab；不能保证这些窗口已具备与头顶模板相同的手调能力，见面板清单逐项迁移 |

### 怎样手动调整

1. 在 Project 中打开 `Assets/Resources/UI/PieceOverheadStatus.prefab` 的 Prefab Mode。修改全局模板时直接保存；只改某类场景时创建 Prefab Variant。
2. 可直接调整 `Title`、`Health`、`Resources`、`ActionPoints`、`Status0–Status5` 的位置/尺寸、锚点与文字设置。保留根组件的字段引用和六个 BuffCell，不删除必要的文字、图片、按钮或裁切组件。
3. 根 `PieceOverheadStatusUI` 的 `Empty Height / Single Row Height / Double Row Height` 控制无状态、一行状态、两行状态时的高度；`Minimum Name Size / Maximum Name Size` 控制姓名缩放范围。直接拖根节点高度会被这三个高度规则更新，应该修改对应字段。
4. 默认勾选 `Apply Shared Style`，绑定时统一面板 sprite、字体和文字色；若要在变体中独立调整这些外观，取消勾选后修改 prefab 图形/文字。资源文字字号、位置本来就不会被共享样式覆盖。
5. 将变体拖到场景或 BattleScene prefab 的 UIManager → `Overhead Status Prefab`。留空时使用正式 Resources 默认模板，不需要为每个棋子挂一份卡片。
6. 在 Edit Mode / Prefab Mode 保存修改。Play Mode 下拖动运行时实例只用于观察，不会自动写回模板。

### 哪些属性由数据控制

- 姓名、生命/AP/弹药/能量、状态条目、预览和阵营色会随角色更新，不能把示例内容当成固定文本。
- 根节点跟随棋子；其屏幕位置、可见性和整体显示缩放由展示层控制，偏移及避让通过根组件参数调整。
- 卡片宽度、普通文字位置、字号及子层级来自 prefab；卡片高度按根组件的三种状态高度字段更新。
- 头顶模板按最新要求不再包含头像；姓名、生命和 AP 区域向左扩展，继续使用同一套 prefab/Variant 机制。
- AP 点阵的数量、排列及消耗动画由 `ActionPointPipsUI` 管理，编辑其父区域改变布局空间。缺图状态仍使用文字，不在刷新时重新定位状态名称。

## 模块入口与复用范围

更新：2026-11-05。用户最新要求跳过剩余面板，后续迁移停止；已完成的 U14/U15 保留，其他项目不计完成。分支：`dev_feature`。B35 第一阶段完成入口盘点、共享模块现状、迁移矩阵及执行顺序；第二步将 U14 暂停窗口、第三步将 U15 结果窗口接入真实嵌套 AVG 模块模板。B35 整体保持进行中，尚未批量替换窗口或宣称全量视觉统一。

### 核对范围与判定

核对 `UIManager` 及其 Windows/Messages 部分、`Assets/Scripts/Battle/UI/` 的 56 个 C# 文件、战斗部署、剧情检定、加载转场、设置及队伍入口；扫描正式 `Assets/` 下 322 个 prefab/unity 文件的脚本和模块 GUID 直接引用。扫描不包含 `tmp/`。

GUID 直接引用不等于运行时实例数量，也不展开 prefab 继承。无直接引用的类仍可能通过 `AddComponent` / `Resources.Load` 创建。以下把场景内嵌、独立 prefab、运行时生成分开说明；不因同名旧资产或零引用而直接删除。

`Assets/Prefab/Battle/BattleScene.prefab` 是战斗 UI 主要承载体，但仍有场景直接内嵌 UIManager/InfoBox/TurnPanel 等组件。后续只改一个 prefab 不能覆盖所有关卡。

### 面板清单

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
| U14 | 暂停窗口 / Esc 面板 | `UIManager.InitializePauseWindow` 实例化 `Resources/UI/BattlePauseWindow.prefab`；`keyPanelDic` 适配旧窗口 | 已接入嵌套 AVG Panel/Header/Button；支持 Prefab Variant 与 Inspector 模板覆盖，见[记录](../验收和结果.md) | `ActivePause` 多持有者、Esc层级、继续/重开/退出；不能因样式替换重建事件或暂停持有 |
| U15 | 胜负 / 奖励 / 重开 | `BattleFinishPanel` + `UIManager.InitializeResultWindow`，统一加载 `Resources/UI/BattleResultWindow.prefab`；旧内嵌实例初始化时停用 | 已接入 AVG Panel/Header/Button 嵌套模板，支持 Variant、滚动奖励；有/无旧面板引用共用同一入口；见[记录](../验收和结果.md) | 奖励唯一提交、继续/重试、结果不可留空遮罩、返回场景；不能新建第二个胜败控制器 |
| U16 | 右上快进 / 设置 | `BattleTimeControls.Create` 运行时生成，优先场景内 SettingPanel，再实例化指定 prefab；`Assets/Prefab/UI/SettingPanel.prefab` | 设置整窗已复用 AVG，保留；快进/齿轮外壳改为共享 Button/Surface，图标保持原绘制 | 短按/长按、1x/3x、失焦恢复、设置暂停；只改战斗实例，不改 AVG 原场景 |
| U17 | 顺序通知 / 操作日志 | `UIManager.Messages` 创建消息 Canvas，重挂 `skillNameDisplay/PlayerLogPanel` | 共用通知容器、正文/强调色及滚动组件；保持不同消息区用途 | 有界队列、真实时间、独立滚动、安全区；不可把伤害跳字也并入同一队列 |
| U18 | 物品 / 经验通知 | `NotificationManager` 的普通/经验池；`Assets/Prefab/UI/ItemGetTipPanel.prefab`、`LevelUpTipPanel.prefab`；`ItemGetPanel/NotificationItem` | 两类数据保留，共用外框/图标/文字槽及入退场表现 | 队列、池复用、关闭及奖励显示；`Market/ItemGetPanel` 跨模块用途需保留 |
| U19 | 教学 / 开场提示 | `TutorialUI`、`TutorialManager`；BattleScene 内嵌、`Assets/Prefab/UI/TutorialPanel.prefab`，AVG/Playing 也有直接引用 | 面板/标题/确认复用；数据和教学顺序保留 | 已读标记、首触发、完成后交还控制；不能把普通提示都判为教学关 |
| U20 | 战斗开场 / 胜利演出 | `BattleStartUIPanel`；BattleScene、`Assets/Prefab/UI/BattleStartPanel.prefab`，AVG/Playing 也有直接引用 | 演出容器保留，文字/按钮统一；不把它替换为暂停或结果控制器 | 演出完成回调、跳过、输入占用；注意 UIManager 对其有胜利提示兼容用法 |
| U21 | 检定面板 / 成败标识 | `Assets/Scripts/剧本/CheckDicePanel.cs`；BattleScene、`Assets/Prefab/UI/DicePanel.prefab` 等 | 保留剧情与战斗共用检定控制；外框/按钮/字体统一，骰子专用表现保留 | 检定等待和最终结算次序，不因换窗跳过确认回调 |
| U22 | 部署面板 / 确认按钮 | `BattleDeploymentController` 独立Canvas加载`Resources/UI/BattleDeploymentPanel` | B81已接入嵌套AVG Panel/Header/Button，独立生命周期保留；待用户验收 | 部署拖放、确认、退出、无部署区回退；不能迁移到会被战斗输入锁提前隐藏的 HUD |
| U23 | 加载 / 转场 / tips | `Assets/Scripts/UI/BattleTransitionPanel.cs`，由常驻 GM 使用；支持引用及回退 loadingPanel，`Resources/UI/LoadingTipCatalog` | 转场遮罩保持专用；tips/标题/进度条外壳统一 AVG，保留回退路径 | 异步加载完成、黑屏保护、tips冷却/分页、切换后清理；不修改实际进度与等待条件 |
| U24 | 伤害 / 命中 / 浮动文字 | `HitInfoPanel`、`TipText`、`Assets/Prefab/UI/TipText 1.prefab`，棋子/对象池入口 | 保留专用表现、阵营和伤害语义；仅统一可读字体及色板 | 不套 Panel 边框、不拦截射线，不改变时间倍率适配或对象池租期 |
| U25 | 移动 / 技能范围 / 目标指示 | `RangeUI` + `.Presentation.cs`、`EnemyAttackIndicator`；`Assets/Prefab/UI/RangeUI.prefab` 等 | 保留世界空间几何与范围素材；可复用色板，不能套窗口模块 | 遮挡/跨层/准星/合法性；B21/B18规则不重写，这是有理由的结构例外 |
| U26 | InfoBox / 消息确认 | `InfoBox` 场景内嵌；`MessagePanel` 在 AVG/Playing 与 `Assets/Prefab/UI/MessagePanel.prefab` | 视觉适配共享确认/取消模块，先核对调用来源，不能仅凭相似外观合并回调 | 动态文本、等待确认/取消及剧情异步续行；MessagePanel 不等同于战斗操作日志 |
| U27 | 队伍 / 角色 / 技能 / 武器页 | `EscPanels/TeamPanel`、`CharactorPanel`、`CharactorSkillPanel`、`CharacterUIPage`、`WeaponPanel`；`Assets/Prefab/UI/TeamPanel.prefab` | 功能页保留，统一页签/正文/按钮外观；角色页的模因入口继续调用 U13 | 队员选择、技能装配及跨地图用途；不能按“旧战斗面板”整体删除 |
| U28 | 保存 / 剧情正文风格基准 | `Assets/Prefab/UI/SavePanel.prefab`、`SaveCell.prefab`、`Assets/Prefab/剧本/AVG正文.prefab` | 作为面板/标题/按钮/文本来源；本阶段不把其业务实例挂到战斗中 | AVG 及存档业务不随本项重写；共享的是视觉资产和样式约定 |

### 共享模块与引用现状

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

### 色板、字体与素材迁移

复用 `GameConst.UIColors` 现有 token，不再新增相近的硬编码蓝灰/金色。常用映射：Background `#11181E`，Surface `#243042`，SurfaceInset `#202C3D`，Border `#A59177`，Accent `#E7A533`，Text `#EEE6CF`，TextMuted `#A3ABAF`；Health `#7DC59E`、Ally `#4AC3D5`、Enemy/DamagePreview `#EA6963` 是已有战斗扩展色，不能称为 AVG 原图提取色。

AVG SettingPanel 的正文引用 `Assets/Fonts/Font/SourceHanSansSC/SourceHanSansSC-Regular.otf`，GUID `43c823e76308de643987ef1596ceaac3`；B34 样式沿用该字体。保留现有 UnityEngine.UI.Text 绑定，不把全量 TMP 迁移夹带进本次视觉统一。

| 现有外观分支 | 迁移处理 |
| --- | --- |
| `Assets/A美术/BattleCommonUI/` 中 PanelOutline、PortraitOutline 及同类窗口/头像边框 | 列为待替换素材；不作为 AVG 基准，也不因纹理暂未引用就立刻删除整目录 |
| 行动行 MenuRowFill/MenuRowOutline、技能详情 panelFill/panelOutline | 对照 U02–U05 逐实例迁移 AVG Button/Panel；保留紧凑布局、悬停/滚动行为 |
| `BattlePanelFrame` 自绘边框 | 它不是 AVG sprite 模块；U14/U15 已改为嵌套 AVG 模板，U01 等其余消费者仍待迁移。保留其他调用者与组件 |
| 暂停、结果回退、状态详情、模因卡片、部署、快进、tips 的硬编码底色与按钮 | 分批替换为共享 token 和模块；动态生成路径与已绑定 prefab 路径均须覆盖 |
| 世界范围、骰子、浮动伤害、快进/齿轮图标 | 保留专用形状与交互，统一允许复用的色板/字体；不把它们列成“无理由不统一”的例外 |

## 范围与瞄准

### 实现范围

B21 统一当前战斗范围与瞄准展示，继续使用 `SkillTargeting.Query` 和 `MoveManager` 的正式查询，不增加射程、掩体或高差规则。

- 移动为青色，进攻为橙色，支援/自身为绿色，其他技能为紫色，无合法目标的瞄准点为灰色。施放边界降低透明度，效果边界和命中目标保持清晰。
- 用世界坐标细线替代旧图片填充及旧准星叠图；瞄准点、目标圈、移动菱形落点共用几何标记。保留原节点和序列化引用，警戒方向、AI 预览及技能落点接口继续可用。
- 圆形分别显示施放距离与正式单目标采样半径；Grenade 分别显示施放距离与爆炸半径；Nova 以自身为中心；扇形包含当前判定的 1 米内圈；Arc 复用正式半径、圆心、角度和宽度，并显示内外边界。
- 范围、路径与几何标记使用同一深度材质：不写深度，无遮挡正常显示，被不透明场景深度遮挡时以 18% 透明度保留定位参考。深度淡化只影响显示，不改变合法性。
- 高亮每帧及确认前重新查询；目标死亡、失活、移动或关系变化不再沿用旧列表。取消、切换技能、组件隐藏、施法者死亡、复制运行时模板和场景重载会清理旧范围。
- 移动标记使用正式路径终点；超程沿路径截断，不可达时隐藏标记并清除旧路径。鼠标不动时仍重查预算与动态障碍，预览和执行使用相同的导航类型、区域过滤；选择期间更新变化后的移动距离。

## 头顶维护

### 手动调整与维护

- 单个棋子的高度：打开其prefab，选择 `OverheadAnchor` 调整Transform；确认 `PieceDisplay.overheadAnchor` 引用保留。修改旧EnemyCanvas不能调整共享卡片位置。
- 所有卡片的样式/布局：编辑 `PieceOverheadStatus.prefab`，或通过UIManager指定Variant；调整宽度、字号、子节点位置、背景透明度、Head Offset及高度字段。共享样式初始化保留手调alpha；关闭Apply Shared Style可独立设置字体/颜色。
- AP图标的大小/间距可分别在ActionPointPipsUI中调整，默认值仍为22/27，不改变左下信息面板；仅头顶模板采用11/13.5。
- `Tools/Battle UI/Upgrade Compact Overhead Template` 仅升级旧版本一次；重复执行不覆盖后续手调。首次生成工具也产生紧凑模板。未改动AVG场景或恢复B35已跳过面板。

夹具、日志、截图、初轮失败与最终结果均在Git忽略的 `tmp/B40/`。最终证据为 `result.json`、`checks.json`、`semantic-assets.json`、`prefab-audit.json`、`anchors.json`、`baseline.json`、`copied-assets.json`、`unity-final-clean.log`、`unity-assets-final.log`。早期失败来自临时夹具对只读死亡属性/未绑定旧血条的使用、预览刷新时点及序列化比较器；标题高度不足导致字形裁切的问题已修正并验证实际字形生成。临时Unity测试入口已停用，正式代码不依赖测试类型。

### 透明度更正与固定斜角外框（2026-11-06）

用户更正为“不透明度70%”，并指出左下和右上斜角过大、压缩高度破坏框形。现已改为背景 alpha0.70。原 AVG PanelCut 素材上下切片各28像素，大于32像素卡片的总高度，会触发切片边角压缩；仅提高切片倍率也会缩细原描边。

改为 `CompactOverheadFrame` 原生UI几何：固定4参考像素斜角、1参考像素描边，尺寸变化只扩展直边；对角线按法线内缩，描边厚度一致。背景底色与金色描边沿用 AVG 原素材取色，背景70%不透明，描边保持清晰；不修改全局AVG贴图或其他面板样式。维持当前200×32/58紧凑布局、文字/AP/Buff尺寸及锚点。

只将共享模板原背景 Image 转为兼容的派生组件，保留其组件fileID和模板GUID，因此护盾变体自动继承，已有变体的alpha0.42手调覆盖也能保持。没有删除或重建子物体。升级菜单 `Tools/Battle UI/Fix Compact Overhead Frame` 幂等，不反复覆盖后续手调；新建模板也使用固定外框。

Inspector 可直接调整背景色调/不透明度、底色、斜角尺寸、描边宽度及颜色，支持撤销、多选及Variant。共享样式初始化仍保留背景alpha，不在运行中强制改回默认值。

## 消息与窗口

### 本次行为

B22 沿用现有 uGUI、胜负入口、场景重载和离场流程，集中处理消息避让及窗口生命周期。

- 技能名、检定结果与聚能开始/结束提示进入同一顺序队列。待播最多 32 条，超限丢弃最旧的待播提示；每条提示同时写入原有的 15 条有界日志。提示不再并发叠加旧闪图或争用 Tween，暂停时停止轮播。
- 获得通知使用独立区域，最多同时显示 3 条，其余排队等待，不因容量不足丢弃。修复经验池扩容误用物品预制体，以及停用后协程状态残留；通知不拦截射线，停用/离场会清理队列和池中动画。
- 日志复用原有 ScrollRect、视口和滚动条，整理内容布局、字体行距及最新内容跟随；阅读旧条目时保持位置。日志区位于通知下方、原日志开关上方。消息、通知、日志及头顶状态共同避让，并限制在安全区内。
- Esc 打开独立暂停窗口，提供继续、重新开始和返回主菜单；再次 Esc 可取消暂停。原配置菜单也各自持有暂停，关闭其中一个不会恢复另一个窗口或显式暂停；暂停期间拦截棋子输入、程序化命令、聚能选择、AI 决策和速度队列推进。释放后保留原命中停顿的剩余时间及速度。
- 开始动画可重复播放、取消和关闭；旧 Tween 不会继续隐藏新动画。战斗内开始窗口独立持有暂停，UI 按真实时间完成后释放。
- 胜败都显示结果窗口，不再胜利后自动离场。胜利提供继续；失败提供重试和返回主菜单。失败不展示奖励预告。已有结果面板继续复用，缺引用时才生成备用面板。
- 结果窗口只预览胜利掉落与经验，不写库存或存档。继续时校验现有 `Playing` 返回场景，再一次性提交掉落/经验和原生命继承流程；重复结果、连续点击与转场重入不重复发奖。失败不发奖。
- 重试沿用 `GM.ReloadCurrentScene`，退出沿用原主菜单入口；请求确认后停止本场调度、反应与延期效果，清理临时消息、日志和暂停，恢复时间后转场。
- 原 Build Settings 中 `Playing` 未启用，而 `GM.BattleEnd` 固定返回此场景；本次只将该已有场景启用，保留原返回路线，不调整其他场景的发布范围。

### 保留边界

- 重试仍是已有场景重载。B28 的战前快照、胜利后重开、部署恢复、随机种子策略及消耗品/模因装配回滚尚未实现，B22 不将其标记完成。
- 本次只把已有掉落、经验移到既有离场入口提交；不新增奖励经济、剧情节点或成就规则。新增成就及剧情回滚仍按对应任务处理。
- 其他关卡、任意菜单嵌套组合、极端奖励内容、全部输入设备及发行构建仍需后续内容级验收。B04 其他历史未验矩阵不会因本项通过而自动完成。

#### B28 补验（2026-10-30）

胜败结果窗口的重开已接入战前快照恢复及重新部署，暂停重开也使用同一路径；原 B22 窗口回归 69 项再次通过。重开、存档保护、奖励与种子等专项结果见[战斗重开快照与B28验收](战斗流程与场景.md)。
