using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>敌我共用的头顶读数；只读取战斗状态，不接管行动或修改阵营。</summary>
public sealed class PieceOverheadStatusUI : MonoBehaviour
{
    public PieceController Piece { get; private set; }
    public IReadOnlyList<BattleStatusEntry> Entries => entries;
    public RectTransform Rect => (RectTransform)transform;
    private int headLife = -1;
    private float headHeight; // 每次棋子初始化固定一次头顶高度，不跟随序列帧边界变化。
    internal Vector3 HeadPosition
    {
        get
        {
            if (headHeightOverride <= 0 && Piece.pieceDisplay?.overheadAnchor != null)
                return Piece.pieceDisplay.overheadAnchor.position;
            if (headLife != Piece.BattleLifeVersion)
            {
                headLife = Piece.BattleLifeVersion;
                var sprite = Piece.pieceDisplay?.pieceSpriteRenderer;
                headHeight = headHeightOverride > 0 ? headHeightOverride : sprite != null
                    ? Mathf.Max(.1f, sprite.bounds.max.y - Piece.transform.position.y) : 2f;
            }
            return Piece.transform.position + Vector3.up * headHeight;
        }
    }
    private BattleOverheadStatusLayer layer;
    [SerializeField] private UnityEngine.UI.Text title, healthText, resources;
    [SerializeField] private UnityEngine.UI.Image bar, shadow, minimumPreview, maximumPreview;
    [SerializeField] private UnityEngine.UI.Image healthTrack, factionAccent;
    [SerializeField] private BattleHealthBarStyle style;
    [SerializeField] private bool applySharedStyle = true; // 关闭后允许 prefab 变体独立调整字体和面板外观。
    [SerializeField] private bool showFullHealth;
    [SerializeField, Min(0)] private float headHeightOverride; // 0优先使用棋子锚点，缺失时回退美术边界；护盾可独立指定高度。
    [SerializeField, Min(1)] private int maximumNameSize = 12;
    [SerializeField, Min(1)] private int minimumNameSize = 9;
    [SerializeField, Min(1)] private float emptyHeight = 32;
    [SerializeField, Min(1)] private float singleRowHeight = 58;
    [SerializeField, HideInInspector] private float doubleRowHeight = 58; // 保留旧模板字段兼容；所有状态始终只占一行。
    [SerializeField] private Vector2 headOffset = new Vector2(0, 24); // 屏幕像素偏移，实际跟随位置由展示层控制。
    [SerializeField] private bool constrainToScreen = true;
    [SerializeField] private bool avoidTopHud = true;
    [SerializeField, HideInInspector] private int compactLayoutVersion;
    /// <summary>模板配置的头顶屏幕像素偏移，不修改棋子坐标。</summary>
    public Vector2 HeadOffset => headOffset;
    /// <summary>是否将卡片保持在屏幕安全区内。</summary>
    public bool ConstrainToScreen => constrainToScreen;
    /// <summary>是否避让当前速度条及快进/设置区域。</summary>
    public bool AvoidTopHud => avoidTopHud;
#if UNITY_EDITOR
    private Sprite constructionFill;
#endif
    private string fullTitle; // 名称未变时不重复测量字体宽度。
    [SerializeField] private ActionPointPipsUI pips;
    private readonly HealthBarFillAnimation healthAnimation = new();
    [SerializeField] private List<BuffCell> cells = new();
    private List<BattleStatusEntry> entries = new();
    private int life = -1;
    private float nextStatusRead;
    public float Height { get; private set; }

    /// <summary>检查 prefab 必要绑定；缺失时拒绝使用，不在运行时重建或覆盖手动层级。</summary>
    public bool HasTemplateBindings => GetComponent<UnityEngine.UI.Image>() != null && title != null && healthText != null && resources != null &&
        bar != null && shadow != null && minimumPreview != null && maximumPreview != null &&
        healthTrack != null && factionAccent != null && pips != null &&
        cells.Count == 6 && cells.TrueForAll(cell => cell != null && cell.nameText != null && cell.countText != null &&
            cell.icon != null && cell.GetComponent<UnityEngine.UI.Button>() != null);

