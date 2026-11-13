using System;
using DG.Tweening;
using UnityEngine;

public partial class 剧本System
{
    [Header("剧情关闭显示")]
    [SerializeField, Min(0f)] private float readerHideDuration = 0.5f;
    private CanvasGroup readerCanvasGroup;
    private Tween readerHideTween;
    private Action readerHiddenCallback;
    private bool readerHiding;
    private bool readerDefaultInteractable;
    private bool readerDefaultBlocksRaycasts;
    private bool storyPlaybackStopped = true; // 未打开及已关闭的阅读器不干扰新界面的音频。

    public bool IsReaderHiding => readerHiding;

    /// <summary>当前阅读会话已关闭；重新显示阅读器或设置新剧本后恢复播放。</summary>
    public bool IsStoryClosed => storyPlaybackStopped;

    private void PlayStorySound(string audioName, int loop = -1)
    {
        if (!storyPlaybackStopped && isActiveAndEnabled && !IsReaderHiding &&
            GM.Ins != null && !GM.Ins.IsTransitioning)
            GM.Ins.AM?.播放音效(audioName, loop);
    }

    private CanvasGroup ReaderCanvasGroup
    {
        get
        {
            if (readerCanvasGroup == null)
            {
                readerCanvasGroup = GetComponent<CanvasGroup>();
                if (readerCanvasGroup == null) readerCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                readerDefaultInteractable = readerCanvasGroup.interactable;
                readerDefaultBlocksRaycasts = readerCanvasGroup.blocksRaycasts;
            }
            return readerCanvasGroup;
        }
    }

    /// <summary>取消关闭动画，并恢复阅读器显示。</summary>
    public void ShowReader()
    {
        RestoreReaderVisibility();
        gameObject.SetActive(true);
    }

    /// <summary>结束当前剧情并关闭阅读器；不写入任务完成进度。</summary>
    public void CloseStory() => CloseStory(null);

    /// <summary>统一结束剧情、停止音乐和音效并关闭阅读器；回调在隐藏后执行，立即关闭时取消旧回调。</summary>
    public void CloseStory(Action onClosed, bool immediately = false)
    {
        EndReadingStory();
        if (immediately)
        {
            HideReaderImmediately();
            onClosed?.Invoke();
        }
        else
        {
            HideReader(onClosed);
        }
    }

    /// <summary>立即停止音乐和音效后淡出阅读器；保留未完成剧情的读档状态。</summary>
    public void HideReader() => HideReader(null);

    /// <summary>立即停止音乐和音效后淡出阅读器，隐藏后执行回调；不标记剧情完成。</summary>
    public void HideReader(Action onHidden)
    {
        StopStoryPlayback();
        if (readerHiding) return;
        if (!gameObject.activeInHierarchy || readerHideDuration <= 0f)
        {
            HideReaderImmediately();
            onHidden?.Invoke();
            return;
        }

        CancelReaderHide();
        readerHiding = true;
        readerHiddenCallback = onHidden;
        autoPlayElapsed = 0f;
        StopReaderShake();
        var group = ReaderCanvasGroup;
        group.interactable = false;
        // 淡出期间仍遮挡地图，防止一次点击同时触发下方任务节点。
        group.blocksRaycasts = true;
        readerHideTween = group.DOFade(0f, readerHideDuration).SetUpdate(true)
            .OnComplete(() =>
            {
                var callback = readerHiddenCallback;
                readerHiddenCallback = null;
                readerHideTween = null;
                gameObject.SetActive(false);
                callback?.Invoke();
            });
    }

    /// <summary>立即停止音乐和音效并隐藏阅读器，取消旧关闭回调；不标记剧情完成。</summary>
    public void HideReaderImmediately()
    {
        StopStoryPlayback();
        CancelReaderHide();
        gameObject.SetActive(false);
    }

    private void StopStoryPlayback()
    {
        autoPlayElapsed = 0f;
        StopReaderShake();
        if (storyPlaybackStopped) return;
        storyPlaybackStopped = true;
        // 同时取消尚未完成的音乐和音效加载，避免关闭后由旧回调重新播放。
        GM.Ins?.AM?.StopAll();
        GM.Ins?.AM?.停止音乐();
    }

    /// <summary>取消关闭动画及回调，防止旧操作影响新剧情。</summary>
    private void CancelReaderHide()
    {
        readerHideTween?.Kill();
        readerHideTween = null;
        readerHiddenCallback = null;
        readerHiding = false;
    }

    private void RestoreReaderVisibility()
    {
        CancelReaderHide();
        storyPlaybackStopped = false;
        var group = ReaderCanvasGroup;
        group.alpha = 1f;
        group.interactable = readerDefaultInteractable;
        group.blocksRaycasts = readerDefaultBlocksRaycasts;
    }
}
