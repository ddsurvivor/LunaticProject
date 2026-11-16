using System;
using DG.Tweening;
using UnityEngine;

public partial class 剧本System
{
    /// <summary>阅读器通过关闭入口完全隐藏时通知宿主；禁用或卸载不触发剧情完成。</summary>
    public event Action ReaderClosed;
    /// <summary>宿主暂停世界时间时使用真实时间推进阅读器，默认保留 AVG 的原计时方式。</summary>
    public bool UseUnscaledTime { get; set; }
    internal object WaitForReaderSeconds(float seconds) => UseUnscaledTime
        ? (object)new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
    [Header("剧情关闭显示")]
    [SerializeField, Min(0f)] private float readerHideDuration = 0.5f;
    private CanvasGroup readerCanvasGroup;
    private Tween readerHideTween;
    private Action readerHiddenCallback;
    private bool readerHiding;
    private bool readerDefaultInteractable;
    private bool readerDefaultBlocksRaycasts;
    private bool readerAudioStopped = true; // 同一次关闭仅清理一次，避免淡出后的禁用误停新页面音乐。

    public bool IsReaderHiding => readerHiding;

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

    /// <summary>立即停止音乐和音效，再淡出隐藏阅读器；不标记剧情完成。</summary>
    public void HideReader() => HideReader(null);

    /// <summary>立即停止音乐和音效，再淡出隐藏阅读器，完全隐藏后执行回调。</summary>
    public void HideReader(Action onHidden)
    {
        StopReaderAudio();
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
                ReaderClosed?.Invoke();
                callback?.Invoke();
            });
    }

    /// <summary>立即停止音乐和音效并隐藏阅读器，取消旧关闭回调。</summary>
    public void HideReaderImmediately()
    {
        StopReaderAudio();
        bool wasVisible = gameObject.activeInHierarchy;
        CancelReaderHide();
        gameObject.SetActive(false);
        if (wasVisible) ReaderClosed?.Invoke();
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
        if (isActiveAndEnabled) readerAudioStopped = false;
        var group = ReaderCanvasGroup;
        group.alpha = 1f;
        group.interactable = readerDefaultInteractable;
        group.blocksRaycasts = readerDefaultBlocksRaycasts;
    }

    private void StopReaderAudio()
    {
        if (readerAudioStopped) return;
        readerAudioStopped = true;
        var audio = GM.Ins != null ? GM.Ins.AM : null;
        if (audio == null) return;
        // 同时取消未完成的加载，关闭后不能由旧回调重启音乐或循环音效。
        audio.StopAll();
        audio.停止音乐();
    }
}
