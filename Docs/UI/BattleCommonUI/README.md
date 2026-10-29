# 战斗通用 UI 设计

## 交付

- `Assets/A美术/BattleCommonUI/`：39 张独立白色透明 PNG、对应 Unity Sprite 导入配置、manifest.json（尺寸、Border、Image.Type）。原始 RGB 恒为 #FFFFFF，形状由 alpha 决定。
- `BattleLayout.png` / `BattleSkills.png`：1920×1080 两级菜单展开示意图，技能详情固定左上；`BattleActions.png`：仅展开一级菜单。使用交付素材与 GameConst.UIColors 合成。
- `AssetOverview.png`、`Palette.png`：素材总览、色板。
- `AVGColorExtraction.json`：36 张源图的不透明像素（alpha ≥ 250）前八位精确 RGB 频次；没有不透明像素的图片保留空记录，不推测背景混色。
- `Tools/Art/generate_battle_ui.py`：确定性素材及精确示意图的生成源，需要 Python/Pillow，项目根目录执行 `python Tools/Art/generate_battle_ui.py`。不会重生成 AI 概念图。

## 视觉规范与来源

延续 AVG/速度行动条的右上、左下 45° 切角、细描边、内外分层与金色强调。边框和底板分开染色，文字使用 TMP，不烘焙到素材中。

| GameConst.UIColors | sRGB | 采样来源/用途 |
|---|---|---|
| Background | #11181E | 对话框/减去顶层 16、存档/矩形 2332-2 |
| Surface | #243042 | 图像设置/侧栏底板、标题栏 |
| SurfaceInset | #202C3D | 对话框/对话框 内层 |
| Border | #A59177 | 对话框/外框、存档/外框 |
| Accent | #E7A533 | 对话框/组选中箭头、选项选中；图像设置/标题金线 |
| Text | #EEE6CF | 对话框/按钮对话框系列 |
| TextHeading | #E5E0CF | 图像设置/标题 |
| TextMuted | #A3ABAF | 图像设置/分类计数 主色之一 |
| Divider | #687A85 | 图像设置/侧栏分隔线 |
| Disabled | #576774 | 图像设置/图像设置列表、设置分类 |
| Ally | #4AC3D5 | 战斗扩展：我方标识、移动范围 |
| Enemy | #EA6963 | 战斗扩展：敌方标识、危险、瞄准目标 |
| Positive / Health | #7DC59E | 战斗扩展：治疗、增益、双方共用血量色 |
| DamagePreview | #EA6963 | 预计损血段 |
| SkillRange | #E7A533 | 技能范围，复用 Accent |

扩展色并非 AVG 提取结果。上述常量为不透明 Color32，alpha 在组件端独立设置；不改动原 AVG 素材与场景现有颜色。代码入口为 `Assets/Scripts/Battle/Tool/GameConst.cs` 的 `UIColors`。

## 切分与使用

每个用途已切成单独 PNG，避免运行时依赖整张图集坐标。Unity Sprite Mode=Single、PPU=100、Full Rect、Clamp、无 mipmap、无压缩、alphaIsTransparency=true。可按项目实际打包策略加入 SpriteAtlas。

| 素材 | Image.Type / Border（左、下、右、上） | 组合方式 |
|---|---|---|
| Panel / Header / Button / Portrait Fill、Outline | Sliced / 20,20,20,20 | 底板 Background/Surface + 描边 Border；选中描边 Accent |
| BuffSlot Fill、Outline | Sliced / 20,20,20,20 | 建议 48×48 及以上，固定切角 |
| Status Fill、Outline | Sliced / 12,12,12,12 | 头顶条，建议 172×78 |
| BarOutline | Sliced / 4,4,4,4 | 血条底框，不跟随血量缩放 |
| BarFill | Filled / Horizontal / Origin Left | fillAmount=当前/最大血量；底轨另放一层 Surface，不按九宫格拉伸填充 |
| APFilled、APOutline | Simple | 菱形实心=可用，空心=已消耗；数量读取实际规则 |
| Icon 系列、Selector、Chevron | Simple / 保持比例 | 图标与文字分别排版；64px 原图建议显示 24–48px |
| Divider、AimDash | Tiled | 32px tile，保持线宽；AimDash 沿瞄准方向旋转/平铺 |
| RangeCell 系列 | Simple | Fill 低透明度 + Outline + 可选 Hatch，不使用 Sliced |
| RangeCircle / Cone / Line、TargetReticle、SelectedRing | Simple | 保持比例、按投影/方向调整；圆锥原图顶点在中心，朝上 90°，从中心定位旋转 |

