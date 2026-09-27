using UnityEngine;

/// <summary>独立于存档槽的本地通关记录，新游戏和读档不会重置。</summary>
public static class LocalGameProgress
{
    private const string GameFinishedKey = "LunaticProject.GameFinished";

    public static bool HasFinishedGame => PlayerPrefs.GetInt(GameFinishedKey, 0) == 1;

    public static void MarkGameFinished()
    {
        if (HasFinishedGame) return;
        PlayerPrefs.SetInt(GameFinishedKey, 1);
        PlayerPrefs.Save();
    }
}