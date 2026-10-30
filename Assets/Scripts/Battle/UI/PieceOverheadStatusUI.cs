using System.Collections.Generic;
using UnityEngine;

/// <summary>敌我共用的头顶读数；只读取战斗状态，不接管行动或修改阵营。</summary>
public sealed class PieceOverheadStatusUI : MonoBehaviour
{
    public PieceController Piece { get; private set; }
    public IReadOnlyList<BattleStatusEntry> Entries => entries;
    public RectTransform Rect => (RectTransform)transform;
    internal UnityEngine.UI.Image Link;
    private BattleOverheadStatusLayer layer;
    private UnityEngine.UI.Text title, healthText, resources;
    private UnityEngine.UI.Image bar, shadow, minimumPreview, maximumPreview;
    private ActionPointPipsUI pips;
    private readonly HealthBarFillAnimation healthAnimation = new();
    private readonly List<BuffCell> cells = new();
    private List<BattleStatusEntry> entries = new();
    private int life = -1;
    private float nextStatusRead;
    internal const float Width = 244;
    public float Height { get; private set; }

    internal void Initialize(BattleOverheadStatusLayer owner, PieceController piece, Font font)
    {
        layer = owner;
        Piece = piece;
        Rect.anchorMin = Rect.anchorMax = Vector2.zero;
        Rect.pivot = new Vector2(.5f, 0);
        var background = gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(.04f, .07f, .1f, .9f);
        background.raycastTarget = false;
        title = Text("Title", 7, 2, 230, 19, 14, font);
        healthText = Text("Health", 7, 23, 230, 14, 11, font);
        Image("HealthTrack", 7, 39, 230, 6, new Color(.22f, .24f, .28f));
        shadow = Image("HealthShadow", 7, 39, 230, 6, Color.white);
        bar = Image("Health", 7, 39, 230, 6, GameConst.UIColors.Ally);
        minimumPreview = Image("MinimumDamage", 7, 39, 230, 6, new Color(1f, .75f, .2f));
        maximumPreview = Image("MaximumDamage", 7, 39, 230, 6, new Color(1f, .22f, .18f));
        foreach (var fill in new[] { shadow, bar, minimumPreview, maximumPreview })
        { fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; }
        minimumPreview.gameObject.SetActive(false);
        maximumPreview.gameObject.SetActive(false);
        var pointRoot = Box("ActionPoints", Rect, 7, 49, 230, 8);
        var seed = Image("Point", 0, 0, 8, 8, GameConst.UIColors.Ally, pointRoot);
        pips = pointRoot.gameObject.AddComponent<ActionPointPipsUI>();
        var source = BattleScene.Ins.UM.pieceInfoPanel.GetComponentInChildren<ActionPointPipsUI>(true);
        pips.Configure(seed, source != null ? source.FilledSprite : owner.FillSprite, source != null ? source.EmptySprite : owner.FillSprite);
        resources = Text("Resources", 7, 60, 230, 15, 11, font);
        for (int i = 0; i < 6; i++)
        {
            var root = Box("Status" + i, Rect, 7 + i % 3 * 77, 80 + i / 3 * 34, 74, 32);
            var cell = root.gameObject.AddComponent<BuffCell>();
            var bg = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(.16f, .21f, .26f);
            cell.icon = Image("Icon", 2, 3, 13, 13, Color.white, root);
            cell.nameText = Text("Name", 17, 0, 55, 17, 11, font, root);
            cell.nameText.resizeTextForBestFit = true;
            cell.nameText.resizeTextMinSize = 8;
            cell.nameText.resizeTextMaxSize = 11;
            cell.countText = Text("Value", 17, 17, 55, 13, 10, font, root);
            root.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(() => layer.ShowDetails(this));
            cells.Add(cell);
        }
        Hide();
    }

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
        bool enemy = battle.AIController.pieces.Contains(Piece);
        string faction = Piece.isPlayerPiece ? "我方" : enemy ? "敌方" : "中立";
        Color color = Piece.isPlayerPiece ? GameConst.UIColors.Ally : enemy ? new Color(1f, .38f, .3f) : new Color(.9f, .77f, .44f);
        title.text = $"[{faction}] {Piece.pieceData?.pieceName ?? Piece.name}";
        title.color = bar.color = color;
        healthText.text = $"生命 {attr.CurHealth}/{attr.MaxHealth}";
        healthAnimation.Update(bar, shadow, attr.MaxHealth > 0 ? (float)attr.CurHealth / attr.MaxHealth : 0f, Piece);
        pips.SetPoints(Mathf.Min(24, attr.CurMovePoint), Mathf.Min(24, attr.MaxMovePoint));
        resources.text = $"AP {attr.CurMovePoint}/{attr.MaxMovePoint} · 弹 {attr.AmmoCount}/{attr.MaxAmmoCount} · 能 {attr.ManaPoint}/{attr.MaxManaPoint}";
        var old = Piece is EnemyController controller ? controller.enemyCanvas?.hpBarUI : attr.hpBarUI;
        bool preview = old != null && old.HasDamagePreview;
        minimumPreview.gameObject.SetActive(preview);
        maximumPreview.gameObject.SetActive(preview);
        if (preview) { minimumPreview.fillAmount = old.PreviewAfterMinimum; maximumPreview.fillAmount = old.PreviewAfterMaximum; }
        if (Time.unscaledTime < nextStatusRead) return;
        nextStatusRead = Time.unscaledTime + .1f;
        entries = BattleStatusReadModel.Read(Piece, battle);
        int shown = Mathf.Min(6, entries.Count);
        Height = entries.Count == 0 ? 80 : entries.Count <= 3 ? 114 : 148;
        Rect.sizeDelta = new Vector2(Width, Height);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].gameObject.SetActive(i < shown);
            if (i >= shown) continue;
            var entry = entries.Count > 6 && i == 5
                ? new BattleStatusEntry("更多状态", "+" + (entries.Count - 5), "点击查看全部状态") : entries[i];
            cells[i].SetEntry(entry);
            bool hasIcon = entry.Icon != null;
            cells[i].nameText.rectTransform.anchoredPosition = new Vector2(hasIcon ? 17 : 3, 0);
            cells[i].nameText.rectTransform.sizeDelta = new Vector2(hasIcon ? 55 : 68, 17);
        }
    }

    internal void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
        if (Link != null) Link.gameObject.SetActive(false);
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
    private UnityEngine.UI.Image Image(string name, float x, float y, float width, float height, Color color, Transform parent = null)
    {
        var image = Box(name, parent != null ? parent : Rect, x, y, width, height).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = layer.FillSprite;
        image.color = color; image.raycastTarget = false; return image;
    }
    private UnityEngine.UI.Text Text(string name, float x, float y, float width, float height, int size, Font font, Transform parent = null)
    {
        var text = Box(name, parent != null ? parent : Rect, x, y, width, height).gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font = font; text.fontSize = size; text.color = Color.white; text.raycastTarget = false;
        text.alignment = TextAnchor.MiddleLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
