using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 自定义高级交互按钮（支持动态缩放扩展）
/// </summary>
public class CustomAdvancedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI Components")]
    [SerializeField] private Image fillImage;         // 用于左右填充的色块图片
    [SerializeField] private Image pressedImage;      // 鼠标点住时显现的图片

    [Header("Animation Settings")]
    [Tooltip("填充和减少填充的速度")]
    [SerializeField] private float fillSpeed = 8f;    

    [Header("Scale Settings (New)")]
    [Tooltip("是否开启按住放大效果")]
    [SerializeField] private bool enableScaleEffect = true; 
    [Tooltip("按住时的放大倍数（1.05 表示放大 5%）")]
    [SerializeField] private float pressedScale = 1.05f;
    [Tooltip("缩放动画的变化速度")]
    [SerializeField] private float scaleSpeed = 12f;

    [Header("Interaction Events")]
    [SerializeField] public UnityEvent onClickEvent;   // 鼠标抬起时触发的自定义事件

    private bool isHovered = false;
    private bool isPressed = false;      // 记录鼠标是否按下
    private Vector3 originalScale;       // 记录物体的初始尺寸

    public AudioCueType clickSound;
    public AudioCueType mouseOnSound;

    private void Start()
    {
        // 记录最初的缩放比例（适配可能已经被美术调整过的初始大小）
        originalScale = transform.localScale;
        
        InitUIComponents();
    }

    private void Update()
    {
        HandleFillAnimation();
        HandleScaleAnimation(); // 处理缩放动画
    }

    /// <summary>
    /// 初始化并确保Image组件的填充模式正确
    /// </summary>
    private void InitUIComponents()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            //fillImage.type = Image.Type.Filled;
            //fillImage.fillMethod = Image.FillMethod.Horizontal;
            //fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        if (pressedImage != null)
        {
            pressedImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 在Update中平滑处理Fill Amount的增加或减少
    /// </summary>
    private void HandleFillAnimation()
    {
        if (fillImage == null) return;

        float targetFill = isHovered ? 1f : 0f;
        fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFill, fillSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 在Update中平滑处理按钮的缩放
    /// </summary>
    private void HandleScaleAnimation()
    {
        if (!enableScaleEffect) return;

        // 目标缩放逻辑：只有当鼠标在按钮内(isHovered)且处于按下状态(isPressed)时，才应用放大尺寸；否则恢复原尺寸
        Vector3 targetScale = (isHovered && isPressed) ? originalScale * pressedScale : originalScale;

        // 使用 Vector3.MoveTowards 实现平滑的线性缩放过渡
        transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
    }

    #region UGUI Event System Interfaces

    // 鼠标进入
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        GM.Ins.AM.PlayAudio(mouseOnSound);
    }

    // 鼠标移出
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        // 注意：如果按住鼠标并拖出按钮范围，按钮应该安全地恢复原大小
    }

    // 鼠标按下
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;

        if (pressedImage != null)
        {
            pressedImage.gameObject.SetActive(true);
        }
        
    }

    // 鼠标抬起
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (pressedImage != null)
        {
            pressedImage.gameObject.SetActive(false);
        }

        GM.Ins.AM.PlayAudio(clickSound);
        // 只有在按钮内部抬起时，才算作一次成功的点击，触发事件
        if (isHovered && onClickEvent != null)
        {
            onClickEvent.Invoke();
        }
    }

    private void OnDisable()
    {
        fillImage.fillAmount = 0f;
        isHovered = false;
    }

    private void OnEnable()
    {
        fillImage.fillAmount = 0f;
        isHovered = false;
    }

    #endregion
}