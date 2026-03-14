using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ChapterPanel : MonoBehaviour
{
    public Text chapterText;

    public float startDelay = 1.0f;

    private Sequence fadeTweener;

    // 开启面板，淡入淡出显示文本章节标题
    public void ShowChapter(string chapterName, float fadeDuration = 1f, float displayDuration = 2f)
    {
        fadeTweener?.Kill(); // 如果之前有正在进行的淡入淡出动画，先停止它
        gameObject.SetActive(true);
        // 使用DOTween fade实现
        chapterText.color =
            new Color(chapterText.color.r, chapterText.color.g, chapterText.color.b, 0f); // 初始透明
        chapterText.text = chapterName;
        fadeTweener = DOTween.Sequence();
        fadeTweener.AppendInterval(startDelay);
        fadeTweener.Append(chapterText.DOFade(1f, fadeDuration).SetEase(Ease.Linear));
        fadeTweener.AppendInterval(displayDuration);
        fadeTweener.Append(chapterText.DOFade(0f, fadeDuration));
        fadeTweener.AppendCallback(() => gameObject.SetActive(false));
    }

    public void ShowChapters(string[] strings, float fadeDuration = 1f, float displayDuration = 2f)
    {
        fadeTweener?.Kill(); // 如果之前有正在进行的淡入淡出动画，先停止它
        gameObject.SetActive(true);
        chapterText.color =
            new Color(chapterText.color.r, chapterText.color.g, chapterText.color.b, 0f); // 初始透明
        fadeTweener = DOTween.Sequence();
        fadeTweener.AppendInterval(startDelay);
        foreach (var str in strings)
        {
            fadeTweener.AppendCallback(() => chapterText.text = str);
            fadeTweener.Append(chapterText.DOFade(1f, fadeDuration).SetEase(Ease.Linear));
            fadeTweener.AppendInterval(displayDuration);
            fadeTweener.Append(chapterText.DOFade(0f, fadeDuration));
        }

        fadeTweener.AppendCallback(() => gameObject.SetActive(false));
    }
}
