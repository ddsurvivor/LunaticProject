using System;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>为单个角色预览、选择及导入序列帧；配置按目标保存在本机编辑器偏好中。</summary>
public class CharacterAnimationImportWindow : EditorWindow
{
    [SerializeField] private PieceDisplay targetDisplay;
    [SerializeField] private CharacterAnimationImporter.Settings settings;
    private CharacterAnimationImporter.Plan plan;
    private Vector2 scroll;
    private string message;
    private bool showOverrides;
    private bool showIgnored;
    private int previewRow;
    private int previewFrame;
    private bool playing;
    private double previewStart;
    private string PreferenceKey => "Lunatic.CharacterAnimationImport." + Application.dataPath + "." + GlobalObjectId.GetGlobalObjectIdSlow(targetDisplay);

    /// <summary>打开角色动画工具；选中棋子时自动定位 PieceDisplay。</summary>
    [MenuItem("Tools/角色动画/序列帧导入")]
    public static void Open()
    {
        ShowFor(Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInChildren<PieceDisplay>(true));
    }

    /// <summary>从 Inspector 打开指定角色，复用本机上次导入设置。</summary>
    public static CharacterAnimationImportWindow ShowFor(PieceDisplay target)
    {
        var window = GetWindow<CharacterAnimationImportWindow>("角色动画导入");
        window.minSize = new Vector2(720, 640);
        window.SetTarget(target);
        window.Show();
        return window;
    }

    private void OnEnable()
    {
        EditorApplication.update += Tick;
        Undo.undoRedoPerformed += OnUndo;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        Undo.undoRedoPerformed -= OnUndo;
    }

    private void Tick()
    {
        if (playing) Repaint();
    }

    private void OnUndo()
    {
        plan = null;
        playing = false;
        Repaint();
    }

    private void SetTarget(PieceDisplay target)
    {
        targetDisplay = target;
        settings = null;
        plan = null;
        message = null;
        playing = false;
        if (target == null) return;
        settings = EditorPrefs.HasKey(PreferenceKey)
            ? JsonUtility.FromJson<CharacterAnimationImporter.Settings>(EditorPrefs.GetString(PreferenceKey))
            : CharacterAnimationImporter.Infer(target);
        if (settings.prefixOverrides == null || settings.prefixOverrides.Length != 8) settings.prefixOverrides = new string[8];
    }

