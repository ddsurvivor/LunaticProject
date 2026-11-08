# UI 模板验收与手动调整

日期：2026-11-05。开发分支：`dev_feature`。本轮重点验收 B34 头顶卡片的运行效果、模板复用与手动调整能力，不把 B35 的面板清单当成已完成的 UI 迁移。

## 哪些已经是正式模板

| UI | 模板与接入方式 | 手动调整能力 |
| --- | --- | --- |
| 敌我头顶卡片 | `Assets/Resources/UI/PieceOverheadStatus.prefab`；`BattleOverheadStatusLayer.CreateView` 实例化，敌我共用同一模板 | 可编辑层级、RectTransform、文字字号及图形；可创建 Prefab Variant，在 UIManager 的 `Overhead Status Prefab` 中指定 |
| 头顶共享外观 | `Assets/Resources/UI/BattleHealthBarStyle.asset` | 调整 AVG 面板 sprite、字体、九宫格倍率；模板关闭 `Apply Shared Style` 后可独立调整外观 |
| 速度条 | `Assets/Resources/UI/SpeedTimeline.prefab`，由 `SpeedTimelineUI` 创建 | 可调整轨道、模板头像、字体及外观；角色顺序、头像位置/数量/密集缩放由运行数据控制，本轮未对其做完整变体调参矩阵 |
| 设置窗口 | `Assets/Prefab/UI/SettingPanel.prefab`；战斗复用窗口实例或指定 prefab | 使用现有 prefab；战斗会适配关闭按钮和暂停，不能把这部分业务行为当成纯静态布局 |
| B35 其余窗口 | 暂停、结果回退、模因换装、状态详情等仍含运行时代码生成结构 | 尚未全部改为共享 prefab；不能保证这些窗口已具备与头顶模板相同的手调能力，见面板清单逐项迁移 |

## 这次修正了什么

此前 B34 只有共享样式资产，实际子层级和坐标由代码生成，不能据此称为可编辑通用 prefab。本轮将卡片完整层级、UI 引用、AP 种子、状态格和样式绑定保存为正式 prefab。默认加载该资源，也支持由 UIManager 指定变体。

运行时仅实例化并绑定数据，不再重建卡片层级，不覆写模板宽度、资源文字位置或字号。模板必要引用缺失时会明确报错，不静默生成另一套布局。菜单 `Tools → Battle UI → Create Overhead Template If Missing` 仅用于首次创建；已有资产文件时直接返回，不覆盖手动编辑。

额外修正了边缘卡片裁切以及与速度条、快进/设置区域的重叠。模板可以控制 `Head Offset`、`Constrain To Screen`、`Avoid Top Hud`。原先固定头顶放置的注释与行为已相应更新。

## 怎样手动调整

1. 在 Project 中打开 `Assets/Resources/UI/PieceOverheadStatus.prefab` 的 Prefab Mode。修改全局模板时直接保存；只改某类场景时创建 Prefab Variant。
2. 可直接调整 `Title`、`Health`、`Resources`、`ActionPoints`、`Status0–Status5` 的位置/尺寸、锚点与文字设置。保留根组件的字段引用和六个 BuffCell，不删除必要的文字、图片、按钮或裁切组件。
3. 根 `PieceOverheadStatusUI` 的 `Empty Height / Single Row Height / Double Row Height` 控制无状态、一行状态、两行状态时的高度；`Minimum Name Size / Maximum Name Size` 控制姓名缩放范围。直接拖根节点高度会被这三个高度规则更新，应该修改对应字段。
4. 默认勾选 `Apply Shared Style`，绑定时统一面板 sprite、字体和文字色；若要在变体中独立调整这些外观，取消勾选后修改 prefab 图形/文字。资源文字字号、位置本来就不会被共享样式覆盖。
5. 将变体拖到场景或 BattleScene prefab 的 UIManager → `Overhead Status Prefab`。留空时使用正式 Resources 默认模板，不需要为每个棋子挂一份卡片。
6. 在 Edit Mode / Prefab Mode 保存修改。Play Mode 下拖动运行时实例只用于观察，不会自动写回模板。

## 哪些属性由数据控制

- 姓名、生命/AP/弹药/能量、状态条目、预览和阵营色会随角色更新，不能把示例内容当成固定文本。
- 根节点跟随棋子；其屏幕位置、可见性和整体显示缩放由展示层控制，偏移及避让通过根组件参数调整。
- 卡片宽度、普通文字位置、字号及子层级来自 prefab；卡片高度按根组件的三种状态高度字段更新。
- 头顶模板按最新要求不再包含头像；姓名、生命和 AP 区域向左扩展，继续使用同一套 prefab/Variant 机制。
- AP 点阵的数量、排列及消耗动画由 `ActionPointPipsUI` 管理，编辑其父区域改变布局空间。缺图状态仍使用文字，不在刷新时重新定位状态名称。

## 验收方法与证据

在 `tmp/Acceptance/B01-B08-20261029/Project` 的隔离 Unity 2021.3.23f1 项目中生成并导入模板，进入 PR4-2 真实运行。通过编辑器 API 模拟 Prefab Mode 修改，保存为真正的 Prefab Variant：宽度改为300，无状态高度112，资源文字位置改为(40,-84)、字号13，并关闭共享样式以保留自定义文字色。

验证默认模板及变体的必要绑定、敌我复用、运行时层级数量、宽度、连续刷新、重新绑定、基础 prefab 哈希不变，以及生成菜单不会覆盖已有资产。模板依赖清单单独检查，不允许依赖临时验收脚本。

当前完整检查结果、截图及日志位于 Git 忽略的 `tmp/UIAcceptance20261105/`；最终统计见本页文末补充。截图包括两种分辨率的卡片、红黄伤害预览、真实战场，以及默认模板与手调变体的对照。原先有顶部裁切的截图保存在 `tmp/B34HealthBars/`，用于对照，不作为修正后的结果。

本轮运行由临时脚本调用正式接口和事件，没有模拟操作系统键鼠；死亡显示通过正式 `SetHealth(0)` 进入死亡状态检查，再以 Init 重建检查复用。它不等同于所有死亡演出、复杂伤害连锁或每个关卡完整回归。尚未完成发行构建、全关卡、所有相机角度和 B35 其余窗口的验收。


## 最终结果

- 最终87项检查全部通过，0失败；146帧，测试运行期Error/Exception/Assert为0。业务与编辑器程序集编译通过，diff检查通过。
- 默认模板与手调Variant在两种分辨率运行；1280×720、1920×1080的实际可见头顶卡片均通过屏幕安全区及顶部HUD不重叠检查。
- 死亡状态隐藏、恢复显示、Init后同视图复用/状态清理/生命快照重置通过。手调宽度、高度、资源位置、字号、自定义颜色及重绑定保留通过。
- 生成器重复调用不改写已有模板；Variant的生成不改变基础prefab哈希。正式模板与验收副本字节一致，依赖清单不包含临时验收类型。
- 本结果仅覆盖上述模板、当前PR4-2及受控用例。B35其余窗口、全关卡和发行构建没有因此自动通过。临时Unity入口在执行后停用。


## 头顶头像移除（2026-11-05）

按用户要求移除头顶模板的头像及占位框，取消对应运行时加载、绑定与必需引用；姓名、生命和AP区域使用释放出的横向空间。仍保留敌我共用prefab及手动调整能力。此前含头像的截图是历史记录，最新截图见 `tmp/NoPortraitUI/`。
