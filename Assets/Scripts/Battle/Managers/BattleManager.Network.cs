using System.Collections.Generic;

public partial class BattleManager
{
    private readonly HashSet<MemeNetworkNode> networkNodes = new(); // 仅记录本管理器拥有连接的节点。
    internal void RegisterNetworkNode(MemeNetworkNode node) => networkNodes.Add(node);
    internal void UnregisterNetworkNode(MemeNetworkNode node) => networkNodes.Remove(node);

    private void ClearNetworkConnections()
    {
        foreach (var node in new List<MemeNetworkNode>(networkNodes))
            if (node != null) node.DisconnectAll();
        networkNodes.Clear();
        ClearPieceNetworkLinks(PlayerController?.pieces);
        ClearPieceNetworkLinks(AIController?.pieces);
        ClearPieceNetworkLinks(summonPieces);
    }

    private static void ClearPieceNetworkLinks(IEnumerable<PieceController> pieces)
    {
        if (pieces == null) return;
        foreach (var piece in pieces)
            if (piece != null) piece.MemeState.ClearNetworkLinks();
    }
}

