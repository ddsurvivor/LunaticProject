using UnityEngine;

/// <summary>旧伤害预制体的兼容入口；战斗伤害直接使用TipTextManager.ShowDamage。</summary>
public class DamageText : MonoBehaviour
{
    public UnityEngine.UI.Text text;
    public float duration = 1.5f;
    public float jumpForce = 1.5f;
    public float randomX = 1f;
    public void JumpOutNum(int num) => JumpOutNum(num, false);

    public void JumpOutNum(int num, bool isCritical, Color? color = null, int? fontSize = null)
    {
        var manager = BattleScene.Ins?.BM?.tipTextManager;
        if (manager != null)
        {
            manager.ShowDamage(transform, num, isCritical, color, fontSize);
            gameObject.SetActive(false);
            return;
        }
        // 独立预制体没有战斗管理器时仍复用同一表现组件。
        var tip = GetComponent<TipText>() ?? gameObject.AddComponent<TipText>();
        tip.ShowTip(num.ToString(), color ?? (isCritical ? Color.red : Color.white),
            fontSize ?? TipTextManager.CalculateDamageFontSize(num), Mathf.Max(0.01f, duration) * 2f);
    }
}