    internal bool Initialize(BattleOverheadStatusLayer owner, PieceController piece, Font font)
    {
        if (!HasTemplateBindings)
        {
            Debug.LogError("头顶状态 prefab 缺少必要绑定，请修复模板引用。", this);
            return false;
        }
        layer = owner;
        Piece = piece;
        life = headLife = -1;
        fullTitle = null;
        // 只绑定业务事件和共享外观；位置、宽度及子物体层级保留 prefab 的编辑值。
        if (applySharedStyle && style != null)
        {
            var background = GetComponent<UnityEngine.UI.Image>();
            float alpha = background.color.a;
            style.ApplyPanel(background);
            var color = background.color;
            color.a = alpha;
            background.color = color;
            foreach (var text in GetComponentsInChildren<UnityEngine.UI.Text>(true))
                style.ApplyText(text, text == resources);
        }
        foreach (var cell in cells)
        {
            var button = cell.GetComponent<UnityEngine.UI.Button>();
            button.onClick.RemoveListener(ShowDetails);
            button.onClick.AddListener(ShowDetails);
        }
        Hide();
        return true;
    }

    private void ShowDetails() => layer?.ShowDetails(this);

#if UNITY_EDITOR
    /// <summary>仅供编辑器首次生成模板；已有子层级时拒绝重建，避免覆盖手动修改。</summary>
    public void BuildTemplate(Font font, Sprite fillSprite, ActionPointPipsUI source)
    {
        if (transform.childCount != 0) throw new System.InvalidOperationException("已有头顶层级，不能重建覆盖。");
        Rect.anchorMin = Rect.anchorMax = Vector2.zero;
        Rect.pivot = new Vector2(.5f, 0);
        constructionFill = fillSprite;
        var background = gameObject.AddComponent<CompactOverheadFrame>();
        style = Resources.Load<BattleHealthBarStyle>("UI/BattleHealthBarStyle");
        background.color = GameConst.UIColors.Background;
        if (style != null) style.ApplyPanel(background);
        background.raycastTarget = false;
        factionAccent = Image("FactionAccent", 16, 8, 3, 18, GameConst.UIColors.Ally);
        title = Text("Title", 24, 5, 212, 22, 14, font);
        title.resizeTextForBestFit = false;
        title.resizeTextMinSize = 10;
        title.resizeTextMaxSize = 14;
        title.verticalOverflow = VerticalWrapMode.Truncate;
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.supportRichText = false;
        title.text = "[阵营] 角色名称";
        healthText = Text("Health", 16, 29, 220, 15, 11, font);
        healthText.text = "生命 100/100";
        healthTrack = Image("HealthTrack", 16, 47, 220, 7, GameConst.UIColors.SurfaceInset);
        shadow = Image("HealthShadow", 16, 47, 220, 7, Color.white);
        bar = Image("Health", 16, 47, 220, 7, GameConst.UIColors.Health);
        minimumPreview = Image("MinimumDamage", 16, 47, 220, 7, GameConst.UIColors.Accent);
        maximumPreview = Image("MaximumDamage", 16, 47, 220, 7, GameConst.UIColors.DamagePreview);
        foreach (var fill in new[] { shadow, bar, minimumPreview, maximumPreview })
        { fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; }
        minimumPreview.gameObject.SetActive(false);
        maximumPreview.gameObject.SetActive(false);
        var pointRoot = Box("ActionPoints", Rect, 16, 57, 220, 22);
        pointRoot.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        var seed = Image("Point", 0, 0, 8, 8, GameConst.UIColors.Ally, pointRoot);
        pips = pointRoot.gameObject.AddComponent<ActionPointPipsUI>();
        pips.Configure(seed, source != null ? source.FilledSprite : fillSprite, source != null ? source.EmptySprite : fillSprite);
        pips.SetPoints(3, 6);
        resources = Text("Resources", 28, 80, 208, 15, 11, font);
        resources.text = "AP 3/6 · 弹 4/6 · 能 10/10";
        if (style != null) style.ApplyText(resources, true);
        for (int i = 0; i < 6; i++)
        {
            var root = Box("Status" + i, Rect, 28 + i % 3 * 70, 102 + i / 3 * 34, 66, 32);
            var cell = root.gameObject.AddComponent<BuffCell>();
            var bg = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.color = GameConst.UIColors.Surface;
            cell.icon = Image("Icon", 2, 3, 13, 13, Color.white, root);
            cell.nameText = Text("Name", 17, 0, 47, 17, 11, font, root);
            cell.nameText.resizeTextForBestFit = true;
            cell.nameText.resizeTextMinSize = 8;
            cell.nameText.resizeTextMaxSize = 11;
            cell.countText = Text("Value", 17, 17, 47, 13, 10, font, root);
            root.gameObject.AddComponent<UnityEngine.UI.Button>();
            cells.Add(cell);
            root.gameObject.SetActive(false);
        }
        Rect.sizeDelta = new Vector2(244, emptyHeight);
        ApplyCompactTemplate();
    }

