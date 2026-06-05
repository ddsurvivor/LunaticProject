using System;
using DG.Tweening;
using UnityEngine;


public class SidePanel : MonoBehaviour
{
    public Transform panel;

    public float slidePosX = 500f;
    public float slideDuration = 0.3f;

    public void Update()
    {
        
    }

    public void ShowPanel()
    {
        // 激活侧栏，并移动到指定点
        panel.gameObject.SetActive(true);
        panel.DOMoveX(0, slideDuration).From(-slidePosX).SetEase(Ease.OutCubic);
    }

    public void ClosePanel()
    {
        // 移动侧栏回原位，并在动画完成后隐藏
        panel.DOMoveX(-slidePosX, slideDuration).SetEase(Ease.InCubic)
            .OnComplete(() =>
        {
            panel.gameObject.SetActive(false);
        });
    }
}