using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>本场统一头顶展示：固定跟随棋子头顶，提供只读状态和分页详情。</summary>
public sealed class BattleOverheadStatusLayer : MonoBehaviour
{
    private BattleManager battle;
    private UIManager ui;
    private Canvas canvas;
    private Font font;
    private PieceOverheadStatusUI viewTemplate; // 所有棋子使用同一正式 prefab，布局调整保留在资产中。
    private readonly Dictionary<PieceController, PieceOverheadStatusUI> views = new();
    private readonly HashSet<PieceController> pieces = new();
    private readonly List<PieceController> staleViews = new();
    private readonly Vector3[] corners = new Vector3[4];
    private readonly List<Rect> hudBounds = new(2); // 同帧各卡片共用顶部HUD边界，仅记录可见区域。
    private readonly Dictionary<CanvasGroup, (float alpha, bool blocks, bool interactable)> legacyGroups = new();
    private readonly List<CanvasGroup> ownedGroups = new();
    private RectTransform details;
    private UnityEngine.UI.Text detailsText, pageText;
    private PieceOverheadStatusUI detailOwner;
    private int page;
    public IReadOnlyDictionary<PieceController, PieceOverheadStatusUI> Views => views;

    /// <summary>从正式资源模板创建敌我通用视图；必要绑定缺失时返回 null，不回退到代码生成布局。</summary>
    public PieceOverheadStatusUI CreateView(PieceController piece, Transform parent = null, PieceOverheadStatusUI template = null)
    {
        template = template != null ? template : piece is RangeBarrier
            ? Resources.Load<PieceOverheadStatusUI>("UI/RangeBarrierStatus") : viewTemplate;
        if (template == null || piece == null) return null;
        var view = Instantiate(template, parent != null ? parent : transform, false);
        view.name = "Status_" + piece.GetInstanceID();
        if (view.Initialize(this, piece, font)) return view;
        Destroy(view.gameObject);
        return null;
    }

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
        layer.viewTemplate = ui.OverheadStatusPrefab != null ? ui.OverheadStatusPrefab : Resources.Load<PieceOverheadStatusUI>("UI/PieceOverheadStatus");
        if (layer.viewTemplate == null || !layer.viewTemplate.HasTemplateBindings)
        {
            Debug.LogError("缺少有效的 Resources/UI/PieceOverheadStatus prefab，无法显示头顶状态。", ui);
            Destroy(go);
            return null;
        }
        layer.font = ui.pieceInfoPanel.pieceName.font;
        layer.BuildDetails();
        return layer;
    }

    private void LateUpdate()
    {
        if (battle == null || canvas == null) return;
        pieces.Clear();
        pieces.UnionWith(battle.PlayerController.pieces);
        pieces.UnionWith(battle.AIController.pieces);
        pieces.UnionWith(battle.summonPieces);
        pieces.UnionWith(battle.RangeBarriers);
        foreach (var explosive in EnemyExplosiveTargets.Active) if (explosive.Piece != null) pieces.Add(explosive.Piece);
        staleViews.Clear();
        foreach (var key in views.Keys)
            if (key == null || !pieces.Contains(key)) staleViews.Add(key);
        foreach (var key in staleViews)
        {
            var old = views[key];
            CloseDetails(old);
            if (old != null) Destroy(old.gameObject);
            views.Remove(key);
        }
        hudBounds.Clear();
        CacheHudBounds(ui.ActionTimelineRect);
        CacheHudBounds(ui.TimeControlsRect);
        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            if (!views.TryGetValue(piece, out var view))
            {
                view = CreateView(piece);
                if (view == null) continue;
                views.Add(piece, view);
            }
            HideLegacy(piece);
            if (!CanShow(piece, view.HeadPosition)) { view.Hide(); continue; }
            view.gameObject.SetActive(true);
            view.transform.localScale = Vector3.one / Mathf.Max(.01f, canvas.scaleFactor);
            view.Refresh(battle);
            Place(view);
        }
        if (ui.pieceStatePanel != null)
        {
            if (ui.pieceStatePanel.upRoot != null) Conceal(ui.pieceStatePanel.upRoot.gameObject);
            // 部分旧场景把资源条放在 upRoot 外，统一隐藏，避免与紧凑模板重复叠在模型上。
            ConcealBar(ui.pieceStatePanel.healthBar);
            ConcealBar(ui.pieceStatePanel.healthShadowBar);
            ConcealBar(ui.pieceStatePanel.manaBar);
            Conceal(ui.pieceStatePanel.mpText);
            Conceal(ui.pieceStatePanel.ammoText);
        }
        RefreshDetails();
    }

    private bool CanShow(PieceController piece, Vector3 head)
    {
        if (!battle.IsBattleRunning || (!battle.SpeedTurnsActive && !battle.IsDeploying) || MemeLoadoutPanel.IsOpen ||
            piece.unitAttrCenter == null || piece.isDead || !piece.gameObject.activeInHierarchy || Camera.main == null) return false;
        if (piece is EnemyController enemy && !enemy.isActived) return false;
        var renderer = piece.pieceDisplay?.pieceSpriteRenderer;
        if (renderer != null && (!renderer.enabled || !renderer.gameObject.activeInHierarchy)) return false;
        var point = Camera.main.WorldToScreenPoint(head);
        if (point.z <= 0 || point.x < 0 || point.x > Screen.width || point.y < 0 || point.y > Screen.height) return false;
        return piece.isPlayerPiece || !Physics.Linecast(Camera.main.transform.position, head, LayerMask.GetMask("Wall"), QueryTriggerInteraction.Ignore);
    }
    private void Place(PieceOverheadStatusUI view)
    {
        var projected = (Vector2)Camera.main.WorldToScreenPoint(view.HeadPosition);
        SetScreenPosition(view.Rect, projected + view.HeadOffset);
        var bounds = ScreenBounds(view.Rect);
        var safe = Screen.safeArea;
        var offset = Vector2.zero;
        if (view.ConstrainToScreen)
        {
            offset.x = bounds.xMin < safe.xMin ? safe.xMin - bounds.xMin : bounds.xMax > safe.xMax ? safe.xMax - bounds.xMax : 0;
            offset.y = bounds.yMin < safe.yMin ? safe.yMin - bounds.yMin : bounds.yMax > safe.yMax ? safe.yMax - bounds.yMax : 0;
            bounds.position += offset;
        }
        // 边缘卡片保持完整，并让出速度条和快进区域；不改变棋子世界坐标。
        if (view.AvoidTopHud)
            foreach (var hud in hudBounds)
            {
                if (!bounds.Overlaps(hud)) continue;
                // 优先横向避开顶部HUD，避免向下挤压时把血条重新盖到头脸上。
                float left = hud.xMin - 8f - bounds.xMax;
                float right = hud.xMax + 8f - bounds.xMin;
                bool leftFits = CanShiftBesideHud(bounds, left, safe);
                bool rightFits = CanShiftBesideHud(bounds, right, safe);
                if (leftFits || rightFits)
                {
                    float horizontal = leftFits && (!rightFits || Mathf.Abs(left) <= Mathf.Abs(right)) ? left : right;
                    offset.x += horizontal;
                    bounds.x += horizontal;
                    continue;
                }
                float down = hud.yMin - 8f - bounds.yMax;
                offset.y += down;
                bounds.y += down;
            }
        SetScreenPosition(view.Rect, projected + view.HeadOffset + offset);
    }

    private bool CanShiftBesideHud(Rect bounds, float horizontal, Rect safe)
    {
        bounds.x += horizontal;
        if (bounds.xMin < safe.xMin || bounds.xMax > safe.xMax) return false;
        foreach (var hud in hudBounds)
            if (bounds.Overlaps(hud)) return false;
        return true;
    }

    private void CacheHudBounds(RectTransform rect) { if (rect != null) hudBounds.Add(ScreenBounds(rect)); }

    private Rect ScreenBounds(RectTransform rect)
    {
        rect.GetWorldCorners(corners);
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private void SetScreenPosition(RectTransform rect, Vector2 point)
    {
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)rect.parent, point, camera, out var world)) rect.position = world;
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
    private void Conceal(UnityEngine.UI.Graphic graphic)
    {
        if (graphic != null) Conceal(graphic.gameObject);
    }
    private void ConcealBar(UnityEngine.UI.Image image)
    {
        if (image != null) Conceal(image.transform.parent != null ? image.transform.parent.gameObject : image.gameObject);
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
    }
}
