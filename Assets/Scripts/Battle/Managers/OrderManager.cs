using System;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    // 战斗管理器里维护一份"当前所有警戒中的单位"列表,而不是去遍历allUnits筛选
    private List<UnitOrderState> activeOrders = new List<UnitOrderState>();
    private bool isMoving;
    private PieceController moveUnit;

    /// <summary>
    /// 开始指令瞄准状态
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="type"></param>
    public void BeginAimOrder(PieceController unit, OrderType type)
    {
        // 逻辑与选择扇形攻击范围类似

        OrderProfile orderProfile = unit.pieceData.orderProfiles.Find(t => t.type == type);

        if (orderProfile == null) return;
    }

    public void BeginIssueOrder(PieceController unit, OrderType type)
    {
        // 1. 根据type从配置表/预设里取出对应OrderProfile(近战或远程的半径/角度参数)
        OrderProfile orderProfile = unit.pieceData.orderProfiles.Find(t => t.type == type);

        if (orderProfile == null) return;


        // 2. 开始瞄准，开启范围显示系统


        // 3. 玩家确认方向后:
        UnitOrderState unitOrderState = new UnitOrderState()
        {
            guard = unit, profile = orderProfile,
            // 方向根据范围指示器
            remainingTriggers = orderProfile.maxTriggerCount
            ,
        };
    }


    private void Update()
    {
        if (isMoving && moveUnit != null)
        {
            CheckMoveUnit();
        }
    }

    public void OnUnitMoveStart(PieceController unit)
    {
        moveUnit = unit;
        isMoving = true;
    }

    public void OnUnityMoveEnd(PieceController unit)
    {
        moveUnit = null;
        isMoving = false;
    }

    private void CheckMoveUnit()
    {
        for (int i = activeOrders.Count - 1; i >= 0; i--)
        {
            var state = activeOrders[i];


            // 警戒者和移动单位同阵营,跳过(不对友军触发)
            if (state.guard.isPlayerPiece == moveUnit.isPlayerPiece) continue;


            // 核心判定:moveUnit当前位置是否落在state的扇形范围内
            bool isInsideNow = IsInsideSector(state, moveUnit.transform.position);


            // 只在"上一帧在外,这一帧在内"的瞬间触发一次,
            // 避免移动单位停在扇形里不动时,每帧重复触发攻击
            if (isInsideNow)
            {
                TriggerOrderAttack(state, moveUnit);
                // 多个单位可同时触发
            }
        }
    }

    private void TriggerOrderAttack(UnitOrderState state, PieceController target)
    {
        // 1. 根据guard的orderState.profile.type区分近战/远程表现:
        // 触发协同普攻
        if (state.guard.isPlayerPiece)
        {
            //closestPartner.StartNormalAttack();
            if (state.profile.type == OrderType.Melee)
            {
                state.guard.CastNormalAttack(target);
                DisarmOrder(state);
            }
            else if (state.profile.type == OrderType.Ranged)
            {
                state.guard.CastNormalAttack(target, true);
                DisarmOrder(state);
            }
        }
        else
        {
            if (state.profile.type == OrderType.Melee)
            {
                state.guard.StartNormalAttack();
                ((EnemyController)state.guard).CastAttackOnTarget(target);
            }
            else if (state.profile.type == OrderType.Ranged)
            {
                state.guard.StartNormalAttack(true);
                ((EnemyController)state.guard).CastAttackOnTarget(target);
            }
        }
    }

    // 解除警戒:从activeOrders移除,销毁常驻扇形显示,解锁guard可操控状态
    private void DisarmOrder(UnitOrderState state)
    {
        activeOrders.Remove(state);
        // 关闭范围显示
    }

    // 扇形几何判定:以guard站立点为顶点,facingDir为朝向,
    // 判断targetPos是否同时满足"距离<=半径"和"夹角<=半张角"
    private bool IsInsideSector(UnitOrderState state, Vector3 targetWorldPos)
    {
        Vector2 toTarget = (Vector2)(targetWorldPos - state.guard.transform.position);
        float dist = toTarget.magnitude;
        if (dist > state.profile.sectorRadius) return false;
        
        float angleToTarget = Vector2.Angle(state.facingDir, toTarget);
        return angleToTarget <= state.profile.sectorAngleDeg * 0.5f;
    }
}

public enum OrderType
{
    Melee
    , // 近战指令：扇形半径短、角度宽（贴身警戒）
    Ranged // 远程指令：扇形半径长、角度窄（狙击警戒）
}

[System.Serializable]
public class OrderProfile
{
    public OrderType type; // 决定攻击伤害及特效
    public float sectorRadius; // 扇形半径
    public float sectorAngleDeg; // 扇形张角（全角，不是半角）
    public int maxTriggerCount = 1; // 一次架枪最多触发几次，默认1次后解除
}

// 警戒单位
public class UnitOrderState
{
    public PieceController guard;
    public OrderProfile profile; // 警戒判定参数是

    public Vector2 facingDir; // 扇形朝向（归一化）

    //public Vector3 originWorldPos;  // 扇形顶点（一般是该角色站立点）
    public int remainingTriggers;
}