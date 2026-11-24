using UnityEngine;

public partial class UIManager
{
    [Header("暂停窗口模板")]
    [SerializeField] private BattlePauseWindow pauseWindowPrefab;
    private BattlePauseWindow pauseWindow;

    /// <summary>当前战斗的暂停窗口实例，默认使用 Resources/UI/BattlePauseWindow，可在 Inspector 指定变体。</summary>
    public BattlePauseWindow PauseWindow => pauseWindow;

    private void InitializePauseWindow(Transform parent)
    {
        var prefab = pauseWindowPrefab != null ? pauseWindowPrefab : Resources.Load<BattlePauseWindow>("UI/BattlePauseWindow");
        if (prefab == null || !prefab.HasTemplateBindings)
        {
            Debug.LogError("缺少有效的暂停窗口 prefab，请检查 Resources/UI/BattlePauseWindow 或指定模板。", this);
            return;
        }
        pauseWindow = InstantiateWindow(prefab, parent);
        pauseWindow.Bind(ClosePauseWindows, OnClickRestartButton, OnClickQuit);
        pauseOverlay = pauseWindow.gameObject;
    }
}
