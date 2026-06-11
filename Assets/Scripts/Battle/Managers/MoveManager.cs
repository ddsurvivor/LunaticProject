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
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    isTracking = false;

                    // 执行外部传入的回调函数（如果存在的话）
                    if (onReachDestination != null)
                    {
                        onReachDestination.Invoke();
                    }
                    pathRenderer.gameObject.SetActive(false); // 隐藏路径渲染器
                    ResetPreviewState(); // 重置预览状态，准备下一次使用
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

    // ======= 请在 MoveManager 类顶部补充这个变量 =======
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
        ClearPathLine();
    }

    /// <summary>
    /// 【核心准备函数】在棋子真正开始平移前调用，动态重置全场棋子的避障权重
    /// </summary>
    /// <param name="movingPawn">当前准备移动的棋子 GameObject</param>
    public void SetupMovementPriorities(GameObject movingPawn)
    {
        if (movingPawn == null)
        {
            Debug.LogWarning("[MoveManager] 准备移动的棋子为空，取消权重设置。");
            return;
        }

        // 1. 将当前移动棋子的优先级降到最低 (99)，使其作为寻路者主动绕行
        NavMeshAgent movingAgent = movingPawn.GetComponent<NavMeshAgent>();
        if (movingAgent != null)
        {
            movingAgent.avoidancePriority = 99;
        }

        // 2. 验证单例引用是否安全
        if (BattleScene.Ins == null || BattleScene.Ins.BM == null)
        {
            Debug.LogError("[MoveManager] 战斗场景单例 BattleScene.Ins 或 BM 未初始化！");
            return;
        }

        // 3. 处理玩家的所有棋子
        var playerController = BattleScene.Ins.BM.PlayerController;
        if (playerController != null && playerController.pieces != null)
        {
            foreach (var piece in playerController.pieces)
            {
                // 过滤无效棋子，并确保不操作当前移动的棋子
                if (piece == null || piece.gameObject == movingPawn) continue;

                // 将静止棋子的优先级拉到最高 (0)，使其稳如泰山，绝不让路
                NavMeshAgent agent = piece.gameObject.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.avoidancePriority = 0;
                }
            }
        }

        // 4. 处理 AI 的所有棋子
        var aiController = BattleScene.Ins.BM.AIController;
        if (aiController != null && aiController.pieces != null)
        {
            foreach (var piece in aiController.pieces)
            {
                // 过滤无效棋子，并确保不操作当前移动的棋子
                if (piece == null || piece.gameObject == movingPawn) continue;

                // 同理，将 AI 静止棋子的优先级拉到最高 (0)
                NavMeshAgent agent = piece.gameObject.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.avoidancePriority = 0;
                }
            }
        }
    }

    /// <summary>
    /// 【公共接口】正式请求移动（此处的计算直接使用最后确认的有效路径，防止漂移）
    /// </summary>
    public float ExecuteMove(GameObject pawnObject, UnityAction onMoveComplete = null)
    {
        if (pawnObject == null) return 0f;

        NavMeshAgent agent = pawnObject.GetComponent<NavMeshAgent>();
        if (agent == null) return 0f;

        SetupMovementPriorities(pawnObject); // 在正式移动前设置权重，确保避障行为正确
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
        return agent.stoppingDistance == 0 ? 0.1f : agent.stoppingDistance;
    }
}