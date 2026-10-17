using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lunatic.BattleTesting.Editor
{
    /// <summary>编辑器安装入口；测试宿主和决策器均为普通运行时代码，不依赖此菜单。</summary>
    public static class BattleBalanceMenu
    {
        [MenuItem("工具/战斗自动测试/创建玩家 AI 测试控制器")]
        public static void CreateRunner()
        {
            var existing = UnityEngine.Object.FindObjectOfType<BattleBalanceRunner>();
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
            var root = new GameObject("BattleBalanceRunner");
            Undo.RegisterCreatedObjectUndo(root, "创建战斗自动测试控制器");
            root.AddComponent<BattleBalanceRunner>();
            Selection.activeGameObject = root;
        }

        [MenuItem("工具/战斗自动测试/开始当前关卡")]
        public static void StartCurrentBattle()
        {
            if (!EditorApplication.isPlaying) { Debug.LogWarning("请先进入 Play Mode，再开始关卡自动测试。"); return; }
            CreateRunner();
            UnityEngine.Object.FindObjectOfType<BattleBalanceRunner>().StartTest();
        }

        [MenuItem("工具/战斗自动测试/停止并导出报告")]
        public static void StopCurrentBattle() => UnityEngine.Object.FindObjectOfType<BattleBalanceRunner>()?.StopTest();

        [MenuItem("工具/战斗自动测试/打开最近报告")]
        public static void OpenReport()
        {
            var runner = UnityEngine.Object.FindObjectOfType<BattleBalanceRunner>();
            string path = runner != null ? runner.LastReportPath : null;
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) EditorUtility.RevealInFinder(path);
            else EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        /// <summary>供 Unity -executeMethod 调用；使用 -battle-balance-scene 指定关卡并进入 Play Mode。</summary>
        public static void RunFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-battle-balance") < 0) throw new ArgumentException("缺少 -battle-balance 参数");
            int index = Array.IndexOf(args, "-battle-balance-scene");
            if (index >= 0 && index + 1 < args.Length) EditorSceneManager.OpenScene(args[index + 1]);
            EditorApplication.isPlaying = true;
        }
    }
}
