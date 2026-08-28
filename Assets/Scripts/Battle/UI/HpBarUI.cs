using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人的生命值面板
/// </summary>
public class HpBarUI : MonoBehaviour
{
    public Image hpBarFill; // 血条填充部分的 Transform
    public Image shadowFill; // 拖影血条
    public Image preDamageFill; // 伤害显示血条(红色)
    public Image preDamageFillYellow; // 预伤害黄色血条
    public Image hpBarRenderer; // 血条的 Image 组件，用于调整透明度
    public List<GameObject> mpIcons;


    private float currentHpPercent = 1f; // 当前血量百分比

    private float shadowSpeed = 1.2f; // 拖影血条的动画速度
    private float barSpeed = 0.3f; // 血条本体的动画速度

    private Tweener shadowTweener; // 拖影动画的 Tweener 对象
    private Tweener hpBarAlphaTweener; // 血条透明度动画
    
    public void FullHp()
    {
        currentHpPercent = 1f;
        hpBarFill.fillAmount = 1f;
        shadowFill.fillAmount = 1f;
        preDamageFill.fillAmount = 1f;
        // 全部隐藏
        hpBarFill.gameObject.SetActive(false);
        shadowFill.gameObject.SetActive(false);
        preDamageFill.gameObject.SetActive(false);
    }
    public void UpdateHpBar(float precent)
    {
        gameObject.SetActive(true);
        shadowTweener?.Kill(); // 结束当前的拖影动画
        hpBarAlphaTweener?.Kill();
        SetHpBarAlpha(1f);
        hpBarFill.gameObject.SetActive(true);
        // 更新当前血量百分比
        currentHpPercent = precent;

        // 直接更新血条填充部分
        hpBarFill.DOFillAmount(currentHpPercent, barSpeed);

        // 启动拖影动画Dotween
        shadowTweener = shadowFill.DOFillAmount(currentHpPercent, shadowSpeed).SetEase(Ease.OutQuad);
        preDamageFill.gameObject.SetActive(false);
    }

    public void ShowPreDamage(float damagePercent)
    {
        gameObject.SetActive(true);
        float afterDamagePercent = Mathf.Clamp01(currentHpPercent - damagePercent);

        // 立即显示预伤害血条
        preDamageFill.fillAmount = afterDamagePercent;
        preDamageFill.gameObject.SetActive(true);

        // 让本体血条透明度循环渐变
        hpBarAlphaTweener?.Kill();
        hpBarAlphaTweener = hpBarAlphaTweener = hpBarRenderer
            .DOFade(0.5f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);;
    }

    public void ShowPreDamage(DamagePreviewResult damagePreviewResult)
    {
        
    }
    
    public void ClosePreDamage()
    {
        hpBarAlphaTweener?.Kill();
        SetHpBarAlpha(1f);
        preDamageFill.gameObject.SetActive(false);
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
    
    public void UpdateMpIcons(int mpCount)
    {
        for (int i = 0; i < mpIcons.Count; i++)
        {
            mpIcons[i].SetActive(i < mpCount);
        }
    }
}