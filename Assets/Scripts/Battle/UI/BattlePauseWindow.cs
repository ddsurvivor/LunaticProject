using UnityEngine;
using UnityEngine.Events;

/// <summary>暂停窗口的共享 prefab 绑定；只连接业务事件，不重建或覆盖手动布局。</summary>
public sealed class BattlePauseWindow : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private UnityEngine.UI.Text title;
    [SerializeField] private UnityEngine.UI.Button resumeButton, retryButton, quitButton;
    private UnityAction resume, retry, quit; // 只替换当前实例拥有的监听，保留 prefab 配置的事件。

    /// <summary>可手动编辑的窗口主体，不包含全屏输入遮罩。</summary>
    public RectTransform Panel => panel;
    /// <summary>检查必要按钮、标题及暂停持有组件，缺失时不接受为有效模板。</summary>
    public bool HasTemplateBindings => panel != null && title != null && resumeButton != null &&
        retryButton != null && quitButton != null && GetComponent<ActivePause>() != null;

    /// <summary>绑定继续、重开及退出；重复绑定不叠加回调，也不修改文字、位置或尺寸。</summary>
    public void Bind(UnityAction onResume, UnityAction onRetry, UnityAction onQuit)
    {
        if (resume != null) resumeButton.onClick.RemoveListener(resume);
        if (retry != null) retryButton.onClick.RemoveListener(retry);
        if (quit != null) quitButton.onClick.RemoveListener(quit);
        resume = onResume; retry = onRetry; quit = onQuit;
        if (resume != null) resumeButton.onClick.AddListener(resume);
        if (retry != null) retryButton.onClick.AddListener(retry);
        if (quit != null) quitButton.onClick.AddListener(quit);
    }

    /// <summary>供首次模板生成器保存引用；已有 prefab 的布局应直接在 Prefab Mode 调整。</summary>
    public void Configure(RectTransform body, UnityEngine.UI.Text heading, UnityEngine.UI.Button resumeAction,
        UnityEngine.UI.Button retryAction, UnityEngine.UI.Button quitAction)
    {
        panel = body; title = heading;
        resumeButton = resumeAction; retryButton = retryAction; quitButton = quitAction;
    }
}
