using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class MoveManager : MonoBehaviour
{
    [Header("引用对象")] [SerializeField] private LineRenderer pathRenderer; // 全局公用的路径渲染器

    [Header("性能优化参数")] [SerializeField]
    private float mouseMoveThreshold = 0.2f; // 鼠标世界坐标变化超过该距离时，才重新计算路径

    private NavMeshPath tempPath;
    private Vector3 lastValidPreviewPosition; // 记录上一次成功渲染的有效目标点

    private NavMeshAgent agent;
    private UnityAction onReachDestination; // 存储外部传进来的回调函数
    private bool isTracking = false;

    private void Awake()
    {
        tempPath = new NavMeshPath();
    }

    private void Update()
    {
        if (!isTracking || agent == null) return;

        // 核心到达判定条件：
        // 1. !agent.pathPending : 路径已经计算完毕
        // 2. remainingDistance <= stoppingDistance : 剩余距离小于等于停止距离
        // 3. (!agent.hasPath || agent.velocity.sqrMagnitude == 0f) : 没有路径了或者速度已经降为 0
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDestinationDistance())
            {
                // 双重保障：确保棋子真的停下来了
                if (!agent.hasPath || agent.velocity.sqrMagnitude <= 2f)
                {
                    isTracking = false;

                    // 执行外部传入的回调函数（如果存在的话）
                    if (onReachDestination != null)
                    {
                        onReachDestination.Invoke();
                    }

                    pathRenderer.gameObject.SetActive(false); // 隐藏路径渲染器
                    ResetPreviewState(); // 重置预览状态，准备下一次使用
                    
                    BattleScene.Ins.BM.orderManager.OnUnityMoveEnd(
                        agent.GetComponent<PieceController>());
                }
            }
        }
    }


    /*/// <summary>
    /// 【新增公共接口】实时预览移动路径（带防抖与性能优化）
    /// </summary>
    /// <param name="pawnObject">当前选中的棋子</param>
    /// <param name="hoverPosition">当前鼠标悬停的世界坐标</param>
    /// <param name="maxDistance">棋子最大移动范围</param>
    public void PreviewMove(GameObject pawnObject, Vector3 hoverPosition, float maxDistance)
    {
        if (pawnObject == null || pathRenderer == null) return;
        pathRenderer.gameObject.SetActive(true);

        // 性能优化点 1：如果鼠标移动距离非常微小，直接拦截，不进行任何寻路计算
        if (Vector3.Distance(hoverPosition, lastValidPreviewPosition) < mouseMoveThreshold)
        {
            return;
        }

        NavMeshAgent agent = pawnObject.GetComponent<NavMeshAgent>();
        if (agent == null) return;

        // 2. 计算路径
        if (NavMesh.CalculatePath(agent.transform.position, hoverPosition, NavMesh.AllAreas, tempPath))
        {
            float totalPathLength = CalculatePathLength(tempPath);

            // 3. 距离判定
            if (totalPathLength <= maxDistance)
            {
                // 可到达：更新 LineRenderer，并更新“最后有效位置”
                DrawPath(tempPath);
                lastValidPreviewPosition = hoverPosition;
            }
            // 如果不可到达（totalPathLength > maxDistance），代码什么都不做
            // 这样 LineRenderer 就会自然而然地保持上一次合法的路径状态
        }
    }*/

    
    private Vector3 lastMousePreviewPosition; // 专门用于存储上一次鼠标的原始悬停位置（用于性能防抖）


    /// <summary>
    /// 【优化升级版】实时预览移动路径（超出范围自动截断）
    /// </summary>
    /// <param name="pawnObject">当前选中的棋子</param>
    /// <param name="hoverPosition">当前鼠标悬停的世界坐标</param>
    /// <param name="maxDistance">棋子最大移动范围</param>
    public void PreviewMove(GameObject pawnObject, Vector3 hoverPosition, float maxDistance)
    {
        if (pawnObject == null || pathRenderer == null) return;
        pathRenderer.gameObject.SetActive(true);

        // 性能优化点 1：使用鼠标原始坐标进行对比。如果鼠标动的距离非常微小，直接拦截
        if (Vector3.Distance(hoverPosition, lastMousePreviewPosition) < mouseMoveThreshold)
        {
            return;
        }

        // 记录本次有效的鼠标输入位置
        lastMousePreviewPosition = hoverPosition;

        NavMeshAgent agent = pawnObject.GetComponent<NavMeshAgent>();
        if (agent == null) return;

        // 2. 计算完整的寻路路径
        if (NavMesh.CalculatePath(agent.transform.position, hoverPosition, NavMesh.AllAreas
                , tempPath))
        {
            float totalPathLength = CalculatePathLength(tempPath);

            // 3. 距离判定
            if (totalPathLength <= maxDistance)
            {
                // 情况 A：在范围内，正常绘制完整路径
                DrawPath(tempPath);
                lastValidPreviewPosition = hoverPosition; // 最终移动终点就是鼠标点
            }
            else
            {
                // 情况 B：超出范围！沿着导航折线截取最大距离的点
                // 这里我们直接调用一个高效的裁剪绘制函数，避免二次进行 NavMesh 寻路计算，极大节省 CPU
                Vector3 croppedPoint = DrawAndGetCroppedPath(tempPath, maxDistance);

                // 此时，玩家点击确定后，棋子将走向这个截断点
                lastValidPreviewPosition = croppedPoint;
            }
        }
    }

    /// <summary>
    /// 【智能版 AI 专属接口】预测 AI 移动路径（目标点非法时自动贴墙/吸附网格边缘 + 超出范围自动截断）
    /// </summary>
    /// <param name="pawnObject">AI 棋子对象</param>
    /// <param name="targetPosition">AI 企图移动的目标点</param>
    /// <param name="maxDistance">AI 最大移动范围</param>
    /// <returns>是否有可达路径。如果返回 false，建议 AI 放弃本次移动</returns>
    public bool PreviewAIMove(GameObject pawnObject, Vector3 targetPosition, float maxDistance)
    {
        if (pawnObject == null || pathRenderer == null) return false;

        NavMeshAgent agent = pawnObject.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.gameObject.activeInHierarchy) return false;
        SetupMovementPriorities(pawnObject);

        Vector3 startPosition = agent.transform.position;
        Vector3 safeTargetPosition = targetPosition;

        // 1. 边缘阻挡检测：从当前位置向目标点发射导航射线
        NavMeshHit hit;
        /*if (NavMesh.Raycast(startPosition, targetPosition, out hit, NavMesh.AllAreas))
        {
            // 射线被阻挡，说明目的地在网格外部或墙内，修正为撞墙的边缘临界点
            safeTargetPosition = hit.position;
        }*/

        // 2. 严谨性兜底：将点垂直吸附到就近的网格表面
        if (!NavMesh.SamplePosition(safeTargetPosition, out hit, 5.0f, NavMesh.AllAreas))
        {
            // 如果方圆 5 米内依然找不到任何网格（比如起点本身非法或掉出地图），直接拦截
            Debug.LogWarning($"[MoveManager] AI 目标点 {targetPosition} 彻底非法，无法修正到网格边缘。");
            return false;
        }

        safeTargetPosition = hit.position;

        // 3. 计算完整的寻路路径
        // 注意：如果起点和安全终点过近（例如已经贴墙且就在原地），CalculatePath 依旧会返回 true 并生成 1 个 corner
        if (NavMesh.CalculatePath(startPosition, safeTargetPosition, NavMesh.AllAreas, tempPath))
        {
            // 健壮性检查：如果路径状态无效，或者属于不可达的孤岛路径
            if (tempPath.status == NavMeshPathStatus.PathInvalid || tempPath.corners.Length == 0)
            {
                return false;
            }

            // 激活路径渲染器
            pathRenderer.gameObject.SetActive(true);
            float totalPathLength = CalculatePathLength(tempPath);

            // 4. 距离步长判定
            if (totalPathLength <= maxDistance)
            {
                // 情况 A：在行动力范围内，且已经贴墙/到位，正常绘制
                DrawPath(tempPath);
                lastValidPreviewPosition = safeTargetPosition;
            }
            else
            {
                // 情况 B：虽然修正到了边缘，但是边缘距离太远，超出了最大行动力！
                // 沿着导航折线截取最大距离的点，并进行裁剪渲染
                Vector3 croppedPoint = DrawAndGetCroppedPath(tempPath, maxDistance);
                lastValidPreviewPosition = croppedPoint;
            }

            return true; // 成功找到并规划了有效路径（无论是完整路径还是截断路径）
        }

        return false; // 寻路计算彻底失败
    }

    /// <summary>
    /// 核心高效率函数：一边裁剪路径线段，一边直接把坐标喂给 LineRenderer，并返回最终截断点的坐标
    /// </summary>
    private Vector3 DrawAndGetCroppedPath(NavMeshPath path, float maxDistance)
    {
        if (path.corners.Length < 2) return path.corners[0];

        // 使用一个临时的动态列表来存储裁剪后的顶点
        System.Collections.Generic.List<Vector3> croppedCorners =
            new System.Collections.Generic.List<Vector3>();

        float accumulatedDistance = 0f;
        // 放入起点（同样垫高 0.05m 规避闪烁）
        croppedCorners.Add(path.corners[0] + Vector3.up * 0.05f);

        Vector3 finalPoint = path.corners[0];

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 start = path.corners[i];
            Vector3 end = path.corners[i + 1];
            float segmentLength = Vector3.Distance(start, end);

            // 检查加上当前这段线段后是否会超过最大行动力
            if (accumulatedDistance + segmentLength >= maxDistance)
            {
                // 计算当前线段还剩下多少额度可以用
                float remainingDistance = maxDistance - accumulatedDistance;
                float percent = remainingDistance / segmentLength;

                // 线性插值精准算出临界点的 3D 坐标
                finalPoint = Vector3.Lerp(start, end, percent);
                croppedCorners.Add(finalPoint + Vector3.up * 0.05f);
                break; // 已经达到最大距离，立刻切断循环
            }

            // 没超限，正常塞入拐点
            accumulatedDistance += segmentLength;
            finalPoint = end;
            croppedCorners.Add(end + Vector3.up * 0.05f);
        }

        // 将裁剪完的顶点数组一次性推给 LineRenderer
        pathRenderer.positionCount = croppedCorners.Count;
        pathRenderer.SetPositions(croppedCorners.ToArray());

        return finalPoint; // 返回这个截断点，用于更新实体移动目标
    }

    /// <summary>
    /// 【别忘了修改重置函数】切换棋子时，把鼠标记录一并重置
    /// </summary>
    public void ResetPreviewState()
    {
        lastValidPreviewPosition = Vector3.positiveInfinity;
        lastMousePreviewPosition = Vector3.positiveInfinity; // 新增重置
        RestoreAllMovementPriorities();
        ClearPathLine();
    }

    /// <summary>
    /// 【优化版】在棋子真正开始平移前调用，直接让其他静止棋子“隐形”，规避二人转
    /// </summary>
    public void SetupMovementPriorities(GameObject movingPawn)
    {
        if (movingPawn == null) return;

        // 1. 激活并初始化当前移动棋子的 Agent
        NavMeshAgent movingAgent = movingPawn.GetComponent<NavMeshAgent>();
        if (movingAgent != null)
        {
            movingAgent.enabled = true;
            movingAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            movingAgent.avoidancePriority = 99;
        }

        if (BattleScene.Ins == null || BattleScene.Ins.BM == null) return;

        // 2. 遍历所有棋子，让静止的棋子完全不参与避障计算（变成空气）
        SetOtherAgentsAvoidance(movingPawn, false);
    }

    /// <summary>
    /// 【新增】当移动结束时，恢复全场棋子的基本避障设置
    /// </summary>
    private void RestoreAllMovementPriorities()
    {
        if (BattleScene.Ins == null || BattleScene.Ins.BM == null) return;

        // 传入 null 代表没有移动棋子，全场恢复默认
        SetOtherAgentsAvoidance(null, false);
    }

