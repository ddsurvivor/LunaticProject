using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 梯子交互行为
/// </summary>
public class LadderArea : InteractArea
{
    //public List<PieceController> pieces = new();
    //public int maxCapacity = 2;
    public GameObject highlightEffect;

    public Transform upPos;
    public Transform downPos;

    public override void TriggerAction(PieceController piece = null)
    {
        // if (pieces.Count >= maxCapacity) return;
        // if (pieces.Contains(piece)) return;
        // // 扣除所有行动力，并且在下个回合开始的时候转移平台
        // piece.unitAttrCenter.CostMP(piece.unitAttrCenter.CurMovePoint);
        // pieces.Add(piece);
        
        // 直接攀爬
        if(!piece.unitAttrCenter.CostMP()) return;
        
        // 如果这个棋子的y坐标与uppos之差小于1，则移动到downpos，否则移动到uppos
        if (Mathf.Abs(piece.transform.position.y - upPos.position.y) < 1f)
        {
            // 记录棋子与当前的upPos的xz偏差值
            Vector3 offset = piece.transform.position - upPos.position;
            offset.y = 0;
            piece.transform.position = (downPos.position);
        }
        else
        {
            // 记录棋子与当前的downPos的xz偏差值
            Vector3 offset = piece.transform.position - downPos.position;
            offset.y = 0;
            piece.transform.position = (upPos.position );
        }
        BattleScene.Ins.BM.camera.SetFollow(piece.transform);
    }

    // public void LeaveSlot(PieceController target)
    // {
    //     pieces.Remove(target);
    // }

    // public void StartMove(bool isPlayerTurn)
    // {
    //     // 回合开始时触发，转移所有棋子
    //     for (var index = pieces.Count - 1; index >= 0; index--)
    //     {
    //         var piece = pieces[index];
    //         if (piece.isPlayerPiece == isPlayerTurn)
    //         {
    //             // 如果这个棋子的y坐标与uppos之差小于1，则移动到downpos，否则移动到uppos
    //             if (Mathf.Abs(piece.transform.position.y - upPos.position.y) < 1f)
    //             {
    //                 // 记录棋子与当前的upPos的xz偏差值
    //                 Vector3 offset = piece.transform.position - upPos.position;
    //                 offset.y = 0;
    //                 piece.transform.position = (downPos.position + offset);
    //                 pieces.Remove(piece);
    //             }
    //             else
    //             {
    //                 // 记录棋子与当前的downPos的xz偏差值
    //                 Vector3 offset = piece.transform.position - downPos.position;
    //                 offset.y = 0;
    //                 piece.transform.position = (upPos.position + offset);
    //                 pieces.Remove(piece);
    //             }
    //         }
    //     }
    // }
    
    private void OnMouseEnter()
    {
        highlightEffect.SetActive(true);
    }
    private void OnMouseExit()
    {
        highlightEffect.SetActive(false);
    }
}
