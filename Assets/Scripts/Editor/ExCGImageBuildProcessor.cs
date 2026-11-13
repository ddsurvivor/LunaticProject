using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>通过菜单手动迁移及恢复 EXC 剧本未使用的图片，不参与构建或编辑器自动恢复。</summary>
public static class ExCGImageBuildProcessor
{
    private const string ImageRoot = "Assets/Resources/CG";
    private const string StoryRoot = "Assets/Resources/Story";
    private const string StagingRoot = "Library/ExCGImageBuildStaging";
    private const string ManifestPath = StagingRoot + "/manifest.json";
    private const string ReportPath = "Library/ExCGImageBuildReport.json";
    private const string ReleaseRoot = @"E:\GameDev\2 Release\LunaticAVG";
    private static readonly Regex ImageCommand = new Regex(
        @"^\s*(CG|FULLCG|HALFCG|CGLOG|SPEAK)\s*\(([^,)]*)", RegexOptions.Compiled);

    [Serializable]
    public class ImageMove
    {
        public string originalPath;
        public string stagedPath;
        public long bytes;
    }

    [Serializable]
    public class ImageBuildReport
    {
        public string[] stories;
        public string[] referencedImages;
        public string[] missingImages;
        public string[] directlyReferencedImages;
        public List<ImageMove> movedImages = new List<ImageMove>();
        public long movedSourceBytes;
        public string outputPath;
        public string buildResult;
        public long buildReportedBytes;
        public long directoryBytesBefore;
        public long directoryBytesAfter;
        public long resourcesBytesBefore;
        public long resourcesBytesAfter;
        public bool restored;
    }

    /// <summary>读取 EXC 剧本第一列，收集图片指令的首个参数。</summary>
    public static ImageBuildReport Scan()
    {
        var report = new ImageBuildReport();
        report.stories = Directory.GetFiles(StoryRoot, "*.bytes")
            .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith("EXC", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (report.stories.Length == 0)
            throw new BuildFailedException("没有找到 EXC 剧本，取消 CG 图片裁剪。");

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in report.stories)
        {
            using (var stream = new MemoryStream(File.ReadAllBytes(path)))
            using (var package = new ExcelPackage(stream))
            {
                var sheet = package.Workbook.Worksheets[1];
                if (sheet.Dimension == null) continue;
                for (int row = 2; row <= sheet.Dimension.End.Row; row++)
                    CollectImageNames(sheet.Cells[row, 1].Text, used);
            }
        }

        var images = AssetDatabase.FindAssets("t:Texture2D", new[] { ImageRoot })
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p, StringComparer.Ordinal).ToArray();
        var keys = new HashSet<string>(images.Select(ImageKey), StringComparer.OrdinalIgnoreCase);
        report.referencedImages = used.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        report.missingImages = used.Where(p => !keys.Contains(p)).OrderBy(p => p, StringComparer.Ordinal).ToArray();

