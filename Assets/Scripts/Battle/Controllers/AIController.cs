using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
        foreach (var piece in pieces)
        {
            if (piece.isActived)
            {
                piece.StartNormalAttack(true);
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
                //EnemyAction_AttackRandom();
                EnemyAction_AttackNearestTarget();
                _timer = 0f;
            }
        }
    }

    private void EnemyAction_AttackRandom()
    {
        // 简单AI逻辑：依次让每个敌人棋子行动，然后结束回合
        foreach (var piece in pieces)
        {
            if (piece.isActived && !piece.isDead)
            {
                if(!piece.unitAttrCenter.CostMP()) continue;
                piece.Attack(GetRandomTarget());
                return;
            }
        }

        BattleScene.Ins.BM.ChangeTurn();
    }
    
    // 敌人攻击距离最近目标的行动模式
    // 查找最近的玩家棋子，如果在近战范围内则近战攻击，如果在远程范围内则远程攻击，
    // 否则移动到最近的玩家棋子附近，到能够远程攻击的位置，然后远程攻击
    private void EnemyAction_AttackNearestTarget()
    {
        foreach (var aiPiece in pieces)
        {
            if (!aiPiece.isActived || aiPiece.isDead) continue;
            if(!aiPiece.unitAttrCenter.CostMP()) continue;
                
            // 获取最近的玩家棋子
            PieceController target = null;
            float minDistance = float.MaxValue;
            foreach (var piece in BattleScene.Ins.BM.PlayerController.pieces)
            {
                if (piece.isDead) continue;
                float distance = Vector3.Distance(piece.transform.position, aiPiece.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = piece;
                }
            }

            if (target == null) continue;

            float meleeRange = aiPiece.unitAttrCenter.attr.GetRange(true); // 近战攻击范围
            float rangedRange = aiPiece.unitAttrCenter.attr.GetRange(false); // 远程攻击范围

            float distanceToTarget = Vector3.Distance(target.transform.position, aiPiece.transform.position);

            if (distanceToTarget <= meleeRange)
            {
                // 近战攻击
                aiPiece.StartNormalAttack();
                aiPiece.Attack(target);
            }
            else if (distanceToTarget <=  rangedRange)
            {
                // 远程攻击
                aiPiece.StartNormalAttack(true);
                aiPiece.Attack(target);
            }
            else
            {
                // 移动到能够远程攻击的位置
                Vector3 direction = (target.transform.position - aiPiece.transform.position).normalized;
                Vector3 newPosition = target.transform.position - direction * (rangedRange - 0.5f);
                aiPiece.transform.DOMove(newPosition, 1.0f).OnComplete(() =>
                {
                    // 远程攻击
                    aiPiece.StartNormalAttack(true);
                    aiPiece.Attack(target);
                });
                aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move, false, 1.0f);
                
            }
            return;
        }
        BattleScene.Ins.BM.ChangeTurn();
        
    }
    
    private PieceController GetRandomTarget()
    {
        // 获取一个随机的玩家棋子
        if (BattleScene.Ins.BM.PlayerController.pieces.Count == 0) return null;
        // 玩家棋子中所有活着的棋子
        List<PieceController> pieces = BattleScene.Ins.BM.PlayerController.pieces.Where(t=> !t.isDead).ToList();
        int randomIndex = UnityEngine.Random.Range(0, pieces.Count);
        return BattleScene.Ins.BM.PlayerController.pieces[randomIndex];
    }

}