四向 Border 总和为最小显示尺寸；避免缩小到 40×40 以下的 20px Border 素材。小头像用保持比例的 Simple 显示。Image 的 Type 由组件设置，PNG meta 仅存 Sprite 边距，manifest 提供建议类型。

范围素材用于地面 quad/棋盘网格的透明材质，或由世界坐标投影驱动的 UI；不能直接把屏幕正方形当作斜视棋盘范围。RangeCellHatch 是危险/不可用的形状辅助，敌我不能仅靠颜色区分。

## 布局与状态

参考 Canvas 1920×1080，CanvasScaler Scale With Screen Size，Match=0.5，四边安全区 32px。低分辨率以锚点和布局组重排，不能整体裁切。

- 顶部速度条：顶部居中，900×124，复用原速度行动条的 100←0 方向。正式接入时仍由现有 SpeedTimelineUI 驱动。
- Info：已应用到 `Assets/Prefab/Battle/BattleScene.prefab`，PR2BATTLE 等引用该预制体的场景继承布局。左下 24px，底板 500×196；PC01C1 透明立绘置于 184×270 的 RectMask2D 内，头肩高出 184×186 装饰框，腰部裁切，名字在下方 184×36 名牌。右侧仅保留行动力、生命、能量、弹药；不显示攻击、防御或速度。运行时仍按所选棋子读取对应 PC 立绘，不锁死为 PC01。
- Action Panel：棋子身旁的纵向列表，位置由棋子世界坐标投影后偏移；一级行建议 206×42，行距 48。移动、近战攻击、远程攻击、技能、道具、警戒指令、待机按实际可用动作显示，装填和环境交互按条件加入。待机显示“全部 AP”。不使用固定右下操作栏。
- 二级菜单：选择“技能”后在一级列表旁展开技能列表（建议行宽 300、高 42），道具、警戒也沿用二级列表。靠右边缘时两级菜单整体翻到棋子左侧，底部不足时向上偏移；保持技能行与二级首行的关联。使用新加的 MenuRowFill / MenuRowOutline，Border 为 20px，角部只有 8px 切角，适合紧凑列表。
- 技能详情：左上固定锚点/pivot=(0,1)，距左 32px、顶部 176px，456×286；避开速度条，展示技能名、消耗、射程/范围、效果。悬停技能显示、移出隐藏；点击技能后关闭两级菜单与悬停详情并进入目标选择。示意图中技能名称和数值为演示。
- 目标选择阶段：只保留地面范围、瞄准反馈及必要确认提示，不把菜单展开与施法瞄准当成同一个交互步骤；图中地面范围为样式预览。
- 头顶状态：敌我共用 Status 底板、HP 条、AP 菱形、BuffSlot；阵营用侧标/名称+青或红，血条统一 Health。buff 图标旁显示层数、剩余回合；多于四个折叠为 +N。不可见/离场棋子隐藏；重叠时纵向避让。
- Range：移动 Ally/16% alpha，技能 SkillRange/20%，危险 Enemy/18% 并加 Hatch；轮廓 75–100%。瞄准使用 TargetReticle + 分段 AimDash，预计伤害显示在血条上，禁止区域保持红色斜纹及禁用提示。轮廓与符号也区分状态，避免仅依靠颜色。

素材与色板已交付；左下信息面板现已修改战斗场景共用 prefab，保留原有运行时数值引用；其余区域仍为设计示意。

## 依据与验证限制

