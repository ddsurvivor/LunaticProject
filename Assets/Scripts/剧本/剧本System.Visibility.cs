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

    /// <summary>淡出并隐藏阅读器。此方法不标记剧情完成。</summary>
    public void HideReader() => HideReader(null);

    /// <summary>淡出并隐藏阅读器，完成后执行回调。</summary>
    public void HideReader(Action onHidden)
    {
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

    /// <summary>战斗专用：不等待淡出，并取消旧关闭回调。</summary>
    public void HideReaderImmediately()
    {
        CancelReaderHide();
        gameObject.SetActive(false);
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
        var group = ReaderCanvasGroup;
        group.alpha = 1f;
        group.interactable = readerDefaultInteractable;
        group.blocksRaycasts = readerDefaultBlocksRaycasts;
    }
}
