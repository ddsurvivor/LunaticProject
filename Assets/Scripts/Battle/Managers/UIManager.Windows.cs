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
        var pauseRect = MakeRect("BattlePauseOverlay", canvas.transform, Vector2.zero);
        pauseRect.anchorMin = Vector2.zero; pauseRect.anchorMax = Vector2.one;
        pauseRect.offsetMin = pauseRect.offsetMax = Vector2.zero;
        pauseRect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .76f);
        pauseOverlay = pauseRect.gameObject; pauseOverlay.SetActive(false); pauseOverlay.AddComponent<ActivePause>();
        var pauseBody = MakeRect("PauseWindow", pauseRect, new Vector2(620, 340));
        pauseBody.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.055f, .085f, .11f, 1);
        var pauseFrame = MakeRect("Frame", pauseBody, new Vector2(620, 340)).gameObject.AddComponent<BattlePanelFrame>();
        pauseFrame.color = new Color(.85f, .66f, .3f); pauseFrame.raycastTarget = false;
        WindowText("Title", pauseBody, new Vector2(500, 60), new Vector2(0, 110), 30).text = "战斗暂停";
        BindWindowButton(WindowButton("Resume", pauseBody, new Vector2(0, 35), "继续战斗"), ClosePauseWindows);
        BindWindowButton(WindowButton("Retry", pauseBody, new Vector2(0, -35), "重新开始"), OnClickRestartButton);
        BindWindowButton(WindowButton("Quit", pauseBody, new Vector2(0, -105), "返回主菜单"), OnClickQuit);
        var rect = MakeRect("BattleResultOverlay", canvas.transform, Vector2.zero);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var shade = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        shade.color = new Color(0, 0, 0, .76f);
        resultOverlay = rect.gameObject;
        resultOverlay.SetActive(false);
        resultOverlay.AddComponent<ActivePause>();
        if (battleFinishPanel == null)
        {
            var panel = MakeRect("BattleFinishPanel", rect, new Vector2(620, 360));
            var background = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(.055f, .085f, .11f, 1);
            var frame = MakeRect("Frame", panel, new Vector2(620, 360)).gameObject.AddComponent<BattlePanelFrame>();
            frame.color = new Color(.85f, .66f, .3f); frame.raycastTarget = false;
            battleFinishPanel = panel.gameObject.AddComponent<BattleFinishPanel>();
            battleFinishPanel.titleText = WindowText("Title", panel, new Vector2(560, 60), new Vector2(0, 115), 30);
            battleFinishPanel.descriptionText = WindowText("Description", panel, new Vector2(560, 160), new Vector2(0, 10), 19);
            battleFinishPanel.continueButton = WindowButton("Continue", panel, new Vector2(130, -130), "继续");
            battleFinishPanel.retryButton = WindowButton("Retry", panel, new Vector2(-130, -130), "重试");
        }
        else
        {
            battleFinishPanel.transform.SetParent(rect, false);
            var panel = (RectTransform)battleFinishPanel.transform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.anchoredPosition = Vector2.zero;
        }
        // 结果必须通过继续或重试离开，不能由旧关闭按钮留下空遮罩。
        foreach (var button in battleFinishPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            if (!IsResultAction(button.transform)) button.gameObject.SetActive(false);
        foreach (var button in battleFinishPanel.GetComponentsInChildren<CustomAdvancedButton>(true))
            if (!IsResultAction(button.transform)) button.gameObject.SetActive(false);
        BindWindowButton(battleFinishPanel.continueButton, () =>
        {
            if (BattleScene.Ins.BM.LastOutcome == BattleOutcome.PlayerVictory) BattleScene.Ins.BM.OnClickQuitBattle();
            else OnClickQuit();
        });
        BindWindowButton(battleFinishPanel.retryButton, OnClickRestartButton);
    }

    private bool IsResultAction(Transform node) => node.IsChildOf(battleFinishPanel.continueButton.transform) ||
        node.IsChildOf(battleFinishPanel.retryButton.transform);

    private RectTransform MakeRect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size; return rect;
    }

    private UnityEngine.UI.Text WindowText(string name, Transform parent, Vector2 size, Vector2 position, int fontSize)
    {
        var rect = MakeRect(name, parent, size); rect.anchoredPosition = position;
        var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font = pieceInfoPanel.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
        text.fontSize = fontSize; text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(.94f, .94f, .87f); text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private GameObject WindowButton(string name, Transform parent, Vector2 position, string label)
    {
        var rect = MakeRect(name, parent, new Vector2(220, 48)); rect.anchoredPosition = position;
        rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.2f, .28f, .34f);
        rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        WindowText("Label", rect, new Vector2(210, 48), Vector2.zero, 22).text = label;
        return rect.gameObject;
    }

    private static void BindWindowButton(GameObject node, UnityEngine.Events.UnityAction callback)
    {
        if (node == null) return;
        var custom = node.GetComponent<CustomAdvancedButton>();
        if (custom != null)
        {
            custom.onClickEvent = new UnityEngine.Events.UnityEvent();
            custom.onClickEvent.AddListener(callback);
            return;
        }
        var button = node.GetComponent<UnityEngine.UI.Button>();
        if (button == null) button = node.AddComponent<UnityEngine.UI.Button>();
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.onClick.AddListener(callback);
    }

    /// <summary>使用现有胜负结果显示窗口；不发奖、不保存、不自动跳转场景。</summary>
    public void ShowBattleResult(bool win, FinishDrop drop, int exp)
    {
        InitializeWindows();
        ClosePauseWindows();
        ClearActionFlow();
        ClearTransientMessages();
        notificationManager?.ClearNotifications();
        if (battleLossPanel != null) battleLossPanel.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);
        resultOverlay.SetActive(true); resultOverlay.transform.SetAsLastSibling();
        battleFinishPanel.ShowPanel(win, drop, exp);
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
