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
                // /// 计算目标方向
                // Vector3 direction = (target.transform.position - aiPiece.transform.position)
                //     .normalized;
                // // 计算理想攻击位置（距离目标 rangedRange - 0.5f）
                // Vector3 idealAttackPos =
                //     target.transform.position - direction * (rangedRange - 0.5f);
                // // 计算自身到理想攻击位置的距离
                // float distanceToIdeal =
                //     Vector3.Distance(aiPiece.transform.position, idealAttackPos);
                //
                // Vector3 moveTargetPos;
                // if (distanceToIdeal <= moveRange)
                // {
                //     // 可以直接到达理想攻击位置
                //     moveTargetPos = idealAttackPos;
                // }
                // else
                // {
                //     // 只能移动到最大移动距离
                //     moveTargetPos = aiPiece.transform.position + direction * moveRange;
                // }
                //
                // aiPiece.transform.DOMove(moveTargetPos, 1.0f);
                // aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move, false, 1.0f);
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

    /*/// <summary>
    /// AI移动函数
    /// </summary>
    /// <param name="aiPiece"></param>
    /// <param name="targetPos"></param>
    /// <param name="range">移动依据的范围</param>
    /// <param name="leave"></param>
    private void EnemyMove(EnemyController aiPiece, Vector3 targetPos, float range
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
        Vector3 direction = (targetPos - aiPiece.transform.position);
        direction.y = 0; // 忽略y轴高度差
        direction = direction.normalized;
        // 计算理想攻击位置（距离目标 rangedRange - 0.5f）
        Vector3 idealAttackPos =
            targetPos - direction * (range - 0.5f);
        if (leave)
        {
            // 如果是远离目标，则反向计算理想位置
            idealAttackPos = targetPos - direction * (range - 0.5f);
        }

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

        /*Debug.Log(
            $"边界范围 X:{groundBounds.min.x}~{groundBounds.max.x} Z:{groundBounds.min.z}~{groundBounds.max.z}");#1#
        Debug.Log(
            $"{aiPiece.enemyAIType}AI{aiPiece.name}移动到 {moveTargetPos}");
        aiPiece.transform.DOMove(moveTargetPos, 1.0f);
        aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move, false, 1.0f);
        aiPiece.CheckFace(moveTargetPos - aiPiece.transform.position);
    }
    */
    // // <summary>
    // /// AI 移动函数 - 使用 BattleManager 通用路径判定进行重构
    // /// </summary>
    // /// <param name="aiPiece">AI 控制器</param>
    // /// <param name="targetPos">目标位置（通常是玩家位置）</param>
    // /// <param name="range">希望保持的距离范围</param>
    // /// <param name="leave">是否执行远离逻辑</param>
    // private void EnemyMove(EnemyController aiPiece, Vector3 targetPos, float range
    //     , bool leave = false)
    // {
    //     // 获取 AI 当前的属性：最大移动距离
    //     float moveRange = aiPiece.unitAttrCenter.MoveRange;
    //     Vector3 currentPos = aiPiece.transform.position;
    //
    //     // 1. 计算方向向量（忽略 Y 轴高度差）
    //     Vector3 direction = (targetPos - currentPos);
    //     direction.y = 0;
    //     direction.Normalize();
    //
    //     // 2. 确定逻辑目标点
    //     // 理想位置：距离目标点 range - 0.5f 的位置
    //     Vector3 idealAttackPos = targetPos - direction * (range - 0.5f);
    //
    //     if (leave)
    //     {
    //         // 如果是“逃跑/远离”模式，理想位置应设在反方向的远处
    //         // 这里基于移动距离 moveRange 计算逃跑点
    //         idealAttackPos = currentPos - direction * moveRange;
    //     }
    //
    //     // 3. 计算想要移动的总距离
    //     float distanceToIdeal = Vector3.Distance(currentPos, idealAttackPos);
    //     // 实际尝试移动的距离不能超过 AI 自身的行动上限
    //     float testDistance = Mathf.Min(distanceToIdeal, moveRange);
    //
    //     // 4. 调用 BM 的通用判定函数（核心重构部分）
    //     // 该函数会自动处理：1. 墙壁阻挡 2. 棋子阻挡 3. 地面边界/虚空
    //     var moveResult = BattleScene.Ins.BM.CalculateValidMovePos(
    //         currentPos,
    //         leave ? -direction : direction, // 如果是远离，则向反方向探测
    //         testDistance,
    //         aiPiece.gameObject
    //     );
    //
    //     Vector3 moveTargetPos = moveResult.FinalPosition;
    //
    //     // 5. 执行位移表现
    //     Debug.Log(
    //         $"{aiPiece.enemyAIType} AI {aiPiece.name} 发起移动。目标：{moveTargetPos}，碰撞中止：{moveResult.IsCollided}");
    //
    //     // 使用 DOTween 进行平滑位移
    //     aiPiece.transform.DOMove(moveTargetPos, 1.0f).SetEase(Ease.InOutQuad);
    //
    //     // 更新动画状态与朝向
    //     aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move, false, 1.0f);
    //     aiPiece.CheckFace(moveTargetPos - currentPos);
    // }

    //bool useBM
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
        
        // 4. 判定是否“撞墙死顶”：发生了碰撞 且 行进路程不足 30%
        // if (moveResult.IsCollided && originalDist < (moveRange * 0.3f))
        // {
        //     Debug.Log($"{aiPiece.name} 遇到障碍物阻挡，尝试左右侧滑绕开...");
        //
        //     // 计算正负 90 度的方向
        //     Vector3 leftDir = Quaternion.Euler(0, -90, 0) * mainDir;
        //     Vector3 rightDir = Quaternion.Euler(0, 90, 0) * mainDir;
        //
        //     // 执行左右两次判定（探测完整的移动范围）
        //     var leftResult = BattleScene.Ins.BM.CalculateValidMovePos(currentPos, leftDir, moveRange
        //         , aiPiece.gameObject, true);
        //     var rightResult = BattleScene.Ins.BM.CalculateValidMovePos(currentPos, rightDir
        //         , moveRange, aiPiece.gameObject, true);
        //
        //     float leftDist = Vector3.Distance(currentPos, leftResult.FinalPosition);
        //     float rightDist = Vector3.Distance(currentPos, rightResult.FinalPosition);
        //
        //     // 对比哪条路径更长
        //
        //     // 选择最长的那条路径更新 moveResult
        //     if (leftDist >= rightDist)
        //     {
        //         moveResult = leftResult;
        //         Debug.Log($"{aiPiece.name} 选择左侧绕行，距离：{leftDist},结果{leftResult.FinalPosition}");
        //     }
        //     else
        //     {
        //         moveResult = rightResult;
        //         Debug.Log($"{aiPiece.name} 选择右侧绕行，距离：{rightDist},结果{rightResult.FinalPosition}");
        //     }
        // }

        // 5. 最终执行位移
        BattleScene.Ins.BM.moveManager.ExecuteMove(aiPiece.gameObject);
        aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move, false, 1.0f);
        aiPiece.CheckFace(targetPos - currentPos);
        //Vector3 moveTargetPos = moveResult.FinalPosition;

        // 强制锁定 Y 轴（根据你的需求，也可以不锁，让 CalculateValidMovePos 决定）
        // moveTargetPos.y = currentPos.y; 

        // Debug.Log($"{aiPiece.enemyAIType} AI {aiPiece.name} 最终移动到：{moveTargetPos}");
        //
        // aiPiece.transform.DOMove(moveTargetPos, 1.0f).SetEase(Ease.InOutQuad).OnComplete(() =>
        // {
        //     Debug.Log($"{aiPiece.name} 移动完成，当前坐标：{aiPiece.transform.position}");
        // });
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
            BattleScene.Ins.BM.camera.SetFollow(aiPiece.transform);
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