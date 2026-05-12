using UnityEngine;

public class EnemyAttackIndicator : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField, Tooltip("指向子物体或外部的 LineRenderer")]
    private LineRenderer lineRenderer;

    [Header("抛物线参数")]
    [SerializeField] private float arcHeight = 2.0f;
    [SerializeField] private int segmentCount = 25;

    /// <summary>
    /// 激活并更新指示器线条
    /// </summary>
    /// <param name="target">玩家棋子的位置</param>
    public void ShowIndicator(Transform target)
    {
        if (lineRenderer == null || target == null) return;

        // 确保物体已激活
        lineRenderer.gameObject.SetActive(true);

        // 设置点数
        lineRenderer.positionCount = segmentCount + 1;

        Vector3 startPos = transform.position;
        Vector3 endPos = target.position;

        // 这里的计算只在调用时执行一次
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            Vector3 point = CalculateParabolaPoint(startPos, endPos, arcHeight, t);
            lineRenderer.SetPosition(i, point);
        }
    }

    /// <summary>
    /// 隐藏指示器
    /// </summary>
    public void HideIndicator()
    {
        if (lineRenderer != null)
        {
            lineRenderer.gameObject.SetActive(false);
        }
    }

    private Vector3 CalculateParabolaPoint(Vector3 start, Vector3 end, float height, float t)
    {
        Vector3 midPoint = Vector3.Lerp(start, end, t);
        // 使用二次函数模拟抛物线弧度
        float yOffset = height * (1 - 4 * Mathf.Pow(t - 0.5f, 2));
        return new Vector3(midPoint.x, midPoint.y + yOffset, midPoint.z);
    }
}