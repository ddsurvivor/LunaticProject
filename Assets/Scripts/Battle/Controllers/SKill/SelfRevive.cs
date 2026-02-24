using Sirenix.OdinInspector;
using UnityEngine;
/// <summary>
/// 角色复活
/// </summary>
public class SelfRevive: MonoBehaviour
{
    public PieceController piece;
    [LabelText("复活恢复百分比")]
    [Range(0f,1f)]
    public float revivePercent = 0.5f; // 复活时恢复的生命百分比
    [ReadOnly]
    public bool isFakeDead = false; // 是否处于假死状态
    [ReadOnly] 
    public int reviveCount = 1; // 可复活次数，默认为1次

    public void OnDead()
    {
        if (reviveCount >= 0)
        {
            isFakeDead = true;
        }
    }
    public void Revive()
    {
        if (piece != null && isFakeDead)
        {
            // 满血满状态复活
            piece.Init(piece.player);
            piece.unitAttrCenter.SetHealth(revivePercent);
            isFakeDead = false;
            reviveCount--;
        }
    }
}