using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class MoveManager : MonoBehaviour
{
    [Header("引用对象")]
    [SerializeField] private LineRenderer pathRenderer; // 全局公用的路径渲染器

    [Header("性能优化参数")]
    [SerializeField] private float mouseMoveThreshold = 0.2f; // 鼠标世界坐标变化超过该距离时，才重新计算路径

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
                        pathRenderer.gameObject.SetActive(false); // 隐藏路径渲染器
                    }
                }
            }
        }
    }
    

    /// <summary>
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
    }

    /// <summary>
    /// 【公共接口】正式请求移动（此处的计算直接使用最后确认的有效路径，防止漂移）
    /// </summary>
    public float ExecuteMove(GameObject pawnObject, UnityAction onMoveComplete = null)
    {
        if (pawnObject == null) return 0f;

        NavMeshAgent agent = pawnObject.GetComponent<NavMeshAgent>();
        if (agent == null) return 0f;
        this.agent = agent;
        this.onReachDestination = onMoveComplete;
        isTracking = true;
        // 直接走向最后一次预览成功的有效位置
        if (NavMesh.CalculatePath(agent.transform.position, lastValidPreviewPosition, NavMesh.AllAreas, tempPath))
        {
            agent.SetPath(tempPath);
            return CalculatePathLength(tempPath);
        }

        return 0f;
    }

    /// <summary>
    /// 【公共接口】重置预览状态（切换棋子或取消选中时调用）
    /// </summary>
    public void ResetPreviewState()
    {
        lastValidPreviewPosition = Vector3.positiveInfinity; // 设为一个无穷远的值，确保下次必定触发计算
        ClearPathLine();
    }

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