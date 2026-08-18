using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ChapterPanel : MonoBehaviour
{
    public Text chapterText;

    public float startDelay = 1.0f;

    private Sequence fadeTweener;

    [SerializeField]private CanvasGroup _canvasGroup;
    [SerializeField]
    private GameObject checkContinuePanel;// 继续或保存面板
    
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;      // 播放视频的 VideoPlayer 组件
    [SerializeField] private GameObject videoRawImageUI;  // 用于渲染视频的 UI 界面 (例如 RawImage)
    [SerializeField] private VideoClip singleChapterVideo;// 针对 ShowChapter 的转场视频
    [SerializeField] private VideoClip multiChapterVideo; // 针对 ShowChapters 的转场视频
    
    // 开启面板，淡入淡出显示文本章节标题
    public void ShowChapter(string chapterName, float fadeDuration = 1f, float displayDuration = 2f)
    {
        fadeTweener?.Kill(); // 如果之前有正在进行的淡入淡出动画，先停止它
        gameObject.SetActive(true);
        checkContinuePanel.SetActive(false); // 隐藏继续或保存面板
        StartCoroutine(PlayVideoAndExecute(singleChapterVideo, () =>
        {
            ExecuteShowChapter(chapterName, fadeDuration, displayDuration);
        }));
    }

    public void ShowChapters(string[] strings, float fadeDuration = 1f, float displayDuration = 2f)
    {
        fadeTweener?.Kill(); // 如果之前有正在进行的淡入淡出动画，先停止它
        gameObject.SetActive(true);
        checkContinuePanel.SetActive(false); // 隐藏继续或保存面板
        StartCoroutine(PlayVideoAndExecute(multiChapterVideo, () =>
        {
            ExecuteShowChapters(strings, fadeDuration, displayDuration);
        }));
    }
    // 播放视频的协程
    private IEnumerator PlayVideoAndExecute(VideoClip clip, System.Action onVideoEnd)
    {
        if (videoPlayer != null && clip != null && videoRawImageUI != null)
        {
            // 激活视频 UI 控件
            videoRawImageUI.SetActive(true);
            
            // 设置并准备视频
            videoPlayer.clip = clip;
            videoPlayer.Prepare();
            
            // 等待视频准备完成
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            // 开始播放视频
            videoPlayer.Play();

            // 等待视频播放结束
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }

            // 隐藏视频 UI 控件
            videoRawImageUI.SetActive(false);
        }

        // 执行原本的章节逻辑
        onVideoEnd?.Invoke();
    }

    // 开启面板，淡入淡出显示文本章节标题
    public void ExecuteShowChapter(string chapterName, float fadeDuration = 1f, float displayDuration = 2f)
    {
        
        // 使用DOTween fade实现
        chapterText.color =
            new Color(chapterText.color.r, chapterText.color.g, chapterText.color.b, 0f); // 初始透明
        chapterText.text = chapterName;
        fadeTweener = DOTween.Sequence();
        fadeTweener.AppendInterval(startDelay);
        fadeTweener.Append(chapterText.DOFade(1f, fadeDuration).SetEase(Ease.Linear));
        fadeTweener.AppendInterval(displayDuration);
        fadeTweener.Append(chapterText.DOFade(0f, fadeDuration));
        fadeTweener.AppendCallback(() => checkContinuePanel.SetActive(true));
        //fadeTweener.AppendCallback(() => gameObject.SetActive(false));
    }

    public void ExecuteShowChapters(string[] strings, float fadeDuration = 1f, float displayDuration = 2f)
    {
        
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

        fadeTweener.AppendCallback(() => checkContinuePanel.SetActive(true));
    }
    
    public void OnClickContinue()
    {
        // 淡出隐藏整体面板
        _canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
        {
            this.gameObject.SetActive(false);
            checkContinuePanel.SetActive(false);
            _canvasGroup.alpha = 1f; // 重置alpha值，以便下次显示时正常显示
            GM.Ins.AM.停止音效("chapter-change");
            GM.Ins.AM.停止音效("day-change");
        });
    }
}
