using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using DG.Tweening; // 引入 DOTween 命名空间

public class UITabController : MonoBehaviour
{
    [System.Serializable]
    public struct TabItem
    {
        public Button tabButton;       // 选项卡按钮
        public Image tabBgImage;       // 选项卡自身的底图
        public GameObject subPage;     // 该选项卡绑定的子页面 (可为空)
    }

    [Header("Tabs Data")]
    [SerializeField] private List<TabItem> tabs;
    [SerializeField] private Sprite activeTabSprite;   // 选中状态的标签底图
    [SerializeField] private Sprite inactiveTabSprite; // 未选中状态的标签底图
    [SerializeField] private int defaultIndex = 0;     // 默认打开第几个

    [Header("Follow Indicator (DOTween)")]
    [SerializeField] private RectTransform indicatorBox; // 跟随选中的方框
    [SerializeField] private float duration = 0.25f;     // 移动耗时
    [SerializeField] private Ease easeType = Ease.OutQuad;// 缓动动画类型

    // 核心解耦机制：向外暴露出切换事件，其他面板想听就听，不听也完全不影响导航栏运行
    public event Action<int> OnTabChanged;

    private int currentSelectedIndex = -1;

    private void Awake()
    {
        InitTabs();
    }

    private void Start()
    {
        // 游戏启动首帧：瞬间切到默认页，不需要飞行过渡动画
        SwitchTab(defaultIndex, true);
    }

    private void InitTabs()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; // 解决闭包陷阱
            if (tabs[i].tabButton != null)
            {
                tabs[i].tabButton.onClick.AddListener(() => SwitchTab(index, false));
            }
        }
    }

    /// <summary>
    /// 核心切换方法
    /// </summary>
    /// <param name="targetIndex">目标索引</param>
    /// <param name="isImmediate">是否瞬间切过去（不播放动画）</param>
    public void SwitchTab(int targetIndex, bool isImmediate)
    {
        // 边界安全检查
        if (targetIndex < 0 || targetIndex >= tabs.Count) return;
        // 如果点的已经是当前页，且不是强制初始化，则无视
        if (targetIndex == currentSelectedIndex && !isImmediate) return;

        currentSelectedIndex = targetIndex;

        // 1. 刷新所有标签的底图和子页面的显示隐藏
        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == targetIndex);
            
            if (tabs[i].subPage != null)
                tabs[i].subPage.SetActive(isActive);

            if (tabs[i].tabBgImage != null && activeTabSprite != null && inactiveTabSprite != null)
                tabs[i].tabBgImage.sprite = isActive ? activeTabSprite : inactiveTabSprite;
        }

        // 2. 控制方框跟随（RectTransform 坐标动画）
        if (indicatorBox != null)
        {
            RectTransform targetButtonRect = tabs[targetIndex].tabButton.GetComponent<RectTransform>();
            if (targetButtonRect != null)
            {
                // 工业防卡死死律：在开启新动画前，必须杀死旧动画，防止玩家疯狂连点导致抖动
                indicatorBox.DOKill();

                // 获取目标按钮的目标局部坐标
                Vector2 targetAnchoredPos = targetButtonRect.anchoredPosition;

                if (isImmediate)
                {
                    indicatorBox.anchoredPosition = targetAnchoredPos;
                }
                else
                {
                    // 使用 DOTween 核心 API 进行平滑缓动
                    indicatorBox.DOAnchorPos(targetAnchoredPos, duration).SetEase(easeType);
                }
            }
        }

        // 3. 广播事件，通知可能存在的外部订阅者
        OnTabChanged?.Invoke(targetIndex);
    }
}