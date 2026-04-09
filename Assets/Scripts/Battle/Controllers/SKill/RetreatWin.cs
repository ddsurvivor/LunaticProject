using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人特殊技能
/// 撤退到指定点时胜利
/// </summary>
public class RetreatWin : MonoBehaviour
{
    public Transform targetPoint; // 撤退目标点
    
    public void CheckTargetReached()
    {
        float distance = Vector3.Distance(transform.position, targetPoint.position);
        Debug.Log($"敌人距离撤退点的距离: {distance}");
        if (distance < 5f) // 可以调整这个距离阈值
        {
            Debug.Log("敌人撤退成功，玩家失败！");
            // 在这里触发游戏失败的逻辑
            BattleScene.Ins.BM.PlayerLoss();
        }
    }
}