本次修订依据用户战斗截图，以及 PieceActionListPanel.Update / ShowPanel / OnActionButtonClicked / OpenSkillListPanel 与 ClickManager.ClickPieceByIndex。代码确认技能条目悬停调用 ShowTooltip、退出调用 HideTooltip，点击后调用 StartSkillAttack 并关闭菜单。技能详情左上位置遵循用户明确要求。

尝试运行时验证：Unity CLI 不支持当前未装 Pipeline 的 Unity 2021 项目；编辑器自动化截图和按键均超时，临时运行探针未返回结果。因此未获得实际游戏内调用接口与截图的验证证据。临时入口已清理，探针及编译结果只留在被 Git 忽略的 tmp/BattleUIDesign。未保存或切换当前有未保存改动的场景。

旧固定底栏概念图已撤出交付目录，以新布局图为设计基准。

左下信息面板已通过 Unity 2021.3 隔离预览场景加载及渲染检查（PC01C1、RectMask2D、行动力和资源文本）。实际预制体已用于 PR2BATTLE 场景；当前编辑器 GameView 捕获仍超时，因此本轮画面为独立 Unity 预览，未声称完成原战斗场景运行验证。验证脚本、预览场景和截图均在 tmp/BattleUIDesign，未加入 Git。

信息面板排版：标签左边缘统一，当前值右对齐，最大值另列左对齐；每行文本框高 30px，垂直居中。生命条高度 12px（原 5px），能量和弹药条高度 8px，三个进度条保持相同起点和 238px 宽度。

本轮采用 GitHub Unity-Technologies/skills 的 ui-ugui skill（https://github.com/Unity-Technologies/skills/blob/main/skills/ui-ugui/SKILL.md），本地已有同款，未重复安装。右侧底板降低明度；行动力改为青色实心/铜金空心菱形，容量取角色 MaxMovePoint，每个点代表 1 AP。SetPlayer 和 UpdateDisplay 均同步点阵。示例截图为 4/6 AP；运行时不写死点数。

SceneBackdrop 已添加到 PieceInfo 最底层：520×336，位置 (-10,-10)，#0B1015 / alpha 0.68，九宫格底图，Raycast Target=false，覆盖头像背后与信息区。后续优化执行提示词见 BattleUIOptimizationPrompt.md。

## 2026-10-24 单次优化执行状态

本批已在现有脚本和共用 BattleScene prefab 接入一级行动列表、技能/道具/警戒二级列表及左上详情的布局改进。菜单改为 Canvas/安全区坐标定位、整组翻转、深色背板及滚动列表；按钮复用现有业务绑定，增加禁用状态与分类/提示清理；详情限制最大高度并提供正文滚动。菜单按 230px 宽、44px 行高、46px 步进装配，保留 legacy Text。生成器已同步菜单尺寸与背板，未覆盖已有素材和设计 PNG。

左下信息面板及 SceneBackdrop 完整保留。静态检查证明本批 prefab 只增加四个素材引用，PR2BATTLE 仍经原共用 prefab 和脚本入口接入。Unity 引用离线编译通过；由于 CLI 无 Pipeline 实例、桌面未发现 Unity 编辑器窗口，**本批没有实际战斗截图或 Play 验收结果**。头顶、范围、P1/P2 尚未推进，旧设计图也不代表这些区域已实现。详见 BattleUIAcceptance.md 的逐项状态、证据及剩余问题。

## Action Panel 整体重构（2026-10-24）

按 ActionPanelOptimizationPrompt.md 已实施统一按钮预制体 `Assets/Prefab/Battle/ActionButton.prefab`、独立按钮状态、两级分类与攻击分组、等间隔缓动展开、子菜单相对父按钮居中、AP/MP/弹药悬停消耗预览，以及 ActionType 英文标识/Odin 中文标签/运行时中英显示。旧按钮层级保留为停用的序列化历史，新菜单从完整预制体建立对象池，不再按枚举顺序绑定旧数组。

