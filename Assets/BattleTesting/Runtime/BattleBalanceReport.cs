using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Lunatic.BattleTesting
{
    public enum BattleBalanceResult { Victory, Defeat, Timeout, Stalled, ActionLimit, Stopped, SceneChanged, StartupTimeout, Error }

    [Serializable]
    public sealed class BattleBalanceUnit
    {
        public int instanceId, pieceId, observedRound, initialHealth, finalHealth, maxHealth;
        public bool player, dead;
        public string name;
        public float initialX, initialY, initialZ, finalX, finalY, finalZ;
    }

    [Serializable]
    public sealed class BattleBalanceAction
    {
        public int sequence, round, actorId, apBefore, apAfter;
        public float elapsed, x, y, z;
        public string actor, kind, skill, result, reason;
    }

    [Serializable]
    public sealed class BattleBalanceReport
    {
        public string suite, scene, scenePath, startedUtc, finishedUtc, unityVersion, message;
        public int run, seed, rounds, commands, rejectedCommands;
        public float activeSeconds;
        public BattleBalanceResult result;
        public PlayerAutoBattleSettings policy;
        public List<BattleBalanceUnit> units = new();
        public List<BattleBalanceAction> actions = new();
    }

    [Serializable]
    public sealed class BattleBalanceSummary
    {
        public int runs, victories, defeats, incomplete;
        public float winRateAllRuns, winRateCompletedRuns, meanRounds, meanActiveSeconds;

        /// <summary>未完成测试单独计数，同时输出全部局数与正式胜负局数两种胜率分母。</summary>
        public void Add(BattleBalanceReport report)
        {
            runs++;
            if (report.result == BattleBalanceResult.Victory) victories++;
            else if (report.result == BattleBalanceResult.Defeat) defeats++;
            else incomplete++;
            winRateAllRuns = victories / (float)runs;
            winRateCompletedRuns = victories + defeats > 0 ? victories / (float)(victories + defeats) : 0f;
            meanRounds += (report.rounds - meanRounds) / runs;
            meanActiveSeconds += (report.activeSeconds - meanActiveSeconds) / runs;
        }
    }

    /// <summary>逐局导出完整 JSON 与聚合 CSV；结果类型保留超时/卡死，不能把未完成局计作胜利。</summary>
    public static class BattleBalanceReportWriter
    {
        public static void WriteSummary(string directory, BattleBalanceSummary summary)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "suite-summary.json"), JsonConvert.SerializeObject(summary, Formatting.Indented));
        }

        public static string Write(string directory, BattleBalanceReport report)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"run-{report.run:D3}-seed-{report.seed}.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
            string csv = Path.Combine(directory, "summary.csv");
            if (!File.Exists(csv)) File.WriteAllText(csv, "run,scene,seed,style,result,rounds,active_seconds,commands,rejected,players_dead,enemies_dead,players_hp_remaining,message\n");
            string[] row = {
                report.run.ToString(), report.scene, report.seed.ToString(), report.policy.style.ToString(), report.result.ToString(),
                report.rounds.ToString(), report.activeSeconds.ToString("F3", CultureInfo.InvariantCulture), report.commands.ToString(), report.rejectedCommands.ToString(),
                report.units.Count(u => u.player && u.dead).ToString(), report.units.Count(u => !u.player && u.dead).ToString(),
                report.units.Where(u => u.player).Sum(u => Math.Max(0, u.finalHealth)).ToString(), report.message ?? ""
            };
            File.AppendAllText(csv, string.Join(",", row.Select(Escape)) + "\n");
            return path;
        }

        private static string Escape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
