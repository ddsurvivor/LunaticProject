using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillReleaseTimingWindow : EditorWindow
{
    [SerializeField] private PieceDisplay targetDisplay;
    [SerializeField] private SkillPackListSO library;
    [SerializeField] private PieceDisplayState action = PieceDisplayState.Skill;
    [SerializeField] private int animationIndex;
    [SerializeField] private string skillId = "";
    [SerializeField] private bool skillOverride;
    private ActionReleaseTiming draft; // 仅保存按钮提交草稿，预览不会改动源资产。
    private PieceDisplay preview;
    private PieceDisplay.Playback playback;
    private bool playing, dirty, loop = true;
    private float elapsed, speed = 1f, interval = 0.4f, wait;
    private double lastTick, flashUntil;
    private Slider timeline;
    private Label status, positionLabel;
    private PopupField<string> skills;
    private EnumField modeField;
    private IntegerField frameField;
    private FloatField secondsField;
    private Button saveButton;
    private List<SkillPack> choices = new();

    /// <summary>从菜单打开调节器，并尝试定位当前选中的棋子。</summary>
    [MenuItem("Tools/角色动画/技能出手调节")]
    public static void Open() => ShowFor(Selection.activeGameObject == null ? null :
        Selection.activeGameObject.GetComponentInChildren<PieceDisplay>(true));

    /// <summary>打开角色出手调节器，使用独立精灵对象在场景中预览，不运行棋子战斗逻辑。</summary>
    public static void ShowFor(PieceDisplay target)
    {
        var window = GetWindow<SkillReleaseTimingWindow>("技能出手调节");
        window.ChangeTarget(target);
        window.minSize = new Vector2(480, 520);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Undo.undoRedoPerformed += OnUndo;
        SceneView.duringSceneGui += DrawSceneMarker;
        AssemblyReloadEvents.beforeAssemblyReload += DestroyPreview;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Undo.undoRedoPerformed -= OnUndo;
        SceneView.duringSceneGui -= DrawSceneMarker;
        AssemblyReloadEvents.beforeAssemblyReload -= DestroyPreview;
        DestroyPreview();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        DestroyPreview();
        if (saveButton != null) saveButton.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
    }

    private bool CanDiscard() => !dirty || EditorUtility.DisplayDialog("未保存的出手设置",
        "切换会丢弃当前预览草稿，是否继续？", "丢弃并继续", "留在当前设置");

    private void ChangeTarget(PieceDisplay next)
    {
        if (next == targetDisplay && draft != null) return;
        if (!CanDiscard()) return;
        DestroyPreview();
        targetDisplay = next;
        skillId = ""; skillOverride = false;
        var data = ActorData();
        if (data?.skillLibrary != null) library = data.skillLibrary;
        LoadDraft();
        if (rootVisualElement != null) CreateGUI();
    }

    /// <summary>构建出手配置与场景预览控制界面，输入值仅作用于草稿。</summary>
    public void CreateGUI()
    {
        rootVisualElement.Clear();
        var root = new ScrollView();
        root.style.paddingLeft = root.style.paddingRight = 12;
        root.style.paddingTop = 10;
        rootVisualElement.Add(root);
        root.Add(new HelpBox("12 FPS · 场景精灵预览 · 单次动作一个出手点。保存到所选 PieceDisplay；场景实例的修改作为实例覆盖保存，应用至预制体请使用 Inspector Overrides。", HelpBoxMessageType.Info));
        var target = new ObjectField("角色 PieceDisplay") { objectType = typeof(PieceDisplay), allowSceneObjects = true, value = targetDisplay };
        target.RegisterValueChangedCallback(e => { ChangeTarget(e.newValue as PieceDisplay); target.SetValueWithoutNotify(targetDisplay); });
        root.Add(target);
        if (library == null) library = AssetDatabase.LoadAssetAtPath<SkillPackListSO>("Assets/ScriptableObjects/SkillPackListSO.asset");
        var source = new ObjectField("技能库") { objectType = typeof(SkillPackListSO), value = library, allowSceneObjects = false };
        source.RegisterValueChangedCallback(e =>
        {
            if (!CanDiscard()) { source.SetValueWithoutNotify(library); return; }
            library = e.newValue as SkillPackListSO; skillId = ""; LoadDraft(); CreateGUI();
        });
        root.Add(source);
        choices = library?.skillPacks?.Where(s => s != null && !string.IsNullOrEmpty(s.skillId)).ToList() ?? new List<SkillPack>();
        var names = new List<string> { "仅调整动画默认" };
        names.AddRange(choices.Select(s => s.skillName + " [" + s.skillId + "]"));
        int selected = choices.FindIndex(s => s.skillId == skillId) + 1;
        skills = new PopupField<string>("预览技能", names, Mathf.Clamp(selected, 0, names.Count - 1));
        skills.RegisterValueChangedCallback(e =>
        {
            if (!CanDiscard()) { CreateGUI(); return; }
            int index = skills.index - 1;
            skillId = index >= 0 ? choices[index].skillId : "";
            if (index >= 0)
            {
                animationIndex = choices[index].animationIndex;
                var data = ActorData();
                action = skillId == data?.meleeSkillId ? PieceDisplayState.Attack :
                    skillId == data?.rangedSkillId ? PieceDisplayState.Shoot : PieceDisplayState.Skill;
            }
            skillOverride = index >= 0; LoadDraft(); CreateGUI();
        });
        root.Add(skills);
        root.Add(new HelpBox("主动技能选择 Skill；校准普通攻击时请选择 Attack（近战）或 Shoot（射击）。同一技能在不同动作入口的配置分别保存。", HelpBoxMessageType.None));
        var actions = new PopupField<string>("动作入口", new List<string> { "Attack", "Shoot", "Skill" }, action == PieceDisplayState.Attack ? 0 : action == PieceDisplayState.Shoot ? 1 : 2);
        actions.RegisterValueChangedCallback(e =>
        {
            if (!CanDiscard()) { CreateGUI(); return; }
            action = actions.index == 0 ? PieceDisplayState.Attack : actions.index == 1 ? PieceDisplayState.Shoot : PieceDisplayState.Skill;
            LoadDraft(); CreateGUI();
        });
        root.Add(actions);
        var group = new IntegerField("技能动画索引（从0开始）") { value = animationIndex };
        group.SetEnabled(action == PieceDisplayState.Skill);
        group.RegisterValueChangedCallback(e =>
        {
            if (!CanDiscard()) { group.SetValueWithoutNotify(animationIndex); return; }
            animationIndex = Mathf.Max(0, e.newValue); LoadDraft(); CreateGUI();
        });
        root.Add(group);
        var scope = new Toggle("仅覆盖所选技能") { value = skillOverride };
        scope.SetEnabled(!string.IsNullOrEmpty(skillId));
        scope.RegisterValueChangedCallback(e =>
        {
            if (!CanDiscard()) { scope.SetValueWithoutNotify(skillOverride); return; }
            skillOverride = e.newValue; LoadDraft(); CreateGUI();
        });
        root.Add(scope);
        if (draft == null) LoadDraft();
        modeField = new EnumField("设置方式", draft.mode);
        modeField.RegisterValueChangedCallback(e =>
        {
            float seconds = draft.GetSeconds(Count);
            draft.mode = (ActionReleaseMode)e.newValue;
            draft.seconds = seconds;
            draft.frame = Mathf.Clamp(Mathf.RoundToInt(seconds * PieceDisplay.FramesPerSecond) + 1, 1, Math.Max(1, Count));
            Changed();
        });
        root.Add(modeField);
        frameField = new IntegerField("出手帧（从1开始）") { value = draft.frame };
        frameField.RegisterValueChangedCallback(e => { draft.frame = Mathf.Clamp(e.newValue, 1, Math.Max(1, Count)); Changed(); });
        root.Add(frameField);
        secondsField = new FloatField("出手延迟（秒）") { value = draft.seconds };
        secondsField.RegisterValueChangedCallback(e =>
        {
            draft.seconds = e.newValue;
            draft.seconds = draft.GetSeconds(Count);
            Changed();
        });
        root.Add(secondsField);
        timeline = new Slider("时间轴（秒）", 0f, Mathf.Max(PieceDisplay.FrameDuration, Duration));
        timeline.RegisterValueChangedCallback(e => Seek(e.newValue));
        root.Add(timeline);
        positionLabel = new Label(); root.Add(positionLabel);
        var controls = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        AddButton(controls, "播放/暂停", TogglePlayback);
        AddButton(controls, "重播", () => { Seek(0); playing = true; lastTick = EditorApplication.timeSinceStartup; });
        AddButton(controls, "上一帧", () => Seek(Mathf.Max(0, Mathf.Round(elapsed * PieceDisplay.FramesPerSecond) - 1) * PieceDisplay.FrameDuration));
        AddButton(controls, "下一帧", () => Seek((Mathf.Floor(elapsed * PieceDisplay.FramesPerSecond) + 1) * PieceDisplay.FrameDuration));
        root.Add(controls);
        AddButton(root, "将当前帧设为出手帧", () =>
        {
            draft.mode = ActionReleaseMode.Frame;
            draft.frame = Mathf.Clamp(Mathf.FloorToInt(elapsed * PieceDisplay.FramesPerSecond) + 1, 1, Math.Max(1, Count));
            Changed();
        });
        var repeat = new Toggle("循环播放") { value = loop };
        repeat.RegisterValueChangedCallback(e => loop = e.newValue); root.Add(repeat);
        var rate = new Slider("预览速度", 0.1f, 2f) { value = speed, showInputField = true };
        rate.RegisterValueChangedCallback(e => speed = Mathf.Clamp(e.newValue, 0.1f, 2f)); root.Add(rate);
        var gap = new FloatField("循环间隔（秒）") { value = interval };
        gap.RegisterValueChangedCallback(e => { interval = float.IsNaN(e.newValue) || float.IsInfinity(e.newValue) ? 0f : Mathf.Max(0, e.newValue); gap.SetValueWithoutNotify(interval); }); root.Add(gap);
        AddButton(root, "在场景中定位预览", () =>
        {
            if (!EnsurePreview()) return;
            SceneView.lastActiveSceneView?.Frame(new Bounds(preview.transform.position, Vector3.one * 4), false);
        });
        saveButton = AddButton(root, "保存当前出手设置", () => Save(false));
        saveButton.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        AddButton(root, "删除当前覆盖，使用回退配置", () => Save(true));
        AddButton(root, "还原未保存调整", () => { LoadDraft(); CreateGUI(); });
        status = new Label { style = { whiteSpace = WhiteSpace.Normal, marginTop = 8 } }; root.Add(status);
        RefreshFields();
    }

    private int Count => targetDisplay == null ? 0 : targetDisplay.GetFrames(action, animationIndex)?.Count ?? 0;
    private float Duration => Count * PieceDisplay.FrameDuration;
    private string ScopeId => skillOverride ? skillId : "";

    private PieceData ActorData()
    {
        var actor = targetDisplay == null ? null : targetDisplay.GetComponentInParent<PieceController>();
        var data = AssetDatabase.LoadAssetAtPath<PieceDataListSO>("Assets/ScriptableObjects/PieceDataListSO.asset");
        return actor == null ? null : data?.pieceDataList?.Find(p => p != null && p.pieceId == actor.pieceID);
    }

    private void LoadDraft()
    {
        DestroyPreview(); dirty = false; elapsed = 0;
        var existing = targetDisplay == null ? null : targetDisplay.FindReleaseTiming(action, animationIndex, ScopeId);
        float fallback = targetDisplay == null ? 0 : targetDisplay.GetReleaseTime(action, animationIndex, ScopeId);
        draft = existing == null ? new ActionReleaseTiming { mode = ActionReleaseMode.Seconds, seconds = fallback,
                frame = Mathf.RoundToInt(fallback * PieceDisplay.FramesPerSecond) + 1 }
            : JsonUtility.FromJson<ActionReleaseTiming>(JsonUtility.ToJson(existing));
        draft.action = action; draft.animationIndex = animationIndex; draft.skillId = ScopeId;
    }

    private void Changed()
    {
        dirty = true;
        bool resume = playing;
        if (preview != null) { ConfigurePreview(); StartPreview(elapsed); }
        playing = resume;
        RefreshFields();
    }

    private void RefreshFields()
    {
        if (frameField == null || draft == null) return;
        float release = draft.GetSeconds(Count);
        frameField.SetValueWithoutNotify(draft.mode == ActionReleaseMode.Frame ? draft.frame : Mathf.FloorToInt(release * PieceDisplay.FramesPerSecond) + 1);
        secondsField.SetValueWithoutNotify(release);
        modeField.SetValueWithoutNotify(draft.mode);
        frameField.SetEnabled(draft.mode == ActionReleaseMode.Frame);
        secondsField.SetEnabled(draft.mode == ActionReleaseMode.Seconds);
        timeline.SetValueWithoutNotify(elapsed);
        positionLabel.text = $"{Count} 帧 · {Duration:F3}秒 · 当前第 {Mathf.Clamp(Mathf.FloorToInt(elapsed * PieceDisplay.FramesPerSecond) + 1, 1, Math.Max(1, Count))} 帧 · 出手 {release:F3}秒";
        status.text = Count == 0 ? "没有可播放的序列，请检查角色、动作入口和动画索引。" :
            (dirty ? "有未保存的调整。" : "草稿与当前配置一致。") + " 出手时场景显示黄色圆圈及文字；预览不扣费、不结算伤害。";
    }

    private bool EnsurePreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || targetDisplay == null || Count == 0) return false;
        if (preview != null) return true;
        // 只复制精灵表现，不实例化棋子、导航、音源或战斗生命周期组件。
        var go = new GameObject("技能出手预览") { hideFlags = HideFlags.HideAndDontSave };
        preview = go.AddComponent<PieceDisplay>();
        preview.pieceSpriteRenderer = go.AddComponent<SpriteRenderer>();
        var source = targetDisplay.pieceSpriteRenderer;
        go.transform.position = !EditorUtility.IsPersistent(targetDisplay) ?
            (source != null ? source.transform.position + Vector3.right * (source.bounds.size.x + 1f) : targetDisplay.transform.position + Vector3.right * 2f) :
            SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        if (source != null)
        {
            go.transform.rotation = source.transform.rotation;
            go.transform.localScale = source.transform.lossyScale;
            preview.pieceSpriteRenderer.sharedMaterial = source.sharedMaterial;
            preview.pieceSpriteRenderer.color = source.color;
            preview.pieceSpriteRenderer.flipX = source.flipX;
            preview.pieceSpriteRenderer.flipY = source.flipY;
            preview.pieceSpriteRenderer.sortingLayerID = source.sortingLayerID;
            preview.pieceSpriteRenderer.sortingOrder = source.sortingOrder + 1;
        }
        ConfigurePreview(); StartPreview(elapsed);
        return true;
    }

    private void ConfigurePreview()
    {
        preview.idleSprite = targetDisplay.idleSprite;
        preview.meleeSprites = targetDisplay.meleeSprites;
        preview.rangeSprites = targetDisplay.rangeSprites;
        preview.skillSpriteList = targetDisplay.skillSpriteList;
        preview.releaseTimings = new List<ActionReleaseTiming> { JsonUtility.FromJson<ActionReleaseTiming>(JsonUtility.ToJson(draft)) };
    }

    private void StartPreview(float time)
    {
        wait = 0;
        playback = preview.PlayAction(action, () => flashUntil = EditorApplication.timeSinceStartup + 0.25, animationIndex, skillId);
        elapsed = Mathf.Clamp(time, 0, Mathf.Max(0, Duration - PieceDisplay.FrameDuration));
        if (elapsed > 0) preview.AdvancePlayback(elapsed);
        lastTick = EditorApplication.timeSinceStartup;
    }

    private void TogglePlayback()
    {
        if (!EnsurePreview()) return;
        if (!playing && (playback == null || playback.IsDone)) StartPreview(0);
        playing = !playing; lastTick = EditorApplication.timeSinceStartup;
    }

    private void Seek(float time)
    {
        playing = false;
        if (!EnsurePreview()) return;
        StartPreview(time); RefreshFields(); SceneView.RepaintAll();
    }

    private void Tick()
    {
        if (!playing || preview == null) return;
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastTick) * speed; lastTick = now;
        if (playback.IsDone)
        {
            if (!loop) { playing = false; RefreshFields(); return; }
            wait += delta;
            if (wait >= interval) StartPreview(0);
        }
        else { elapsed = Mathf.Min(Duration, elapsed + delta); preview.AdvancePlayback(delta); }
        RefreshFields(); SceneView.RepaintAll();
    }

    private void DrawSceneMarker(SceneView view)
    {
        if (preview == null) return;
        Vector3 point = preview.transform.position;
        bool released = EditorApplication.timeSinceStartup < flashUntil;
        Handles.color = released ? Color.yellow : Color.gray;
        Handles.DrawWireDisc(point, view.camera.transform.forward, 0.4f);
        Handles.Label(point + Vector3.up * 0.6f, released ? "出手！" : "技能出手预览");
    }

    private void Save(bool remove)
    {
        if (targetDisplay == null || Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (skillOverride && string.IsNullOrEmpty(skillId)) return;
        // 由 Unity 序列化保存新增配置，保留原序列及 Odin 数据；场景实例只登记自身覆盖。
        Undo.RegisterCompleteObjectUndo(targetDisplay, "调整技能出手时机");
        var list = targetDisplay.releaseTimings ?? new List<ActionReleaseTiming>();
        list.RemoveAll(t => t != null && t.action == action &&
            (action != PieceDisplayState.Skill || t.animationIndex == animationIndex) && (t.skillId ?? "") == ScopeId);
        if (!remove)
        {
            draft.frame = Mathf.Clamp(draft.frame, 1, Math.Max(1, Count));
            draft.seconds = draft.GetSeconds(Count);
            list.Add(JsonUtility.FromJson<ActionReleaseTiming>(JsonUtility.ToJson(draft)));
        }
        targetDisplay.releaseTimings = list;
        EditorUtility.SetDirty(targetDisplay);
        if (EditorUtility.IsPersistent(targetDisplay)) PrefabUtility.SavePrefabAsset(targetDisplay.transform.root.gameObject);
        else
        {
            if (PrefabUtility.IsPartOfPrefabInstance(targetDisplay)) PrefabUtility.RecordPrefabInstancePropertyModifications(targetDisplay);
            EditorSceneManager.MarkSceneDirty(targetDisplay.gameObject.scene);
        }
        LoadDraft(); CreateGUI();
    }

    private void OnUndo() { LoadDraft(); CreateGUI(); }

    private void DestroyPreview()
    {
        playing = false; playback = null;
        if (preview != null) DestroyImmediate(preview.gameObject);
        preview = null;
    }

    private static Button AddButton(VisualElement root, string text, Action action)
    {
        var button = new Button(action) { text = text };
        root.Add(button); return button;
    }
}
