using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡ai敌人总管理器
/// </summary>
public class AIController : PlayerController
{
    /// <summary>
    /// 敌人解锁顺序
    /// </summary>
    public Dictionary<GameObject, List<PieceController>> enemyPiecesDict = new();

    private float _timer;
    private float _actionInterval = 2.0f; // 每个动作之间的间隔时间

    public void OnScanFog(GameObject fog)
    {
        if (enemyPiecesDict.ContainsKey(fog))
        {
            foreach (var piece in enemyPiecesDict[fog])
            {
                piece.isActived = true;
                piece.gameObject.SetActive(true);
                piece.StartNormalAttack(true);
            }
        }
    }
    public override void TurnStart()
    {
        base.TurnStart();
        
        // 激活敌人棋子
        foreach (var pair in enemyPiecesDict)
        {
            if (!pair.Key.gameObject.activeInHierarchy)
            {
                foreach (var piece in pair.Value)
                {
                    piece.isActived = true;
                    piece.gameObject.SetActive(true);
                    piece.StartNormalAttack(true);
                }
            }
        }
    }

    public void Update()
    {
        if (isInTurn)
        {
            if (_timer < _actionInterval)
            {
                _timer += Time.deltaTime;
                return;
            }
            else
            {
                EnemyAction();
                _timer = 0f;
            }
        }
    }

    private void EnemyAction()
    {
        // 简单AI逻辑：依次让每个敌人棋子行动，然后结束回合
        foreach (var piece in pieces)
        {
            if (piece.isActived)
            {
                if(!piece.unitAttrCenter.CostMP()) continue;
                piece.Attack(GetRandomTarget());
                return;
            }
        }

        // // 检查是否所有敌人棋子都已行动
        // bool allActed = true;
        // foreach (var piece in pieces)
        // {
        //     if (piece.unitAttrCenter.CurMovePoint>0)
        //     {
        //         allActed = false;
        //         break;
        //     }
        // }
        //
        // // 如果所有敌人棋子都已行动，结束敌人回合
        // if (allActed)
        // {
        //     BattleScene.Ins.BM.ChangeTurn();
        // }
        
        BattleScene.Ins.BM.ChangeTurn();
    }

    public PieceController GetRandomTarget()
    {
        // 获取一个随机的玩家棋子
        if (BattleScene.Ins.BM.PlayerController.pieces.Count == 0) return null;
        int randomIndex = UnityEngine.Random.Range(0, BattleScene.Ins.BM.PlayerController.pieces.Count);
        return BattleScene.Ins.BM.PlayerController.pieces[randomIndex];
    }
}
