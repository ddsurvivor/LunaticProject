using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class UIManager
{
    private GameObject resultOverlay;
    private GameObject pauseOverlay;
    private readonly List<ActivePause> pauseWindows = new List<ActivePause>();
    /// <summary>结果窗口显示期间禁止其他菜单接管输入。</summary>
    public bool IsResultVisible => resultOverlay != null && resultOverlay.activeInHierarchy;
    /// <summary>开始与结果窗口的输入占用状态；暂停本身由TimeManager持有。</summary>
    public bool BlocksWorldInput => IsResultVisible || (battleStartUIPanel != null && battleStartUIPanel.IsPlaying);

    private void InitializeWindows()
    {
        if (resultOverlay != null) return;
        foreach (var entry in keyPanelDic)
            if (entry.Value != null && entry.Value.GetComponent<ActivePause>() == null)
                entry.Value.AddComponent<ActivePause>();
        var canvas = pieceInfoPanel.GetComponentInParent<Canvas>().rootCanvas;
        InitializePauseWindow(canvas.transform);
        InitializeResultWindow(canvas.transform);
    }

    private RectTransform MakeRect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size; return rect;
    }

    // 模板即使被手动设为启用，也要在绑定前保持停用，避免提前取得暂停。
    private T InstantiateWindow<T>(T prefab, Transform parent) where T : Component
    {
        var staging = MakeRect("WindowStaging", parent, Vector2.zero);
        staging.gameObject.SetActive(false);
        var view = Instantiate(prefab, staging, false);
        view.gameObject.SetActive(false);
        view.transform.SetParent(parent, false);
        Destroy(staging.gameObject);
        return view;
    }

    /// <summary>使用现有胜负结果显示窗口；不发奖、不保存、不自动跳转场景。</summary>
    public void ShowBattleResult(bool win, FinishDrop drop, int exp, int currency = 0)
    {
        InitializeWindows();
        BattleScene.Ins.TM.SetFastForward(false);
        ClosePauseWindows();
        ClearActionFlow();
        ClearTransientMessages();
        notificationManager?.ClearNotifications();
        if (battleLossPanel != null) battleLossPanel.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);
        resultOverlay.SetActive(true); resultOverlay.transform.SetAsLastSibling();
        battleFinishPanel.ShowPanel(win, drop, exp, currency);
        var retryLabel = battleFinishPanel.retryButton.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (retryLabel != null) retryLabel.text = "重开战斗";
        var label = battleFinishPanel.continueButton.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (label != null) label.text = win ? "继续" : "退出到主菜单";
    }

    private void ClosePauseWindows()
    {
        pauseWindows.Clear();
        pauseWindows.AddRange(pieceInfoPanel.GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<ActivePause>(true));
        foreach (var window in pauseWindows)
            if (window != null && window.gameObject.activeInHierarchy &&
                (resultOverlay == null || !window.transform.IsChildOf(resultOverlay.transform)))
                window.gameObject.SetActive(false);
    }

    private void HandleWindowKeys()
    {
        if (IsResultVisible || GM.Ins.IsTransitioning || BlocksWorldInput) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseWindow();
            return;
        }
        foreach (var entry in keyPanelDic)
            if (entry.Value != null && Input.GetKeyDown(entry.Key))
                entry.Value.SetActive(!entry.Value.activeSelf);
    }

    /// <summary>切换Esc暂停窗口；关闭仅释放菜单持有的暂停，结果及转场期间不打开。</summary>
    public void TogglePauseWindow()
    {
        if (IsResultVisible || BlocksWorldInput || GM.Ins.IsTransitioning) return;
        var canvas = pieceInfoPanel.GetComponentInParent<Canvas>().rootCanvas;
        foreach (var window in canvas.GetComponentsInChildren<ActivePause>(true))
            if (window.gameObject.activeInHierarchy) { ClosePauseWindows(); return; }
        if (pauseOverlay != null)
        { pauseOverlay.SetActive(true); pauseOverlay.transform.SetAsLastSibling(); }
    }

    /// <summary>转场前关闭本场窗口和临时消息，解除暂停并清理动画，不提交奖励。</summary>
    public void PrepareBattleTransition()
    {
        BattleScene.Ins.BM.StopForWindowTransition();
        ClosePauseWindows();
        if (resultOverlay != null) resultOverlay.SetActive(false);
        if (battleStartUIPanel != null) battleStartUIPanel.CancelAnimation();
        ClearTransientMessages();
        notificationManager?.ClearNotifications();
        logPanel?.ClearLog();
        BattleScene.Ins.CM?.ClearTurnSelection();
        BattleScene.Ins.TM.ResetTime();
    }
}
