using UnityEngine;
using UnityEngine.EventSystems;

public class ActionBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UnityEngine.UI.Text actionNameText;
    public UnityEngine.UI.Text costText;
    private UnityEngine.UI.Image outline;
    private UnityEngine.UI.Text arrow;
    private bool selected;

    /// <summary>在现有按钮上装配稳定的名称、消耗和箭头列，装饰不拦截射线。</summary>
    public void Configure(Sprite fill, Sprite border)
    {
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image == null) image = gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = fill;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = GameConst.UIColors.Surface;
        image.raycastTarget = true;
        var button = GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = GameConst.UIColors.Ally;
        colors.pressedColor = GameConst.UIColors.Accent;
        colors.disabledColor = GameConst.UIColors.Disabled;
        button.colors = colors;
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            if (graphic != image) graphic.raycastTarget = false;
        if (outline == null && transform.Find("MenuRowOutline") != null)
            outline = transform.Find("MenuRowOutline").GetComponent<UnityEngine.UI.Image>();
        if (outline == null)
        {
            var go = new GameObject("MenuRowOutline", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(transform, false);
            outline = go.GetComponent<UnityEngine.UI.Image>();
            outline.rectTransform.anchorMin = Vector2.zero;
            outline.rectTransform.anchorMax = Vector2.one;
            outline.rectTransform.offsetMin = outline.rectTransform.offsetMax = Vector2.zero;
        }
        outline.sprite = border;
        outline.type = UnityEngine.UI.Image.Type.Sliced;
        outline.color = GameConst.UIColors.Border;
        outline.raycastTarget = false;
        ConfigureText(actionNameText, 12, -86, TextAnchor.MiddleLeft);
        ConfigureText(costText, 0, -28, TextAnchor.MiddleRight);
        if (costText != null)
        {
            costText.rectTransform.anchorMin = new Vector2(1, 0);
            costText.rectTransform.anchorMax = Vector2.one;
            costText.rectTransform.offsetMin = new Vector2(-82, 0);
        }
        if (arrow == null && transform.Find("SubmenuArrow") != null)
            arrow = transform.Find("SubmenuArrow").GetComponent<UnityEngine.UI.Text>();
        if (arrow == null && actionNameText != null)
        {
            arrow = Instantiate(actionNameText, transform, false);
            arrow.name = "SubmenuArrow";
            ConfigureText(arrow, 0, -10, TextAnchor.MiddleRight);
            arrow.text = "›";
        }
        SetSubmenu(false);
    }

    private static void ConfigureText(UnityEngine.UI.Text text, float left, float right, TextAnchor alignment)
    {
        if (text == null) return;
        var rect = text.rectTransform;
        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, 0);
        rect.offsetMax = new Vector2(right, 0);
        text.fontSize = 18;
        text.resizeTextForBestFit = false;
        text.alignment = alignment;
        text.color = GameConst.UIColors.Text;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    /// <summary>标识当前展开的分类，切换或重开时由菜单同步清除。</summary>
    public void SetSelected(bool value)
    {
        selected = value;
        if (outline != null) outline.color = value ? GameConst.UIColors.Accent : GameConst.UIColors.Border;
    }

    public void SetSubmenu(bool value) { if (arrow != null) arrow.gameObject.SetActive(value); }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outline != null) outline.color = GameConst.UIColors.Accent;
    }
    public void OnPointerExit(PointerEventData eventData) => SetSelected(selected);
}
