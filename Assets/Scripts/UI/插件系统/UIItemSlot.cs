using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 插件道具格子
/// </summary>
public class UIItemSlot : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public int currentItemID; // 当前格子存储的ID
    private WeaponPanel mainPage;
    private Player equippedOwner;
    private UnityEngine.UI.Text equippedText;

    public void Init(int id, WeaponPanel page, Player owner = null)
    {
        mainPage = page;
        currentItemID = id;
        equippedOwner = owner;
        
        // 从配置表获取数据来显示
        ComponentData data = GM.Ins.DM.componentConfig.GetData(id);
        if (data != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }
        ComponentInventoryUI.SetOwnerLabel(transform, ref equippedText, data != null ? owner : null,
            page != null && page.detailPanel != null ? page.detailPanel.titleText : null);
    }
    public void Clear()
    {
        currentItemID = 0;
        equippedOwner = null;
        iconImage.enabled = false;
        ComponentInventoryUI.SetOwnerLabel(transform, ref equippedText, null, null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && currentItemID != 0)
        {
            mainPage.ShowDetail(currentItemID, transform.position, equippedOwner);
        }
    }
}

// 两个插件背包共用装配标记，旧预制体无需补挂引用。
public static class ComponentInventoryUI
{
    public static void SetOwnerLabel(Transform parent, ref UnityEngine.UI.Text label,
        Player owner, UnityEngine.UI.Text style)
    {
        if (label == null)
        {
            var existing = parent.Find("EquippedOwnerLabel");
            if (existing != null) label = existing.GetComponentInChildren<UnityEngine.UI.Text>(true);
        }
        if (label == null && owner != null)
        {
            // 背包预制体使用 400 像素格子再缩放，装备页使用 100 像素格子。
            // 随格子尺寸调整标记，避免缩放后的文字过小。
            float labelScale = Mathf.Max(1f, ((RectTransform)parent).rect.width / 100f);
            var banner = new GameObject("EquippedOwnerLabel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            banner.layer = parent.gameObject.layer;
            banner.transform.SetParent(parent, false);
            var rect = (RectTransform)banner.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 24 * labelScale);
            var background = banner.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            background.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(banner.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(2, 0);
            textRect.offsetMax = new Vector2(-2, 0);
            label = textObject.GetComponent<UnityEngine.UI.Text>();
            label.font = style != null ? style.font : null;
            label.fontSize = Mathf.RoundToInt(14 * labelScale);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.RoundToInt(10 * labelScale);
            label.resizeTextMaxSize = label.fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.color = Color.white;
            label.raycastTarget = false;
            label.supportRichText = false;
        }
        if (label == null) return;
        if (style != null) label.font = style.font;
        label.text = owner != null ? $"{owner.NAME}已装配" : "";
        label.transform.parent.gameObject.SetActive(owner != null);
    }

    public static void EnsureScrollableGrid(RectTransform content)
    {
        var grid = content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid == null) return;
        if (content.GetComponentInParent<UnityEngine.UI.ScrollRect>() == null)
        {
            var root = new GameObject("PluginScrollView", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
            root.layer = content.gameObject.layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(content.parent, false);
            rect.SetSiblingIndex(content.GetSiblingIndex());
            rect.anchorMin = content.anchorMin;
            rect.anchorMax = content.anchorMax;
            rect.pivot = content.pivot;
            rect.sizeDelta = content.sizeDelta;
            rect.anchoredPosition = content.anchoredPosition;
            rect.localScale = content.localScale;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D), typeof(UnityEngine.UI.Image));
            viewport.layer = content.gameObject.layer;
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(rect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            content.SetParent(viewportRect, false);
            content.localScale = Vector3.one;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, rect.rect.height);
            content.anchoredPosition = Vector2.zero;
            var scroll = root.GetComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
        }
        if (grid.constraint != UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount)
        {
            float width = content.rect.width - grid.padding.horizontal;
            grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((width + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        }
        var fitter = content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
    }
}