预览接入当前角色实际跟随状态 `Assets/Prefab/UI/PieceStatePanel.prefab`，并同步左下 PieceInfo；AP 使用当前可用点右侧呼吸，能量和弹药使用独立消耗区间叠层。分类箭头占用消耗列，分类本身不预览/不扣费；展开锁定保留选中视觉。ActionType 原整数值保持，历史中文 JSON 值与字典键保留读取兼容。

Unity 引用编译、133 条枚举/JSON/语言/计时回归与新增 prefab 引用检查通过。编辑器窗口已能发现，但截图与恢复激活均超时，故本轮真实 Play、分辨率及视觉验收仍未通过；详见 BattleUIAcceptance.md 最新章节。已有设计 PNG 是旧设计示意，尚未重生成，不能作为本次运行证据。

设计生成源也已同步攻击分类、独立三角箭头/费用互斥和子菜单居中示例；现有 PNG 尚未重生成。

最新交互调整：两级菜单取消整块背景板；不可用技能/道具保留悬停详情，但无高亮/缩放/费用呼吸。可用按钮悬停从左侧中心缓动放大至 1.06 倍，行距至少 6px，并预留裁剪余量；已展开父分类维持选中且不放大。相关执行提示词和生成源已同步，Play 视觉回归仍待完成。

审查修复：移出按钮立即停止费用预览，详情保留1.25秒转移宽限，鼠标进入详情区后可持续滚动阅读；材料只读查询去除日志与临时分配，保持重复材料合计与溢出校验；分类箭头与 AP 共用右侧消耗列。编译、资源检查及10009项临时断言通过，真实 Play 验收仍待完成，详见 BattleUIAcceptance.md。

新增行动反馈：费用预览移交给 UIManager.ActionFlow，在范围选择中持续到执行或取消；真实消耗的 AP 点以 DOTween 同时放大2倍并淡出1秒。行动表现结束后重新聚焦当前行动者，仍可操作时重建并展开目录；移动不再直接激活旧菜单。29项临时流程/动画参数断言与编译通过，真实 Play 的动画、镜头和输入回归仍待执行。

截图问题修复：顶部资源组按角色立绘头部定位并避让行动序列/安全区；详情停用旧背景与角标，仅保留当前单套边界并按正文收紧高度；攻击扇形直边缩窄、整体降低不透明度，射程和判定保持。编译和引用检查通过，修改后的真实画面仍待复核，详见验收记录。

左下外框清晰度（2026-10-25）：PieceInfo 的面板/头像外框改用 BattlePanelFrame 按实际尺寸绘制固定线宽与切角，带屏幕像素羽化；原造型、颜色、布局及底板保留。其他 UI 继续使用原九宫格素材。编译及两组件定点差异检查通过，运行清晰度待截图复核。

速度条头像（2026-10-25）：现有战斗加载的 SpeedTimeline prefab 增加60×60蒙版，上半身裁切补偿 Sprite pivot，专用头像支持完整构图；PieceData 可配置独立缩放、位移、有效区域和上半身占比。扩大双方轨道间距、增加进度两端与同阵营头像避让，高亮使用无填充的清晰描边，缺图显示问号。编译、5376项临时断言及预制体引用/几何检查通过。启动 Unity 时许可证客户端连接超时，以199退出，尚未取得实际战斗截图；详细配置规则与未完成验收项见 BattleUIAcceptance.md。

速度条B03修复（2026-10-29）：当前行动者在本阵营轨道优先，其余满条头像按调度队列的到达时间和注册次序显示；未满条仍按进度展示。新头像直接进入安全位置，之后的缓动、增员、交棒和窗口缩放每帧重做间距约束；超出轨道容量时仍压缩间距。开启“覆盖默认构图”且缩放1/位移0/上半身占比0.42时保持自动取景；“有效区域”默认全图值表示自动网格区域，旧非全图自定义值仍有效，明确需要全部留白时开启“使用整图区域（含留白）”。定向规则/布局断言、编译与资源静态检查通过，真实画面仍待验。当天已成功加载Unity2021并连接许可证，但没有可定位窗口；历史许可失败不代表当前环境状态。