    /// <summary>编辑器一次性将旧模板收紧为两行读数和单行图标；版本标记防止再次覆盖手调。</summary>
    public void ApplyCompactTemplate()
    {
        if (compactLayoutVersion >= 1) return;
        emptyHeight = 32; singleRowHeight = doubleRowHeight = 58;
        showFullHealth = true;
        maximumNameSize = 12; minimumNameSize = 9;
        headOffset = new Vector2(0, 24);
        Rect.sizeDelta = new Vector2(200, emptyHeight);
        var background = GetComponent<UnityEngine.UI.Image>();
        var tint = background.color; tint.a = .7f; background.color = tint;
        Position(title.rectTransform, 8, 0, 96, 20);
        title.fontSize = 12;
        healthText.gameObject.SetActive(false);
        resources.gameObject.SetActive(false);
        factionAccent.gameObject.SetActive(false);
        foreach (var image in new[] { healthTrack, shadow, bar, minimumPreview, maximumPreview })
            Position(image.rectTransform, 8, 22, 184, 5);
        Position((RectTransform)pips.transform, 108, 4, 84, 12);
        var pointConfig = new UnityEditor.SerializedObject(pips);
        pointConfig.FindProperty("maximumPointSize").floatValue = 11;
        pointConfig.FindProperty("maximumPointSpacing").floatValue = 13.5f;
        pointConfig.ApplyModifiedPropertiesWithoutUndo();
        pips.ResetPresentation(); pips.SetPoints(3, 6);
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            Position((RectTransform)cell.transform, 8 + i * 28, 34, 22, 22);
            Position(cell.icon.rectTransform, 0, 0, 22, 22);
            cell.icon.preserveAspect = true;
            cell.nameText.gameObject.SetActive(false);
            Position(cell.countText.rectTransform, 9, 10, 13, 12);
            cell.countText.fontSize = 10;
            cell.countText.alignment = TextAnchor.LowerRight;
            var config = new UnityEditor.SerializedObject(cell);
            config.FindProperty("iconsOnly").boolValue = true;
            config.FindProperty("fallbackIcon").objectReferenceValue = pips.EmptySprite;
            config.ApplyModifiedPropertiesWithoutUndo();
        }
        compactLayoutVersion = 1;
    }

    private static void Position(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
#endif

    internal void Refresh(BattleManager battle)
    {
        if (Piece.BattleLifeVersion != life)
        {
            life = Piece.BattleLifeVersion;
            healthAnimation.Reset();
            pips.ResetPresentation();
            nextStatusRead = 0;
            layer.CloseDetails(this);
        }
        var attr = Piece.unitAttrCenter;
        bool enemy = battle.AIController.pieces.Contains(Piece) ||
            Piece is RangeBarrier barrier && battle.AIController.pieces.Contains(barrier.Emitter?.Owner);
        Color color = Piece.isPlayerPiece ? GameConst.UIColors.Ally : enemy ? GameConst.UIColors.Enemy : GameConst.UIColors.Accent;
        UpdateTitle(Piece.pieceData?.pieceName ?? Piece.name);
        factionAccent.color = color;
        healthAnimation.Update(bar, shadow, attr.MaxHealth > 0 ? (float)attr.CurHealth / attr.MaxHealth : 0f, Piece);
        pips.SetPoints(Mathf.Min(24, attr.CurMovePoint), Mathf.Min(24, attr.MaxMovePoint));
        var old = Piece is EnemyController controller ? controller.enemyCanvas?.hpBarUI : attr.hpBarUI;
        bool preview = old != null && old.HasDamagePreview;
        var shieldPreview = (Piece as RangeBarrier)?.ReadPreview();
        preview |= shieldPreview != null;
        bool showHealth = showFullHealth || attr.CurHealth < attr.MaxHealth || preview; // 护盾变体可常显完整耐久。
        // 仅隐藏图形，保留满血时的动画快照，首次受伤仍能从原血量播放拖影。
        healthTrack.enabled = showHealth;
        bar.enabled = showHealth;
        shadow.enabled = showHealth;
        minimumPreview.gameObject.SetActive(preview);
        maximumPreview.gameObject.SetActive(preview);
        if (shieldPreview != null)
        {
            minimumPreview.fillAmount = Mathf.Clamp01((attr.CurHealth - shieldPreview.Total.MinDamage) / (float)attr.MaxHealth);
            maximumPreview.fillAmount = Mathf.Clamp01((attr.CurHealth - shieldPreview.Total.MaxDamage) / (float)attr.MaxHealth);
        }
        else if (preview) { minimumPreview.fillAmount = old.PreviewAfterMinimum; maximumPreview.fillAmount = old.PreviewAfterMaximum; }
        if (Time.unscaledTime < nextStatusRead) return;
        nextStatusRead = Time.unscaledTime + .1f;
        entries = BattleStatusReadModel.Read(Piece, battle);
        int shown = Mathf.Min(6, entries.Count);
        Height = entries.Count == 0 ? emptyHeight : singleRowHeight;
        Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].gameObject.SetActive(i < shown);
            if (i >= shown) continue;
            var entry = entries.Count > 6 && i == 5
                ? new BattleStatusEntry("更多状态", "+" + (entries.Count - 5), "点击查看全部状态") : entries[i];
            cells[i].SetEntry(entry);
        }
    }

    private void UpdateTitle(string value)
    {
        if (fullTitle == value) return;
        fullTitle = value;
        var settings = title.GetGenerationSettings(Vector2.zero);
        settings.resizeTextForBestFit = false;
        settings.fontSize = Mathf.Max(1, maximumNameSize);
        while (settings.fontSize > Mathf.Clamp(minimumNameSize, 1, maximumNameSize) &&
            title.cachedTextGeneratorForLayout.GetPreferredWidth(value, settings) / title.pixelsPerUnit > title.rectTransform.rect.width)
            settings.fontSize--;
        title.fontSize = settings.fontSize;
        string shown = value;
        var boundaries = StringInfo.ParseCombiningCharacters(value);
        int count = boundaries.Length;
        // 最小字号仍容不下时按完整字符省略，避免长姓名换行后只剩阵营标签。
        while (count > 1 && title.cachedTextGeneratorForLayout.GetPreferredWidth(shown, settings) / title.pixelsPerUnit > title.rectTransform.rect.width)
            shown = value.Substring(0, boundaries[--count]) + "…";
        title.text = shown;
    }

    internal void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
        layer?.CloseDetails(this);
    }
    private void OnDisable() { healthAnimation.Reset(); if (pips != null) pips.ResetPresentation(); }

    internal static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }
#if UNITY_EDITOR
    private UnityEngine.UI.Image Image(string name, float x, float y, float width, float height, Color color, Transform parent = null)
    {
        var image = Box(name, parent != null ? parent : Rect, x, y, width, height).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = constructionFill;
        image.color = color; image.raycastTarget = false; return image;
    }
    private UnityEngine.UI.Text Text(string name, float x, float y, float width, float height, int size, Font font, Transform parent = null)
    {
        var text = Box(name, parent != null ? parent : Rect, x, y, width, height).gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font = font; text.fontSize = size; text.color = Color.white; text.raycastTarget = false;
        if (style != null) style.ApplyText(text);
        text.alignment = TextAnchor.MiddleLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
#endif
}
