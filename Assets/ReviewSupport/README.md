# 可删除的 Steam 审核辅助模块

运行代码只在 `UNITY_EDITOR || (DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN)` 下存在。编辑器进入 Play Mode 和 Windows Development Player 均自动启用补档、快捷键及提示层，编辑器非运行状态不启动。普通 Player 不注册启动器、不安装存档、不监听快捷键、不显示提示。构建前校验仅对 Windows Development Build 执行。

启用方法：Windows Build Settings 勾选 **Development Build**，保持 `StartScene` 与 `AVG` 勾选，将构建上传到 Steam。无需挂载组件或修改任何现有场景。当前项目使用旧 Input Manager，快捷键沿用 `UnityEngine.Input`。

编辑器调试：从 StartScene 进入 Play Mode，点击 Game 视图使其获得键盘焦点，然后使用相同的 Ctrl+Alt+1–9 和 Ctrl+Alt+0 快捷键，无需勾选 Development Build。补档使用编辑器的 `Application.persistentDataPath`，可能与本机 Player 共用目录；自动补档仍仅补缺，Ctrl+Alt+9 仍先备份后恢复。

启动时从 `StreamingAssets/ReviewSaves/review.json` 读取清单，在首个场景 Awake 前把模板补到 `Application.persistentDataPath/Datas/PlayerProfiles_{slot}.json`，仅补缺失槽位，不依赖 `.streamingassets_migrated` 标记。现有同名槽位保留；快捷键始终读取随包原始模板。若槽位已被占用，可用 Ctrl+Alt+9 显式恢复，原文件逐一备份到 `Datas/ReviewBackups/时间-GUID/`。设置文件和其他槽位不变。恢复后普通存档界面刷新，当前正在游玩的档案不替换。

Ctrl+Alt+2/3/4：第二、三、四章开头。Ctrl+Alt+5/6/7：死亡结局、受伤流血结尾、遭袭受伤。Ctrl+Alt+8：包含自杀文字的 EXC1-3 开头。Ctrl+Alt+1：隐藏/显示英文提示。Ctrl+Alt+0：打开当前用户存档目录。所有跳转替换当前未保存进度，并经由 GM 正常转场；磁盘存档不被跳转覆盖。转场期间忽略跳转。

清单是唯一快捷键和槽位映射来源，修改模板时同时维护 `script/map/chapter`，保持 `finishNodeDic`、`currentScene=AVG`、`currentScriptFile`、`resumeStoryScript`、`isStoryInProgress=true`、`isNewGame=false` 一致。模板存放于 ReviewSaves，不能放回 Datas：现有 DataManager 会在正式版迁移 Datas 下的存档。

全部移除：删除 `Assets/ReviewSupport` 和 `Assets/StreamingAssets/ReviewSaves`（连同对应 .meta），不需要修改核心代码。仅取消 Development Build 会去掉运行功能，但 StreamingAssets 中的模板和说明仍会随包，正式发行若不想携带它们，应删除/从发行源目录排除 ReviewSaves。已安装到用户目录的存档不会自动删除。

验证：`tmp/Tests/ReviewSupport/Run.ps1` 检查实际安装逻辑的临时目录行为；`tmp/Tests/ReviewSupport/validate_assets.py` 检查七个入口、前置节点和模板对应的场景/剧本。另需在 Windows Development Player 实测键盘、提示层、转场和剧情首句。独立编译和文件检查不能替代 Player 测试。
