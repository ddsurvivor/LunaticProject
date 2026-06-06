using System;
using DG.Tweening;
using UnityEngine;


public class SidePanel : MonoBehaviour
{
    public Transform panel;

    public float slidePosX = 500f;
    public float slideDuration = 0.3f;
    public KeyCode toggleKey = KeyCode.Tab;

    public void Update()
    {
        if (Input.GetKeyUp(toggleKey))
        {
            // 如果侧栏当前不可见，则显示；如果可见，则关闭
            if (!panel.gameObject.activeSelf)
            {
                ShowPanel();
            }
            else
            {
                ClosePanel();
            }
        }
    }

    public void ShowPanel()
    {
        // 激活侧栏，并移动到指定点
        panel.gameObject.SetActive(true);
        panel.DOMoveX(0, slideDuration).From(-slidePosX).SetEase(Ease.OutCubic);
        GM.Ins.AM.PlayAudio(AudioCueType.Expand);
    }

    public void ClosePanel()
    {
        // 移动侧栏回原位，并在动画完成后隐藏
        panel.DOMoveX(-slidePosX, slideDuration).SetEase(Ease.InCubic)
            .OnComplete(() =>
        {
            panel.gameObject.SetActive(false);
        });
        GM.Ins.AM.PlayAudio(AudioCueType.Collapse);
    }
}