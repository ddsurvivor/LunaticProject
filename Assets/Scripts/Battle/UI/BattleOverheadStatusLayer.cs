using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>本场统一头顶展示：屏幕安全区、行动条避让、拥挤排布与分页详情，全部只读。</summary>
public sealed class BattleOverheadStatusLayer : MonoBehaviour
{
    private BattleManager battle;
    private UIManager ui;
    private Canvas canvas;
    private Font font;
    private readonly Dictionary<PieceController, PieceOverheadStatusUI> views = new();
    private readonly List<Rect> occupied = new(); // 本帧已占用的屏幕区域，避免标签相互遮挡。
    private readonly Dictionary<CanvasGroup, (float alpha, bool blocks, bool interactable)> legacyGroups = new();
    private readonly List<CanvasGroup> ownedGroups = new();
    private RectTransform details;
    private UnityEngine.UI.Text detailsText, pageText;
    private PieceOverheadStatusUI detailOwner;
    private int page;
    internal Sprite FillSprite { get; private set; }
    public IReadOnlyDictionary<PieceController, PieceOverheadStatusUI> Views => views;

    /// <summary>绑定当前场景的原Canvas和字体；初始化重复调用时沿用同一个展示层。</summary>
    public static BattleOverheadStatusLayer Ensure(UIManager ui)
    {
        var existing = ui.OverheadStatuses;
        if (existing != null) return existing;
        var rootCanvas = ui.pieceInfoPanel != null ? ui.pieceInfoPanel.GetComponentInParent<Canvas>()?.rootCanvas : null;
        if (rootCanvas == null) return null;
        var go = new GameObject("BattleOverheadStatus", typeof(RectTransform));
        go.transform.SetParent(rootCanvas.transform, false);
        go.transform.SetAsFirstSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var layer = go.AddComponent<BattleOverheadStatusLayer>();
        layer.ui = ui; layer.canvas = rootCanvas; layer.battle = BattleScene.Ins.BM;
        layer.font = ui.pieceInfoPanel.pieceName.font;
        layer.FillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f);
        layer.BuildDetails();
        return layer;
    }

    private void LateUpdate()
    {
        if (battle == null || canvas == null) return;
        var pieces = new HashSet<PieceController>(battle.PlayerController.pieces);
        pieces.UnionWith(battle.AIController.pieces);
        pieces.UnionWith(battle.summonPieces);
        foreach (var explosive in EnemyExplosiveTargets.Active) if (explosive.Piece != null) pieces.Add(explosive.Piece);
        foreach (var key in new List<PieceController>(views.Keys))
            if (key == null || !pieces.Contains(key))
            {
                var old = views[key];
                CloseDetails(old);
                if (old != null) { if (old.Link != null) Destroy(old.Link.gameObject); Destroy(old.gameObject); }
                views.Remove(key);
            }
        var candidates = new List<PieceOverheadStatusUI>();
        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            if (!views.TryGetValue(piece, out var view))
            {
                var go = new GameObject("Status_" + piece.GetInstanceID(), typeof(RectTransform));
                go.transform.SetParent(transform, false);
                view = go.AddComponent<PieceOverheadStatusUI>();
                view.Initialize(this, piece, font);
                var link = PieceOverheadStatusUI.Box("Link", transform, 0, 0, 1, 1).gameObject.AddComponent<UnityEngine.UI.Image>();
                link.color = new Color(.7f, .85f, .9f, .45f); link.raycastTarget = false;
                link.rectTransform.pivot = new Vector2(0, .5f);
                link.transform.SetAsFirstSibling(); view.Link = link;
                views.Add(piece, view);
            }
            HideLegacy(piece);
            if (!CanShow(piece)) { view.Hide(); continue; }
            view.gameObject.SetActive(true);
            view.transform.localScale = Vector3.one / Mathf.Max(.01f, canvas.scaleFactor);
            view.Refresh(battle);
            candidates.Add(view);
        }
        if (ui.pieceStatePanel != null && ui.pieceStatePanel.upRoot != null) Conceal(ui.pieceStatePanel.upRoot.gameObject);
        occupied.Clear();
        Reserve(ui.ActionTimelineRect);
        Reserve(ui.NoticeRect);
        if (ui.notificationManager != null && ui.notificationManager.ActiveNotificationCount > 0) Reserve(ui.NotificationRect);
        if (ui.logPanel != null) Reserve(ui.logPanel.transform as RectTransform);
        if (ui.pieceInfoPanel != null && ui.pieceInfoPanel.gameObject.activeInHierarchy) ReserveGraphics(ui.pieceInfoPanel.transform);
        if (ui.burstButton != null && ui.burstButton.gameObject.activeInHierarchy) Reserve((RectTransform)ui.burstButton.transform);
        candidates.Sort((a, b) => Priority(a.Piece).CompareTo(Priority(b.Piece)) != 0
            ? Priority(a.Piece).CompareTo(Priority(b.Piece)) : a.Piece.GetInstanceID().CompareTo(b.Piece.GetInstanceID()));
        foreach (var view in candidates) Place(view);
        RefreshDetails();
    }

    private int Priority(PieceController piece) => piece == battle.ActivePiece ? 0 : piece.isPlayerPiece ? 1 : 2;
    private bool CanShow(PieceController piece)
    {
        if (!battle.IsBattleRunning || (!battle.SpeedTurnsActive && !battle.IsDeploying) || MemeLoadoutPanel.IsOpen ||
            piece.unitAttrCenter == null || piece.isDead || !piece.gameObject.activeInHierarchy || Camera.main == null) return false;
        if (piece is EnemyController enemy && !enemy.isActived) return false;
        var renderer = piece.pieceDisplay?.pieceSpriteRenderer;
        if (renderer != null && (!renderer.enabled || !renderer.gameObject.activeInHierarchy)) return false;
        var head = Head(piece);
        var point = Camera.main.WorldToScreenPoint(head);
        if (point.z <= 0 || point.x < 0 || point.x > Screen.width || point.y < 0 || point.y > Screen.height) return false;
        return piece.isPlayerPiece || !Physics.Linecast(Camera.main.transform.position, head, LayerMask.GetMask("Wall"), QueryTriggerInteraction.Ignore);
    }
    private static Vector3 Head(PieceController piece)
    {
        var sprite = piece.pieceDisplay?.pieceSpriteRenderer;
        return sprite != null ? new Vector3(sprite.bounds.center.x, sprite.bounds.max.y, sprite.bounds.center.z) : piece.transform.position + Vector3.up * 2;
    }

    private void Place(PieceOverheadStatusUI view)
    {
        var projected = (Vector2)Camera.main.WorldToScreenPoint(Head(view.Piece));
        var safe = Screen.safeArea;
        safe = Rect.MinMaxRect(safe.xMin + 8, safe.yMin + 8, safe.xMax - 8, safe.yMax - 8);
        float width = PieceOverheadStatusUI.Width, height = view.Height;
        // 优先贴近本体，再在安全区内分层错开；无空位时隐藏低优先级标签，不覆盖其他标签。
        for (int row = 0; row < 8; row++)
            for (int column = 0; column < 7; column++)
            {
                int side = column == 0 ? 0 : (column + 1) / 2 * (column % 2 == 1 ? -1 : 1);
                float x = Mathf.Clamp(projected.x - width / 2 + side * (width + 6), safe.xMin, Mathf.Max(safe.xMin, safe.xMax - width));
                float y = Mathf.Clamp(projected.y + 12 + (row % 2 == 0 ? row / 2 : -(row + 1) / 2) * (height + 6), safe.yMin, Mathf.Max(safe.yMin, safe.yMax - height));
                var candidate = new Rect(x, y, width, height);
                bool overlaps = false;
                foreach (var other in occupied) if (candidate.Overlaps(other)) { overlaps = true; break; }
                if (overlaps) continue;
                occupied.Add(new Rect(x - 2, y - 2, width + 4, height + 4));
                SetScreenPosition(view.Rect, new Vector2(x + width / 2, y));
                var end = new Vector2(x + width / 2, y);
                var link = view.Link.rectTransform;
                link.gameObject.SetActive(Vector2.Distance(projected, end) > 30);
                link.localScale = Vector3.one / Mathf.Max(.01f, canvas.scaleFactor);
                SetScreenPosition(link, projected);
                link.sizeDelta = new Vector2(Vector2.Distance(projected, end), 1);
                link.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y - projected.y, end.x - projected.x) * Mathf.Rad2Deg);
                return;
            }
        view.Hide();
    }
    private void SetScreenPosition(RectTransform rect, Vector2 point)
    {
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)rect.parent, point, camera, out var world)) rect.position = world;
    }
    private void Reserve(RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return;
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        if (max.x - min.x < Screen.width * .9f && max.y - min.y < Screen.height * .9f)
            occupied.Add(Rect.MinMaxRect(min.x - 6, min.y - 6, max.x + 6, max.y + 6));
    }

    private void ReserveGraphics(Transform root)
    {
        bool found = false;
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var corners = new Vector3[4];
        // 部分旧面板根矩形覆盖整个Canvas，只用实际绘制的图形确定占位。
        foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
        {
            if (!graphic.enabled || graphic.color.a <= 0) continue;
            graphic.rectTransform.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            found = true;
        }
        if (found && max.x - min.x < Screen.width * .9f && max.y - min.y < Screen.height * .9f)
            occupied.Add(Rect.MinMaxRect(min.x - 6, min.y - 6, max.x + 6, max.y + 6));
    }

    private void HideLegacy(PieceController piece)
    {
        if (piece is EnemyController enemy && enemy.enemyCanvas != null)
        {
            if (enemy.enemyCanvas.hpBarUI != null) Conceal(enemy.enemyCanvas.hpBarUI.gameObject);
            if (enemy.enemyCanvas.BuffRoot != null) Conceal(enemy.enemyCanvas.BuffRoot.gameObject);
        }
    }
    private void Conceal(GameObject go)
    {
        var group = go.GetComponent<CanvasGroup>();
        if (group == null) { group = go.AddComponent<CanvasGroup>(); ownedGroups.Add(group); }
        if (!legacyGroups.ContainsKey(group)) legacyGroups.Add(group, (group.alpha, group.blocksRaycasts, group.interactable));
        group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
    }

    /// <summary>打开状态完整分页；溢出只收起头顶单元，不丢弃条目。</summary>
    public void ShowDetails(PieceOverheadStatusUI view)
    {
        detailOwner = view; page = 0; RefreshDetails();
    }
    public void CloseDetails(PieceOverheadStatusUI owner)
    {
        if (detailOwner != owner) return;
        detailOwner = null;
        if (details != null) details.gameObject.SetActive(false);
    }
    private void BuildDetails()
    {
        details = PieceOverheadStatusUI.Box("StateDetails", transform, 0, 0, 360, 310);
        details.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.035f, .055f, .085f, .98f);
        detailsText = MakeText(details, "Details", 12, 12, 336, 245, 13);
        pageText = MakeText(details, "Page", 125, 270, 110, 26, 13);
        Button("上一页", 12, () => { page--; RefreshDetails(); });
        Button("下一页", 245, () => { page++; RefreshDetails(); });
        Button("关闭", 285, () => CloseDetails(detailOwner), 0);
        details.gameObject.SetActive(false);
    }
    private void RefreshDetails()
    {
        if (detailOwner == null || !detailOwner.gameObject.activeInHierarchy) { if (details != null) details.gameObject.SetActive(false); return; }
        details.gameObject.SetActive(true); details.SetAsLastSibling();
        details.localScale = Vector3.one / Mathf.Max(.01f, canvas.scaleFactor);
        var safe = Screen.safeArea;
        SetScreenPosition(details, new Vector2(Mathf.Max(safe.xMin + 8, safe.xMax - 368), Mathf.Min(safe.yMax - 8, safe.yMin + 470)));
        int pages = Mathf.Max(1, (detailOwner.Entries.Count + 2) / 3);
        page = Mathf.Clamp(page, 0, pages - 1);
        var text = new StringBuilder();
        text.AppendLine(detailOwner.Piece.pieceData?.pieceName ?? "状态详情");
        if (detailOwner.Entries.Count == 0) text.Append("无附加状态");
        for (int i = page * 3; i < Mathf.Min(detailOwner.Entries.Count, page * 3 + 3); i++)
        {
            var entry = detailOwner.Entries[i];
            text.AppendLine($"<color=#8edbe8>{entry.Name} · {entry.Value}</color>");
            text.AppendLine(entry.Detail);
        }
        detailsText.text = text.ToString(); pageText.text = $"{page + 1} / {pages}";
    }
    private UnityEngine.UI.Text MakeText(Transform parent, string name, float x, float y, float w, float h, int size)
    {
        var text = PieceOverheadStatusUI.Box(name, parent, x, y, w, h).gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font = font; text.fontSize = size; text.color = Color.white; text.raycastTarget = false; text.lineSpacing = 1.3f; return text;
    }
    private void Button(string label, float x, UnityEngine.Events.UnityAction action, float y = 270)
    {
        float width = label == "关闭" ? 55 : 100;
        var rect = PieceOverheadStatusUI.Box(label, details, x, y, width, 26);
        rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.17f, .25f, .32f);
        rect.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(action);
        var text = MakeText(rect, "Label", 0, 0, width, 26, 13); text.text = label; text.alignment = TextAnchor.MiddleCenter;
    }
    private void OnDisable()
    {
        foreach (var view in views.Values) if (view != null) view.Hide();
        foreach (var pair in legacyGroups) if (pair.Key != null) { pair.Key.alpha = pair.Value.alpha; pair.Key.blocksRaycasts = pair.Value.blocks; pair.Key.interactable = pair.Value.interactable; }
        legacyGroups.Clear();
    }
    private void OnDestroy()
    {
        foreach (var group in ownedGroups) if (group != null) Destroy(group);
        if (FillSprite != null) Destroy(FillSprite);
    }
}
