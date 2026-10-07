# AVG 存档恢复测试

在项目根目录运行 `Tests/AvgSaveRecovery/Run.ps1`，验证实际 PLAYERPROFILE、剧情存档状态和 JsonTool 的序列化往返，包括分支文件切换、完成后清除恢复标记、旧存档、新游戏重置以及战斗日志不覆盖 AVG 剧情。

Unity 编辑器中退出 Play 模式并等待脚本导入，然后执行 `Tests > AVG > Validate Save Recovery`。此测试自动进入 Play，使用真实 AVG 场景的 EXC1-3 节点，验证阅读中存档重新加载后从头开启、转场完成前不播放、临时隐藏阅读器保留状态，以及 END 完成后重载地图、隐藏已完成节点、解锁后续节点且不重播。测试结束自动退出 Play；测试 JSON 和结果位于项目 `Temp/AvgSaveRecoveryValidation`，不使用玩家存档槽。
