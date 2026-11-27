using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人的生命值面板
/// </summary>
public class HpBarUI : MonoBehaviour
{
    public UnityEngine.UI.Image hpBarFill; // 血条填充部分的 Transform
    public UnityEngine.UI.Image shadowFill; // 拖影血条
    public UnityEngine.UI.Image preDamageFill; // 确定扣除的白色伤害区段。
    public UnityEngine.UI.Image preDamageFillYellow; // 额外可能扣除的黄色呼吸区段。
    public UnityEngine.UI.Image hpBarRenderer; // 血条的 Image 组件，用于调整透明度
    public List<GameObject> mpIcons;


    /// <summary>统一头顶读取的伤害预览快照；剩余血量按最大生命归一化，关闭或停用即失效。</summary>
    public bool HasDamagePreview { get; private set; }
    public float PreviewAfterMaximum { get; private set; }
    public float PreviewAfterMinimum { get; private set; }
    private float currentHpPercent = 1f; // 当前血量百分比

    private float shadowSpeed = 1.2f; // 拖影血条的动画速度
    private float barSpeed = 0.3f; // 血条本体的动画速度

    private Tweener shadowTweener; // 拖影动画的 Tweener 对象
    private readonly DamagePreviewFillAnimation damagePreview = new();
    
    public void FullHp()
    {
        StopAnimations();
        HasDamagePreview = false;
        currentHpPercent = 1f;
        hpBarFill.fillAmount = 1f;
        shadowFill.fillAmount = 1f;
        preDamageFill.fillAmount = 1f;
        // 全部隐藏
        hpBarFill.gameObject.SetActive(false);
        shadowFill.gameObject.SetActive(false);
        preDamageFill.gameObject.SetActive(false);
        if (preDamageFillYellow != null) preDamageFillYellow.gameObject.SetActive(false);
    }
    public void UpdateHpBar(float precent)
    {
        HasDamagePreview = false;
        hpBarFill.DOKill();
        gameObject.SetActive(true);
        shadowTweener?.Kill(); // 结束当前的拖影动画
        SetHpBarAlpha(1f);
        hpBarFill.gameObject.SetActive(true);
        // 更新当前血量百分比
        currentHpPercent = precent;

        // 直接更新血条填充部分
        hpBarFill.DOFillAmount(currentHpPercent, barSpeed).SetUpdate(true);

        // 启动拖影动画Dotween
        shadowTweener = shadowFill.DOFillAmount(currentHpPercent, shadowSpeed).SetEase(Ease.OutQuad).SetUpdate(true);
        preDamageFill.gameObject.SetActive(false);
        if (preDamageFillYellow != null) preDamageFillYellow.gameObject.SetActive(false);
    }

    /// <summary>显示固定伤害预览并更新共享头顶快照；重复刷新保留呼吸相位。</summary>
    public void ShowPreDamage(float damagePercent)
    {
        gameObject.SetActive(true);
        if (preDamageFillYellow != null) preDamageFillYellow.gameObject.SetActive(false);
        float afterDamagePercent = Mathf.Clamp01(currentHpPercent - damagePercent);
        HasDamagePreview = true;
        PreviewAfterMaximum = PreviewAfterMinimum = afterDamagePercent;

        RefreshPreview();
    }

    /// <summary>按最大生命换算伤害区间，沿用正式伤害预览结果，不重算战斗公式。</summary>
    public void ShowPreDamage(DamagePreviewResult preview, int maxHealth)
    {
        // 1. 统一用最大生命值将伤害换算为血条比例。
        if (maxHealth <= 0)
        {
            ClosePreDamage();
            return;
        }
        ShowPreDamage((float)preview.MaxDamage / maxHealth);
        PreviewAfterMinimum = Mathf.Clamp01(currentHpPercent - (float)preview.MinDamage / maxHealth);

        RefreshPreview();
    }

    /// <summary>清除预览快照和区段，立即恢复正常生命显示。</summary>
    public void ClosePreDamage()
    {
        HasDamagePreview = false;
        damagePreview.Reset(preDamageFill, preDamageFillYellow);
        SetHpBarAlpha(1f);
        preDamageFill.gameObject.SetActive(false);
        if (preDamageFillYellow != null) preDamageFillYellow.gameObject.SetActive(false);
        if(currentHpPercent > 0.99f)
            gameObject.SetActive(false);
    }
    private void SetHpBarAlpha(float alpha)
    {
        if (hpBarRenderer != null)
        {
            var c = hpBarRenderer.color;
            c.a = alpha;
            hpBarRenderer.color = c;
        }
    }
    
    private void OnDisable() => StopAnimations();
    private void StopAnimations()
    {
        HasDamagePreview = false;
        shadowTweener?.Kill();
        damagePreview.Reset(preDamageFill, preDamageFillYellow);
        if (hpBarFill != null) hpBarFill.DOKill();
        SetHpBarAlpha(1f);
    }

    public void UpdateMpIcons(int mpCount)
    {
        for (int i = 0; i < mpIcons.Count; i++)
        {
            mpIcons[i].SetActive(i < mpCount);
        }
    }

    private void LateUpdate() => RefreshPreview();

    private void RefreshPreview() => damagePreview.Update(hpBarFill, preDamageFill, preDamageFillYellow,
        currentHpPercent, PreviewAfterMinimum, PreviewAfterMaximum, HasDamagePreview);
}
