using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public partial class GM
{
    private sealed class BattleSession
    {
        internal string ProfileJson, Scene, EndLog;
        internal PLAYERPROFILE Original; // 战前引用不参与本场消费和拾取，离场前不可被战斗改写。
        internal PLAYERPROFILE Owner; // 仅操作本会话拥有的存档，外部测试或换档后不覆盖新对象。
        internal int Setting; // 首次入场的战斗预设。
        internal int Seed; // 当前尝试的规则种子，重掷配置关闭时保持。
        internal int Attempts = 1; // 同一战前快照下的尝试次数。
        internal bool Transient; // 隔离存档的禁写标记必须跨恢复保留。
        internal bool FinishedGame; // 本次尝试尚未提交的全局通关标记。
        internal UnityEngine.Random.State OutsideRandom; // 离开会话时恢复战斗之外的随机序列。
        internal UnityEngine.Random.State InitialWorldRandom; // 对象初始化前的序列，重开时原样恢复。
        internal readonly HashSet<string> Achievements = new(); // 本次尝试尚未对平台提交的成就。
    }

    private BattleSession battleSession; // 随 GM 常驻，跨重载保留最初战前快照。
    /// <summary>战斗会话包含部署、结算和重载空档；整个期间禁止写入存档。</summary>
    public bool HasBattleSession => battleSession != null;
    public int BattleSeed => battleSession?.Seed ?? 0;
    public int BattleAttempt => battleSession?.Attempts ?? 0;
    public bool IsRestartedBattle => BattleAttempt > 1;
    public int PendingBattleAchievements => battleSession?.Achievements.Count ?? 0;

    /// <summary>棋子初始化前捕获战前数据；重开后的新管理器沿用原快照与种子。</summary>
    internal void BeginBattleSession(BattleManager manager)
    {
        string scene = manager.gameObject.scene.name;
        if (battleSession != null && (battleSession.Scene != scene || !ReferenceEquals(battleSession.Owner, PLAYERPROFILE)))
            battleSession = null;
        if (battleSession == null)
        {
            var outside = UnityEngine.Random.state;
            var original = PLAYERPROFILE;
            string snapshot = JsonConvert.SerializeObject(original);
            bool transient = DataManager.IsTransientProfile(original);
            // 自动战斗宿主已安装禁写副本，保持其引用归属；正式存档则使用独立的战斗工作副本。
            var working = transient ? original : JsonConvert.DeserializeObject<PLAYERPROFILE>(snapshot);
            PLAYERPROFILE = working;
            battleSession = new BattleSession { ProfileJson = snapshot, Original = original,
                Owner = working, Scene = scene, EndLog = endLog, Setting = battleSetting,
                Seed = UnityEngine.Random.Range(0, int.MaxValue), Transient = transient,
                OutsideRandom = outside };
            // 自动战斗宿主已设置种子，保留其完整状态；正式会话使用可重开的独立种子。
            if (transient) UnityEngine.Random.state = outside;
            else UnityEngine.Random.InitState(battleSession.Seed);
            battleSession.InitialWorldRandom = UnityEngine.Random.state;
        }
        else UnityEngine.Random.state = battleSession.InitialWorldRandom;
    }

    /// <summary>部署与开场结束后设置本次战斗的起始随机状态；自动战斗宿主仍自行管理其种子。</summary>
    internal void BeginBattleRules(BattleManager manager)
    {
        if (battleSession != null && ReferenceEquals(battleSession.Owner, PLAYERPROFILE) && !manager.HasExternalPlayerControl)
            UnityEngine.Random.InitState(battleSession.Seed);
    }

    /// <summary>恢复最初战前存档并重载到部署；连点只接受一次，不提交奖励、剧情、成就或写档。</summary>
    public bool RestartBattle()
    {
        var scene = BattleScene.Ins;
        var session = battleSession;
        if (scene == null || session == null || !ReferenceEquals(session.Owner, PLAYERPROFILE) ||
            !CanLoadScene(session.Scene)) return false;
        var restored = RestoreProfile(session);
        // 先占用转场权，再触发窗口和协程清理，防止回调重入消耗第二次尝试。
        IsTransitioning = true;
        scene.UM.PrepareBattleTransition();
        QuiesceBattleScene(scene);
        PLAYERPROFILE = session.Owner = restored;
        battleScene = session.Scene; endLog = session.EndLog; battleSetting = session.Setting;
        session.Achievements.Clear();
        session.FinishedGame = false;
        session.Attempts++;
        if (DM.gameConstSO.RerollBattleSeedOnRestart)
        {
            int previous = session.Seed;
            do { session.Seed = Guid.NewGuid().GetHashCode(); } while (session.Seed == previous);
            UnityEngine.Random.InitState(session.Seed);
            session.InitialWorldRandom = UnityEngine.Random.state;
        }
        StartCoroutine(TransitionRoutine(session.Scene, true, ""));
        return true;
    }

    private static PLAYERPROFILE RestoreProfile(BattleSession session)
    {
        var profile = JsonConvert.DeserializeObject<PLAYERPROFILE>(session.ProfileJson);
        if (session.Transient || DataManager.IsTransientProfile(session.Owner)) DataManager.MarkTransientProfile(profile);
        return profile;
    }

    /// <summary>战斗中的平台成就只登记本次尝试；重开或放弃会丢弃，不干扰此前的平台队列。</summary>
    public bool DeferBattleAchievement(string name)
    {
        if (battleSession == null) return false;
        if (ReferenceEquals(battleSession.Owner, PLAYERPROFILE)) battleSession.Achievements.Add(name.Trim());
        return true;
    }

    /// <summary>战斗中暂存通关标记；重开、部署退出和隔离运行不会写入全局进度文件。</summary>
    public bool DeferBattleGameFinished()
    {
        if (battleSession == null) return false;
        if (ReferenceEquals(battleSession.Owner, PLAYERPROFILE)) battleSession.FinishedGame = true;
        return true;
    }

    private static void QuiesceBattleScene(BattleScene scene)
    {
        // 重载有淡出空档，先停用旧场景全部对象，阻止弹道、引信及单位回调污染恢复后的存档。
        foreach (var root in scene.gameObject.scene.GetRootGameObjects()) root.SetActive(false);
    }

    private void FinishBattleSession(bool commit)
    {
        var session = battleSession;
        if (session == null) return;
        if (!ReferenceEquals(session.Owner, PLAYERPROFILE)) { battleSession = null; return; }
        var scene = BattleScene.Ins;
        if (scene != null && scene.gameObject.scene.name == session.Scene)
        {
            scene.UM.PrepareBattleTransition();
            QuiesceBattleScene(scene);
        }
        battleSession = null;
        UnityEngine.Random.state = session.OutsideRandom;
        if (!commit) { PLAYERPROFILE = RestoreProfile(session); return; }
        if (!DataManager.IsTransientProfile(PLAYERPROFILE))
        {
            foreach (var achievement in session.Achievements) SteamAchievements.Unlock(achievement);
            if (session.FinishedGame) LocalGameProgress.MarkGameFinished();
        }
    }

    private void OnBattleSessionSceneLoaded(UnityEngine.SceneManagement.Scene scene,
        UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (battleSession != null && battleSession.Scene != scene.name) FinishBattleSession(false);
    }

    private void OnDestroy() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnBattleSessionSceneLoaded;
}
