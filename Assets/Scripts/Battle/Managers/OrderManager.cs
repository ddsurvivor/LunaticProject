using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 警戒指令管理器
/// </summary>
public class OrderManager : MonoBehaviour
{
    // 战斗管理器里维护一份"当前所有警戒中的单位"列表,而不是去遍历allUnits筛选
    private List<UnitOrderState> activeOrders = new List<UnitOrderState>();
    private bool isMoving;
    private PieceController moveUnit;
    private Vector3 previousMovePosition; // 同一移动的相邻采样线段，覆盖一帧穿越扇形。
    private BattleActionContext moveAction; // 一次移动中的所有警戒共享根行动。

    /// <summary>
    /// 确认下达警戒指令
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="orderProfile"></param>
    public void ConfirmOrder(PieceController unit, OrderProfile orderProfile, Quaternion rotate)
    {
        if (!BattleRelations.CanInitiateAttack(unit) || orderProfile == null || unit.rangeUI == null || unit.rangeUI.fanRoot == null) return;
        ClearForPiece(unit);
        // 朝向在确认时快照，后续攻击转身不改变已下达警戒。
        UnitOrderState unitOrderState = new UnitOrderState()
        {
            guard = unit, profile = orderProfile,
            // 方向根据范围指示器
            remainingTriggers = Mathf.Max(1, orderProfile.maxTriggerCount),
            facingDir = new Vector2(unit.rangeUI.fanRoot.transform.forward.x, unit.rangeUI.fanRoot.transform.forward.z)
        };
        activeOrders.Add(unitOrderState);
        BattleScene.Ins.BM.tipTextManager.ShowTip(unit.transform, "确认指令");
    }


    private void Update()
    {
        if (isMoving && moveUnit != null)
        {
            CheckMoveUnit();
        }
    }

    /// <summary>仅解除指定棋子的警戒，供该棋子开始普通或聚能行动时调用。</summary>
    public void ClearForPiece(PieceController piece)
    {
        for (int i = activeOrders.Count - 1; i >= 0; i--)
            if (activeOrders[i].guard == piece) DisarmOrder(activeOrders[i]);
    }

    /// <summary>记录实际移动起点；逐帧及移动结束时检查经过的线段。</summary>
    public void OnUnitMoveStart(PieceController unit)
    {
        moveUnit = unit;
        isMoving = unit != null;
        if (!isMoving) return;
        previousMovePosition = unit.transform.position;
        moveAction = BattleActionContext.Current ?? new BattleActionContext(unit);
        CheckMoveUnit();
    }

    /// <summary>移动结束前补查最后一段，避免Update顺序漏掉终点前的穿越。</summary>
    public void OnUnityMoveEnd(PieceController unit)
    {
        if (unit != moveUnit) return;
        CheckMoveUnit();
        CancelMovementTracking();
    }

    /// <summary>中止只释放移动跟踪和待执行标记，不补发反应。</summary>
    public void CancelMovementTracking()
    {
        moveUnit = null;
        moveAction = null;
        isMoving = false;
        foreach (var state in activeOrders) state.reactionQueued = false;
    }

    private void OnDisable() => CancelMovementTracking();

    private void CheckMoveUnit()
    {
        if (moveUnit == null) return;
        var current = moveUnit.transform.position;
        for (int i = activeOrders.Count - 1; i >= 0; i--)
        {
            var state = activeOrders[i];
            if (state.guard == null || state.guard.isDead || !state.guard.isActiveAndEnabled)
            {
                DisarmOrder(state);
                continue;
            }
            if (moveUnit.isDead || !moveUnit.isActiveAndEnabled || state.reactionQueued ||
                !BattleRelations.CanAttack(state.guard, moveUnit)) continue;
            if (!CrossesSector(state, previousMovePosition, current)) continue;
            state.reactionQueued = BattleScene.Ins.BM.QueueOrderReaction(state, moveUnit, moveAction);
        }
        previousMovePosition = current;
    }

    /// <summary>供反应队列在出手前确认警戒尚未解除。</summary>
    public bool IsOrderActive(UnitOrderState state) => activeOrders.Contains(state) && state.remainingTriggers > 0;

    /// <summary>只在成功发动攻击后消耗次数；双方均按配置次数解除警戒。</summary>
    public void ConsumeOrder(UnitOrderState state)
    {
        if (--state.remainingTriggers <= 0) DisarmOrder(state);
    }

