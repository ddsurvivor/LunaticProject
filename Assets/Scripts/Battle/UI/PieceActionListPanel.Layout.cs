using UnityEngine;

public partial class PieceActionListPanel
{
    [SerializeField] private Sprite menuRowFill;
    [SerializeField] private Sprite menuRowOutline;
    private bool menuReady;
    private RectTransform primaryRoot, primaryContent, secondaryContent;
    private UnityEngine.UI.ScrollRect primaryScroll, secondaryScroll;
    private Canvas menuCanvas;
    private const float RowWidth = 230f;
    private const float RowHeight = 46f;
    private const float MenuGap = 8f;
    private const float SafeMargin = 24f; // Canvas 单位，另叠加设备安全区。
    private ActionType category;

    private void BuildMenuLayout()
    {
        menuReady = true;
        menuCanvas = GetComponentInParent<Canvas>().rootCanvas;
        transform.localScale = Vector3.one;
        var root = (RectTransform)transform;
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0, 1);
        primaryRoot = NewRect("ActionViewport", root);
        primaryScroll = BuildScroll(primaryRoot, out primaryContent);
        var second = (RectTransform)skillListPanel.transform;
        foreach (var layout in second.GetComponents<UnityEngine.UI.LayoutGroup>()) layout.enabled = false;
        var fitter = second.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        secondaryScroll = BuildScroll(second, out secondaryContent);
        foreach (var button in actionButtons)
        {
            button.transform.SetParent(primaryContent, false);
            StyleRow(button);
        }
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child != primaryRoot && child != second) child.gameObject.SetActive(false);
        }
        foreach (var button in skillButtons)
        {
            button.transform.SetParent(secondaryContent, false);
            StyleRow(button);
        }
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
        var background = root.GetComponent<UnityEngine.UI.Image>();
        if (background == null) background = root.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.sprite = menuRowFill;
        background.type = UnityEngine.UI.Image.Type.Sliced;
        background.color = new Color32(11, 16, 21, 230);
        background.raycastTarget = true;
        var viewport = NewRect("Viewport", root);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(4, 4);
        viewport.offsetMax = new Vector2(-4, -4);
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
        group.spacing = 2;
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

    private void StyleRow(UnityEngine.UI.Button button)
    {
        button.enabled = true;
        var row = button.GetComponent<ActionBtn>();
        if (row == null) row = button.gameObject.AddComponent<ActionBtn>();
        if (row.actionNameText == null) row.actionNameText = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
        row.Configure(menuRowFill, menuRowOutline);
        var layout = button.GetComponent<UnityEngine.UI.LayoutElement>();
        if (layout == null) layout = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.ignoreLayout = false;
        layout.minHeight = layout.preferredHeight = RowHeight - 2;
        button.transform.localScale = Vector3.one;
        var hover = button.GetComponent<HoverScale>();
        if (hover == null) hover = button.gameObject.AddComponent<HoverScale>();
        hover.ConfigureMenuRow();
    }

    private static void SetRow(UnityEngine.UI.Button button, string title, string cost, bool hasChildren)
    {
        var row = button.GetComponent<ActionBtn>();
        if (row == null) return;
        row.actionNameText.text = title;
        if (row.costText != null) row.costText.text = cost;
        row.SetSubmenu(hasChildren);
    }

    private void EnsureSkillButtons(int count)
    {
        if (skillButtons.Count == 0) return;
        // 仅扩充缺少的容量，后续展开复用按钮与组件。
        while (skillButtons.Count < count)
        {
            var button = Instantiate(skillButtons[0], secondaryContent, false);
            button.onClick.RemoveAllListeners();
            skillButtons.Add(button);
            StyleRow(button);
        }
    }

    private void PrepareCategory()
    {
        HideTooltip();
        foreach (var button in skillButtons)
        {
            var hover = button.GetComponent<HoverScale>();
            if (hover == null) continue;
            hover.onHoverEnter.RemoveAllListeners();
            hover.onHoverExit.RemoveAllListeners();
            button.enabled = true;
        }
    }

    private void SelectCategory(ActionType type)
    {
        category = type;
        foreach (var pair in actionButtonDic)
            pair.Value.GetComponent<ActionBtn>()?.SetSelected(pair.Key == type);
    }

    private void HideTooltip()
    {
        if (BattleScene.Ins != null && BattleScene.Ins.UM != null && BattleScene.Ins.UM.skillTooltipUI != null)
            BattleScene.Ins.UM.skillTooltipUI.HideTooltip();
    }

    private void LateUpdate()
    {
        if (menuReady && pc != null) RefreshMenuLayout(false);
    }

    private static int CountVisible(System.Collections.Generic.List<UnityEngine.UI.Button> buttons)
    {
        int count = 0;
        foreach (var button in buttons) if (button.gameObject.activeSelf) count++;
        return count;
    }

    private void RefreshMenuLayout(bool resetScroll)
    {
        if (!menuReady || pc == null || Camera.main == null) return;
        var parent = (RectTransform)transform.parent;
        var uiCamera = menuCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : menuCanvas.worldCamera;
        Rect safe = Screen.safeArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safe.min, uiCamera, out var min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safe.max, uiCamera, out var max);
        min += Vector2.one * SafeMargin;
        max -= Vector2.one * SafeMargin;
        float availableHeight = Mathf.Max(RowHeight, max.y - min.y);
        float width = Mathf.Min(RowWidth, (max.x - min.x - MenuGap) / 2);
        int primaryCount = CountVisible(actionButtons);
        int secondaryCount = CountVisible(skillButtons);
        float height = Mathf.Min(availableHeight, primaryCount * RowHeight + 8);
        float secondHeight = Mathf.Min(availableHeight, Mathf.Max(1, secondaryCount) * RowHeight + 8);
        primaryRoot.anchorMin = primaryRoot.anchorMax = new Vector2(0, 1);
        primaryRoot.pivot = new Vector2(0, 1);
        primaryRoot.anchoredPosition = Vector2.zero;
        primaryRoot.sizeDelta = new Vector2(width, height);
        var root = (RectTransform)transform;
        root.sizeDelta = primaryRoot.sizeDelta;
        var screen = Camera.main.WorldToScreenPoint(pc.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out var point);
        bool expanded = skillListPanel.activeSelf;
        float totalWidth = expanded ? width * 2 + MenuGap : width;
        bool flip = point.x + 36 + totalWidth > max.x;
        float x = flip ? point.x - 36 - totalWidth : point.x + 36;
        x = Mathf.Clamp(x, min.x, Mathf.Max(min.x, max.x - totalWidth));
        if (resetScroll)
        {
            primaryScroll.StopMovement();
            secondaryScroll.StopMovement();
            primaryScroll.verticalNormalizedPosition = secondaryScroll.verticalNormalizedPosition = 1;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(primaryContent);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(secondaryContent);
        }
        float rowOffset = 0;
        if (expanded && actionButtonDic.TryGetValue(category, out var trigger))
            rowOffset = Mathf.Max(0, -((RectTransform)trigger.transform).anchoredPosition.y + primaryContent.anchoredPosition.y);
        float combinedHeight = Mathf.Max(height, expanded ? rowOffset + secondHeight : 0);
        rowOffset = Mathf.Min(rowOffset, Mathf.Max(0, availableHeight - secondHeight));
        combinedHeight = Mathf.Min(availableHeight, combinedHeight);
        float y = Mathf.Clamp(point.y + 80, min.y + combinedHeight, max.y);
        // 整组翻转后再限制边界，确保二级菜单与一级菜单一起留在安全区。
        root.localPosition = new Vector3(x + (flip && expanded ? width + MenuGap : 0), y, 0);
        var second = (RectTransform)skillListPanel.transform;
        second.anchorMin = second.anchorMax = new Vector2(0, 1);
        second.pivot = new Vector2(0, 1);
        second.anchoredPosition = new Vector2(flip ? -width - MenuGap : width + MenuGap, -rowOffset);
        second.sizeDelta = new Vector2(width, secondHeight);

    }
}
