using System.Collections.Generic;
using DG.Tweening;
using Sirenix.Serialization;
using UnityEngine;

public class SelfExploreSummonPiece : MonoBehaviour
{
    [OdinSerialize]
    public SkillPack skillPack;
    //public AttackPack attackPack;
    [Header("爆炸设置")]
    [SerializeField] private float explosionRadius = 5f;   // 爆炸半径
    [SerializeField] private LayerMask effectLayer;       // 建议设置层级，过滤掉不需要检测的物体
    [Header("自爆兵属性")]
    private float moveRange = 20.0f;      // 单次回合最大移动距离
    private float explodeRange = 5f;   // 自爆触发范围
    private bool isActionFinished = false;
    public PieceController pieceController;
    
    public Animator animator;
    /// <summary>
    /// 由 TurnManager 调用的核心AI入口
    /// </summary>
    public void ExecuteAI()
    {
        isActionFinished = false;

        // 1. 搜寻最近的敌人棋子
        GameObject targetEnemy = FindNearestEnemy();

        if (targetEnemy == null)
        {
            // 战场上已经没有敌人了，直接结束回合
            FinishAction();
            return;
        }

        // 2. 计算朝向敌人的移动目标点（受最大移动范围限制）
        Vector3 targetPosition = CalculateMovementTarget(targetEnemy.transform.position);
        
        bool canMove = BattleScene.Ins.BM.moveManager.PreviewAIMove(this.gameObject, targetPosition, moveRange);

        // 5. 最终执行位移
        if(!canMove) return;
        
        // 3. 【动画控制】开始移动，激活跑步/移动动画
        if (animator != null)
        {
            animator.SetBool("isMoving", true);
        }
        //aiPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move);
        BattleScene.Ins.BM.moveManager.ExecuteMove(this.gameObject
            , () =>
            {
                // 4. 移动完成后，再次检查与该敌人的距离
                float distanceToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);
            
                if (animator != null)
                {
                    animator.SetBool("isMoving", false);
                }
                if (distanceToEnemy <= explodeRange)
                {
                    // 进入爆炸范围，触发自爆
                    TriggerExplosion(targetEnemy);
                }
                else
                {
                    // 未进入爆炸范围，安全结束当前轮次行动
                    FinishAction();
                }
            });
        //aiPiece.CheckFace(targetPos - currentPos);
    }

    /// <summary>
    /// 寻找最近的敌人棋子
    /// </summary>
    private GameObject FindNearestEnemy()
    {
        // 实际项目中推荐从专门的 BattleManager 获取棋子列表，这里用 FindObjectsByType 演示逻辑

        GameObject nearestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var piece in BattleScene.Ins.BM.AIController.pieces)
        {
            // 过滤掉已被销毁的、或者和自己同阵营（玩家阵营）的棋子
            if (piece == null || piece.isPlayerPiece) continue;

            float distance = Vector3.Distance(transform.position, piece.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = piece.gameObject;
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// 根据自身移动范围限制，计算最终落脚点（保持安全间距，防止重叠）
    /// </summary>
    private Vector3 CalculateMovementTarget(Vector3 enemyPos)
    {
        Vector3 currentPos = transform.position;
        float distanceToEnemy = Vector3.Distance(currentPos, enemyPos);

        // 1. 计算方向向量
        Vector3 direction = (enemyPos - currentPos).normalized;

        // 2. 计算在不重叠的情况下，理论上最多可以移动多少距离
        // 例如：距离敌人 5 米，停止间距 1 米，那我们最多只能走 4 米
        float maxMovableDistance = distanceToEnemy - 1f;

        // 3. 边界安全判定：如果当前已经离敌人非常近了（甚至小于停止距离），就不再前进了
        if (maxMovableDistance <= 0)
        {
            return currentPos; 
        }

        // 4. 取【最大可移动距离】和【自身移动范围】的最小值，作为本次实际移动的距离
        // 如果移动范围是 3 米，最大可走 4 米 -> 只走 3 米（被行动力限制）
        // 如果移动范围是 3 米，最大可走 1 米 -> 只走 1 米（被停止距离限制，刚好停在敌人面前）
        float actualMoveDistance = Mathf.Min(moveRange, maxMovableDistance);

        // 5. 计算出最终坐标
        return currentPos + direction * actualMoveDistance;
    }

    /// <summary>
    /// 触发自爆逻辑
    /// </summary>
    private void TriggerExplosion(GameObject target)
    {
        Debug.Log($"{gameObject.name} 触发了自爆！");

        // 1. 【动画控制】触发死亡/自爆动画
        if (animator != null)
        {
            // 这里假设你的 Animator 中 isDeath 是一个 Bool 参数。
            // 如果你在 Animator 里的参数类型是 Trigger，请将其改为: animator.SetTrigger("isDeath");
            animator.SetBool("isDeath", true); 
        }
        // TODO: 1. 在这里调用你的伤害系统，例如：
        // 2. 核心：寻找爆炸范围内的所有碰撞体
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, effectLayer);
        Debug.Log($"碰撞检测到 {colliders.Length} 个物体。");
        List<PieceController> targetPieces = new List<PieceController>();
        foreach (Collider hit in colliders)
        {
            // 3. 尝试获取 PieceController
            // 注意：如果你的 PieceController 在父物体上，请使用 GetComponentInParent
            var piece = hit.GetComponent<PieceController>();

            if (piece != null && piece != pieceController && !targetPieces.Contains(piece)) // 4. 排除自己
            {
                targetPieces.Add(piece);
            }
        }
        Debug.Log($"自爆影响了 {targetPieces.Count} 个棋子。");
        BattleScene.Ins.BM.PieceSkill(pieceController, targetPieces,skillPack);
        
        // TODO: 2. 播放爆炸特效和音效
        AudioSource.PlayClipAtPoint(skillPack.skillSound, Camera.main.transform.position);
        ObjectPool.Ins.GenerateObject(
            skillPack.skillVFXType,
            transform.position + Vector3.up * 3f, Quaternion.identity);

        // 3. 释放/销毁自身
        //Destroy(gameObject);
        DOVirtual.DelayedCall(1f, () =>
        {
            gameObject.SetActive(false);
        });
        //BattleScene.Ins.BM.summonPieces.Remove(this.pieceController);

        // 4. 重点：因为物体被销毁了，立刻标记结束，防止协程因等待不到布尔值而卡死
        FinishAction();
    }

    private void FinishAction()
    {
        isActionFinished = true;
    }
}