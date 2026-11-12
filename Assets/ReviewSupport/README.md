# 可删除的 Steam 审核辅助模块

运行代码只在 `UNITY_EDITOR` 下存在。编辑器进入 Play Mode 时自动启用补档、快捷键及提示层，编辑器非运行状态不启动。所有 Player（包括 Development Build）均不携带调试存档、不注册审核启动器、不安装审核档、不监听审核快捷键、不显示审核提示。

启用方法：在编辑器从 StartScene 进入 Play Mode。无需挂载组件或修改任何现有场景。当前项目使用旧 Input Manager，快捷键沿用 `UnityEngine.Input`。Development Build 不再启用审核辅助。

编辑器调试：从 StartScene 进入 Play Mode，点击 Game 视图使其获得键盘焦点，然后使用相同的 Ctrl+Alt+1–9 和 Ctrl+Alt+0 快捷键，无需勾选 Development Build。补档使用编辑器的 `Application.persistentDataPath`，可能与本机 Player 共用目录；自动补档仍仅补缺，Ctrl+Alt+9 仍先备份后恢复。

编辑器从 `Assets/ReviewSupport/Editor/DevelopmentStreamingAssets/ReviewSaves` 读取清单。在首个场景 Awake 前把模板补到 `Application.persistentDataPath/Datas/PlayerProfiles_{slot}.json`，仅补缺失槽位，不依赖 `.streamingassets_migrated` 标记。现有同名槽位保留；快捷键始终读取原始模板。若槽位已被占用，可用 Ctrl+Alt+9 显式恢复，原文件逐一备份到 `Datas/ReviewBackups/时间-GUID/`。设置文件和其他槽位不变。恢复后普通存档界面刷新，当前正在游玩的档案不替换。

Ctrl+Alt+2/3/4：第二、三、四章开头。Ctrl+Alt+5/6/7：死亡结局、受伤流血结尾、遭袭受伤。Ctrl+Alt+8：包含自杀文字的 EXC1-3 开头。Ctrl+Alt+1：隐藏/显示英文提示。Ctrl+Alt+0：打开当前用户存档目录。所有跳转替换当前未保存进度，并经由 GM 正常转场；磁盘存档不被跳转覆盖。转场期间忽略跳转。

清单是唯一快捷键和槽位映射来源，修改模板时同时维护 `script/map/chapter`，保持 `finishNodeDic`、`currentScene=AVG`、`currentScriptFile`、`resumeStoryScript`、`isStoryInProgress=true`、`isNewGame=false` 一致。预置存档统一存放于 `Editor/DevelopmentStreamingAssets`：审核模板位于 `ReviewSaves`，旧槽位位于 `Datas`。构建回调不再向 StreamingAssets 添加调试存档；如果源 StreamingAssets 中出现 ReviewSaves 目录或 PlayerProfiles_*.json，则中止构建并提示移走。所有 Player 均不从 StreamingAssets 导入预置槽位，设置文件仍沿用原有迁移流程。

全部移除：删除 `Assets/ReviewSupport`（连同对应 .meta），不需要修改核心代码。无论是否勾选 Development Build，构建都不携带预置存档和审核说明。已安装到用户目录的存档不会自动删除；编辑器与本机 Player 可能共用该目录，因此旧调试档仍可能显示在读档界面。验证新包时使用全新的构建输出目录，避免旧包残留文件干扰。

验证：`tmp/Tests/ReviewSupport/Run.ps1` 检查实际安装逻辑的临时目录行为；`tmp/Tests/ReviewSupport/validate_assets.py` 检查七个入口、前置节点和模板对应的场景/剧本。另需在编辑器 Play Mode 实测键盘、提示层、转场和剧情首句，并在全新输出目录确认 Development 与普通 Player 的 StreamingAssets 不含调试档。独立编译和文件检查不能替代 Player 测试。
