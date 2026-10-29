using UnityEngine;

/// <summary>复用真实角色状态显示行动费用；预览叠层不修改数值或原进度条填充。</summary>
public sealed class ActionCostPreviewUI : MonoBehaviour
{
    [SerializeField] private UnitAttrCenter unit;
    [SerializeField] private ActionPointPipsUI points;
    [SerializeField] private UnityEngine.UI.Image manaBar;
    [SerializeField] private UnityEngine.UI.Image ammoBar;
    [SerializeField] private UnityEngine.UI.Image manaOverlay;
    [SerializeField] private UnityEngine.UI.Image ammoOverlay;
    [SerializeField] private bool driveResourceBars;
    private ActionResourceCost cost;
    private bool previewing;

    /// <summary>角色切换时先恢复旧预览，再绑定跟随面板的真实数据源。</summary>
    public void Bind(UnitAttrCenter source)
    {
        Clear();
        if (unit != source && points != null) points.ResetPresentation();
        unit = source;
    }

    /// <summary>绑定当前角色及只读费用，后续刷新持续使用该角色的真实资源。</summary>
    public void Show(UnitAttrCenter source, ActionResourceCost value)
    {
        if (source == null) { Clear(); return; }
        if (unit != source) Bind(source);
        unit = source;
        cost = value;
        previewing = true;
    }

    /// <summary>立即清除呼吸叠层并恢复 AP 正常颜色，保留真实资源显示。</summary>
    public void Clear()
    {
        previewing = false;
        if (points != null) points.SetPreview(0);
        if (manaOverlay != null) manaOverlay.gameObject.SetActive(false);
        if (ammoOverlay != null) ammoOverlay.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (unit == null || !unit.gameObject.activeInHierarchy || unit.CurHealth <= 0) { Clear(); return; }
        if (points != null)
        {
            points.SetPoints(unit.CurMovePoint, unit.MaxMovePoint);
            points.SetPreview(previewing ? cost.AP : 0);
        }
        if (driveResourceBars)
        {
            if (manaBar != null) manaBar.fillAmount = unit.MaxManaPoint > 0 ? (float)unit.ManaPoint / unit.MaxManaPoint : 0;
            if (ammoBar != null) ammoBar.fillAmount = unit.MaxAmmoCount > 0 ? (float)unit.AmmoCount / unit.MaxAmmoCount : 0;
        }
        float alpha = 0.25f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2));
        UpdateOverlay(manaOverlay, unit.ManaPoint, unit.MaxManaPoint, previewing ? cost.Mana : 0, alpha);
        UpdateOverlay(ammoOverlay, unit.AmmoCount, unit.MaxAmmoCount, previewing ? cost.Ammo : 0, alpha);
    }

    private static void UpdateOverlay(UnityEngine.UI.Image overlay, int current, int maximum, int amount, float alpha)
    {
        if (overlay == null) return;
        bool visible = amount > 0 && maximum > 0 && current > 0;
        overlay.gameObject.SetActive(visible);
        if (!visible) return;
        overlay.rectTransform.anchorMin = new Vector2(Mathf.Clamp01((float)Mathf.Max(0, current - amount) / maximum), 0);
        overlay.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)current / maximum), 1);
        overlay.rectTransform.offsetMin = overlay.rectTransform.offsetMax = Vector2.zero;
        var color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }

    private void OnDisable() => Clear();
}
