using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>保存在独立 JSON 中的全局通关记录，新游戏和读档不会重置。</summary>
public static class LocalGameProgress
{
    private const string LegacyGameFinishedKey = "LunaticProject.GameFinished"; // 仅用于迁移旧版 PlayerPrefs。
    private static string SavePath => Path.Combine(Application.persistentDataPath, "Datas", "GlobalProgress.json");

    [Serializable]
    private sealed class ProgressData
    {
        public bool hasFinishedGame; // 独立于十个剧情存档槽的通关标记。
    }

    /// <summary>读取 JSON 通关标记；首次访问时迁移旧版记录，读取失败时返回未通关并保留原文件。</summary>
    public static bool HasFinishedGame => Load()?.hasFinishedGame ?? false;

    /// <summary>将全局通关状态写入 JSON；不依赖当前剧情存档槽。</summary>
    public static void MarkGameFinished()
    {
        if (GM.Ins != null && GM.Ins.DeferBattleGameFinished()) return;
        var data = Load();
        if (data == null || data.hasFinishedGame) return;
        data.hasFinishedGame = true;
        Save(data);
    }

    /// <summary>显式重置 JSON 通关标记，保留剧情存档；可用于修复损坏的全局记录。</summary>
    public static void ClearGameFinished()
    {
        Save(new ProgressData());
    }

    private static ProgressData Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                var data = JsonTool.LoadJson<ProgressData>(SavePath);
                if (data == null) throw new InvalidDataException("全局通关记录为空。");
                // JSON 优先，旧键不能覆盖云端下载的记录或显式清除的状态。
                RemoveLegacyKey();
                return data;
            }

            var migrated = new ProgressData { hasFinishedGame = PlayerPrefs.GetInt(LegacyGameFinishedKey, 0) == 1 };
            return Save(migrated) ? migrated : null;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            Debug.LogError($"[Progress] 读取全局通关记录失败，原数据已保留：{exception.Message}");
            return null;
        }
    }

    private static bool Save(ProgressData data)
    {
        try
        {
            string path = SavePath;
            // 先完整写入同目录临时文件，再替换正式记录；失败时保留旧 JSON 和旧键。
            string temporary = path + ".tmp";
            JsonTool.SaveJson(data, temporary);
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
            RemoveLegacyKey();
            return true;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            Debug.LogError($"[Progress] 保存全局通关记录失败：{exception.Message}");
            return false;
        }
    }

    private static void RemoveLegacyKey()
    {
        if (!PlayerPrefs.HasKey(LegacyGameFinishedKey)) return;
        PlayerPrefs.DeleteKey(LegacyGameFinishedKey);
        PlayerPrefs.Save();
    }

    private static bool IsStorageException(Exception exception)
    {
        return exception is IOException || exception is InvalidDataException ||
               exception is UnauthorizedAccessException || exception is JsonException;
    }
}
