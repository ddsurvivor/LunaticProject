using UnityEngine;
using UnityEngine.Events;

/// <summary>部署面板的模板绑定；只更新提示和按钮状态，保留 prefab 的共享模块与手动布局。</summary>
public sealed class BattleDeploymentPanel : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text selectionText, statusText;
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private CanvasGroup confirmGroup;
    private UnityAction confirm; // 仅管理本实例绑定的确认回调，不清除模板事件。

    public bool HasTemplateBindings => selectionText != null && statusText != null && confirmButton != null && confirmGroup != null;

    /// <summary>接入原部署确认入口；重复绑定不叠加监听，不修改模板文字样式。</summary>
    public void Bind(UnityAction onConfirm)
    {
        if (confirm != null) confirmButton.onClick.RemoveListener(confirm);
        confirm = onConfirm;
        if (confirm != null) confirmButton.onClick.AddListener(confirm);
    }

    /// <summary>刷新当前棋子、放置反馈及拖拽占用；实际合法性仍由部署控制器确认。</summary>
    public void Refresh(string pieceName, string status, bool dragging)
    {
        selectionText.text = string.IsNullOrEmpty(pieceName) ? "当前未选择棋子" : $"当前棋子：{pieceName}";
        statusText.text = status;
        confirmButton.interactable = !dragging;
        confirmGroup.alpha = dragging ? 0.4f : 1f;
    }
}
