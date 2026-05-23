using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 迷雾交互行为
/// </summary>
public class FogArea : InteractArea
{
    public GameObject fogEffect;
    public GameObject hightlightEffect;

    private float delay = 0.8f;

    /// <summary>
    /// 关闭迷雾效果
    /// </summary>
    public override void TriggerAction(PieceController piece = null)
    {
        base.TriggerAction();
        //fogEffect?.SetActive(false);
        // fog effect 下的所有sprite renderer全部淡出，之后再关闭对象
        if (fogEffect != null)
        {
            SpriteRenderer[] renderers = fogEffect.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.DOFade(0, delay)
                    .OnComplete(() => { renderer.gameObject.SetActive(false); });
            }
        }

        DOVirtual.DelayedCall(delay, () => { gameObject.SetActive(false); });

        BattleScene.Ins.BM.AIController.OnScanFog(fogEffect);
        piece?.interactAreas.Remove(this);
    }

    // 鼠标移入时显示高亮
    private void OnMouseEnter()
    {
        if (hightlightEffect != null)
        {
            hightlightEffect.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        if (hightlightEffect != null)
        {
            hightlightEffect.SetActive(false);
        }
    }
}