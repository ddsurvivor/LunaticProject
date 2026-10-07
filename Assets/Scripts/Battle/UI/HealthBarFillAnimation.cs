using DG.Tweening;
using UnityEngine;

/// <summary>前层即时更新，白色后层保留原血量后追上；不延迟实际生命值。</summary>
internal sealed class HealthBarFillAnimation
{
    private UnityEngine.UI.Image image;
    private UnityEngine.UI.Image shadowImage;
    private object owner;
    private float target;
    private bool initialized;
    private Tween tween;

    public void Update(UnityEngine.UI.Image bar, UnityEngine.UI.Image shadow, float percent, object currentOwner)
    {
        if (bar == null) { Reset(); return; }
        percent = float.IsNaN(percent) ? 0f : Mathf.Clamp01(percent);
        bool snap = !initialized || image != bar || shadowImage != shadow ||
            !ReferenceEquals(owner, currentOwner) || !bar.gameObject.activeInHierarchy;
        if (!snap && Mathf.Approximately(target, percent)) return;

        tween?.Kill();
        tween = null;
        float previousFill = bar.fillAmount;
        image = bar;
        shadowImage = shadow;
        owner = currentOwner;
        target = percent;
        initialized = true;
        bar.fillAmount = target;
        if (shadow == null) return;
        if (snap) { shadow.fillAmount = target; return; }
        // 连续伤害保留尚未追上的白色区段；治疗立即覆盖对应区段。
        shadow.fillAmount = Mathf.Max(shadow.fillAmount, Mathf.Max(previousFill, target));
        if (!Mathf.Approximately(shadow.fillAmount, target))
            tween = shadow.DOFillAmount(target, 1.2f).SetDelay(0.15f)
                .SetEase(Ease.OutQuad).SetUpdate(false);
    }

    public void Reset()
    {
        tween?.Kill();
        tween = null;
        if (initialized && image != null) image.fillAmount = target;
        if (initialized && shadowImage != null) shadowImage.fillAmount = target;
        image = shadowImage = null;
        owner = null;
        initialized = false;
    }
}
