public partial class 剧本System
{
    private bool readingMapStory;
    private string readingMapScript;

    private void RecordReadingStory(string scriptFile)
    {
        // 战斗日志等也使用剧本System，只记录大地图的剧情阅读器。
        if (大地图System.instance == null || 大地图System.instance.剧情 != this ||
            已储存剧本 == null || 已储存剧本.Length == 0) return;
        readingMapStory = true;
        readingMapScript = scriptFile;
        GM.Ins.PLAYERPROFILE.BeginStory(scriptFile);
    }

    public void CaptureReadingState(PLAYERPROFILE profile)
    {
        // 关闭设置、隐藏阅读器或读档转场时不等于剧情完成。
        if (readingMapStory) profile.BeginStory(readingMapScript);
    }

    public void EndReadingStory()
    {
        readingMapStory = false;
        readingMapScript = null;
        if (大地图System.instance != null && 大地图System.instance.剧情 == this)
            GM.Ins.PLAYERPROFILE.EndStory();
    }
}
