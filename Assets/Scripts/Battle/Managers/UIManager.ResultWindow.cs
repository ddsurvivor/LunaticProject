using UnityEngine;

public partial class UIManager
{
    [Header("结果窗口模板")]
    [SerializeField] private BattleFinishPanel resultWindowPrefab;
    private BattleFinishPanel alarmWindow;

    /// <summary>报警复用结果窗口模板和独立暂停；只显示确认按钮，不触发胜负或整场重开。</summary>
    public void ShowDeviceAlarmConfirmation(System.Func<bool> onConfirm)
    {
        InitializeWindows();
        ClosePauseWindows();
        BattleScene.Ins.TM.SetFastForward(false);
        if (alarmWindow == null)
        {
            var prefab = resultWindowPrefab != null ? resultWindowPrefab : Resources.Load<BattleFinishPanel>("UI/BattleResultWindow");
            if (prefab == null || !prefab.HasTemplateBindings) throw new System.InvalidOperationException("缺少报警提示所需的结果窗口模板。");
            alarmWindow = InstantiateWindow(prefab, pieceInfoPanel.GetComponentInParent<Canvas>().rootCanvas.transform);
        }
        bool accepted = false; // 同次报警确认只能消费一次，旧按钮回调不能再次回退。
        alarmWindow.ShowConfirmation("触发警报", "警戒塔发现了队伍。确认后返回上一回合。", () =>
        {
            if (accepted || !alarmWindow.gameObject.activeInHierarchy || GM.Ins.IsTransitioning) return;
            accepted = true;
            if (onConfirm?.Invoke() == true) alarmWindow.gameObject.SetActive(false);
            else accepted = false;
        });
    }

    private void InitializeResultWindow(Transform parent)
    {
        var prefab = resultWindowPrefab != null ? resultWindowPrefab : Resources.Load<BattleFinishPanel>("UI/BattleResultWindow");
        if (prefab == null || !prefab.HasTemplateBindings)
        {
            Debug.LogError("缺少有效的结果窗口 prefab，请检查 Resources/UI/BattleResultWindow 或指定模板。", this);
            return;
        }
        var view = InstantiateWindow(prefab, parent);
        // 旧场景内嵌窗口停止显示；所有正式结果入口仍通过同一 UIManager 引用。
        if (battleFinishPanel != null) battleFinishPanel.gameObject.SetActive(false);
        battleFinishPanel = view;
        resultOverlay = view.gameObject;
        view.BindActions(() =>
        {
            if (BattleScene.Ins.BM.LastOutcome == BattleOutcome.PlayerVictory) BattleScene.Ins.BM.OnClickQuitBattle();
            else OnClickQuit();
        }, OnClickRestartButton);
    }
}
