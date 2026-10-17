#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SteamReviewSupport
{
    /// <summary>审核入口清单项，供运行时和构建前校验共用。</summary>
    public sealed class ReviewEntry
    {
        public int shortcutNumber { get; set; } // Ctrl+Alt+2 至 Ctrl+Alt+8 的数字键编号。
        public int slot { get; set; } // 零基普通存档槽位。
        public string title { get; set; }
        public string file { get; set; }
        public string script { get; set; }
        public string map { get; set; }
        public int chapter { get; set; }
    }

    /// <summary>随包审核入口集合。</summary>
    public sealed class ReviewCatalog
    {
        public ReviewEntry[] entries { get; set; }
    }

    /// <summary>独立审核档的校验及文件安装接口；不依赖游戏场景。</summary>
    public static class ReviewSaveStore
    {
        /// <summary>检查审核清单和全部模板；任何模板无效时不开始安装。</summary>
        public static ReviewCatalog Load(string sourceDirectory)
        {
            var catalog = JsonConvert.DeserializeObject<ReviewCatalog>(
                File.ReadAllText(Path.Combine(sourceDirectory, "review.json")));
            if (catalog?.entries == null || catalog.entries.Length == 0)
                throw new InvalidDataException("Review catalog is empty.");
            var keys = new HashSet<int>();
            var slots = new HashSet<int>();
            foreach (var entry in catalog.entries)
            {
                if (entry == null || entry.shortcutNumber < 2 || entry.shortcutNumber > 8 ||
                    entry.slot < 0 || entry.slot > 9 || !keys.Add(entry.shortcutNumber) || !slots.Add(entry.slot) ||
                    string.IsNullOrWhiteSpace(entry.title) || string.IsNullOrWhiteSpace(entry.script) ||
                    string.IsNullOrWhiteSpace(entry.map) || entry.chapter < 1 || entry.chapter > 4)
                    throw new InvalidDataException("Invalid or duplicate review entry.");
                ReadTemplate(sourceDirectory, entry);
            }
            return catalog;
        }

        /// <summary>读取独立的审核模板，核对剧情恢复、地图、完成节点和基本角色数据。</summary>
        public static string ReadTemplate(string sourceDirectory, ReviewEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.file) || Path.GetFileName(entry.file) != entry.file ||
                entry.file.Contains("/") || entry.file.Contains("\\") || !entry.file.EndsWith(".json", StringComparison.Ordinal))
                throw new InvalidDataException("Review filename must be a local JSON filename.");
            string json = File.ReadAllText(Path.Combine(sourceDirectory, entry.file));
            var profile = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            var nodes = profile["finishNodeDic"] as JObject;
            var players = profile["player"] as JArray;
            if ((string)profile["currentScene"] != "AVG" || (string)profile["currentMap"] != entry.map ||
                (string)profile["resumeStoryScript"] != entry.script || (string)profile["currentScriptFile"] != entry.script ||
                (bool?)profile["isStoryInProgress"] != true || (bool?)profile["isNewGame"] != false ||
                (int?)profile["chapterNumber"] != entry.chapter || (int?)profile["curSmallMapIndex"] != 0 ||
                nodes == null || !nodes.HasValues || (int?)nodes[entry.script] >= 1 ||
                players == null || players.Count != 20)
                throw new InvalidDataException("Review save has inconsistent progress: " + entry.file);
            for (int i = 0; i < 3; i++)
                if (!(players[i] is JObject player) || (int?)player["HP"] == null || (int)player["HP"] <= 0)
                    throw new InvalidDataException("Review save is missing a playable character: " + entry.file);
            return json;
        }

        /// <summary>安装到普通存档槽；默认仅补缺，显式恢复时先备份再替换指定槽位。</summary>
        public static int Install(string sourceDirectory, string destinationDirectory, ReviewCatalog catalog, bool restore)
        {
            // 先检查全部源文件，避免中途发现坏模板才留下部分安装结果。
            foreach (var entry in catalog.entries) ReadTemplate(sourceDirectory, entry);
            Directory.CreateDirectory(destinationDirectory);
            int count = 0;
            foreach (var entry in catalog.entries)
            {
                string destination = Path.Combine(destinationDirectory, $"PlayerProfiles_{entry.slot}.json");
                if (File.Exists(destination))
                {
                    if (!restore) continue;
                    string backup = Path.Combine(destinationDirectory, "ReviewBackups",
                        DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(backup);
                    File.Copy(destination, Path.Combine(backup, Path.GetFileName(destination)), false);
                }
                File.Copy(Path.Combine(sourceDirectory, entry.file), destination, restore);
                count++;
            }
            return count;
        }
    }
}
#endif
