using UnityEngine;

/// <summary>玩家棋子自身的血条，直接读取最终生命值，避免依赖当前选中的状态面板。</summary>
public sealed class PlayerHealthBarUI : MonoBehaviour
{
    public UnitAttrCenter unitAttrCenter;
    public UnityEngine.UI.Image healthBar;
    public UnityEngine.UI.Image healthShadowBar;
    private readonly HealthBarFillAnimation healthAnimation = new();

    private void LateUpdate()
    {
        if (unitAttrCenter == null) return;
        float percent = unitAttrCenter.MaxHealth > 0
            ? (float)unitAttrCenter.CurHealth / unitAttrCenter.MaxHealth : 0f;
        healthAnimation.Update(healthBar, healthShadowBar, percent, unitAttrCenter);
    }

    private void OnDisable() => healthAnimation.Reset();
}
