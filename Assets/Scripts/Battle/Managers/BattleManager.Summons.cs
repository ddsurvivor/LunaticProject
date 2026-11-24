using System.Linq;

public partial class BattleManager
{
    /// <summary>登记初始化完成的召唤物；同一对象仅保留一项，回收/重新初始化时由棋子撤销登记。</summary>
    public bool RegisterSummon(PieceController piece)
    {
        if (!isActiveAndEnabled || piece == null || !piece.gameObject.activeInHierarchy) return false;
        piece.SetSummonOwner(this);
        summonPieces.RemoveAll(existing => existing == null);
        int first = summonPieces.IndexOf(piece);
        if (first < 0) summonPieces.Add(piece);
        else for (int i = summonPieces.Count - 1; i > first; i--)
            if (summonPieces[i] == piece) summonPieces.RemoveAt(i);
        return true;
    }

    private void ClearSummonRegistry()
    {
        foreach (var piece in summonPieces.Where(p => p != null).Distinct().ToArray()) piece.ReleaseSummonOwner(this);
        summonPieces.Clear();
    }
}
