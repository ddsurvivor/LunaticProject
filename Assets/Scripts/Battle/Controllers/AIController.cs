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
    [Header("敌人解锁顺序")]
    /// <summary>
    /// 战争迷雾管理
    /// </summary>
    public FogController fogController;

    // 增援波次管理
    public WaveController waveController;

    private float _timer;
    private float _actionInterval = 2.0f; // 每个动作之间的间隔时间

    public void OnScanFog(GameObject fog)
    {
        if (fogController == null)
        {
            return;
        }

        if (fogController.enemyPiecesDict.ContainsKey(fog))
        {
            foreach (var piece in fogController.enemyPiecesDict[fog])
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

        if (fogController != null)
        {
            // 激活迷雾敌人棋子
            foreach (var pair in fogController.enemyPiecesDict)
            {
                if (!pair.Key.gameObject.activeInHierarchy)
                {
                    foreach (var piece in pair.Value)
                    {
                        if (!piece.isDead)
                        {
                            piece.isActived = true;
                            piece.gameObject.SetActive(true);
                            piece.StartNormalAttack(true);
                        }
                    }
                }
            }
        }

        if (waveController != null)
        {
            // 刷新增援波次敌人棋子
            waveController.RefreshEnemies(BattleScene.Ins.BM.TunrNumber);
        }

        foreach (EnemyController piece in pieces)
        {
            if (piece.isActived && !piece.isDead)
            {
                piece.StartNormalAttack(true);
                // 每回合清空仇恨值
                // piece.damageDic = new();
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
                //EnemyAction_AttackNearestTarget();
                EnemyAction_Calculate();
                _timer = 0f;
            }
        }
    }

    /*
    private void EnemyAction_AttackRandom()
    {
        // 简单AI逻辑：依次让每个敌人棋子行动，然后结束回合
        foreach (EnemyController piece in pieces)
        {
            if (piece.isActived && !piece.isDead)
            {
                if (!piece.unitAttrCenter.CostMP()) continue;
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
        foreach (EnemyController aiPiece in pieces)
        {
            if (!aiPiece.isActived || aiPiece.isDead) continue;
            if (!aiPiece.unitAttrCenter.CostMP()) continue;

            // 获取最近的玩家棋子
            PieceController target = null;
            float minDistance = float.MaxValue;
            foreach (var piece in BattleScene.Ins.BM.PlayerController.pieces)
            {
                if (piece.isDead) continue;
                float distance =
                    Vector3.Distance(piece.transform.position, aiPiece.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = piece;
                }
            }

            if (target == null) continue;

            float meleeRange = aiPiece.unitAttrCenter.attr.GetRange(true); // 近战攻击范围
            float rangedRange = aiPiece.unitAttrCenter.attr.GetRange(false); // 远程攻击范围

            float distanceToTarget =
                Vector3.Distance(target.transform.position, aiPiece.transform.position);

            if (distanceToTarget <= meleeRange)
            {
                // 近战攻击
                aiPiece.StartNormalAttack();
                aiPiece.Attack(target);
            }
            else if (distanceToTarget <= rangedRange)
            {
                // 远程攻击
                aiPiece.StartNormalAttack(true);
                aiPiece.Attack(target);
            }
            else
            {
                // 移动到能够远程攻击的位置
                Vector3 direction = (target.transform.position - aiPiece.transform.position)
                    .normalized;
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
        List<PieceController> pieces =
            BattleScene.Ins.BM.PlayerController.pieces.Where(t => !t.isDead).ToList();
        int randomIndex = UnityEngine.Random.Range(0, pieces.Count);
        return BattleScene.Ins.BM.PlayerController.pieces[randomIndex];
    }*/

    private void EnemyAttack(EnemyController aiPiece, PieceController target)
    {
        if (target == null) return;

        float meleeRange = aiPiece.GetRange(true); // 近战攻击范围
        float rangedRange = aiPiece.GetRange(false); // 远程攻击范围
        float moveRange = aiPiece.unitAttrCenter.MoveRange; // 移动范围

        // 计算忽略Y轴的距离（仅XZ平面）
        Vector3 diff = target.transform.position - aiPiece.transform.position;
        diff.y = 0; // 忽略Y轴差异
        float distanceToTarget = diff.magnitude;

        EnemyAIType enemyAIType = aiPiece.enemyAIType;

        if (enemyAIType == EnemyAIType.AttackNearest)
        {
            EnemyNormalAttack(aiPiece, target, distanceToTarget, meleeRange, rangedRange);
        }
        else if (enemyAIType == EnemyAIType.Shoot)
        {
            if (distanceToTarget <= rangedRange)
            {
                // 判定弹药是否足够
                if (aiPiece.unitAttrCenter.AmmoCount <= 0)
                {
                    // 重新装填
                    aiPiece.ReloadAmmo();
                }
                else
                {
                    // 远程攻击
                    aiPiece.StartNormalAttack(true);
                    aiPiece.CastAttackOnTarget(target);
                }
            }
            else
            {
                /// 计算目标方向
                Vector3 direction = (target.transform.position - aiPiece.transform.position)
                    .normalized;
                // 计算理想攻击位置（距离目标 rangedRange - 0.5f）
                Vector3 idealAttackPos =
                    target.transform.position - direction * (rangedRange - 0.5f);
                // 计算自身到理想攻击位置的距离
                float distanceToIdeal =
                    Vector3.Distance(aiPiece.transform.position, idealAttackPos);

                Vector3 moveTargetPos;
                if (distanceToIdeal <= moveRange)
                {
                    // 可以直接到达理想攻击位置
                    moveTargetPos = idealAttackPos;
                }
                else
                {
                    // 只能移动到最大移动距离
                    moveTargetPos = aiPiece.transform.position + direction * moveRange;
                }

                aiPiece.transform.DOMove(moveTargetPos, 1.0f);
                aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move, false, 1.0f);
            }
        }
        else if (enemyAIType == EnemyAIType.SkillUser) // 技能型AI
        {
            // 优先计算是否使用技能
            int skillRoll = UnityEngine.Random.Range(1, 101);
            if (aiPiece.availableSkills.Count > 0 && skillRoll <= GameConst.enemySkillRate)
            {
                // 随机选择一个技能
                int randomIndex = UnityEngine.Random.Range(0, aiPiece.availableSkills.Count);
                SkillPack skillPack = aiPiece.availableSkills[randomIndex];

                float skillRange = skillPack.rangeValue;

                if (distanceToTarget <= skillRange)
                {
                    // 释放技能
                    aiPiece.StartSkillAttack(skillPack);
                    aiPiece.CastSkillOnTarget(target, skillPack);
                    return;
                }
                else
                {
                    EnemyMove(aiPiece, target, skillRange);
                }
            }
            else
            {
                EnemyNormalAttack(aiPiece, target, distanceToTarget, meleeRange, rangedRange);
            }
        }
    }

    private void EnemyNormalAttack(EnemyController aiPiece, PieceController target
        , float distanceToTarget, float meleeRange, float rangedRange)
    {
        if (distanceToTarget <= meleeRange)
        {
            // 近战攻击
            aiPiece.StartNormalAttack();
            aiPiece.CastAttackOnTarget(target);
        }
        else if (distanceToTarget <= rangedRange)
        {
            // 判定弹药是否足够
            if (aiPiece.unitAttrCenter.AmmoCount <= 0)
            {
                // 重新装填
                aiPiece.ReloadAmmo();
            }
            else
            {
                // 远程攻击
                aiPiece.StartNormalAttack(true);
                aiPiece.CastAttackOnTarget(target);
            }
        }
        else
        {
            EnemyMove(aiPiece, target, rangedRange);
        }
    }

    /// <summary>
    /// AI移动函数
    /// </summary>
    /// <param name="aiPiece"></param>
    /// <param name="target"></param>
    /// <param name="range">移动依据的范围</param>
    /// <param name="leave"></param>
    private void EnemyMove(EnemyController aiPiece, PieceController target, float range
        , bool leave = false)
    {
        float moveRange = aiPiece.unitAttrCenter.MoveRange; // 移动范围
        // 1. 获取当前脚下的ground物体
        RaycastHit hit;
        GameObject groundObj = null;
        if (Physics.Raycast(aiPiece.transform.position + Vector3.up, Vector3.down
                , out hit
                , 1.5f))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                groundObj = hit.collider.gameObject;
            }
        }

        if (groundObj == null)
        {
            Debug.LogWarning(
                $"AI{aiPiece.name}移动失败，未找到ground物体");
            return;
        }

        // 2. 获取ground的边界
        var groundBounds = groundObj.GetComponent<Collider>().bounds;

        /// 计算目标方向
        Vector3 direction = (target.transform.position - aiPiece.transform.position);
        direction.y = 0; // 忽略y轴高度差
        direction = direction.normalized;
        // 计算理想攻击位置（距离目标 rangedRange - 0.5f）
        Vector3 idealAttackPos =
            aiPiece.transform.position + direction * (range + 0.5f);
        // 计算自身到理想攻击位置的距离
        float distanceToIdeal =
            Vector3.Distance(aiPiece.transform.position, idealAttackPos);

        Vector3 moveTargetPos;
        if (distanceToIdeal <= moveRange)
        {
            // 可以直接到达理想攻击位置
            moveTargetPos = idealAttackPos;
        }
        else
        {
            // 只能移动到最大移动距离
            moveTargetPos = aiPiece.transform.position + direction * moveRange;
        }

        // 4. 保证目标点在ground边界内
        moveTargetPos.x =
            Mathf.Clamp(moveTargetPos.x, groundBounds.min.x + 3.5f, groundBounds.max.x - 3.5f);
        //moveTargetPos.y = Mathf.Clamp(moveTargetPos.y, groundBounds.min.y, groundBounds.max.y);
        moveTargetPos.z =
            Mathf.Clamp(moveTargetPos.z, groundBounds.min.z + 3.5f, groundBounds.max.z - 3.5f);

        Debug.Log(
            $"边界范围 X:{groundBounds.min.x}~{groundBounds.max.x} Z:{groundBounds.min.z}~{groundBounds.max.z}");
        Debug.Log(
            $"技能型AI{aiPiece.name}移动到 {moveTargetPos}");
        aiPiece.transform.DOMove(moveTargetPos, 1.0f);
        aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move, false, 1.0f);
        aiPiece.CheckFace(moveTargetPos- aiPiece.transform.position);
    }

    /// <summary>
    /// 敌人行动，根据所有目标的威胁值计算
    /// </summary>
    private void EnemyAction_Calculate()
    {
        foreach (EnemyController aiPiece in pieces)
        {
            if (!aiPiece.isActived || aiPiece.isDead) continue;
            if (!aiPiece.unitAttrCenter.CostMP()) continue;
            CheckEnemyAction(aiPiece);
            return;
        }

        BattleScene.Ins.BM.ChangeTurn();
    }

    /// <summary>
    /// 对于一个敌人棋子，检查其所有可能的行动，并选择威胁值最高的行动执行
    /// </summary>
    /// <param name="aiPiece"></param>
    private void CheckEnemyAction(EnemyController aiPiece)
    {
        // 计算所有目标棋子的威胁值
        Dictionary<PieceController, int> threatValues = new();

        PieceController nearTarget = null; // 距离最近的棋子
        // 伤害最高的棋子
        PieceController highDamageTarget = null;
        float minDistance = float.MaxValue;
        int maxDamage = -1;

        foreach (var playerPiece in BattleScene.Ins.BM.PlayerController.pieces)
        {
            if (playerPiece.isDead) continue;
            threatValues.Add(playerPiece, 0);


            // 获取最近的玩家棋子
            float distance =
                Vector3.Distance(playerPiece.transform.position, aiPiece.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearTarget = playerPiece;
            }

            // 伤害最高的玩家棋子
            if (aiPiece.damageDic.ContainsKey(playerPiece))
            {
                int damage = aiPiece.damageDic[playerPiece];
                if (damage > maxDamage)
                {
                    maxDamage = damage;
                    highDamageTarget = playerPiece;
                }
            }

            // 计算嘲讽值
            threatValues[playerPiece] += playerPiece.unitAttrCenter.TauntValue;
        }

        if (nearTarget != null) threatValues[nearTarget] += 1; // 距离最近的地方棋子威胁值+1
        if (highDamageTarget != null)
        {
            threatValues[highDamageTarget] += 2; // 伤害最高的玩家棋子威胁值+2
        }

        // 选择威胁值最高的目标进行攻击
        PieceController target = threatValues.Aggregate((l, r)
            => l.Value > r.Value ? l : r).Key;
        EnemyAttack(aiPiece, target);
    }
}