        // 保留构建场景和其他 Resources 资源直接引用的图片。
        var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)
            .Concat(AssetDatabase.FindAssets("", new[] { "Assets/Resources" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.StartsWith(ImageRoot + "/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)))
            .ToArray();
        var dependencies = new HashSet<string>(AssetDatabase.GetDependencies(roots, true), StringComparer.Ordinal);
        report.directlyReferencedImages = images.Where(dependencies.Contains).ToArray();
        foreach (string path in images)
        {
            if (used.Contains(ImageKey(path)) || dependencies.Contains(path)) continue;
            report.movedImages.Add(new ImageMove
            {
                originalPath = path,
                stagedPath = StagingRoot + "/files/" + path.Substring(ImageRoot.Length + 1),
                bytes = new FileInfo(path).Length
            });
        }
        report.movedSourceBytes = report.movedImages.Sum(i => i.bytes);
        return report;
    }

    /// <summary>忽略空的 SPEAK 参数，按完整指令名称识别图片。</summary>
    public static void CollectImageNames(string commands, ISet<string> used)
    {
        foreach (string command in (commands ?? "").Split('+'))
        {
            var match = ImageCommand.Match(command);
            if (!match.Success) continue;
            string name = match.Groups[2].Value.Trim();
            if (name.Length > 0) used.Add(name);
        }
    }

    private static string ImageKey(string path)
    {
        string relative = path.Substring(ImageRoot.Length + 1);
        return relative.Substring(0, relative.Length - Path.GetExtension(relative).Length);
    }

    /// <summary>手动移出未使用的图片及 meta；已有清单时拒绝重复迁移，中断后须手动恢复。</summary>
    [MenuItem("Tools/CG构建/手动迁移未使用的图片")]
    public static void StageUnusedImages()
    {
        if (File.Exists(ManifestPath))
            throw new InvalidOperationException("已有 CG 迁移清单，请先通过菜单恢复临时移出的图片。");
        var report = Scan();
        Directory.CreateDirectory(StagingRoot);
        // 在第一次移动前写清单，保证移动中断后仍能恢复。
        File.WriteAllText(ManifestPath, JsonUtility.ToJson(report, true));
        AssetDatabase.SaveAssets();
        foreach (var image in report.movedImages)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(image.stagedPath));
            MoveFile(image.originalPath, image.stagedPath);
            MoveFile(image.originalPath + ".meta", image.stagedPath + ".meta");
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[EXC CG] 扫描 {report.stories.Length} 个剧本，手动移出 {report.movedImages.Count} 张图片；恢复须通过菜单手动执行。");
        if (report.missingImages.Length > 0)
            Debug.LogWarning("[EXC CG] 剧本引用的图片不存在：" + string.Join(", ", report.missingImages));
    }

    private static void MoveFile(string source, string destination)
    {
        if (File.Exists(destination)) throw new IOException("恢复或暂存路径已存在，停止覆盖：" + destination);
        File.Move(source, destination);
    }

    /// <summary>恢复图片及原始 meta。遇到同名文件时保留清单，不覆盖文件。</summary>
    [MenuItem("Tools/CG构建/恢复临时移出的图片")]
    public static void Restore()
    {
        if (!File.Exists(ManifestPath)) return;
        var report = JsonUtility.FromJson<ImageBuildReport>(File.ReadAllText(ManifestPath));
        foreach (var image in report.movedImages)
        {
            ValidatePath(image.originalPath, ImageRoot);
            ValidatePath(image.stagedPath, StagingRoot + "/files");
            Directory.CreateDirectory(Path.GetDirectoryName(image.originalPath));
            if (File.Exists(image.stagedPath)) MoveFile(image.stagedPath, image.originalPath);
            if (File.Exists(image.stagedPath + ".meta")) MoveFile(image.stagedPath + ".meta", image.originalPath + ".meta");
            if (!File.Exists(image.originalPath) || !File.Exists(image.originalPath + ".meta"))
                throw new IOException("图片恢复不完整：" + image.originalPath);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        report.restored = true;
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        File.Delete(ManifestPath);
        Debug.Log($"[EXC CG] 已恢复 {report.movedImages.Count} 张图片及 meta。报告：{ReportPath}");
    }

    private static void ValidatePath(string path, string root)
    {
        string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new IOException("图片路径超出预期目录：" + path);
    }

    [MenuItem("Tools/CG构建/扫描 EXC 图片引用")]
    public static void ScanOnly()
    {
        var report = Scan();
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log($"[EXC CG] 待移出 {report.movedImages.Count} 张图片，缺失引用 {report.missingImages.Length} 个。报告：{ReportPath}");
    }

    /// <summary>构建指定 Windows 发布目录，不迁移或恢复 CG 图片。</summary>
    [MenuItem("Tools/CG构建/构建 LunaticAVG 发布包")]
    public static void BuildRelease()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(ReleaseRoot, "EI Iris2.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new BuildFailedException("EXC 发布包构建失败：" + report.summary.result);
    }

}