    private void Scan()
    {
        plan = CharacterAnimationImporter.Scan(targetDisplay, settings);
        EditorPrefs.SetString(PreferenceKey, JsonUtility.ToJson(settings));
        previewRow = Mathf.Clamp(previewRow, 0, Math.Max(0, plan.rows.Count - 1));
        previewFrame = 0;
        playing = false;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("角色动画序列帧", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("扫描素材 → 核对变化 → 导入保存", EditorStyles.miniLabel);
        var next = (PieceDisplay)EditorGUILayout.ObjectField("目标棋子", targetDisplay, typeof(PieceDisplay), true);
        if (next != targetDisplay) SetTarget(next);
        if (targetDisplay == null || settings == null) return;
        EditorGUI.BeginChangeCheck();
        var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(settings.folder ?? "");
        var nextFolder = (DefaultAsset)EditorGUILayout.ObjectField("素材文件夹", folder, typeof(DefaultAsset), false);
        if (nextFolder != folder) settings.folder = AssetDatabase.GetAssetPath(nextFolder);
        settings.prefix = EditorGUILayout.TextField("角色前缀", settings.prefix);
        settings.preserveIdle = EditorGUILayout.Toggle("保留原待机编排", settings.preserveIdle);
        showOverrides = EditorGUILayout.Foldout(showOverrides, "按动作覆盖前缀（移动同时识别 move / run）", true);
        if (showOverrides)
            for (int i = 0; i < 8; i++) settings.prefixOverrides[i] = EditorGUILayout.TextField(CharacterAnimationImporter.Labels[i], settings.prefixOverrides[i]);
        if (EditorGUI.EndChangeCheck()) { plan = null; playing = false; message = null; }
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("扫描预览", GUILayout.Height(28))) Run(() => { Scan(); message = "扫描完成；缺失动作保留，异常动作需处理或取消勾选。"; });
            bool blocked = plan != null && plan.rows.Any(r => r.selected && r.blocked);
            using (new EditorGUI.DisabledScope(blocked))
                if (GUILayout.Button("一键导入并保存", GUILayout.Height(28))) Run(() =>
                {
                    if (plan == null) Scan();
                    int count = CharacterAnimationImporter.Apply(plan);
                    Scan();
                    message = count == 0 ? "绑定已是最新，无需修改。" : "已更新 " + count + " 个动作；资源已保存，场景对象请保存场景。";
                });
            if (GUILayout.Button("从当前绑定推断", GUILayout.Width(130), GUILayout.Height(28)))
            { settings = CharacterAnimationImporter.Infer(targetDisplay); plan = null; playing = false; }
        }
        EditorGUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        if (plan == null) return;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var row in plan.rows)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            row.selected = EditorGUILayout.Toggle(row.selected, GUILayout.Width(18));
            GUILayout.Label(row.label, EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label((row.before?.Count ?? 0) + " → " + row.frames.Count + " 帧", GUILayout.Width(100));
            GUILayout.Label(row.blocked ? "需处理" : row.Changed ? "待更新" : "保持", GUILayout.Width(65));
            if (GUILayout.Button("查看序列", GUILayout.Width(85))) { previewRow = plan.rows.IndexOf(row); previewFrame = 0; playing = false; }
            EditorGUILayout.EndHorizontal();
            if (row.frames.Count > 0)
            {
                EditorGUILayout.LabelField("首帧 / 末帧", row.frames.First()?.name + "  /  " + row.frames.Last()?.name, EditorStyles.miniLabel);
            }
            foreach (string note in row.notes) EditorGUILayout.LabelField(note, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }
        if (plan.rows.Count > 0) DrawPreview();
        if (plan.textureIssues.Count > 0 && GUILayout.Button("修复异常 PNG 为 Sprite / Single 并重新扫描"))
            Run(() => { CharacterAnimationImporter.RepairTextures(plan); Scan(); });
        showIgnored = EditorGUILayout.Foldout(showIgnored, "未采用文件（" + plan.ignored.Count + "）：副本、B 版或命名不匹配", true);
        if (showIgnored) foreach (string file in plan.ignored) EditorGUILayout.LabelField(file, EditorStyles.miniLabel);
        EditorGUILayout.HelpBox("普通动作 6 帧/秒，移动 12 帧/秒；闪避共 1 秒。攻击在序列中点出手，增加帧数会延长动作。", MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    private void DrawPreview()
    {
        previewRow = Mathf.Clamp(previewRow, 0, plan.rows.Count - 1);
        var row = plan.rows[previewRow];
        if (row.frames.Count == 0) return;
        float fps = row.action == 1 ? 12f : row.action == 4 ? row.frames.Count : 6f;
        if (playing) previewFrame = (int)((EditorApplication.timeSinceStartup - previewStart) * fps) % row.frames.Count;
        previewFrame = Mathf.Clamp(previewFrame, 0, row.frames.Count - 1);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(row.label + "  " + (previewFrame + 1) + " / " + row.frames.Count, EditorStyles.boldLabel);
        if (GUILayout.Button(playing ? "暂停预览" : "播放预览", GUILayout.Width(100)))
        { playing = !playing; previewStart = EditorApplication.timeSinceStartup - previewFrame / fps; }
        EditorGUILayout.EndHorizontal();
        previewFrame = EditorGUILayout.IntSlider(previewFrame, 0, row.frames.Count - 1);
        var sprite = row.frames[previewFrame];
        if (sprite == null) return;
        Rect rect = GUILayoutUtility.GetRect(100, 180, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.14f, 0.16f, 0.19f));
        float scale = Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
        var fitted = new Rect(rect.center.x - sprite.rect.width * scale / 2, rect.center.y - sprite.rect.height * scale / 2,
            sprite.rect.width * scale, sprite.rect.height * scale);
        Rect uv = sprite.rect;
        uv.x /= sprite.texture.width; uv.width /= sprite.texture.width;
        uv.y /= sprite.texture.height; uv.height /= sprite.texture.height;
        GUI.DrawTextureWithTexCoords(fitted, sprite.texture, uv, true);
        EditorGUILayout.LabelField(sprite.name, EditorStyles.centeredGreyMiniLabel);
    }

    private void Run(Action action)
    {
        try { action(); }
        catch (Exception exception) { message = exception.Message; Debug.LogWarning("角色动画导入：" + exception.Message, targetDisplay); }
        Repaint();
    }
}

[CustomEditor(typeof(PieceDisplay))]
public class PieceDisplayEditor : OdinEditor
{
    /// <summary>保留 Odin 嵌套技能列表的编辑能力，并提供序列帧导入入口。</summary>
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("打开角色动画导入")) CharacterAnimationImportWindow.ShowFor((PieceDisplay)target);
        base.OnInspectorGUI();
    }
}
