using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>扫描角色单张 Sprite 序列并更新 PieceDisplay；缺失动作保留原绑定，歧义动作禁止写入。</summary>
public static class CharacterAnimationImporter
{
    public static readonly string[] Actions = { "idle", "move", "melee", "range", "dodge", "hit", "death", "skill" };
    public static readonly string[] Labels = { "待机", "移动", "近战", "远程", "闪避", "受击", "死亡", "技能" };

    [Serializable]
    public class Settings
    {
        public string folder;
        public string prefix;
        public string[] prefixOverrides = new string[8];
        public bool preserveIdle = true; // 保留旧待机中的重复帧和人工编排。
    }

    public sealed class Row
    {
        public int action;
        public int skillIndex = -1; // 技能文件编号减一，不压缩缺失组。
        public string label;
        public List<Sprite> before;
        public List<Sprite> frames = new List<Sprite>();
        public readonly List<string> notes = new List<string>();
        public bool blocked;
        public bool selected = true;
        public bool Changed => !Same(before, frames);
    }

    public sealed class Plan
    {
        public PieceDisplay target;
        public Settings settings;
        public readonly List<Row> rows = new List<Row>();
        public readonly List<string> ignored = new List<string>();
        public readonly List<string> textureIssues = new List<string>();
        public string fingerprint;
    }

    private sealed class Frame
    {
        internal string path;
        internal int number;
        internal string format;
    }

    /// <summary>从已绑定图片推断目录和动作前缀，不使用预制体中历史遗留的导入参数。</summary>
    public static Settings Infer(PieceDisplay target)
    {
        var settings = new Settings();
        var sprite = target.idleSprite?.FirstOrDefault(s => s != null);
        string path = AssetDatabase.GetAssetPath(sprite);
        settings.folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
        settings.prefix = Regex.Replace(Path.GetFileNameWithoutExtension(path), "idle.*$", "", RegexOptions.IgnoreCase);
        for (int i = 1; i < 7; i++)
        {
            path = AssetDatabase.GetAssetPath(GetFrames(target, i)?.FirstOrDefault(s => s != null));
            var match = Regex.Match(Path.GetFileNameWithoutExtension(path), "^(.*?)(?:" +
                (i == 1 ? "move|run" : Actions[i]) + ")", RegexOptions.IgnoreCase);
            if (match.Success && !string.Equals(match.Groups[1].Value, settings.prefix, StringComparison.OrdinalIgnoreCase))
                settings.prefixOverrides[i] = match.Groups[1].Value;
        }
        return settings;
    }

