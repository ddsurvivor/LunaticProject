using UnityEngine;

/// <summary>
/// 梯子交互行为
/// </summary>
public class LadderArea : InteractArea
{
    public GameObject ladderFloorPos;

    public override void TriggerAction(PieceController piece=null)
    {
        base.TriggerAction();
        piece.transform.position = ladderFloorPos.transform.position;
    }
}