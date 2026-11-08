using UnityEngine;

/// <summary>敌我头顶卡片共用的 AVG 素材与字体；只修改实例 UI，不改共享材质或原始素材。</summary>
public sealed class BattleHealthBarStyle : ScriptableObject
{
    public Sprite panelSprite;
    public Font font;
    [Min(1f)] public float borderScale = 1f; // 默认保留 AVG 单像素描边，避免缩细后丢边。

    /// <summary>将 AVG 原切片应用到实例面板；素材缺失时仍保留可读底色。</summary>
    public void ApplyPanel(UnityEngine.UI.Image image)
    {
        image.sprite = panelSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = Mathf.Max(1f, borderScale);
        image.color = panelSprite != null ? Color.white : (Color)GameConst.UIColors.Background;
        image.raycastTarget = false;
    }

    /// <summary>沿用 AVG 字体及文字色；不改变文字内容和交互。</summary>
    public void ApplyText(UnityEngine.UI.Text text, bool muted = false)
    {
        if (font != null) text.font = font;
        text.color = muted ? GameConst.UIColors.TextMuted : GameConst.UIColors.Text;
    }
}