    /// <summary>扫描目录内文件，支持旧技能分隔编号、新四位帧号和数字排序；不修改资源。</summary>
    public static Plan Scan(PieceDisplay target, Settings settings)
    {
        if (target == null) throw new ArgumentException("请先指定 PieceDisplay。");
        if (settings == null || string.IsNullOrWhiteSpace(settings.prefix) ||
            string.IsNullOrEmpty(settings.folder) || !settings.folder.StartsWith("Assets/", StringComparison.Ordinal) ||
            settings.folder.Split('/').Any(p => p == "..") || !AssetDatabase.IsValidFolder(settings.folder))
            throw new ArgumentException("请指定 Assets 下的有效素材目录和角色前缀。");
        var plan = new Plan { target = target, settings = JsonUtility.FromJson<Settings>(JsonUtility.ToJson(settings)) };
        string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { settings.folder })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => string.Equals(Path.GetDirectoryName(p)?.Replace('\\', '/'), settings.folder, StringComparison.Ordinal))
            .Where(p => string.Equals(Path.GetExtension(p), ".png", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        var recognized = new HashSet<string>();
        for (int action = 0; action < Actions.Length; action++)
        {
            string prefix = settings.prefixOverrides != null && action < settings.prefixOverrides.Length &&
                !string.IsNullOrWhiteSpace(settings.prefixOverrides[action]) ? settings.prefixOverrides[action] : settings.prefix;
            var groups = new SortedDictionary<int, List<Frame>>();
            foreach (string path in paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (!TryParseName(name, prefix, action, out int group, out int number, out string format)) continue;
                recognized.Add(path);
                if (!groups.ContainsKey(group)) groups[group] = new List<Frame>();
                groups[group].Add(new Frame { path = path, number = number, format = format });
            }
            if (action != 7 && groups.Count == 0) groups[-1] = new List<Frame>();
            if (action == 7)
            {
                int count = Math.Max(target.skillSpriteList?.Count ?? 0, groups.Count == 0 ? 0 : groups.Keys.Max() + 1);
                for (int group = 0; group < count; group++)
                    if (!groups.ContainsKey(group)) groups[group] = new List<Frame>();
            }
            foreach (var group in groups)
            {
                var row = new Row { action = action, skillIndex = group.Key,
                    label = Labels[action] + (action == 7 ? " " + (group.Key + 1) + " / index " + group.Key : ""),
                    before = action == 7 ? GetSkill(target, group.Key) : GetFrames(target, action) };
                var ordered = group.Value.OrderBy(f => f.number).ThenBy(f => f.path, StringComparer.Ordinal).ToList();
                if (ordered.GroupBy(f => f.number).Any(g => g.Count() > 1) || ordered.Select(f => f.format).Distinct().Count() > 1)
                {
                    row.blocked = true;
                    row.notes.Add("存在重复编号或混用序列，请整理素材后重新扫描。");
                }
                // 无编号首帧接 2、3 的旧资源以 1 为起点；接 0001 的新资源以 0 为起点。
                if (ordered.Count > 1 && ordered[0].number == 0 && ordered[1].number == 2 &&
                    !char.IsDigit(Path.GetFileNameWithoutExtension(ordered[0].path).Last())) ordered[0].number = 1;
                for (int i = 1; i < ordered.Count; i++)
                    if (ordered[i].number > ordered[i - 1].number + 1) row.notes.Add("编号断档：" + ordered[i - 1].number + " → " + ordered[i].number);
                foreach (var frame in ordered)
                {
                    var importer = AssetImporter.GetAtPath(frame.path) as TextureImporter;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(frame.path);
                    if (importer == null || importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || sprite == null)
                    {
                        row.blocked = true;
                        row.notes.Add("需要 Sprite / Single：" + Path.GetFileName(frame.path));
                        plan.textureIssues.Add(frame.path);
                    }
                    else row.frames.Add(sprite);
                }
                if (row.frames.Select(s => s.pixelsPerUnit).Distinct().Count() > 1)
                    row.notes.Add("帧间 PPU 不一致，播放时可能改变角色尺寸；请在纹理 Inspector 中核对。");
                if (row.frames.Select(s => new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height)).Distinct().Count() > 1)
                    row.notes.Add("帧间归一化轴心不一致，播放时可能抖动；请在纹理 Inspector 中核对。");
                if (row.before != null && row.before.Any(s => s == null)) row.notes.Add("原绑定含失效引用。");
                if (ordered.Count == 0 || action == 0 && settings.preserveIdle)
                {
                    row.frames = row.before == null ? new List<Sprite>() : new List<Sprite>(row.before);
                    row.selected = false;
                    row.blocked = false;
                    row.notes.Add(ordered.Count == 0 ? "未找到素材，保留原绑定。" : "保留原待机编排；取消上方选项可重建。");
                }
                plan.rows.Add(row);
            }
        }
        foreach (string path in paths)
            if (!recognized.Contains(path)) plan.ignored.Add(Path.GetFileName(path));
        plan.fingerprint = Fingerprint(plan, paths);
        return plan;
    }

    /// <summary>只写入已勾选的有效动作；重新校验扫描快照，支持撤销并保存资源或标记场景待保存。</summary>
    public static int Apply(Plan plan)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请退出运行模式后导入。");
        if (plan == null || plan.target == null) throw new ArgumentException("请先扫描。");
        var fresh = Scan(plan.target, plan.settings);
        if (fresh.fingerprint != plan.fingerprint) throw new InvalidOperationException("素材或目标绑定已变化，请重新扫描。");
        if (plan.rows.Any(r => r.selected && r.blocked)) throw new InvalidOperationException("已勾选动作存在歧义或纹理错误。");
        var changes = plan.rows.Where(r => r.selected && r.Changed && !r.blocked).ToList();
        if (changes.Count == 0) return 0;
        var target = plan.target;
        if (PrefabUtility.IsPartOfPrefabInstance(target) && !EditorUtility.IsPersistent(target))
            throw new InvalidOperationException("请打开源预制体后导入，避免 Odin 嵌套技能列表成为不可保存的实例覆盖。");
        string assetPath = EditorUtility.IsPersistent(target) ? AssetDatabase.GetAssetPath(target) : null;
        string originalPrefab = string.IsNullOrEmpty(assetPath) ? null : File.ReadAllText(assetPath);
        string componentPattern = null;
        if (originalPrefab != null)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string _, out long fileId);
            componentPattern = @"(?m)^--- !u!114 &" + fileId + @"[^\r\n]*\r?\n[\s\S]*?(?=^--- !u!|\z)";
            if (!Regex.IsMatch(originalPrefab, componentPattern))
                throw new InvalidOperationException("请将预制体保存为文本序列化格式后再导入。");
        }
        ((ISerializationCallbackReceiver)target).OnBeforeSerialize();
        Undo.RegisterCompleteObjectUndo(target, "导入角色动画序列帧");
        foreach (var row in changes)
        {
            var frames = new List<Sprite>(row.frames);
            switch (row.action)
            {
                case 0: target.idleSprite = frames; break;
                case 1: target.moveSprite = frames; break;
                case 2: target.meleeSprites = frames; break;
                case 3: target.rangeSprites = frames; break;
                case 4: target.dodgeSprite = frames; break;
                case 5: target.hitSprite = frames; break;
                case 6: target.deathSprites = frames; break;
                case 7:
                    if (target.skillSpriteList == null) target.skillSpriteList = new List<List<Sprite>>();
                    // 缺失技能组留空位，保持 SkillPack.animationIndex 的原有含义。
                    while (target.skillSpriteList.Count <= row.skillIndex) target.skillSpriteList.Add(new List<Sprite>());
                    target.skillSpriteList[row.skillIndex] = frames;
                    break;
            }
        }
        ((ISerializationCallbackReceiver)target).OnBeforeSerialize();
        EditorUtility.SetDirty(target);
        if (EditorUtility.IsPersistent(target))
        {
            // 让 Unity 生成 Odin 数据，再只回填本组件，避免其他组件的 OnValidate 改写旧预制体。
            PrefabUtility.SavePrefabAsset(target.transform.root.gameObject);
            try
            {
                string generated = File.ReadAllText(assetPath);
                var block = Regex.Match(generated, componentPattern);
                if (!block.Success) throw new InvalidOperationException("Unity 保存后无法定位动画组件。");
                string newline = originalPrefab.Contains("\r\n") ? "\r\n" : "\n";
                string component = Regex.Replace(block.Value.Replace("\r\n", "\n"), @"[ \t]+$", "", RegexOptions.Multiline).Replace("\n", newline);
                File.WriteAllText(assetPath, Regex.Replace(originalPrefab, componentPattern, _ => component), new System.Text.UTF8Encoding(false));
            }
            catch
            {
                File.WriteAllText(assetPath, originalPrefab, new System.Text.UTF8Encoding(false));
                throw;
            }
            finally { AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate); }
        }
        else
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene == target.gameObject.scene)
                PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
            else EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        }
        return changes.Count;
    }

    /// <summary>仅将扫描中无法读取的 PNG 修复为单张 Sprite，保留 PPU、轴心及滤镜设置。</summary>
    public static void RepairTextures(Plan plan)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请退出运行模式后修复。");
        foreach (string path in plan.textureIssues.Distinct())
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            Undo.RecordObject(importer, "修复角色 Sprite 导入类型");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }

    private static bool TryParseName(string name, string prefix, int action, out int group, out int number, out string format)
    {
        group = -1;
        number = 0;
        format = Actions[action];
        string token = action == 1 ? "(?:move|run)" : Actions[action];
        string head = "^" + Regex.Escape(prefix) + token;
        var match = Regex.Match(name, head + (action == 7 ? @"(?:(\d+)-(\d+)|(\d+)(\d{4}))$" : @"(\d*)$"), RegexOptions.IgnoreCase);
        if (!match.Success) return false;
        if (action == 7)
        {
            bool legacy = match.Groups[1].Success;
            if (!int.TryParse(match.Groups[legacy ? 1 : 3].Value, out int skill) || skill < 1 || skill > 100 ||
                !int.TryParse(match.Groups[legacy ? 2 : 4].Value, out number)) return false;
            group = skill - 1;
            format = legacy ? "分隔编号" : "四位帧号";
        }
        else
        {
            string digits = match.Groups[1].Value;
            if (!int.TryParse(digits.Length == 0 ? "0" : digits, out number)) return false;
            if (action == 1 && name.Substring(prefix.Length).StartsWith("run", StringComparison.OrdinalIgnoreCase)) format = "run";
        }
        return true;
    }

    private static string Fingerprint(Plan plan, string[] paths)
    {
        string value = JsonUtility.ToJson(plan.settings) + string.Join("|", paths.Select(p => p + AssetDatabase.GetAssetDependencyHash(p))) +
            string.Join("|", plan.rows.Select(r => r.label + ":" + string.Join(",", (r.before ?? new List<Sprite>()).Select(s => s == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(s).ToString()))));
        return Hash128.Compute(value).ToString();
    }

    private static bool Same(List<Sprite> a, List<Sprite> b) => (a ?? new List<Sprite>()).SequenceEqual(b ?? new List<Sprite>());
    private static List<Sprite> GetSkill(PieceDisplay target, int index) => target.skillSpriteList != null && index < target.skillSpriteList.Count ? target.skillSpriteList[index] : null;
    private static List<Sprite> GetFrames(PieceDisplay target, int action)
    {
        switch (action)
        {
            case 0: return target.idleSprite;
            case 1: return target.moveSprite;
            case 2: return target.meleeSprites;
            case 3: return target.rangeSprites;
            case 4: return target.dodgeSprite;
            case 5: return target.hitSprite;
            case 6: return target.deathSprites;
            default: return null;
        }
    }
}
