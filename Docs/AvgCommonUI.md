# AVG 风格通用 UI 与速度条

## 交付位置

- `Assets/Art/UI/AvgCommon`：8 张独立 PNG，保留透明背景。复制自现有 AVG UI 素材，不改动原图及其导入配置。
- `Assets/Prefab/UI/AvgCommon`：Panel、Header、Button、Separator、Track、Portrait 六个通用 uGUI 组件。
- `Assets/Resources/UI/SpeedTimeline.prefab`：可直接编辑的完整速度条。
- `Artifacts/UI/SpeedTimelinePreview.png`：Unity 实际渲染的组件预览。

## 素材与九宫格

| 素材 | AVG 来源 | Border：左/下/右/上 |
|---|---|---|
| PanelCut | 返回开始菜单/切角面板 | 28/28/28/28 |
| HeaderCut | 返回开始菜单/标题栏 | 26/26/26/26 |
| ButtonNormal | 对话框/选项 | 40/12/28/12 |
| ButtonSelected | 对话框/选项选中 | 40/12/28/12 |
| Grid | 返回开始菜单/细网格 | 0，普通图像 |
| Accent | 返回开始菜单/标题强调 | 0，普通图像 |
| Close | 返回开始菜单/关闭/48×48 | 0，图标 |
| Scrollbar | 对话框/滚动条 | 0，普通图像 |

四种面板/按钮素材通过 Unity TextureImporter 设置九宫格；对应 Image 使用 Sliced。按 RectTransform 尺寸调整组件，避免直接缩放整个对象。按钮包含正常、高亮、按下和选中状态，业务点击事件留给使用者绑定。

## 速度条接入

`UIManager.speedTimelinePrefab` 可显式指定预制体；为空时自动加载 `Resources/UI/SpeedTimeline`。战斗开始后实例化到现有根 Canvas 顶部中央，不新增 Canvas 或 EventSystem。

`SpeedTimelineUI` 的 Track、Marker Template、Lane Offset 可在 Inspector 中调整。角色头像从右向左移动；青色/红色底线标识敌我，金色外框突出当前行动者。所有速度条图像和文字均关闭 Raycast Target。

PortraitTemplate 默认隐藏，只作为实例模板，不会在实际战斗中多显示一个头像。预览图中的角色位置与头像是展示样例；实际战斗读取当前棋子、速度进度和角色头像配置。

## 重建与验证

重建与校验脚本已迁入本地临时目录 `tmp/Assets/Scripts/Editor/SpeedTimelinePrefabBuilder.cs`，不再随仓库提交，也不会默认加载到 Unity。需要重建时按临时测试方式加载并执行，完成后清理入口；测试报告存放在 `tmp/Results/`。

重建会覆盖专用输出目录中的 PNG 和预制体，并校验引用、切片、射线遮挡及组件预览；手动调整后的预制体请不要直接重建覆盖。

已完成 Unity 2021 批处理编译、导入、预制体引用与切片校验和离屏视觉检查；未在实际战斗 Play Mode 中完整运行。
