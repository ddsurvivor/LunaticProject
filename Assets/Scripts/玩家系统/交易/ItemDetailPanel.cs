using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private RectTransform rectTransform; // 自身的 RectTransform

    private Canvas rootCanvas;

    private void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Show(string name , string desc, Vector2 mousePosition)
    {
        //ItemData data = GM.Ins.marketSystem.marketItemListSO.GetData(pack.itemName);
        //if (data == null) return;

        gameObject.SetActive(true);
        titleText.text = name;
        descriptionText.text = desc;

        // 立即强制刷新 UI 布局，确保获取到动态改变后的真实宽高
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // 执行屏幕边缘自适应算法
        AdjustPosition(mousePosition);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 根据鼠标位置和屏幕大小，动态调整详情面板的轴心(Pivot)以防止超出屏幕
    /// </summary>
    private void AdjustPosition(Vector2 mousePosition)
    {
        // 将屏幕坐标转换为父容器的本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform, 
            mousePosition, 
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera, 
            out Vector2 localPoint
        );

        // 设置基本位置
        rectTransform.anchoredPosition = localPoint;

        // 智能调整轴心点 (Pivot) 
        // 如果鼠标在屏幕右半边，Pivot.x = 1 (向左展开)；反之 Pivot.x = 0 (向右展开)
        float pivotX = (mousePosition.x > Screen.width * 0.5f) ? 1f : 0f;
        // 如果鼠标在屏幕上半边，Pivot.y = 1 (向下展开)；反之 Pivot.y = 0 (向上展开)
        float pivotY = (mousePosition.y > Screen.height * 0.5f) ? 1f : 0f;

        rectTransform.pivot = new Vector2(pivotX, pivotY);
    }
}