// 提取的辅助内部方法：用来批量开启/关闭其他棋子的避障
    private void SetOtherAgentsAvoidance(GameObject movingPawn, bool enableAvoidance)
    {
        var bm = BattleScene.Ins.BM;

        // 处理玩家棋子
        if (bm.PlayerController?.pieces != null)
        {
            foreach (var piece in bm.PlayerController.pieces)
            {
                if (piece == null || piece.gameObject == movingPawn) continue;
                var agent = piece.gameObject.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = enableAvoidance;
                    /*// 如果不参与避障，直接设为 NoAvoidance，杜绝二人转；恢复时设为默认
                    agent.obstacleAvoidanceType = enableAvoidance
                        ? ObstacleAvoidanceType.LowQualityObstacleAvoidance
                        : ObstacleAvoidanceType.NoObstacleAvoidance;
                    agent.avoidancePriority = enableAvoidance ? 50 : 0;*/
                }
            }
        }

        // 处理 AI 棋子
        if (bm.AIController?.pieces != null)
        {
            foreach (var piece in bm.AIController.pieces)
            {
                if (piece == null || piece.gameObject == movingPawn) continue;
                var agent = piece.gameObject.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.obstacleAvoidanceType = enableAvoidance
                        ? ObstacleAvoidanceType.LowQualityObstacleAvoidance
                        : ObstacleAvoidanceType.NoObstacleAvoidance;
                    agent.avoidancePriority = enableAvoidance ? 50 : 0;
                }
            }
        }
    }

    /*/// <summary>
    /// 【公共接口】正式请求移动（此处的计算直接使用最后确认的有效路径，防止漂移）
    /// </summary>
    public float ExecuteMove(GameObject pawnObject, UnityAction onMoveComplete = null)
    {
        if (pawnObject == null) return 0f;

        NavMeshAgent agent = pawnObject.GetComponent<NavMeshAgent>();
        if (agent == null) return 0f;

        BattleScene.Ins.BM.orderManager.OnUnitMoveStart(pawnObject.GetComponent<PieceController>());

        SetupMovementPriorities(pawnObject.gameObject); // 在正式移动前设置权重，确保避障行为正确
        this.agent = agent;
        this.onReachDestination = onMoveComplete;
        isTracking = true;
        // 直接走向最后一次预览成功的有效位置
        if (NavMesh.CalculatePath(agent.transform.position, lastValidPreviewPosition
                , NavMesh.AllAreas, tempPath))
        {
            agent.SetPath(tempPath);
            return CalculatePathLength(tempPath);
        }

        return 0f;
    }*/
    /// <summary>
    /// 【公共接口】正式请求移动
    /// </summary>
    /// <param name="pawnObject">要移动的棋子</param>
    /// <param name="onMoveComplete">完成后的回调</param>
    /// <param name="customTargetPosition">可选：AI或脚本直接指定的目的地。如果不传，则默认使用上一次鼠标预览的有效点</param>
    public float ExecuteMove(GameObject pawnObject, UnityAction onMoveComplete = null
        , Vector3? customTargetPosition = null)
    {
        if (pawnObject == null) return 0f;

        NavMeshAgent agent = pawnObject.GetComponent<NavMeshAgent>();
        if (agent == null) return 0f;

        BattleScene.Ins.BM.orderManager.OnUnitMoveStart(pawnObject.GetComponent<PieceController>());

        SetupMovementPriorities(pawnObject.gameObject); // 在正式移动前设置权重，确保避障行为正确
        this.agent = agent;
        this.onReachDestination = onMoveComplete;
        isTracking = true;

        // 确定最终目的地：如果传了自定义坐标就用自定义的，否则用玩家预览的坐标
        Vector3 finalTarget = customTargetPosition ?? lastValidPreviewPosition;

        // 容错防御：如果最终目的地依旧是无穷大（说明既没有AI指定，玩家也没预览过）
        if (float.IsPositiveInfinity(finalTarget.x))
        {
            Debug.LogError($"[MoveManager] 试图移动到无效的目的地(Infinity)！已拦截。物体: {pawnObject.name}");
            isTracking = false;
            return 0f;
        }

        // 正式计算并设置路径
        if (NavMesh.CalculatePath(agent.transform.position, finalTarget, NavMesh.AllAreas
                , tempPath))
        {
            agent.SetPath(tempPath);
            return CalculatePathLength(tempPath);
        }

        return 0f;
    }

    /*/// <summary>
    /// 【公共接口】重置预览状态（切换棋子或取消选中时调用）
    /// </summary>
    public void ResetPreviewState()
    {
        lastValidPreviewPosition = Vector3.positiveInfinity; // 设为一个无穷远的值，确保下次必定触发计算
        ClearPathLine();
    }*/

    public void ClearPathLine()
    {
        if (pathRenderer != null) pathRenderer.positionCount = 0;
    }

    /// <summary>
    /// 【新增公共接口】专治技能瞬移退回原点 Bug
    /// </summary>
    /// <param name="pawnObject">要瞬移的棋子</param>
    /// <param name="targetPosition">技能锁定的目标点（可能不精准）</param>
    /// <returns>是否成功定位并瞬移</returns>
    public bool TeleportPawnSuccess(GameObject pawnObject, Vector3 targetPosition)
    {
        if (pawnObject == null) return false;

        NavMeshAgent agent = pawnObject.GetComponent<NavMeshAgent>();

        // 1. 先关闭 Agent，防止它在此期间进行任何是非合法的坐标修正
        if (agent != null)
        {
            agent.enabled = false;
        }

        // 2. 核心：在目标点半径 2.0m 范围内，寻找最近的合法 NavMesh 烘焙表面点
        // 如果你的地图落差很大，可以把 2.0f 稍微放大
        NavMeshHit hit;
        Vector3 safePosition = targetPosition;

        if (NavMesh.SamplePosition(targetPosition, out hit, 2.0f, NavMesh.AllAreas))
        {
            // 找到了最近的合法网格点
            safePosition = hit.position;
        }
        else
        {
            // 如果方圆 2 米内连物理网格都没有，说明这个技能点选得太离谱（比如掉出地图外了）
            Debug.LogWarning($"[MoveManager] 瞬移目标点 {targetPosition} 附近没有合法的 NavMesh，强行矫正可能会出错！");
            // 容错：可以保持原样，或者返回 false 拒绝瞬移
        }

        // 3. 强行更改物理坐标
        pawnObject.transform.position = safePosition;

        // 4. 重新开启 Agent（内部会自动刷新它在 NavMesh 上的采样点）
        /*if (agent != null)
        {
            agent.enabled = true;
        }*/

        return true;
    }

    #region 内部辅助方法（计算长度与渲染）

    private float CalculatePathLength(NavMeshPath path)
    {
        if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2) return 0f;

        float totalDistance = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return totalDistance;
    }

    private void DrawPath(NavMeshPath path)
    {
        if (pathRenderer == null || path.corners.Length < 2) return;

        pathRenderer.positionCount = path.corners.Length;
        for (int i = 0; i < path.corners.Length; i++)
        {
            pathRenderer.SetPosition(i, path.corners[i] + Vector3.up * 0.05f);
        }
    }

    #endregion
}

public static class NavMeshAgentExtensions
{
    public static float stoppingDestinationDistance(this NavMeshAgent agent)
    {
        // 健壮性处理：如果停止距离设置得太小，强制给一个极小的物理容错范围
        return agent.stoppingDistance == 0 ? 0.3f : agent.stoppingDistance;
    }
}