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

    // 地图寻路管理器
    public MapController mapController;


    private float _timer;
    private float _actionInterval = 3.0f; // 每个动作之间的间隔时间

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
    

    private void EnemyAttack(EnemyController aiPiece, PieceController target)
    {
        if (target == null) return;

        if (aiPiece.navigate) // 如果正在导航中，优先保持导航状态，不进行攻击
        {
            if (Mathf.Abs(target.transform.position.y - aiPiece.transform.position.y) > 1.0f)
            {
                // 如果目标在不同高度，则优先移动到与目标相同高度的位置
                EnemyMoveToLadder(aiPiece);
                return;
            }
        }

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
        else if (enemyAIType == EnemyAIType.Melee)
        {
            if (distanceToTarget <= meleeRange)
            {
                // 近战攻击
                aiPiece.StartNormalAttack();
                aiPiece.CastAttackOnTarget(target);
            }
            else
            {
                EnemyMove(aiPiece, target.transform.position, meleeRange);
            }
        }
        else if (enemyAIType == EnemyAIType.Shoot)
        {
            if (distanceToTarget <= rangedRange * 0.6f)
            {
                // 如果在远程攻击范围的60%内则逃跑
                EnemyMove(aiPiece, target.transform.position, rangedRange, true);
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
                EnemyMove(aiPiece, target.transform.position, rangedRange);
            }
        }
        else if (enemyAIType == EnemyAIType.SkillUser) // 技能型AI
        {
            // 优先近战
            if (distanceToTarget <= meleeRange)
            {
                // 近战攻击
                aiPiece.StartNormalAttack();
                aiPiece.CastAttackOnTarget(target);
                return;
            }

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
                    BattleScene.Ins.UM.PopSkillName(skillPack.skillName);
                    return;
                }
                else
                {
                    EnemyMove(aiPiece, target.transform.position, skillRange);
                }
            }
            else
            {
                EnemyNormalAttack(aiPiece, target, distanceToTarget, meleeRange, rangedRange);
            }
        }
        else if (enemyAIType == EnemyAIType.Combine) // 远程和近战混合型AI
        {
            if (aiPiece.unitAttrCenter.CurMovePoint >= 2 &&
                distanceToTarget <= (moveRange + meleeRange)) // 一步后可以近战的情况
            {
                Debug.Log(
                    $"{aiPiece.enemyAIType}AI{aiPiece.name}选择移动到近战范围攻击{distanceToTarget} <= {moveRange + meleeRange}");
                EnemyMove(aiPiece, target.transform.position, meleeRange);
            }
            else if (aiPiece.unitAttrCenter.CurMovePoint >= 2 &&
                     distanceToTarget <= (moveRange + rangedRange)) // 一步后可以远程的情况
            {
                Debug.Log(
                    $"{aiPiece.enemyAIType}AI{aiPiece.name}选择移动到远程范围攻击{distanceToTarget} <= {moveRange + rangedRange}");
                EnemyMove(aiPiece, target.transform.position, rangedRange);
            }
            else if (distanceToTarget <= meleeRange)
            {
                Debug.Log(
                    $"{aiPiece.enemyAIType}AI{aiPiece.name}选择近战攻击{distanceToTarget} <= {meleeRange}");
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
                    Debug.Log(
                        $"{aiPiece.enemyAIType}AI{aiPiece.name}选择远程攻击{distanceToTarget} <= {rangedRange}, 目标在{target.name}");
                    // 远程攻击
                    aiPiece.StartNormalAttack(true);
                    aiPiece.CastAttackOnTarget(target);
                }
            }
            else
            {
                Debug.Log($"{aiPiece.enemyAIType}AI{aiPiece.name}选择移动到{target.transform.position}");
                EnemyMove(aiPiece, target.transform.position, rangedRange);
            }
        }
        else if (enemyAIType == EnemyAIType.Special)
        {
            // 特殊行为由关卡设计决定，这里暂时不实现具体逻辑
            // 撤退到指定点时胜利
            RetreatWin retreatWin = aiPiece.GetComponent<RetreatWin>();
            if (retreatWin != null)
            {
                Debug.Log("移动到指定点");
                Vector3 pos = retreatWin.targetPoint.position;
                EnemyMove(aiPiece, pos, 0.5f);
                DOVirtual.DelayedCall(1.0f, () => retreatWin.CheckTargetReached()); // 行动后判定胜利
            }
        }
    }

    private void EnemyNormalAttack(EnemyController aiPiece, PieceController target
        , float distanceToTarget, float meleeRange, float rangedRange)
    {
        if (distanceToTarget <= meleeRange)
        {
            //Debug.LogError($"敌人近战攻击，实际距离{distanceToTarget}, 近战范围{meleeRange}");
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
            EnemyMove(aiPiece, target.transform.position, meleeRange);
        }
    }

    
    
    public void EnemyMove(EnemyController aiPiece, Vector3 targetPos, float range
        , bool leave = false)
    {
        float moveRange = aiPiece.unitAttrCenter.MoveRange;
        Vector3 currentPos = aiPiece.transform.position;

        // 1. 计算基础方向向量
        Vector3 direction = (targetPos - currentPos);
        direction.y = 0;
        direction.Normalize();

        // 最终要探测的方向（处理远离逻辑）
        Vector3 mainDir = leave ? -direction : direction;

        // 2. 确定逻辑目标点距离
        Vector3 idealAttackPos = leave
            ? (currentPos + mainDir * moveRange)
            : (targetPos - direction * (range - 0.5f));
        float distanceToIdeal = Vector3.Distance(currentPos, idealAttackPos);
        float testDistance = Mathf.Min(distanceToIdeal, moveRange);

        // 3. 第一次尝试移动
        // var moveResult =
        //     BattleScene.Ins.BM.CalculateValidMovePos(currentPos, mainDir, testDistance
        //         , aiPiece.gameObject, true);
        // float originalDist = Vector3.Distance(currentPos, moveResult.FinalPosition);
        BattleScene.Ins.BM.moveManager.PreviewMove(aiPiece.gameObject, idealAttackPos, moveRange);
        

        // 5. 最终执行位移
        aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move);
        BattleScene.Ins.BM.moveManager.ExecuteMove(aiPiece.gameObject, () =>
        {
            aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
        });
        aiPiece.CheckFace(targetPos - currentPos);
    }

    /// <summary>
    /// 敌人移动到梯子位置的函数
    /// </summary>
    /// <param name="aiPiece"></param>
    private void EnemyMoveToLadder(EnemyController aiPiece)
    {
        LadderArea ladderArea = mapController?.GetLadder(aiPiece.transform.position);
        if (ladderArea == null) return;
        Vector3 targetPos = ladderArea.GetNearPos(aiPiece.transform.position);
        if ((targetPos - aiPiece.transform.position).magnitude < 2f)
        {
            // 如果已经在梯子附近，则直接触发梯子交互
            ladderArea.TriggerAction(aiPiece);
            return;
        }

        Debug.Log($"敌人{aiPiece.name}移动到梯子位置 {ladderArea.GetNearPos(aiPiece.transform.position)}");
        EnemyMove(aiPiece, ladderArea.GetNearPos(aiPiece.transform.position), 0f);
    }

    /// <summary>
    /// 敌人行动，根据所有目标的威胁值计算
    /// </summary>
    private void EnemyAction_Calculate()
    {
        foreach (EnemyController aiPiece in pieces)
        {
            if (!aiPiece.isActived || aiPiece.isDead) continue;
            if (aiPiece.unitAttrCenter.HasMP())
            {
                CheckEnemyAction(aiPiece);
            }

            if (!aiPiece.unitAttrCenter.CostMP()) continue;
            BattleScene.Ins.BM.GetComponent<CameraController>().SetFollow(aiPiece.transform);
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

        if (threatValues.Count == 0) return;
        // 选择威胁值最高的目标进行攻击
        PieceController target = threatValues.Aggregate((l, r)
            => l.Value > r.Value ? l : r).Key;
        EnemyAttack(aiPiece, target);
    }

    public PieceController CheckEnemyTarget(EnemyController aiPiece)
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

        if (threatValues.Count == 0) return null;
        // 选择威胁值最高的目标进行攻击
        PieceController target = threatValues.Aggregate((l, r)
            => l.Value > r.Value ? l : r).Key;
        return target;
    }
}