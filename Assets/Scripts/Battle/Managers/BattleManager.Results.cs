using UnityEngine;

public partial class BattleManager
{
    private bool leavingBattle; // 离场请求只接受一次。
    private bool rewardsCommitted; // 同一结果的奖励只在离场时提交一次。

    private void PresentBattleOutcome(bool win)
    {
        BattleScene.Ins.CM?.ClearTurnSelection();
        CancelBattleStartup();
        inBattle = false;
        AIController.isInTurn = false;
        BattleScene.Ins.UM.ShowBattleResult(win, finishDrop, finishExp);
    }

    /// <summary>重试或离场请求确认后停止本场调度及在途效果，避免淡出期间继续行动。</summary>
    internal void StopForWindowTransition()
    {
        CancelBattleStartup();
        CancelBattleReactions();
        _timedMemeStatuses?.ClearAll();
        ClearNetworkConnections();
        inBattle = false;
    }

    /// <summary>胜利离场时提交现有掉落和经验；重复调用、失败或隔离运行不发奖。</summary>
    internal void CommitBattleRewards()
    {
        if (rewardsCommitted || LastOutcome != BattleOutcome.PlayerVictory || SuppressExternalPresentation) return;
        // 奖励回调可能重入离场入口，先占用本场提交权。
        rewardsCommitted = true;
        if (finishDrop != null) finishDrop.DropItems();
        if (finishExp > 0)
            foreach (var player in GM.Ins.PLAYERPROFILE.player)
                if (player != null && player.HP > 0) player.AccessAttribute(22, AttrOp.Add, finishExp);
    }
}