    private bool CrossesSector(UnitOrderState state, Vector3 from, Vector3 to)
    {
        Vector3 origin = state.guard.transform.position;
        Vector2 a = new Vector2(from.x - origin.x, from.z - origin.z);
        Vector2 b = new Vector2(to.x - origin.x, to.z - origin.z);
        Vector2 delta = b - a;
        var cuts = new List<float> { 0f, 1f };
        // 圆弧、内圈及两条角边界将线段切开，各段中点即可完整判定，不依赖采样步长。
        AddCircleCuts(a, delta, state.profile.sectorRadius, cuts);
        AddCircleCuts(a, delta, 1f, cuts);
        float halfAngle = state.profile.sectorAngleDeg * .5f;
        foreach (float angle in new[] { -halfAngle, halfAngle })
        {
            Vector2 edge = Rotate(state.facingDir, angle);
            float denominator = Cross(delta, edge);
            if (Mathf.Abs(denominator) > .000001f) AddCut(-Cross(a, edge) / denominator, cuts);
        }
        cuts.Sort();
        for (int i = 0; i < cuts.Count; i++)
        {
            if (Inside(a + delta * cuts[i], state)) return true;
            if (i > 0 && Inside(a + delta * ((cuts[i - 1] + cuts[i]) * .5f), state)) return true;
        }
        return false;
    }

    private static bool Inside(Vector2 point, UnitOrderState state) => point.magnitude >= 1f &&
        point.magnitude <= state.profile.sectorRadius &&
        Vector2.Angle(state.facingDir, point) <= state.profile.sectorAngleDeg * .5f;

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    private static Vector2 Rotate(Vector2 value, float degrees)
    {
        float angle = degrees * Mathf.Deg2Rad;
        return new Vector2(value.x * Mathf.Cos(angle) - value.y * Mathf.Sin(angle),
            value.x * Mathf.Sin(angle) + value.y * Mathf.Cos(angle));
    }
    private static void AddCut(float t, List<float> cuts) { if (t >= 0f && t <= 1f) cuts.Add(t); }
    private static void AddCircleCuts(Vector2 a, Vector2 delta, float radius, List<float> cuts)
    {
        float aa = delta.sqrMagnitude;
        if (aa < .000001f) return;
        float bb = 2f * Vector2.Dot(a, delta);
        float cc = a.sqrMagnitude - radius * radius;
        float discriminant = bb * bb - 4f * aa * cc;
        if (discriminant < 0f) return;
        float root = Mathf.Sqrt(discriminant);
        AddCut((-bb - root) / (2f * aa), cuts);
        AddCut((-bb + root) / (2f * aa), cuts);
    }

    // 解除警戒:从activeOrders移除,销毁常驻扇形显示,解锁guard可操控状态
    private void DisarmOrder(UnitOrderState state)
    {
        activeOrders.Remove(state);
        // 关闭范围显示
        state.reactionQueued = false;
        if (state.guard == null) return;
        state.guard.rangeUI?.CloseRange();
        state.guard.isGrauding = false;
    }

    public List<PieceController> IsInsideSector(Vector3 origin, Vector3 forward, float sectorAngleDeg, float sectorRadius)
    {
        // 1. 获取扇形参数
        float halfAngle = sectorAngleDeg / 2f;
        float range = sectorRadius;
        forward.y = 0; // 忽略y轴
        //Vector3 origin = start.transform.position;
        //Vector3 forward = state.guard.rangeUI.fanRoot.transform.forward;
        // 1. 首先圆形范围内进行射线判定
        Collider[] colliders = Physics.OverlapSphere(origin, range);
        List<PieceController> targetPieces = new List<PieceController>();

        foreach (var collider1 in colliders)
        {
            PieceController piece = collider1.GetComponent<PieceController>();
            if (piece == null) continue;
            Vector3 dir = (collider1.transform.position - origin);
            dir.y = 0; // 忽略y轴
            //if (dir.magnitude > range || dir.magnitude < 1f) continue; // 超出半径
            float angle = Vector3.Angle(forward, dir);
            if (angle <= halfAngle)
            {
                targetPieces.Add(piece);
            }
        }

        return targetPieces;
    }

    /// <summary>
    /// 关闭所有警戒
    /// </summary>
    public void ClearAll(bool isPlayer)
    {
        for (var i = activeOrders.Count - 1; i >= 0; i--)
        {
            var activeOrder = activeOrders[i];
            if (activeOrder.guard == null || activeOrder.guard.isPlayerPiece == isPlayer) DisarmOrder(activeOrder);
        }

    }
}

public enum OrderType
{
    Melee
    , // 近战指令：扇形半径短、角度宽（贴身警戒）
    Ranged // 远程指令：扇形半径长、角度窄（狙击警戒）
}

/// <summary>
/// 警戒预设范围
/// </summary>
[System.Serializable]
public class OrderProfile
{
    public string orderName;
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
    public bool reactionQueued; // 已捕获穿越，排队期间不重复提交。
}
