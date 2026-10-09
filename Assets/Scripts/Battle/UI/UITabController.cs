using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using DG.Tweening; // 引入 DOTween 命名空间

/// <summary>切换标签页，并让指示框跟随选中的按钮。</summary>
public class UITabController : MonoBehaviour
{
    [System.Serializable]
    public struct TabItem
    {
        public GameObject tabButton;       // 选项卡按钮
        public Image tabBgImage;       // 选项卡自身的底图
        public GameObject selected;    // 选中状态的标记 (可为空)
        public GameObject subPage;     // 该选项卡绑定的子页面 (可为空)
    }

    [Header("Tabs Data")]
    [SerializeField] private List<TabItem> tabs;
    [SerializeField] private Sprite activeTabSprite;   // 选中状态的标签底图
    [SerializeField] private Sprite inactiveTabSprite; // 未选中状态的标签底图

    [Header("Follow Indicator (DOTween)")]
    [SerializeField] private RectTransform indicatorBox; // 跟随选中的方框
    [SerializeField] private float duration = 0.25f;     // 移动耗时
    [SerializeField] private Ease easeType = Ease.OutQuad;// 缓动动画类型

    /// <summary>通知订阅者当前选中的标签页。</summary>
    public event Action<int> OnTabChanged;

    private int currentSelectedIndex = -1;
    private Tween indicatorTween;
    private Vector3 indicatorMoveStart;
    private float indicatorProgress = 1f;

    [SerializeField]
    private UIPanel _uiPanel;

    private void Awake()
    {
        InitTabs();
    }

    /// <summary>首次打开第 0 页，再次打开恢复上次选择。</summary>
    private void OnEnable()
    {
        Canvas.willRenderCanvases += UpdateIndicatorPosition;
        // 首次初始化选中 0；重新打开时恢复关闭前的页面和指示框位置。
        SwitchTab(currentSelectedIndex < 0 ? 0 : currentSelectedIndex, true);
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdateIndicatorPosition;
        indicatorTween?.Kill();
        indicatorTween = null;
    }

    private void InitTabs()
    {
        if (tabs == null) return;
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; // 解决闭包陷阱
            if (tabs[i].tabButton != null)
            {
                tabs[i].tabButton.GetComponent<CustomAdvancedButton>()?.onClickEvent.AddListener(() => SwitchTab(index, false));
                tabs[i].selected?.SetActive(false); // 初始化时，所有选中标记都隐藏
            }
        }
    }

    /// <summary>
    /// 更新标签状态、页面显示和指示框位置。
    /// </summary>
    /// <param name="targetIndex">目标索引</param>
    /// <param name="isImmediate">是否瞬间切过去（不播放动画）</param>
    public void SwitchTab(int targetIndex, bool isImmediate)
    {
        // 边界安全检查
        if (tabs == null || targetIndex < 0 || targetIndex >= tabs.Count) return;
        // 如果点的已经是当前页，且不是强制初始化，则无视
        //if (targetIndex == currentSelectedIndex && !isImmediate) return;

        currentSelectedIndex = targetIndex;

        // 1. 刷新所有标签的底图和子页面的显示隐藏
        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == targetIndex);
            tabs[i].selected?.SetActive(false);
            if (tabs[i].subPage != null)
            {
                if (tabs[i].subPage.GetComponent<UIPanel>() != null)
                {
                    if (isActive)
                    {
                        tabs[i].selected?.SetActive(true);
                        tabs[i].subPage.GetComponent<UIPanel>()?.Open();
                    }
                    else
                    {
                        tabs[i].subPage.GetComponent<UIPanel>()?.Close();
                    }
                }
                else
                {
                    tabs[i].subPage.SetActive(isActive);
                }
            }

            if (tabs[i].tabBgImage != null && activeTabSprite != null && inactiveTabSprite != null)
                tabs[i].tabBgImage.sprite = isActive ? activeTabSprite : inactiveTabSprite;
        }

        // 2. 控制方框跟随（RectTransform 坐标动画）
        if (indicatorBox != null && tabs[targetIndex].tabButton != null)
        {
            RectTransform targetButtonRect = tabs[targetIndex].tabButton.GetComponent<RectTransform>();
            if (targetButtonRect != null)
            {
                indicatorTween?.Kill();
                indicatorTween = null;

                indicatorBox.gameObject.SetActive(true);
                indicatorMoveStart = indicatorBox.localPosition;
                // 强制布局会触发 willRenderCanvases；先锁住起点，避免提前跳到终点。
                indicatorProgress = 0f;
                Canvas.ForceUpdateCanvases();

                if (isImmediate || duration <= 0f)
                {
                    indicatorProgress = 1f;
                    UpdateIndicatorPosition();
                }
                else
                {
                    indicatorProgress = 0f;
                    indicatorTween = DOTween.To(() => indicatorProgress, value =>
                    {
                        indicatorProgress = value;
                        UpdateIndicatorPosition();
                    }, 1f, duration).SetEase(easeType).SetTarget(indicatorBox);
                }
            }
        }

        // 3. 广播事件，通知可能存在的外部订阅者
        OnTabChanged?.Invoke(targetIndex);
    }

    /// <summary>在指示框父级坐标中对齐按钮中心。</summary>
    private void UpdateIndicatorPosition()
    {
        if (indicatorBox == null || tabs == null || currentSelectedIndex < 0 || currentSelectedIndex >= tabs.Count)
            return;
        var button = tabs[currentSelectedIndex].tabButton;
        if (button == null || !(button.transform is RectTransform target)) return;

        // 对齐矩形中心，而非 pivot；父级、锚点、pivot 和缩放不同也使用同一坐标系。
        Vector3 center = target.TransformPoint(target.rect.center);
        if (indicatorBox.parent != null) center = indicatorBox.parent.InverseTransformPoint(center);
        Vector3 pivotOffset = indicatorBox.localRotation * Vector3.Scale(indicatorBox.rect.center, indicatorBox.localScale);
        Vector3 destination = center - pivotOffset;
        // 每次渲染前取最新目标，兼容布局重建和 UIPanel 的缩放、位移动画。
        indicatorBox.localPosition = Vector3.LerpUnclamped(indicatorMoveStart, destination, indicatorProgress);
    }
    
    /// <summary>打开面板，并直接显示指定标签页。</summary>
    public void ShowTab(int index)
    {
        if (tabs == null || index < 0 || index >= tabs.Count) return;
        //gameObject.SetActive(true);
        if (_uiPanel != null) _uiPanel.Open();
        else gameObject.SetActive(true);
        SwitchTab(index, true);
    }
}
