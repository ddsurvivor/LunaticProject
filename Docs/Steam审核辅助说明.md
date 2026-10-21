# 本次 Steam 审核反馈与交付

截图对应 BuildID 25571146。审核未通过的直接原因是内容问卷列出的项目无法核验；审核员只看到两个“前传”读档，没有找到反馈所称的“大厅”。截图没有认定这些内容被禁止，也不能据此认定修复工具必然通过审核。

需核验的问卷分类是自杀描写，以及非露骨性内容（反馈列出的卖淫、夸张色情/过度情色关注等）。它们与一般死亡、战斗受伤、吐血不是同一分类。

已确认的文本依据：EXC1-3 第一工作表第 15 行明确写出“吞枪自尽”；EXC4-1 第 46 行提及密涅瓦自杀。新增 Ctrl+Alt+8 从 EXC1-3 开头载入，按正常对话推进即可到达。关键词扫描未定位明确性内容，但扫描不覆盖 CG 画面和全部隐喻；需人工查看 EXC 文本和 CG，确认问卷是否误选。若确实没有该内容，按实际情况修正问卷并在工单解释，不应将血腥片段充当性内容证据。

## 已实现

独立代码：`Assets/ReviewSupport`。独立模板：`Assets/StreamingAssets/ReviewSaves`。没有修改 GM、DataManager、剧情恢复逻辑或场景挂载。

Windows Development Player 和 Editor Play Mode 自动启动，普通 Player 不启动。编辑器中从 StartScene 进入 Play Mode 后点击 Game 视图获取键盘焦点，即可使用相同快捷键，无需勾选 Development Build。自动把七个模板补到 `persistentDataPath/Datas`，已有同名档不覆盖。编辑器与本机 Player 可能共用此存档目录。Ctrl+Alt+9 可备份并恢复审核槽位，适用于审核员曾运行旧包或槽位已有内容的情况。旧迁移标记不会阻止本模块安装。

| 按键 | 普通读档槽 | 剧本 | 地图 | 用途 |
| --- | --- | --- | --- | --- |
| Ctrl+Alt+2 | 03 | EXC2-1 | StealthPlatform | 第二章开头 |
| Ctrl+Alt+3 | 04 | EXC3-1 | Settlement | 第三章开头 |
| Ctrl+Alt+4 | 05 | EXC4-1 | Confrontation | 第四章开头 |
| Ctrl+Alt+5 | 06 | EXC1-3-B | YamaClass | 用户指出的死亡结局 |
| Ctrl+Alt+6 | 07 | EXC4-F | Confrontation | 用户指出的受伤吐血结尾 |
| Ctrl+Alt+7 | 08 | EXC2-8-B | StealthPlatform | 用户指出的遭袭受伤情节 |
| Ctrl+Alt+8 | 09 | EXC1-3 | YamaClass | 已定位的自杀文字 |

Ctrl+Alt+1 隐藏/显示英文提示层；Ctrl+Alt+0 打开本机实际存档目录。提示层不接收鼠标事件。跳转从原始模板载入，丢弃当前未保存进度，磁盘档案不因跳转而变化。每个模板都有 finishNodeDic、地图、AVG 场景、章节标识及未完成剧情恢复字段；上一章节地图触发节点也已标记，避免显示已过期节点。

## 重提审核

1. Windows Build Settings 勾选 Development Build，确认 StartScene 和 AVG 已启用。构建前脚本会阻止缺模板、缺剧情、缺地图名、未启用 AVG 的开发包继续构建。
2. 在独立测试账户/干净存档环境启动真正的 Player，验证 03–09 槽位和 Ctrl+Alt+2–8 每个入口的剧情首句、返回地图及后续节点。再用已有旧档环境测试跳转与 Ctrl+Alt+9 备份恢复。
3. 通过 Steam 上传该构建，使用可供 Valve 审核的构建或 beta 分支。在工单写清新 BuildID、分支名、密码（如有）、具体入口、内容类别及正常推进方式。确认审核员拿到的是新包，并说明当前 demo 仅包含 EXC 章节。不要直接声称已解决“大厅”问题：先核对该名称是否来自旧说明或原提交资料，再准确描述现在提供的入口。
4. 随包有英文 `StreamingAssets/ReviewSaves/REVIEWER_GUIDE.txt`。将快捷键和自杀文字位置也写进工单，避免审核员必须自行在安装目录找说明。
5. 性内容项必须在人工确认后给出真实的定位，或修正问卷并解释。下方回复稿是待填写草稿，不是可未经核对直接提交的承诺。

Steam 官方分支文档：https://partner.steamgames.com/doc/store/application/branches
Steam 官方审核流程：https://partner.steamgames.com/doc/store/review_process
截图明确要求存档/调试包经 Steam 提供，不能使用 Google Drive、Dropbox 等外链替代。

## 英文工单回复草稿

> We have prepared a Windows development build with bundled review saves and direct story shortcuts. BuildID: [NEW BUILD ID]. Branch: [BRANCH NAME]. Password, if required: [PASSWORD].
>
> Please launch the game through Steam and wait for the title screen. No manual save installation is required. Ctrl+Alt+2/3/4 loads the openings of Chapters 2/3/4. Ctrl+Alt+8 loads EXC1-3; advance the dialogue to the passage describing a character taking her own life with a firearm (source spreadsheet row 15). Ctrl+Alt+5/6/7 provide additional death/injury scenes for violence/blood inspection.
>
> Ctrl+Alt+1 hides/shows the English guide. If old save slots are present, the shortcuts still load the pristine bundled templates. Ctrl+Alt+9 restores the seven review slots with backups. This demo contains the EXC story chapters; [CLARIFY THE EARLIER HALL/LOBBY INSTRUCTION AFTER CHECKING YOUR ORIGINAL SUBMISSION].
>
> Regarding the non-explicit sexual content item: [AFTER MANUAL REVIEW, INSERT THE EXACT SCENE AND ACCESS INSTRUCTIONS, OR EXPLAIN THAT THE SURVEY WAS CORRECTED TO MATCH THE ACTUAL CONTENT].

## 移除和验证边界

删除 `Assets/ReviewSupport` 与 `Assets/StreamingAssets/ReviewSaves` 及各自 .meta 即移除全部游戏内功能，无核心脚本依赖。测试工具位于 `tmp/Tests/ReviewSupport`，也可以删除。仅取消 Development Build 会排除运行代码，但模板仍属于 StreamingAssets，发行源目录中应按需要排除它们。用户目录内已经安装的文件不会自动删除。

自动验证检查临时目录内首次安装、旧档保护、重复启动、显式恢复备份、设置保留、坏模板拒绝及路径限制；同时编译开发/发行条件和 Editor 构建校验代码，并静态核对七个模板的地图与前置节点。尚未代替真实 Player 实测，也未上传 Steam 或重提工单。
