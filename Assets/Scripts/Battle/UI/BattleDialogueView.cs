using BattleDialogue;
using UnityEngine;

/// <summary>为共享 AVG 阅读器接入战斗暂停与完成回调，不改变剧本配置或存档规则。</summary>
public sealed class BattleDialogueView : MonoBehaviour
{
    public 剧本System reader;
    public TimeManager clock;
    public BattleDialogueManager dialogue;
    public UIManager ui;
    public CustomAdvancedButton recordButton;

    private void Awake()
    {
        reader.UseUnscaledTime = true;
        reader.ReaderClosed += OnReaderClosed;
        if (recordButton != null) recordButton.SetInteractable(false);
    }

    private void OnEnable()
    {
        if (clock != null) clock.AcquirePause(this);
    }

    private void OnDisable()
    {
        if (clock != null) clock.ReleasePause(this);
    }

    private void OnDestroy()
    {
        if (reader != null) reader.ReaderClosed -= OnReaderClosed;
    }

    private void OnReaderClosed()
    {
        // 转场或宿主失效只清理暂停，不能继续旧关卡的对话回调。
        if (dialogue != null && dialogue.isActiveAndEnabled &&
            (GM.Ins == null || !GM.Ins.IsTransitioning)) dialogue.OnDialogueFinished();
    }

    /// <summary>阅读器设置按钮复用本场已有设置窗口及其独立暂停持有者。</summary>
    public void OpenSettings() => ui.OpenDialogueSettings();
}
