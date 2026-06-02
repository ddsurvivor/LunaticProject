using UnityEngine;
using UnityEngine.AI;

public class ClickToMoveController : MonoBehaviour
{
    [Header("配置参数")]
    [SerializeField] private LayerMask groundLayer;      // 地面的 Layer
    [SerializeField] private float maxMoveDistance = 5.0f; // 棋子的最大移动范围（距离）

    [Header("引用对象")]
    [SerializeField] private NavMeshAgent targetAgent;   // 目标棋子
    [SerializeField] private LineRenderer pathRenderer;  // 用于显示路径的 LineRenderer

    private NavMeshPath calculatedPath; // 用于暂存计算结果的路径对象

    private void Awake()
    {
        // 初始化路径对象
        calculatedPath = new NavMeshPath();
    }

    private void Update()
    {
        // 1. 处理鼠标点击
        if (Input.GetMouseButtonDown(0))
        {
            HandleMovementInput();
        }
    }

    /// <summary>
    /// 处理点击移动与路径验证
    /// </summary>
    private void HandleMovementInput()
    {
        if (targetAgent == null || pathRenderer == null)
        {
            Debug.LogWarning("未完全配置 TargetAgent 或 PathRenderer！");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            Vector3 targetPosition = hit.point;

            // 2. 提前计算寻路路径（此时棋子尚未开始移动）
            // 参数：起点、终点、可行走区域掩码、存储路径的对象
            if (NavMesh.CalculatePath(targetAgent.transform.position, targetPosition, NavMesh.AllAreas, calculatedPath))
            {
                // 3. 计算这条寻路路径的实际总长度
                float pathLength = CalculatePathLength(calculatedPath);

                // 4. 判断是否在移动范围内
                if (pathLength <= maxMoveDistance)
                {
                    // 在范围内，允许移动
                    targetAgent.SetPath(calculatedPath);

                    // 5. 渲染路径线段
                    DrawPath(calculatedPath);
                }
                else
                {
                    // 超过范围，拒绝移动，并清除上一次的线段
                    Debug.Log($"目标点太远！实际需要距离: {pathLength:F2}，最大限制: {maxMoveDistance}");
                    ClearPathLine();
                }
            }
        }
    }

    /// <summary>
    /// 计算 NavMeshPath 的多段线总长度
    /// </summary>
    private float CalculatePathLength(NavMeshPath path)
    {
        // 如果路径无效或节点太少，距离为 0
        if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
            return 0f;

        float totalDistance = 0f;

        // 累加各个拐点（Corners）之间的距离
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return totalDistance;
    }

    /// <summary>
    /// 使用 LineRenderer 将路径绘制到场景中
    /// </summary>
    private void DrawPath(NavMeshPath path)
    {
        if (path.corners.Length < 2) return;

        // 设置 LineRenderer 的顶点数量
        pathRenderer.positionCount = path.corners.Length;

        // 将拐点坐标数组直接赋给 LineRenderer
        for (int i = 0; i < path.corners.Length; i++)
        {
            // 微微抬高一点 Y 轴（例如 0.05），防止线段与地面发生层级冲突（Z-Fighting）而闪烁
            Vector3 vertexPosition = path.corners[i] + Vector3.up * 0.05f;
            pathRenderer.SetPosition(i, vertexPosition);
        }
    }

    /// <summary>
    /// 清除路径线
    /// </summary>
    private void ClearPathLine()
    {
        pathRenderer.positionCount = 0;
    }
}