using UnityEngine;

/// <summary>只覆盖将被扣除的生命区段；黄色额外伤害按未缩放时间呼吸，不改变真实血条。</summary>
internal sealed class DamagePreviewFillAnimation
{
    private bool showing;
    private float startedAt; // 同一次瞄准持续刷新时保留呼吸相位。

    public void Update(UnityEngine.UI.Image bar, UnityEngine.UI.Image minimum, UnityEngine.UI.Image maximum,
        float current, float afterMinimum, float afterMaximum, bool visible)
    {
        if (!visible)
        {
            Reset(minimum, maximum);
            return;
        }
        if (!showing) startedAt = Time.unscaledTime;
        showing = true;
        current = Mathf.Clamp01(current);
        afterMinimum = Mathf.Clamp(afterMinimum, 0, current);
        afterMaximum = Mathf.Clamp(afterMaximum, 0, afterMinimum);
        // 白色是确定扣除部分，黄色只覆盖最小伤害以外的可能损血，不能铺到剩余生命上。
        Segment(bar, minimum, afterMinimum, current, GameConst.UIColors.DamagePreviewMinimum);
        Color tint = GameConst.UIColors.DamagePreviewMaximum;
        float wave = (1f - Mathf.Cos((Time.unscaledTime - startedAt) * (2f * Mathf.PI / 1.8f))) * .5f;
        tint.a = Mathf.Lerp(.18f, 1f, wave);
        Segment(bar, maximum, afterMaximum, afterMinimum, tint);
    }

    public void Reset(UnityEngine.UI.Image minimum, UnityEngine.UI.Image maximum)
    {
        showing = false;
        if (minimum != null) { minimum.color = GameConst.UIColors.DamagePreviewMinimum; minimum.gameObject.SetActive(false); }
        if (maximum != null) { maximum.color = GameConst.UIColors.DamagePreviewMaximum; maximum.gameObject.SetActive(false); }
    }

    private static void Segment(UnityEngine.UI.Image bar, UnityEngine.UI.Image image, float from, float to, Color color)
    {
        if (bar == null || image == null) return;
        image.gameObject.SetActive(to > from);
        image.color = color;
        image.fillAmount = 1;
        var source = bar.rectTransform;
        var rect = image.rectTransform;
        rect.anchorMin = source.anchorMin;
        rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot;
        rect.localRotation = source.localRotation;
        rect.localScale = source.localScale;
        rect.sizeDelta = source.sizeDelta + new Vector2(source.rect.width * (to - from - 1), 0);
        rect.anchoredPosition3D = source.anchoredPosition3D + Vector3.right *
            (source.rect.width * (from + source.pivot.x * (to - from - 1)));
    }
}
