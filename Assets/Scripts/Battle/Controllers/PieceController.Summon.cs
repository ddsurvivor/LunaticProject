public partial class PieceController
{
    private BattleManager summonOwner; // 登记归属随当前生命结束清除，不借用可能已切换的场景单例。

    /// <summary>转移当前召唤物登记；先撤销旧归属，确保池对象复用时不会同时留在两个战斗中。</summary>
    internal void SetSummonOwner(BattleManager owner)
    {
        if (summonOwner == owner) return;
        ReleaseSummonRegistration();
        summonOwner = owner;
    }

    private void ReleaseSummonRegistration()
    {
        var owner = summonOwner;
        summonOwner = null;
        if (owner != null) owner.summonPieces.RemoveAll(piece => piece == this);
    }

    /// <summary>仅清除指定战斗的归属，旧场景清理不能撤销已转移到新战斗的登记。</summary>
    internal void ReleaseSummonOwner(BattleManager owner)
    {
        if (summonOwner == owner) ReleaseSummonRegistration();
    }
}
