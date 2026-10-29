using UnityEngine;

public partial class PieceActionListPanel
{
    [SerializeField] private Sprite menuRowFill;
    [SerializeField] private Sprite menuRowOutline;
    [SerializeField, Min(0.01f)] private float rowRevealDuration = 0.2f;
    [SerializeField, Min(0)] private float maximumRowInterval = 0.2f;
    [SerializeField, Min(0.01f)] private float maximumRevealTime = 0.8f;
    private bool menuReady;
    private RectTransform primaryRoot, primaryContent, secondaryRoot, secondaryContent;
    private UnityEngine.UI.ScrollRect primaryScroll, secondaryScroll;
    private UnityEngine.UI.Text emptyLabel;
    private Canvas menuCanvas;
    private const float RowWidth = 230f;
    private const float ButtonHeight = 44f;
    private float RowSpacing => Mathf.Max(6, Mathf.Ceil(ButtonHeight * (actionButtonPrefab.HoverScaleMultiplier - 1)) + 2);
    private float RowHeight => ButtonHeight + RowSpacing;
    private float RightPadding => Mathf.Ceil(RowWidth * (actionButtonPrefab.HoverScaleMultiplier - 1)) + 16;
    private float VerticalPadding => Mathf.Max(4, Mathf.Ceil(ButtonHeight * (actionButtonPrefab.HoverScaleMultiplier - 1) / 2) + 2);
    private float HorizontalPadding => RightPadding + 4;
    private const float MenuGap = 8f;
    private const float SafeMargin = 24f; // Canvas 单位，叠加设备安全区。

    private void BuildMenuLayout()
    {
        menuCanvas = GetComponentInParent<Canvas>().rootCanvas;
        var root = (RectTransform)transform;
        foreach (Transform child in root) child.gameObject.SetActive(false);
        root.localScale = Vector3.one;
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0, 1);
        primaryRoot = NewRect("ActionViewport", root);
        primaryScroll = BuildScroll(primaryRoot, out primaryContent);
        secondaryRoot = NewRect("SecondaryViewport", root);
        secondaryScroll = BuildScroll(secondaryRoot, out secondaryContent);
        skillListPanel = secondaryRoot.gameObject;
        emptyLabel = Instantiate(actionButtonPrefab.actionNameText, secondaryContent, false);
        emptyLabel.name = "EmptyState";
        emptyLabel.alignment = TextAnchor.MiddleCenter;
        var emptySize = emptyLabel.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        emptySize.preferredHeight = ButtonHeight;
        emptyLabel.gameObject.SetActive(false);
        secondaryRoot.gameObject.SetActive(false);
        menuReady = true;
        gameObject.SetActive(false);
    }

    private static RectTransform NewRect(string title, Transform parent)
    {
        var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.gameObject.layer = parent.gameObject.layer;
        return rect;
    }

    private UnityEngine.UI.ScrollRect BuildScroll(RectTransform root, out RectTransform content)
    {
        var background = root.gameObject.AddComponent<UnityEngine.UI.Image>();
        // 保留透明命中区域防止菜单空隙误点棋盘，不再绘制整块菜单底板。
        background.color = Color.clear;
        background.raycastTarget = true;
        var viewport = NewRect("Viewport", root);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        var maskImage = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
        maskImage.color = Color.white;
        var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false;
        content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        var group = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        // 为相邻按钮同时缩放的过渡帧留间距，并在裁剪区域内预留放大余量。
        group.spacing = RowSpacing;
        group.padding = new RectOffset(4, Mathf.CeilToInt(RightPadding), Mathf.CeilToInt(VerticalPadding), Mathf.CeilToInt(VerticalPadding));
        var fit = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = RowHeight;
        return scroll;
    }

    private void RevealRows(System.Collections.Generic.List<ActionBtn> rows)
    {
        int count = 0;
        foreach (var row in rows) if (row.gameObject.activeSelf) count++;
        float duration = Mathf.Min(rowRevealDuration, maximumRevealTime);
        float interval = ActionMenuTiming.Interval(count, duration, maximumRowInterval, maximumRevealTime);
        int index = 0;
        foreach (var row in rows)
            if (row.gameObject.activeSelf) row.Reveal(index++ * interval, duration);
    }

    private void LateUpdate()
    {
        if (!menuReady || pc == null) return;
        if (!CanAct()) { HidePanel(); return; }
        foreach (var row in primaryRows) if (row.gameObject.activeSelf) row.Refresh();
        foreach (var row in secondaryRows) if (row.gameObject.activeSelf) row.Refresh();
        RefreshHover();
        RefreshDetails();
        emptyLabel.text = ActionTypeLabels.English ? "No available entries" : "暂无可用条目";
        RefreshMenuLayout(false);
    }

    private void RefreshMenuLayout(bool resetScroll)
    {
        if (!menuReady || pc == null || Camera.main == null) return;
        var root = (RectTransform)transform;
        var parent = (RectTransform)root.parent;
        var camera = menuCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : menuCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.min, camera, out var min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.max, camera, out var max);
        min += Vector2.one * SafeMargin;
        max -= Vector2.one * SafeMargin;
        float availableHeight = Mathf.Max(RowHeight, max.y - min.y);
        float width = Mathf.Max(80 + HorizontalPadding, Mathf.Min(RowWidth + HorizontalPadding, (max.x - min.x - MenuGap) / 2));
        float height = Mathf.Min(availableHeight, primaryEntries.Count * RowHeight - RowSpacing + 2 * VerticalPadding);
        float secondHeight = Mathf.Min(availableHeight, Mathf.Max(1, secondaryEntries.Count) * RowHeight - RowSpacing + 2 * VerticalPadding);
        Place(primaryRoot, Vector2.zero, new Vector2(width, height));
        root.sizeDelta = primaryRoot.sizeDelta;
        Place(secondaryRoot, Vector2.zero, new Vector2(width, secondHeight));
        if (resetScroll)
        {
            primaryScroll.StopMovement();
            secondaryScroll.StopMovement();
            secondaryScroll.verticalNormalizedPosition = 1;
            if (expandedRow == null) primaryScroll.verticalNormalizedPosition = 1;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(primaryContent);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(secondaryContent);
        }
        bool expanded = expandedRow != null && skillListPanel.activeSelf;
        float secondTop = 0;
        if (expanded)
        {
            var trigger = (RectTransform)expandedRow.transform;
            float center = root.InverseTransformPoint(trigger.TransformPoint(trigger.rect.center)).y;
            // 先保持可见面板中心与触发行中心一致，超出整组可容纳范围时再约束。
            secondTop = Mathf.Clamp(center + secondHeight / 2,
                secondHeight - availableHeight, availableHeight - height);
        }
        float localTop = expanded ? Mathf.Max(0, secondTop) : 0;
        float localBottom = expanded ? Mathf.Min(-height, secondTop - secondHeight) : -height;
        var screen = Camera.main.WorldToScreenPoint(pc.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out var point);
        float totalWidth = expanded ? width * 2 + MenuGap : width;
        bool flip = point.x + 36 + totalWidth > max.x;
        float x = flip ? point.x - 36 - totalWidth : point.x + 36;
        x = Mathf.Clamp(x, min.x, Mathf.Max(min.x, max.x - totalWidth));
        float y = Mathf.Clamp(point.y + 80, min.y - localBottom, max.y - localTop);
        root.localPosition = new Vector3(x + (flip && expanded ? width + MenuGap : 0), y, 0);
        secondaryRoot.anchoredPosition = new Vector2(flip ? -width - MenuGap : width + MenuGap, secondTop);
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
