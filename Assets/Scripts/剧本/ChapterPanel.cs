using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ChapterPanel : MonoBehaviour
{
    public Text chapterText;
    
    // 开启面板，淡入淡出显示文本章节标题
    public void ShowChapter(string chapterName, float fadeDuration = 1f, float displayDuration = 2f)
    {
        gameObject.SetActive(true);
        // 使用DOTween fade实现
        chapterText.color = new Color(chapterText.color.r, chapterText.color.g, chapterText.color.b, 0f); // 初始透明
        chapterText.text = chapterName;
        chapterText.DOFade(1f, fadeDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            // 显示章节标题一段时间后淡出
            chapterText.DOFade(0f, fadeDuration).SetDelay(displayDuration).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        });
    }
}
