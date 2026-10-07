using System;
using System.IO;
static class Program
{
    static int checks;
    static void Check(bool condition,string label) { checks++;if(!condition)throw new Exception(label); }
    static void Main(string[] args)
    {
        string directory=args[0];
        var reader=new 剧本System();
        大地图System.instance=new 大地图System { 剧情=reader };
        var profile=GM.Ins.PLAYERPROFILE;
        profile.新游戏初始化数值();profile.currentMap="YamaClass";profile.currentScene="AVG";
        reader.Open("EXC1-3");
        Check(profile.isStoryInProgress && !profile.isNewGame,"reading state excludes new-game startup");
        Check(profile.TryGetStoryToResume(out string file) && file=="EXC1-3","exact script preserved");
        profile.保存任务进度("other-task",1);
        Check(profile.isStoryInProgress,"SAVE/progress update is not END");
        reader.Open("EXC1-4");
        reader.CaptureReadingState(profile);
        Check(profile.resumeStoryScript=="EXC1-4","NEXT records current script rather than entry node/title");
        string path=Path.Combine(directory,"reading.json");
        JsonTool.SaveJson(profile,path);
        var loaded=JsonTool.LoadJson<PLAYERPROFILE>(path);
        Check(loaded.TryGetStoryToResume(out file) && file=="EXC1-4","reading JSON survives fresh profile load");
        Check(loaded.currentMap=="YamaClass" && loaded.currentScene=="AVG" && loaded.获取任务进度("other-task")==1,"map/scene/progress survive serialization");
        var battleLog=new 剧本System();battleLog.Open("BattleLog");
        Check(profile.resumeStoryScript=="EXC1-4","battle logger cannot overwrite map reader state");
        reader.EndReadingStory();
        reader.CaptureReadingState(profile);
        Check(!profile.isStoryInProgress && profile.resumeStoryScript==null,"completion clears state and capture cannot resurrect it");
        path=Path.Combine(directory,"completed.json");JsonTool.SaveJson(profile,path);
        loaded=JsonTool.LoadJson<PLAYERPROFILE>(path);
        Check(!loaded.TryGetStoryToResume(out file),"completed JSON does not request replay");
        path=Path.Combine(directory,"legacy.json");File.WriteAllText(path,"{\"isNewGame\":false,\"currentScriptFile\":\"EXC1-3\",\"finishNodeDic\":{\"EXC1-3\":1}}");
        loaded=JsonTool.LoadJson<PLAYERPROFILE>(path);
        Check(!loaded.TryGetStoryToResume(out file) && loaded.获取任务进度("EXC1-3")==1,"legacy profile does not guess replay and keeps progress");
        reader.Missing("missing");
        Check(!profile.isStoryInProgress,"failed script load does not begin reading");
        profile.BeginStory("EXC1-3");profile.新游戏初始化数值();
        Check(profile.isNewGame && !profile.isStoryInProgress,"new game clears prior reading state");
        profile.BeginStory(" ");Check(!profile.TryGetStoryToResume(out file),"empty script cannot resume");
        Console.WriteLine($"AVG save recovery: {checks} checks passed against actual profile, reader state and JsonTool using project Newtonsoft JSON.");
    }
}
