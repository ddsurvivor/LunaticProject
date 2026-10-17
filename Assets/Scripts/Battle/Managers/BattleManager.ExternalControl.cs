using System;
using UnityEngine;

public enum BattleOutcome { None, PlayerVictory, PlayerDefeat }

public partial class BattleManager
{
    private UnityEngine.Object externalControlOwner; // 同一场战斗只允许一个外部控制器接管玩家。
    private bool isolateExternalResults;
    /// <summary>外部控制器持有玩家输入时，鼠标输入入口应让出控制权。</summary>
    public bool HasExternalPlayerControl => externalControlOwner != null;
    /// <summary>隔离外部运行的交互展示、保存和胜负副作用，不影响战斗规则。</summary>
    public bool SuppressExternalPresentation => HasExternalPlayerControl && isolateExternalResults;
    /// <summary>战斗是否仍处于生命周期中，包含部署和开战等待阶段。</summary>
    public bool IsBattleRunning => inBattle;
    /// <summary>读取正式胜负结算结果；未结束或重新初始化后的战斗为 None。</summary>
    public BattleOutcome LastOutcome { get; private set; }
    /// <summary>正式胜负判定时触发一次；隔离模式会先停止调度再发送通知。</summary>
    public event Action<BattleOutcome> BattleCompleted;

    /// <summary>独占接管程序化玩家输入；隔离模式下结果不发奖励、不切剧情场景。</summary>
    public bool TryAcquirePlayerControl(UnityEngine.Object owner, bool isolateResults = true)
    {
        if (owner == null || LastOutcome != BattleOutcome.None || externalControlOwner != null && externalControlOwner != owner) return false;
        externalControlOwner = owner;
        isolateExternalResults = isolateResults;
        BattleScene.Ins?.CM?.ClearEnemyTargetLineInteraction();
        return true;
    }

    /// <summary>只有持有者可释放输入，禁用或销毁控制器时应调用；不取消已经启动的动作。</summary>
    public void ReleasePlayerControl(UnityEngine.Object owner)
    {
        if (externalControlOwner != owner) return;
        externalControlOwner = null;
        isolateExternalResults = false;
    }

    /// <summary>程序化命令仍须经过正常回合、费用、导航与目标校验，不能操作敌方或非当前棋子。</summary>
    public BattleCommandResult TryIssuePlayerCommand(UnityEngine.Object owner, PieceController actor, PlayerBattleCommand command)
    {
        if (externalControlOwner == null || externalControlOwner != owner || actor == null ||
            !actor.isPlayerPiece || !PlayerController.pieces.Contains(actor) || !CanControlPiece(actor)) return BattleCommandResult.Rejected;
        return actor.ExecutePlayerCommand(command);
    }

    private bool ObserveBattleOutcome(BattleOutcome outcome)
    {
        if (LastOutcome != BattleOutcome.None) return true;
        bool intercept = SuppressExternalPresentation; // 回调可能释放持有权，先快照是否拦截结算副作用。
        if (intercept)
        {
            CancelBattleStartup();
            inBattle = false;
            AIController.isInTurn = false;
        }
        if (LastOutcome == BattleOutcome.None)
        {
            LastOutcome = outcome;
            BattleCompleted?.Invoke(outcome);
        }
        return intercept;
    }
}
