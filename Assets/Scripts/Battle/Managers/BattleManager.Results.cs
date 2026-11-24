using UnityEngine;

public partial class BattleManager
{
    private bool leavingBattle; // 离场请求只接受一次。
    private bool rewardsCommitted; // 同一结果的奖励只在离场时提交一次。
    private int pendingCurrencyReward; // 本次尝试累计的货币，重开或放弃时不提交。

    /// <summary>本次尝试尚待胜利离场提交的标准货币，结果窗口只读展示。</summary>
    public int PendingCurrencyReward => pendingCurrencyReward;

    /// <summary>登记本场玩家棋子的货币收益；结算后、非本场棋子或无会话时拒绝。</summary>
    internal bool TryAddBattleCurrencyReward(PieceController owner, int amount)
    {
        if (!isActiveAndEnabled || !inBattle || LastOutcome != BattleOutcome.None || leavingBattle ||
            rewardsCommitted || owner == null || !owner.isPlayerPiece || !PlayerController.pieces.Contains(owner) ||
            GM.Ins == null || !GM.Ins.HasBattleSession || amount <= 0 || amount > int.MaxValue - pendingCurrencyReward)
            return false;
        pendingCurrencyReward += amount;
        return true;
    }

    private void PresentBattleOutcome(bool win)
    {
        BattleScene.Ins.CM?.ClearTurnSelection();
        CancelBattleStartup();
        inBattle = false;
        AIController.isInTurn = false;
        BattleScene.Ins.UM.ShowBattleResult(win, finishDrop, finishExp, pendingCurrencyReward);
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

    /// <summary>胜利离场时提交掉落、经验和累计货币；重复调用、失败或隔离运行不发奖。</summary>
    internal void CommitBattleRewards()
    {
        if (rewardsCommitted || LastOutcome != BattleOutcome.PlayerVictory || SuppressExternalPresentation) return;
        // 奖励回调可能重入离场入口，先占用本场提交权。
        rewardsCommitted = true;
        var profile = GM.Ins.PLAYERPROFILE;
        profile.coins = (int)System.Math.Min(int.MaxValue, (long)profile.coins + pendingCurrencyReward);
        if (finishDrop != null) finishDrop.DropItems();
        if (finishExp > 0)
            foreach (var player in GM.Ins.PLAYERPROFILE.player)
                if (player != null && player.HP > 0) player.AccessAttribute(22, AttrOp.Add, finishExp);
    }
}
