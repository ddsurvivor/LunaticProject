using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class UIManager
{
    private readonly Queue<string> notices = new Queue<string>();
    private float noticeRemaining; // 当前提示剩余的真实秒数，暂停时不递减。
    private Canvas messageCanvas;
    public int PendingNoticeCount => notices.Count;
    public RectTransform NoticeRect => skillNameDisplay;
    public RectTransform NotificationRect => notificationManager != null ? notificationManager.container as RectTransform : null;

    private void InitializeMessageLanes()
    {
        messageCanvas = pieceInfoPanel.GetComponentInParent<Canvas>().rootCanvas;
        if (skillNameDisplay != null)
        {
            skillNameDisplay.SetParent(messageCanvas.transform, false);
            PlaceMessageRect(skillNameDisplay, new Vector2(.5f, 1), new Vector2(0, -270), new Vector2(500, 64));
            skillNamText.transform.SetParent(skillNameDisplay, false);
            foreach (var graphic in skillNameDisplay.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            { graphic.raycastTarget = false; if (graphic != skillNamText) graphic.enabled = false; }
            var background = skillNameDisplay.GetComponent<UnityEngine.UI.Image>();
            if (background == null) background = skillNameDisplay.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.enabled = true; background.raycastTarget = false; background.color = new Color(.055f, .085f, .11f, .94f);
            var frame = MakeRect("MessageFrame", skillNameDisplay, new Vector2(500, 64)).gameObject.AddComponent<BattlePanelFrame>();
            frame.color = new Color(.85f, .66f, .3f); frame.raycastTarget = false;
            skillNamText.transform.SetAsLastSibling();
            if (skillNamText != null)
            {
                skillNamText.rectTransform.anchorMin = Vector2.zero; skillNamText.rectTransform.anchorMax = Vector2.one;
                skillNamText.rectTransform.offsetMin = new Vector2(12, 4); skillNamText.rectTransform.offsetMax = new Vector2(-12, -4);
                skillNamText.fontStyle = FontStyle.Normal; skillNamText.fontSize = 22; skillNamText.alignment = TextAnchor.MiddleCenter;
            }
            skillNameDisplay.gameObject.SetActive(false);
        }
        if (NotificationRect != null)
        {
            NotificationRect.SetParent(messageCanvas.transform, false);
            PlaceMessageRect(NotificationRect, Vector2.one, new Vector2(-18, -280), new Vector2(340, 280));
        }
        if (logPanel != null)
        {
            var rect = logPanel.transform as RectTransform;
            rect.SetParent(messageCanvas.transform, false);
            PlaceMessageRect(rect, new Vector2(1, 0), new Vector2(-18, 180), new Vector2(420, 300));
            logPanel.EnsureScrolling();
            logPanel.ClearLog();
        }
    }

    private static void PlaceMessageRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
        rect.sizeDelta = size; rect.anchoredPosition = position;
    }

    /// <summary>临时提示写入现有有界日志并按顺序显示；待播最多32条，超限丢弃最旧提示。</summary>
    public void EnqueueNotice(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || IsResultVisible) return;
        logPanel?.PlayerLog(message);
        if (notices.Count >= 32) notices.Dequeue();
        notices.Enqueue(message);
    }

    /// <summary>聚能提示使用消息区；旧闪图立即清理，不遮挡选人或连续弹出。</summary>
    public void ShowBurstNotice(bool entering)
    {
        foreach (var flash in new[] { burstStart, burstEnd })
            if (flash != null) { flash.DOKill(); flash.raycastTarget = false; flash.gameObject.SetActive(false); }
        EnqueueNotice(entering ? "聚能开始" : "聚能结束");
    }

    private void UpdateMessageLanes()
    {
        if (messageCanvas == null) return;
        if (skillNameDisplay != null && !IsResultVisible && !BattleScene.Ins.TM.IsPaused)
        {
            noticeRemaining -= Time.unscaledDeltaTime;
            if (noticeRemaining <= 0)
            {
                if (notices.Count > 0)
                {
                    skillNamText.text = notices.Dequeue();
                    skillNameDisplay.gameObject.SetActive(true);
                    noticeRemaining = 1.2f;
                }
                else skillNameDisplay.gameObject.SetActive(false);
            }
        }
        ClampMessageRect(skillNameDisplay);
        ClampMessageRect(NotificationRect);
        if (logPanel != null) ClampMessageRect(logPanel.transform as RectTransform);
    }

    private void ClampMessageRect(RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return;
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var camera = messageCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : messageCanvas.worldCamera;
        var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        var safe = Screen.safeArea;
        var offset = new Vector2(min.x < safe.xMin ? safe.xMin - min.x : max.x > safe.xMax ? safe.xMax - max.x : 0,
            min.y < safe.yMin ? safe.yMin - min.y : max.y > safe.yMax ? safe.yMax - max.y : 0);
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)messageCanvas.transform,
            min + offset, camera, out var target)) rect.position += target - corners[0];
    }

    /// <summary>取消本场待播提示与旧Tween，避免隐藏窗口或转场后回调复活。</summary>
    public void ClearTransientMessages()
    {
        notices.Clear(); noticeRemaining = 0;
        if (skillNameDisplay != null) { skillNameDisplay.DOKill(); skillNameDisplay.gameObject.SetActive(false); }
        foreach (var image in new[] { burstStart, burstEnd, checkSuccess, checkFail })
            if (image != null) { image.DOKill(); image.gameObject.SetActive(false); }
    }

}
