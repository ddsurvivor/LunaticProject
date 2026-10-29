using UnityEngine;

public partial class SkillTooltipUI
{
    [SerializeField] private Sprite panelFill;
    [SerializeField] private Sprite panelOutline;
    private RectTransform detailsViewport;
    private UnityEngine.UI.ScrollRect detailsScroll;

    /// <summary>供行动菜单判断鼠标是否已进入可滚动详情区域，隐藏时返回 false。</summary>
    public bool ContainsPointer
    {
        get
        {
            if (!isActiveAndEnabled) return false;
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, Input.mousePosition, camera);
        }
    }

    private void RefreshLayout(bool reset)
    {
        var root = (RectTransform)transform;
        if (detailsScroll == null)
        {
            transform.localScale = Vector3.one;
            foreach (var layout in GetComponents<UnityEngine.UI.LayoutGroup>()) layout.enabled = false;
            var rootFit = GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (rootFit != null) rootFit.enabled = false;
            nameText.transform.SetParent(root, false);
            tooltipImage.transform.SetParent(root, false);
            infoText.transform.SetParent(root, false);
            // 旧卡片背景和角标仍在序列化层级中；统一由根背景与 DetailsOutline 绘制。
            foreach (Transform child in root)
                if (child != nameText.transform && child != tooltipImage.transform && child != infoText.transform)
                    child.gameObject.SetActive(false);
            nameText.transform.localScale = tooltipImage.transform.localScale = Vector3.one;
            foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget = false;
            var background = GetComponent<UnityEngine.UI.Image>();
            if (background == null) background = gameObject.AddComponent<UnityEngine.UI.Image>();
            background.sprite = panelFill;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            background.color = new Color32(11, 16, 21, 235);
            background.raycastTarget = true;
            var borderObject = new GameObject("DetailsOutline", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            borderObject.transform.SetParent(root, false);
            var border = borderObject.GetComponent<UnityEngine.UI.Image>();
            border.sprite = panelOutline;
            border.type = UnityEngine.UI.Image.Type.Sliced;
            border.color = GameConst.UIColors.Border;
            border.raycastTarget = false;
            border.rectTransform.anchorMin = Vector2.zero;
            border.rectTransform.anchorMax = Vector2.one;
            border.rectTransform.offsetMin = border.rectTransform.offsetMax = Vector2.zero;
            var go = new GameObject("DetailsViewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
            detailsViewport = go.GetComponent<RectTransform>();
            detailsViewport.SetParent(transform, false);
            go.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            go.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            detailsViewport.anchorMin = Vector2.zero;
            detailsViewport.anchorMax = Vector2.one;
            detailsViewport.offsetMin = new Vector2(18, 16);
            detailsViewport.offsetMax = new Vector2(-18, -72);
            infoText.transform.SetParent(detailsViewport, false);
            infoText.transform.localScale = Vector3.one;
            infoText.rectTransform.anchorMin = new Vector2(0, 1);
            infoText.rectTransform.anchorMax = Vector2.one;
            infoText.rectTransform.pivot = new Vector2(0.5f, 1);
            infoText.rectTransform.anchoredPosition = Vector2.zero;
            infoText.fontSize = 20;
            infoText.resizeTextForBestFit = false;
            infoText.color = GameConst.UIColors.Text;
            infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoText.verticalOverflow = VerticalWrapMode.Overflow;
            infoText.alignment = TextAnchor.UpperLeft;
            detailsScroll = gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            detailsScroll.content = infoText.rectTransform;
            detailsScroll.viewport = detailsViewport;
            detailsScroll.horizontal = false;
            detailsScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            detailsScroll.scrollSensitivity = 32;
            var fit = infoText.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fit == null) fit = infoText.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        }
        var parent = (RectTransform)root.parent;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.min, camera, out var min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.max, camera, out var max);
        float width = Mathf.Min(420, max.x - min.x - 48);
        infoText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 36);
        float height = Mathf.Min(Mathf.Max(120, infoText.preferredHeight + 88), max.y - min.y - 208);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0, 1);
        root.sizeDelta = new Vector2(width, height);
        root.localPosition = new Vector3(min.x + 24, max.y - 160, 0);
        var title = nameText.rectTransform;
        title.anchorMin = title.anchorMax = new Vector2(0, 1);
        title.pivot = new Vector2(0, 1);
        title.anchoredPosition = new Vector2(18, -12);
        title.sizeDelta = new Vector2(width - (tooltipImage.gameObject.activeSelf ? 90 : 36), 52);
        nameText.fontSize = 24;
        nameText.resizeTextForBestFit = false;
        nameText.color = GameConst.UIColors.TextHeading;
        var icon = tooltipImage.rectTransform;
        icon.anchorMin = icon.anchorMax = new Vector2(1, 1);
        icon.pivot = new Vector2(1, 1);
        icon.anchoredPosition = new Vector2(-16, -12);
        icon.sizeDelta = new Vector2(48, 48);
        tooltipImage.preserveAspect = true;
        var content = infoText.rectTransform;
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = new Vector2(0, infoText.preferredHeight);
        if (reset) { detailsScroll.StopMovement(); detailsScroll.verticalNormalizedPosition = 1; }
    }

    private void LateUpdate() => RefreshLayout(false);
}
