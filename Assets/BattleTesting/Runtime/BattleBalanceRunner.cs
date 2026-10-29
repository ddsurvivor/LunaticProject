using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lunatic.BattleTesting
{
    /// <summary>独立玩家 AI 测试宿主，可在 Play Mode、开发构建或命令行运行，不依赖外部 AI 服务。</summary>
    [DisallowMultipleComponent]
    public sealed class BattleBalanceRunner : MonoBehaviour
    {
        [LabelText("进入运行状态后自动开始")] public bool autoStart;
        [LabelText("连续测试局数"), Min(1)] public int repeatCount = 1;
        [LabelText("首局随机种子")] public int baseSeed = 1000;
        [LabelText("每局最大真实秒数"), Min(10f)] public float maxSeconds = 600f;
        [LabelText("最大无进展秒数"), Min(10f)] public float stallSeconds = 90f;
        [LabelText("开战等待秒数"), Min(10f)] public float startupTimeout = 90f;
        [LabelText("最大行动命令数"), Min(1)] public int maxCommands = 5000;
        [LabelText("最大标准回合数"), Min(1)] public int maxRounds = 300;
        [LabelText("决策间隔（游戏秒）"), Min(0.02f)] public float decisionInterval = 0.15f;
        [LabelText("报告根目录（留空使用项目tmp）")] public string reportDirectory;
        [LabelText("批处理完成后退出进程")] public bool quitWhenFinished;
        [LabelText("玩家 AI 策略")] public PlayerAutoBattleSettings playerAI = new();
        [ShowInInspector, ReadOnly] public bool IsRunning { get; private set; }
        [ShowInInspector, ReadOnly] public string Status { get; private set; } = "未运行";
        [ShowInInspector, ReadOnly] public string LastReportPath { get; private set; }

        private BattleManager battle;
        private BattleManager previousBattle;
        private PlayerAutoBattlePolicy policy;
        private PlayerAutoBattleSettings policySnapshot;
        private BattleBalanceReport report;
        private BattleBalanceSummary summary;
        private string suiteId, outputDirectory, expectedScenePath;
        private int runIndex, victories, completedRuns;
        private float waitingSeconds, lastProgressSeconds, nextDecisionAt;
        private bool reloading, stopRequested, randomOwned;
        private bool suiteFailed; // 超时、卡死、取消或基础设施失败会让批处理返回非零退出码。
        private UnityEngine.Random.State originalRandom; // 整个测试批次持有随机序列，退出时恢复。
        private GM profileOwner;
        private PLAYERPROFILE originalProfile, sandboxProfile; // 测试只消费副本中的血量、道具和掉落。
        private BattleOutcome pendingOutcome;
        private readonly Dictionary<int, PieceController> observed = new();
        private readonly Dictionary<int, BattleBalanceUnit> unitRecords = new();
        private readonly Dictionary<int, Vector3> previousMoveOrigins = new();

        private void Start() { if (autoStart) StartTest(); }

        /// <summary>接管当前或即将启动的战斗；多局测试使用同一初始玩家存档副本和递增种子重载场景。</summary>
        [Button("开始关卡自动测试"), ContextMenu("开始关卡自动测试")]
        public void StartTest()
        {
            if (!Application.isPlaying || IsRunning) return;
            if (!isActiveAndEnabled) { Status = "请先启用测试控制器"; return; }
            if (GetComponent<BattleScene>() != null || GetComponent<BattleManager>() != null || GetComponent<GM>() != null)
            { Status = "请把测试控制器放到独立 GameObject，避免场景重载时保留战斗系统"; return; }
            if (FindObjectsOfType<BattleBalanceRunner>().Any(r => r != this && r.IsRunning)) { Status = "已有测试控制器正在运行"; return; }
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            runIndex = victories = completedRuns = 0;
            summary = new BattleBalanceSummary();
            waitingSeconds = 0f;
            stopRequested = reloading = suiteFailed = false;
            LastReportPath = null;
            suiteId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            outputDirectory = Path.Combine(string.IsNullOrWhiteSpace(reportDirectory) ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tmp", "BattleBalanceReports")) : reportDirectory, suiteId);
            policySnapshot = JsonConvert.DeserializeObject<PlayerAutoBattleSettings>(JsonConvert.SerializeObject(playerAI)) ?? new PlayerAutoBattleSettings();
            policy = new PlayerAutoBattlePolicy(policySnapshot);
            report = NewReport();
            originalRandom = UnityEngine.Random.state;
            randomOwned = true;
            UnityEngine.Random.InitState(baseSeed);
            IsRunning = true;
            Status = "等待战斗初始化";
        }

        /// <summary>停止测试、写入未完成结果并归还控制权；不会回滚已经执行的场内动画或导航。</summary>
        [Button("停止并导出报告"), ContextMenu("停止并导出报告")]
        public void StopTest()
        {
            if (!IsRunning) return;
            stopRequested = true;
            try { if (report != null) CompleteRun(BattleBalanceResult.Stopped, "手动停止测试"); }
            finally { FinishSuite(); }
        }

        private void Update()
        {
            if (!IsRunning) return;
            try { Tick(); }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                suiteFailed = true;
                stopRequested = true;
                try { if (report != null) CompleteRun(BattleBalanceResult.Error, error.ToString()); }
                finally { FinishSuite(); Status = "测试错误：" + error.Message; }
            }
        }

        private void Tick()
        {
            var scene = BattleScene.Ins;
            var found = scene != null ? scene.BM : null;
            if (scene == null || scene.TM == null || !scene.TM.IsPaused) waitingSeconds += Time.unscaledDeltaTime;
            if (reloading)
            {
                if (found == null || found == previousBattle)
                {
                    if (waitingSeconds > startupTimeout) throw new TimeoutException("场景重载后未找到新的 BattleManager");
                    return;
                }
                if (found.gameObject.scene.path != expectedScenePath) throw new InvalidOperationException("测试批次被切换到其他场景");
                reloading = false;
            }
            if (!ReferenceEquals(battle, null) && (battle == null || found != battle))
            { CompleteRun(BattleBalanceResult.SceneChanged, "运行过程中离开了被测战斗场景"); return; }
            if (battle == null)
            {
                if (found == null || GM.Ins == null || GM.Ins.PLAYERPROFILE == null)
                {
                    if (waitingSeconds > startupTimeout) CompleteRun(BattleBalanceResult.StartupTimeout, "等待战斗或 GM 初始化超时");
                    return;
                }
                Attach(found);
            }
            if (found != battle) { CompleteRun(BattleBalanceResult.SceneChanged, "运行过程中离开了被测战斗场景"); return; }
            ObserveUnits();
            if (pendingOutcome != BattleOutcome.None)
            {
                CompleteRun(pendingOutcome == BattleOutcome.PlayerVictory ? BattleBalanceResult.Victory : BattleBalanceResult.Defeat, "正式战斗胜负结算");
                return;
            }
            if (scene.TM != null && scene.TM.IsPaused) return;
            if (!battle.SpeedTurnsActive)
            {
                battle.StartBattle();
                if (battle.IsDeploying) battle.ConfirmDeployment();
                battle.OnClickSkip();
                var tutorial = battle.tutorialManager != null ? battle.tutorialManager.tutorialPanel : null;
                if (tutorial != null && tutorial.gameObject.activeInHierarchy) tutorial.CompleteTutorial();
                battle.CompleteBattleIntroduction();
                if (waitingSeconds > startupTimeout) CompleteRun(BattleBalanceResult.StartupTimeout, "无法进入正式战斗；检查出生站位、教程或关卡初始化");
                return;
            }
            waitingSeconds = 0f;
            report.activeSeconds += Time.unscaledDeltaTime;
            report.rounds = battle.TunrNumber;
            if (report.activeSeconds >= maxSeconds || report.rounds >= maxRounds) { CompleteRun(BattleBalanceResult.Timeout, "达到时长或标准回合上限"); return; }
            if (report.commands >= maxCommands) { CompleteRun(BattleBalanceResult.ActionLimit, "达到行动命令上限"); return; }
            if (report.activeSeconds - lastProgressSeconds >= stallSeconds) { CompleteRun(BattleBalanceResult.Stalled, "长期未观察到棋子移动、血量变化或新单位"); return; }
            if (battle.HasCombatPresentation || battle.moveManager.IsMoving || Time.time < nextDecisionAt) return;
            if (policySnapshot.useBurst && HandleBurst()) return;
            if (battle.BurstInputPending) return;
            var actor = battle.ActivePiece;
            if (actor == null || !battle.CanControlPiece(actor) || !battle.PlayerController.pieces.Contains(actor)) return;
            int id = actor.GetInstanceID();
            Vector3 origin = actor.transform.position;
            var decision = policy.Decide(battle, actor, previousMoveOrigins.TryGetValue(id, out var previous) ? previous : (Vector3?)null);
            int ap = actor.unitAttrCenter.CurMovePoint;
            var result = battle.TryIssuePlayerCommand(this, actor, decision.command);
            report.commands++;
            if (result == BattleCommandResult.Rejected) report.rejectedCommands++;
            if (decision.command.kind == PlayerBattleCommandKind.Move && result == BattleCommandResult.Executed) previousMoveOrigins[id] = origin;
            if (report.actions.Count < 20000) report.actions.Add(new BattleBalanceAction {
                sequence = report.commands, round = report.rounds, actorId = id, actor = actor.name, apBefore = ap,
                apAfter = actor.unitAttrCenter.CurMovePoint, elapsed = report.activeSeconds,
                kind = decision.command.kind.ToString(), skill = decision.command.skill?.skillName,
                result = result.ToString(), reason = decision.reason,
                x = decision.command.position.x, y = decision.command.position.y, z = decision.command.position.z });
            // 无效命令不循环重试到耗尽上限，本次让出行动并保留拒绝状态与原始决策。
            if (result == BattleCommandResult.Rejected)
                battle.TryIssuePlayerCommand(this, actor, new PlayerBattleCommand { kind = PlayerBattleCommandKind.EndTurn });
            nextDecisionAt = Time.time + Mathf.Max(0.02f, decisionInterval);
            Status = $"第 {runIndex + 1}/{Mathf.Max(1, repeatCount)} 局，回合 {report.rounds}，命令 {report.commands}";
        }

        private void Attach(BattleManager manager)
        {
            if (originalProfile == null)
            {
                profileOwner = GM.Ins;
                originalProfile = profileOwner.PLAYERPROFILE;
                InstallSandbox();
            }
            if (!manager.TryAcquirePlayerControl(this)) throw new InvalidOperationException("战斗已结束或被其他外部控制器占用");
            battle = manager;
            battle.BattleCompleted += OnBattleCompleted;
            pendingOutcome = BattleOutcome.None;
            observed.Clear(); unitRecords.Clear(); previousMoveOrigins.Clear();
            lastProgressSeconds = nextDecisionAt = 0f;
            report = new BattleBalanceReport { suite = suiteId, scene = manager.gameObject.scene.name, scenePath = manager.gameObject.scene.path,
                run = runIndex + 1, seed = unchecked(baseSeed + runIndex), policy = policySnapshot,
                startedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
        }

        private BattleBalanceReport NewReport()
        {
            var scene = SceneManager.GetActiveScene();
            return new BattleBalanceReport { suite = suiteId, run = runIndex + 1, seed = unchecked(baseSeed + runIndex),
                scene = scene.name, scenePath = scene.path, policy = policySnapshot, startedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
        }

        private void InstallSandbox()
        {
            sandboxProfile = JsonConvert.DeserializeObject<PLAYERPROFILE>(JsonConvert.SerializeObject(originalProfile));
            if (sandboxProfile == null) throw new InvalidOperationException("无法创建测试存档副本");
            // 在替换全局引用前禁止副本落盘，覆盖释放控制权至下一场景接管之间的空档。
            DataManager.MarkTransientProfile(sandboxProfile);
            profileOwner.PLAYERPROFILE = sandboxProfile;
        }

        private void ObserveUnits()
        {
            foreach (var piece in battle.PlayerController.pieces.Concat(battle.AIController.pieces).Concat(battle.summonPieces).Distinct())
            {
                if (piece == null || piece.unitAttrCenter == null || piece.unitAttrCenter.MaxHealth <= 0) continue;
                int id = piece.GetInstanceID();
                if (observed.ContainsKey(id)) continue;
                observed[id] = piece;
                if (piece.isPlayerPiece && piece.playerData != null) piece.playerData = sandboxProfile.GetPlayer(piece.pieceID - 1);
                var p = piece.transform.position;
                var record = new BattleBalanceUnit { instanceId = id, pieceId = piece.pieceID, name = piece.name, player = piece.isPlayerPiece,
                    observedRound = battle.TunrNumber, initialHealth = piece.unitAttrCenter.CurHealth, maxHealth = piece.unitAttrCenter.MaxHealth,
                    initialX = p.x, initialY = p.y, initialZ = p.z };
                unitRecords[id] = record; report.units.Add(record);
                lastProgressSeconds = report.activeSeconds;
            }
            foreach (var entry in observed)
            {
                var piece = entry.Value;
                if (piece == null) continue;
                var record = unitRecords[entry.Key]; var p = piece.transform.position;
                if (record.finalHealth != piece.unitAttrCenter.CurHealth || Mathf.Abs(record.finalX - p.x) + Mathf.Abs(record.finalY - p.y) + Mathf.Abs(record.finalZ - p.z) > 0.1f)
                    lastProgressSeconds = report.activeSeconds;
                record.finalHealth = piece.unitAttrCenter.CurHealth;
                record.dead = piece.isDead;
                record.finalX = p.x; record.finalY = p.y; record.finalZ = p.z;
            }
        }

        private bool HandleBurst()
        {
            if (battle.IsChoosingBurst)
            {
                foreach (var piece in battle.PlayerController.pieces)
                    if (piece != null && !piece.isDead && !piece.cantControl && piece.unitAttrCenter.GetBuffStacks(BuffType.Stun) == 0 &&
                        battle.SelectedBurstCount < battle.PlayerController.AffordableBurstActors) battle.ToggleBurstActor(piece);
                if (battle.SelectedBurstCount == 0) battle.CancelBurstSelection(); else battle.OnBurstButton();
                return true;
            }
            if (!battle.PlayerController.isBursting && !battle.BurstInputPending && battle.PlayerController.ableBurst && battle.ActivePiece != null)
            { battle.OnBurstButton(); return true; }
            return false;
        }

        private void OnBattleCompleted(BattleOutcome outcome) { pendingOutcome = outcome; }

        private void CompleteRun(BattleBalanceResult result, string message)
        {
            if (report == null) return;
            if (battle != null) ObserveUnits();
            report.result = result; report.message = message; report.finishedUtc = DateTime.UtcNow.ToString("O");
            suiteFailed |= result != BattleBalanceResult.Victory && result != BattleBalanceResult.Defeat;
            try
            {
                LastReportPath = BattleBalanceReportWriter.Write(outputDirectory, report);
                summary.Add(report);
                BattleBalanceReportWriter.WriteSummary(outputDirectory, summary);
            }
            finally { Detach(); }
            completedRuns++;
            if (result == BattleBalanceResult.Victory) victories++;
            Debug.Log($"[BattleBalance] {result}，报告：{LastReportPath}", this);
            string scenePath = report.scenePath;
            report = null;
            if (stopRequested || ++runIndex >= Mathf.Max(1, repeatCount)) { FinishSuite(); return; }
            if (result == BattleBalanceResult.SceneChanged || result == BattleBalanceResult.Error || result == BattleBalanceResult.StartupTimeout || string.IsNullOrEmpty(scenePath))
            { FinishSuite(); return; }
            InstallSandbox();
            UnityEngine.Random.InitState(unchecked(baseSeed + runIndex));
            report = NewReport();
            waitingSeconds = 0f; reloading = true; expectedScenePath = scenePath;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            if (!Application.CanStreamedLevelBeLoaded(scenePath)) throw new InvalidOperationException("批量测试场景未加入 Build Settings");
            SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
#endif
        }

        private void Detach()
        {
            if (battle != null) { battle.BattleCompleted -= OnBattleCompleted; battle.ReleasePlayerControl(this); }
            previousBattle = battle;
            battle = null;
        }

        private void FinishSuite()
        {
            if (!IsRunning && !randomOwned) return;
            Detach();
            if (profileOwner != null && profileOwner.PLAYERPROFILE == sandboxProfile) profileOwner.PLAYERPROFILE = originalProfile;
            if (originalProfile != null)
                foreach (var piece in observed.Values)
                    if (piece != null && piece.isPlayerPiece) piece.playerData = originalProfile.GetPlayer(piece.pieceID - 1);
            originalProfile = sandboxProfile = null;
            profileOwner = null;
            if (randomOwned) { UnityEngine.Random.state = originalRandom; randomOwned = false; }
            IsRunning = reloading = false;
            Status = $"已结束：{completedRuns} 局，胜利 {victories} 局；报告目录 {outputDirectory}";
            if (quitWhenFinished && Application.isBatchMode)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.Exit(suiteFailed ? 2 : 0);
#else
                Application.Quit(suiteFailed ? 2 : 0);
#endif
            }
        }

        private void OnDisable() { if (IsRunning) StopTest(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Contains("-battle-balance")) return;
            var runner = FindObjectOfType<BattleBalanceRunner>();
            if (runner != null && runner.IsRunning) return;
            if (runner == null) runner = new GameObject("BattleBalanceRunner").AddComponent<BattleBalanceRunner>();
            runner.enabled = true;
            string Value(string key) { int index = Array.IndexOf(args, key); return index >= 0 && index + 1 < args.Length ? args[index + 1] : null; }
            if (int.TryParse(Value("-battle-balance-runs"), out int runs)) runner.repeatCount = Mathf.Max(1, runs);
            if (int.TryParse(Value("-battle-balance-seed"), out int seed)) runner.baseSeed = seed;
            runner.reportDirectory = Value("-battle-balance-report");
            runner.quitWhenFinished = args.Contains("-battle-balance-quit");
            runner.StartTest();
        }
    }
}
