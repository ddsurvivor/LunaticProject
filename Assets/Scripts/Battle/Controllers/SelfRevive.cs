using UnityEngine;
/// <summary>
/// 角色复活
/// </summary>
public class SelfRevive: MonoBehaviour
{
    public PieceController piece;
    public void Revive()
    {
        if (piece != null)
        {
            // 满血满状态复活
            piece.Init(piece.player);
        }
    }
}