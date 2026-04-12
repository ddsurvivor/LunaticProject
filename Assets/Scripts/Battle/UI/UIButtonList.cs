using UnityEngine;
using System.Collections.Generic;

public class UIButtonList : MonoBehaviour
{
    [Header("布局设置")]
    [SerializeField] private float spacing = 10f; // 按钮之间的固定间距
    [SerializeField] private float leftPadding = 0f; // 靠左的偏移量

    private List<RectTransform> _buttonRects = new List<RectTransform>();

    private void Awake()
    {
        RefreshButtonList();
    }

    // 可以在编辑器中实时调试，也可以在运行时调用
    private void Update()
    {
        UpdateLayout();
    }

    /// <summary>
    /// 初始化或刷新按钮列表（如果有动态增减按钮时调用）
    /// </summary>
    public void RefreshButtonList()
    {
        _buttonRects.Clear();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                _buttonRects.Add(child as RectTransform);
            }
        }
    }

    /// <summary>
    /// 核心布局计算逻辑
    /// </summary>
    private void UpdateLayout()
    {
        if (_buttonRects.Count == 0) return;

        float currentY = 0;

        for (int i = 0; i < _buttonRects.Count; i++)
        {
            RectTransform rect = _buttonRects[i];
            
            // 关键改动：如果按钮当前未激活，直接跳过，不参与 Y 坐标累加
            if (rect == null || !rect.gameObject.activeInHierarchy) 
                continue;
            // 确保锚点和中心点设置正确 (建议：Pivot(0, 1), Anchor(0, 1))
            // 这样放大时会向右、向下扩张，方便我们计算
            
            // 1. 处理靠左对齐：将 Local X 设为固定值
            // 考虑到放大时 Scale 的影响，保持 X 为 leftPadding
            float targetX = leftPadding;

            // 2. 计算当前按钮的高度（考虑缩放后的视觉高度）
            // rect.rect.height 是原始高度，乘以 localScale.y 得到当前实际高度
            float scaledHeight = rect.rect.height * rect.localScale.y;

            // 3. 设置位置
            // 我们基于顶部对齐，所以 Y 是负值往下走
            rect.localPosition = new Vector3(targetX, currentY, 0);

            // 4. 累加偏移量：当前高度 + 固定间距
            // 关键点：这里直接用缩放后的高度计算，确保下方按钮自动被“推开”
            currentY -= (scaledHeight + spacing);
        }
    }
}