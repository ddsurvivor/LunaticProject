using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;


public class PlayerController : SerializedMonoBehaviour
{
    public List<PieceController> pieces = new();
    public bool isInTurn;
    
    public virtual void TurnStart()
    {
        // 所有棋子重置状态
        foreach (var piece in pieces)
        {
            piece.TurnStart();
        }
        BattleScene.Ins.UM.endTurnButton.gameObject.SetActive(false);
    }
    public virtual void TurnEnd()
    {
        // 可以在这里添加玩家回合结束时的逻辑
        foreach (var piece in pieces)
        {
            piece.TurnEnd();
        }
    }
    public void OnClickTurnEnd()
    {
        BattleScene.Ins.BM.ChangeTurn();
        
        BattleScene.Ins.UM.endTurnButton.gameObject.SetActive(false);
    }
}