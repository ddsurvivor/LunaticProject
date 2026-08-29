using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class PieceMover : MonoBehaviour
{

    [SerializeField] private PieceController piece;
    [Header("路径显示")]
    [SerializeField] private LineRenderer pathRenderer; // 每个棋子专属的路径渲染器
    [SerializeField] private float pathHeightOffset = 0.05f; // 抬高路径避免与地面穿插闪烁

    private NavMeshAgent agent;
    private NavMeshPath currentPath; // 缓存棋子当前的路径
    private UnityAction onReachDestination;
    private bool isTracking = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentPath = new NavMeshPath();

        // 自动获取身上的 LineRenderer
        if (pathRenderer == null)
        {
            pathRenderer = GetComponentInChildren<LineRenderer>();
        }
        
        ClearPath();
    }

    /// <summary>
    /// 1. 预览移动路径（由 MoveManager 统筹调用）
    /// 返回最终被截断/实际确认的终点坐标
    /// </summary>
    public Vector3 PreviewPath(Vector3 targetPosition, float maxDistance)
    {
        if (pathRenderer == null) return agent.transform.position;
        pathRenderer.gameObject.SetActive(true);

        if (NavMesh.CalculatePath(agent.transform.position, targetPosition, NavMesh.AllAreas, currentPath))
        {
            float totalPathLength = CalculatePathLength(currentPath);

            if (totalPathLength <= maxDistance)
            {
                // 没超出范围，完整绘制
                DrawPath(currentPath);
                return targetPosition;
            }
            else
            {
                // 超出范围，进行裁剪绘制，并返回裁剪后的极限点位
                return DrawAndGetCroppedPath(currentPath, maxDistance);
            }
        }
        return agent.transform.position; // 寻路失败返回原地
    }

    /// <summary>
    /// 2. 正式开始移动
    /// </summary>
    public void StartMove(Vector3 finalTargetPosition, UnityAction onComplete)
    {
        this.onReachDestination = onComplete;
        this.isTracking = true;
        agent.enabled = true;

        // 设置目的地，复用之前计算好的 NavMeshPath 或让 Agent 自动走
        if (NavMesh.CalculatePath(agent.transform.position, finalTargetPosition, NavMesh.AllAreas, currentPath))
        {
            agent.SetPath(currentPath);
            // 移动期间如果想继续显示路径，可以调用 DrawPath(currentPath)
            // 否则如果不想显示可以调用 ClearPath() 隐藏
        }
    }

    private void Update()
    {
        if (!isTracking || agent == null) return;

        // 核心到达判定条件（与原先 MoveManager 一致）
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) // 这里的 stoppingDistance 可结合你的扩展方法
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 2f)
            {
                isTracking = false;
                
                ClearPath(); // 到达终点后清理并隐藏路径

                // 触发回调
                onReachDestination?.Invoke();

                // 通知全局管理器
                if (BattleScene.Ins != null && BattleScene.Ins.BM != null)
                {
                    BattleScene.Ins.BM.orderManager.OnUnityMoveEnd(piece);
                }
            }
        }
    }

    #region 路径计算与渲染 (移植自原 MoveManager)

    private void DrawPath(NavMeshPath path)
    {
        if (pathRenderer == null || path.corners.Length < 2) return;

        pathRenderer.positionCount = path.corners.Length;
        for (int i = 0; i < path.corners.Length; i++)
        {
            // 加上高度偏移，避免Z-fighting
            pathRenderer.SetPosition(i, path.corners[i] + Vector3.up * pathHeightOffset);
        }
    }

    private Vector3 DrawAndGetCroppedPath(NavMeshPath path, float maxDistance)
    {
        if (path.corners.Length < 2) return path.corners[0];

        List<Vector3> croppedCorners = new List<Vector3>();
        float accumulatedDistance = 0f;
        croppedCorners.Add(path.corners[0] + Vector3.up * pathHeightOffset);

        Vector3 finalPoint = path.corners[0];

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 start = path.corners[i];
            Vector3 end = path.corners[i + 1];
            float segmentLength = Vector3.Distance(start, end);

            if (accumulatedDistance + segmentLength >= maxDistance)
            {
                float percent = (maxDistance - accumulatedDistance) / segmentLength;
                finalPoint = Vector3.Lerp(start, end, percent);
                croppedCorners.Add(finalPoint + Vector3.up * pathHeightOffset);
                break;
            }

            accumulatedDistance += segmentLength;
            finalPoint = end;
            croppedCorners.Add(end + Vector3.up * pathHeightOffset);
        }

        pathRenderer.positionCount = croppedCorners.Count;
        pathRenderer.SetPositions(croppedCorners.ToArray());

        return finalPoint;
    }

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

    public void ClearPath()
    {
        if (pathRenderer != null)
        {
            pathRenderer.positionCount = 0;
            pathRenderer.gameObject.SetActive(false);
        }
    }

    #endregion
}