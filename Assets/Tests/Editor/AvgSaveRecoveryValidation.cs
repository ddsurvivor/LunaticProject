using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>使用实际 AVG 场景验证存档恢复；测试 JSON 放在项目 Temp，不占用玩家存档槽。</summary>
public static class AvgSaveRecoveryValidation
{
    private const string RunningKey = "AVG.SaveRecovery.Running";
    private static readonly string Output = Path.GetFullPath("Temp/AvgSaveRecoveryValidation");
    private static readonly string Request = Path.GetFullPath("Temp/AvgSaveRecoveryValidation.request");
    private static int stage, checks, firstCursor;
    private static string scriptFile, mapName;
    private static 剧本System oldReader;
    private static double deadline;
    private static bool checkedTransition;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(Request);
                Run();
            }
            if (SessionState.GetBool(RunningKey, false))
                deadline = EditorApplication.timeSinceStartup + 90;
        };
    }

    [MenuItem("Tests/AVG/Validate Save Recovery")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "legacy.json"),
            "{\"isNewGame\":false,\"currentScene\":\"AVG\",\"currentScriptFile\":\"EXC1-3\",\"finishNodeDic\":{\"EXC1-3\":1}}");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.WriteAllText(Path.Combine(Output, "result.txt"), "BLOCKED: run this validation outside Play Mode.");
            return;
        }
        stage = checks = 0;
        File.WriteAllText(Path.Combine(Output, "result.txt"), "RUNNING: AVG save-recovery validation.");
        checkedTransition = false;
        SessionState.SetBool(RunningKey, true);
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.isPlaying = true;
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        checks++;
    }

    private static PLAYERPROFILE RoundTrip(string name)
    {
        大地图System.instance.CaptureMapState();
        var profile = GM.Ins.PLAYERPROFILE;
        profile.currentScene = SceneManager.GetActiveScene().name;
        profile.lastSaveTime = DateTime.Now;
        string path = Path.Combine(Output, name + ".json");
        JsonTool.SaveJson(profile, path);
        return JsonTool.LoadJson<PLAYERPROFILE>(path);
    }

    private static int Cursor(剧本System reader)
    {
        return (int)typeof(剧本System).GetField("已阅读", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
    }

    private static void Finish(string result)
    {
        File.WriteAllText(Path.Combine(Output, "result.txt"), result);
        Debug.Log(result);
        SessionState.SetBool(RunningKey, false);
        EditorApplication.isPlaying = false;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false) && File.Exists(Request) &&
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
        {
            File.Delete(Request);
            Run();
            return;
        }
        if (!SessionState.GetBool(RunningKey, false) || EditorApplication.isCompiling) return;
        try
        {
            // 进入 Play Mode 的域重载会重置静态字段，update 可能早于 delayCall。
            if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 90;
            if (EditorApplication.timeSinceStartup > deadline)
                throw new Exception("AVG validation timed out at stage " + stage);
            if (!EditorApplication.isPlaying) return;
            if (GM.Ins == null || GM.Ins.PLAYERPROFILE == null) return;

            var map = 大地图System.instance;
            if (stage == 0)
            {
                if (GM.Ins.IsTransitioning) return;
                var profile = new PLAYERPROFILE();
                profile.新游戏初始化数值();
                profile.isNewGame = false;
                profile.currentScene = "AVG";
                profile.currentMap = "YamaClass";
                GM.Ins.PLAYERPROFILE = profile;
                GM.Ins.LoadPlayingScene("AVG");
                stage = 1;
                return;
            }
            if (GM.Ins.IsTransitioning)
            {
                if (stage == 3 && map != null && map.剧情 != oldReader)
                {
                    Check(!map.剧情.gameObject.activeSelf, "Reader opened before loading transition finished");
                    checkedTransition = true;
                }
                return;
            }
            if (map == null || map.当前地图 == null) return;

            if (stage == 1)
            {
                Check(SceneManager.GetActiveScene().name == "AVG", "AVG scene not loaded");
                Check(!map.剧情.gameObject.activeSelf, "Map-only profile auto-opened story");
                // EXC1-1 是自动序章，没有对应的可点击地图节点；使用实际节点 EXC1-3。
                var node = map.NodeList.First(n => n.name.Replace("(Clone)", "") == "EXC1-3");
                for (int i = 0; i < node.前置任务要求.Length; i++)
                    GM.Ins.PLAYERPROFILE.保存任务进度(node.前置任务要求[i], node.前置任务进度要求[i]);
                map.RefreshAllNodes();
                scriptFile = node.name.Replace("(Clone)", "");
                mapName = map.当前地图.name;
                node.OnClickStart();
                stage = 2;
                return;
            }
            if (stage == 2)
            {
                if (!map.剧情.gameObject.activeSelf || Cursor(map.剧情) == 0) return;
                map.剧情.StopAutoPlay();
                firstCursor = Cursor(map.剧情);
                map.剧情.Next();
                Check(Cursor(map.剧情) > firstCursor, "Fixture did not advance beyond beginning");
                map.剧情.HideReader();
                Check(map.剧情.IsReaderHiding, "Reader did not start fading out");
                int closingCursor = Cursor(map.剧情);
                map.剧情.Next();
                Check(Cursor(map.剧情) == closingCursor, "Closing reader advanced story");
                var hidden = RoundTrip("hidden-reader");
                Check(hidden.isStoryInProgress, "Temporarily hidden reader lost reading state");
                map.剧情.ShowReader();
                Check(!map.剧情.IsReaderHiding && map.剧情.GetComponent<CanvasGroup>().alpha == 1f,
                    "Reopening reader did not cancel fade and restore visibility");
                var loaded = RoundTrip("reading");
                Check(loaded.TryGetStoryToResume(out string saved) && saved == scriptFile, "Reading state/file not serialized");
                Check(!loaded.isNewGame, "Reading save retained new-game flag");
                oldReader = map.剧情;
                GM.Ins.PLAYERPROFILE = loaded;
                GM.Ins.LoadPlayingScene(loaded.currentScene);
                stage = 3;
                return;
            }
            if (stage == 3)
            {
                if (map.剧情 == oldReader || !map.剧情.gameObject.activeSelf) return;
                Check(checkedTransition, "Loading-phase reader guard not observed");
                Check(map.当前地图.name == mapName, "Saved map was not restored");
                Check(Cursor(map.剧情) == firstCursor, "Reading save did not restart at beginning");
                Check(GM.Ins.PLAYERPROFILE.resumeStoryScript == scriptFile, "Wrong script resumed");
                // 使用真实 END 指令更新任务进度、关闭阅读器、刷新节点。
                map.剧情.进行指令("END(" + scriptFile + ",1)");
                stage = 31;
                return;
            }
            if (stage == 31)
            {
                if (map.剧情.IsReaderHiding) return;
                Check(!map.剧情.gameObject.activeSelf, "END did not close reader");
                var loaded = RoundTrip("completed");
                Check(!loaded.isStoryInProgress && loaded.resumeStoryScript == null, "Completed story retained replay state");
                Check(loaded.获取任务进度(scriptFile) == 1, "Completion progress not serialized");
                oldReader = map.剧情;
                GM.Ins.PLAYERPROFILE = loaded;
                GM.Ins.LoadPlayingScene(loaded.currentScene);
                stage = 4;
                return;
            }
            if (stage == 4)
            {
                if (map.剧情 == oldReader) return;
                Check(!map.剧情.gameObject.activeSelf, "Completed save reopened story");
                Check(map.当前地图.name == mapName, "Completed save lost map");
                Check(GM.Ins.PLAYERPROFILE.获取任务进度(scriptFile) == 1, "Completed save lost task progress");
                var completed = map.NodeList.Where(n => n.name.Replace("(Clone)", "") == scriptFile).ToArray();
                Check(completed.Length > 0 && completed.All(n => !n.gameObject.activeSelf), "Completed node was not hidden");
                var unlocked = map.NodeList.Where(n => n.前置任务要求.Contains(scriptFile) &&
                    n.前置任务要求.Select((task, i) => GM.Ins.PLAYERPROFILE.获取任务进度(task) >= n.前置任务进度要求[i]).All(x => x) &&
                    GM.Ins.PLAYERPROFILE.获取任务进度(n.name.Replace("(Clone)", "")) < 1).ToArray();
                Check(unlocked.Length > 0 && unlocked.All(n => n.gameObject.activeSelf), "Following task nodes not unlocked");
                // 重复初始化/加载完成回调也不能开启第二段剧情。
                map.InitializeMap();
                map.StartStoryAfterLoad();
                Check(!map.剧情.gameObject.activeSelf, "Repeated startup callback opened story");
                var legacy = JsonTool.LoadJson<PLAYERPROFILE>(Path.Combine(Output, "legacy.json"));
                Check(!legacy.TryGetStoryToResume(out _), "Old save incorrectly inferred pending story");
                Finish("PASS: " + checks + " AVG scene save-recovery checks; reading restart, post-load timing, END completion, node unlocks, repeat startup, legacy compatibility. Test JSON only in project Temp.");
            }
        }
        catch (Exception exception)
        {
            Finish("FAIL at stage " + stage + ": " + exception);
        }
    }
}
