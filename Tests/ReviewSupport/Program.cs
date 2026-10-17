using System;
using System.IO;
using System.Linq;
using SteamReviewSupport;

internal static class Program
{
    private static int checks; // 文件安装、恢复及错误拒绝的断言总数。

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    /// <summary>在临时目录运行审核存档安装回归，不触碰真实玩家存档。</summary>
    private static void Main(string[] args)
    {
        string source = args[0];
        string sandbox = args[1];
        Directory.CreateDirectory(sandbox);
        var catalog = ReviewSaveStore.Load(source);
        Check(catalog.entries.Length == 7, "Seven review entry points");
        string target = Path.Combine(sandbox, Guid.NewGuid().ToString("N"));
        Check(ReviewSaveStore.Install(source, target, catalog, false) == 7, "Fresh installation");
        foreach (var entry in catalog.entries)
            Check(File.ReadAllText(Path.Combine(target, $"PlayerProfiles_{entry.slot}.json")) == ReviewSaveStore.ReadTemplate(source, entry), "Exact template installed");
        Check(ReviewSaveStore.Install(source, target, catalog, false) == 0, "Repeat launch preserves slots");
        string occupied = Path.Combine(target, "PlayerProfiles_2.json");
        File.WriteAllText(occupied, "reviewer's existing progress");
        File.WriteAllText(Path.Combine(target, "PlayerProfiles_0.json"), "other slot");
        File.WriteAllText(Path.Combine(target, "PlayerSettingsData.json"), "settings");
        File.WriteAllText(Path.Combine(target, ".streamingassets_migrated"), "old marker");
        Check(ReviewSaveStore.Install(source, target, catalog, false) == 0 && File.ReadAllText(occupied) == "reviewer's existing progress", "Do not overwrite on startup");
        File.Delete(Path.Combine(target, "PlayerProfiles_3.json"));
        Check(ReviewSaveStore.Install(source, target, catalog, false) == 1, "Install despite existing legacy migration marker");
        Check(ReviewSaveStore.Install(source, target, catalog, true) == 7, "Explicit restore installs all review slots");
        Check(Directory.GetFiles(Path.Combine(target, "ReviewBackups"), "*.json", SearchOption.AllDirectories).Length == 7, "Every overwritten slot has a backup");
        Check(Directory.GetFiles(Path.Combine(target, "ReviewBackups"), "PlayerProfiles_2.json", SearchOption.AllDirectories).Any(file => File.ReadAllText(file) == "reviewer's existing progress"), "Original progress recoverable");
        Check(File.ReadAllText(Path.Combine(target, "PlayerProfiles_0.json")) == "other slot", "Unrelated slots unchanged");
        Check(File.ReadAllText(Path.Combine(target, "PlayerSettingsData.json")) == "settings", "Settings unchanged");
        string invalidSource = Path.Combine(sandbox, "invalid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(invalidSource);
        foreach (string file in Directory.GetFiles(source, "*.json")) File.Copy(file, Path.Combine(invalidSource, Path.GetFileName(file)));
        File.WriteAllText(Path.Combine(invalidSource, catalog.entries[6].file), "{}");
        string invalidTarget = Path.Combine(sandbox, "must-not-exist-" + Guid.NewGuid().ToString("N"));
        bool rejected = false;
        try { ReviewSaveStore.Install(invalidSource, invalidTarget, catalog, false); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected && !Directory.Exists(invalidTarget), "Reject bad templates before any destination writes");
        var entryWithTraversal = new ReviewEntry { file = "../outside.json" };
        rejected = false;
        try { ReviewSaveStore.ReadTemplate(source, entryWithTraversal); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected, "Reject path traversal");
        string manifest = File.ReadAllText(Path.Combine(source, "review.json"));
        File.WriteAllText(Path.Combine(invalidSource, "review.json"), manifest.Replace("\"slot\": 3", "\"slot\": 2"));
        File.Copy(Path.Combine(source, catalog.entries[6].file), Path.Combine(invalidSource, catalog.entries[6].file), true);
        rejected = false;
        try { ReviewSaveStore.Load(invalidSource); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected, "Reject duplicate slot mappings");
        Console.WriteLine($"PASS: {checks} review support checks; real player files untouched.");
    }
}
