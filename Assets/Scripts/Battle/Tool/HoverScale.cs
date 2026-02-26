using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 挂载在Button上，实现鼠标悬停时放大，移出时恢复。
/// 通过每帧检测鼠标位置来弥补 OnPointerExit 可能不触发的问题。
/// </summary>
[RequireComponent(typeof(Button))] // 确保挂载在按钮上，但非必须
public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("缩放设置")]
    [SerializeField] private float hoverScale = 1.2f;          // 悬停时放大倍数
    [SerializeField] private float animationDuration = 0.2f;   // 动画时长（秒）
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 动画曲线

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private bool isHovered = false;        // 标记当前是否处于悬停状态（由事件或检测更新）
    
    private Button button; // 可选：如果需要根据按钮状态调整行为
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("HoverScale requires a RectTransform on the same GameObject.");
            enabled = false;
            return;
        }
        originalScale = rectTransform.localScale;
        button = GetComponent<Button>(); // 可选：获取按钮组件以检查状态
    }

    void OnEnable()
    {
        // 确保初始化状态正确
        if (rectTransform != null)
            rectTransform.localScale = originalScale;
        isHovered = false;
    }

    void OnDisable()
    {
        // 禁用时立即恢复原始大小，避免残留放大状态
        if (rectTransform != null)
            rectTransform.localScale = originalScale;
        isHovered = false;
    }

    // Update 作为后备检测：当 OnPointerExit 由于遮挡未能触发时，手动检查鼠标位置
    void Update()
    {
        // 如果当前标记为悬停，但鼠标实际已不在按钮区域内，则强制退出
        if (isHovered && !IsPointerOverButton())
        {
            // 手动触发退出效果
            SetHoverState(false);
        }
    }

    /// <summary>
    /// 判断鼠标是否在按钮区域内
    /// </summary>
    private bool IsPointerOverButton()
    {
        // 如果没有有效的 EventSystem 或鼠标，返回 false
        if (EventSystem.current == null) return false;

        // 获取鼠标位置（支持多个指针，但通常用鼠标左键位置）
        Vector2 pointerPosition = Input.mousePosition;

        // 将屏幕坐标转换为按钮的本地矩形坐标
        bool isOver = RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            pointerPosition,
            GetComponentInParent<Canvas>()?.worldCamera ?? null
        );

        return isOver;
    }

    // IPointerEnterHandler
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverState(true);
    }

    // IPointerExitHandler
    public void OnPointerExit(PointerEventData eventData)
    {
        // 注意：即使 OnPointerExit 被触发，我们也会设置状态，但仍由 Update 进行双重保障
        SetHoverState(false);
    }

    /// <summary>
    /// 设置悬停状态，并启动缩放动画
    /// </summary>
    private void SetHoverState(bool hovered)
    {
        if(button!=null && button.enabled == false) // 如果按钮不可用，不处理悬停效果
        {
            hovered = false;
        }
        // 如果状态没变，不处理
        if (isHovered == hovered) return;
        
        isHovered = hovered;
        Vector3 targetScale = hovered ? originalScale * hoverScale : originalScale;

        // 停止正在进行的动画
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        // 启动新的缩放动画
        scaleCoroutine = StartCoroutine(ScaleTo(targetScale, animationDuration));
    }

    /// <summary>
    /// 协程：平滑缩放到目标大小
    /// </summary>
    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = rectTransform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 使用不受Time.timeScale影响的计时，保证动画流畅
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = scaleCurve.Evaluate(t); // 应用动画曲线
            rectTransform.localScale = Vector3.LerpUnclamped(start, target, curveValue);
            yield return null;
        }

        rectTransform.localScale = target; // 确保最终值精确
        scaleCoroutine = null;
    }
}