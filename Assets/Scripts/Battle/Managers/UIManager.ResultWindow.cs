using UnityEngine;

public partial class UIManager
{
    [Header("结果窗口模板")]
    [SerializeField] private BattleFinishPanel resultWindowPrefab;

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
