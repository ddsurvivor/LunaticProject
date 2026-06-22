using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 自定义高级交互按钮（支持动态缩放扩展与键盘长按蓄力触发）
/// </summary>
public class CustomAdvancedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI Components")]
    [SerializeField] private Image fillImage;         // 用于左右填充的色块图片
    [SerializeField] private Image pressedImage;      // 鼠标点住时显现的图片

    [Header("Animation Settings")]
    [Tooltip("填充和减少填充的速度（鼠标悬停或松开按键回退时生效）")]
    [SerializeField] private float fillSpeed = 8f;    

    [Header("Scale Settings")]
    [Tooltip("是否开启按住放大效果")]
    [SerializeField] private bool enableScaleEffect = true; 
    [Tooltip("按住时的放大倍数（1.05 表示放大 5%）")]
    [SerializeField] private float pressedScale = 1.05f;
    [Tooltip("缩放动画的变化速度")]
    [SerializeField] private float scaleSpeed = 12f;

    [Header("Keyboard Binding Settings (New)")]
    [Tooltip("是否开启键盘按键映射触发")]
    [SerializeField] private bool enableKeyBinding = true;
    [Tooltip("绑定的触发键盘按键")]
    [SerializeField] private KeyCode activationKey = KeyCode.E;
    [Tooltip("需要长按充满的时间（秒）")]
    [SerializeField] private float keyHoldDuration = 1.0f;

    [Header("Interaction Events")]
    [SerializeField] public UnityEvent onClickEvent;   // 核心触发自定义事件

    private bool isHovered = false;
    private bool isPressed = false;       // 记录鼠标是否按下
    private Vector3 originalScale;        // 记录物体的初始尺寸

    // 键盘长按状态私有变量
    private bool isKeyHolding = false;
    private float currentKeyHoldTime = 0f;
    private bool keyTriggered = false;    // 防止单次长按满后重复触发

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
        HandleKeyboardInput(); // 优先处理键盘输入逻辑
        HandleFillAnimation();  // 处理填充动画
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
        }

        if (pressedImage != null)
        {
            pressedImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 处理键盘长按的核心状态机
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (!enableKeyBinding) return;

        // 当玩家按住绑定按键时
        if (Input.GetKey(activationKey))
        {
            if (!keyTriggered)
            {
                isKeyHolding = true;
                currentKeyHoldTime += Time.deltaTime;

                // 表现层：按住时让高亮/按下贴图显现
                if (pressedImage != null) pressedImage.gameObject.SetActive(true);

                // 蓄力时间全满，触发点击
                if (currentKeyHoldTime >= keyHoldDuration)
                {
                    TriggerButtonClick();
                    keyTriggered = true; // 标记本轮已触发，不再重复触发
                }
            }
        }
        else
        {
            // 玩家松开按键，重置键盘蓄力相关的状态
            if (isKeyHolding || keyTriggered)
            {
                isKeyHolding = false;
                currentKeyHoldTime = 0f;
                keyTriggered = false;

                // 如果此时鼠标没有点着，就关闭按下贴图
                if (!isPressed && pressedImage != null) 
                {
                    pressedImage.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 在Update中平滑处理Fill Amount的增加或减少
    /// </summary>
    private void HandleFillAnimation()
    {
        if (fillImage == null) return;

        // 如果开启了键盘绑定且玩家正在长按蓄力，填充进度由长按时间百分比绝对控制
        if (enableKeyBinding && isKeyHolding)
        {
            fillImage.fillAmount = Mathf.Clamp01(currentKeyHoldTime / keyHoldDuration);
        }
        else
        {
            // 否则维持原有的鼠标悬停填充逻辑（松开键盘后，进度条会通过 MoveTowards 丝滑地退回 0）
            float targetFill = isHovered ? 1f : 0f;
            fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFill, fillSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 在Update中平滑处理按钮的缩放
    /// </summary>
    private void HandleScaleAnimation()
    {
        if (!enableScaleEffect) return;

        // 目标缩放逻辑：鼠标处于悬停且按下状态，或者键盘正在长按蓄力时，均应用放大尺寸
        bool shouldScale = (isHovered && isPressed) || (enableKeyBinding && isKeyHolding);
        Vector3 targetScale = shouldScale ? originalScale * pressedScale : originalScale;

        // 使用 Vector3.MoveTowards 实现平滑的线性缩放过渡
        transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 触发核心点击事件（由鼠标抬起或键盘长按蓄力满时调用）
    /// </summary>
    private void TriggerButtonClick()
    {
        GM.Ins.AM.PlayAudio(clickSound);
        if (onClickEvent != null)
        {
            onClickEvent.Invoke();
        }
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

        // 如果此时键盘也没有在长按，才关闭按下贴图
        if (!isKeyHolding && pressedImage != null)
        {
            pressedImage.gameObject.SetActive(false);
        }

        // 只有在按钮内部抬起时，才算作一次成功的鼠标点击
        if (isHovered)
        {
            TriggerButtonClick();
        }
    }

    private void OnDisable()
    {
        ResetAllStates();
    }

    private void OnEnable()
    {
        ResetAllStates();
    }

    /// <summary>
    /// 当UI激活、隐藏时，安全重置所有交互状态，防止逻辑卡死
    /// </summary>
    private void ResetAllStates()
    {
        if (fillImage != null) fillImage.fillAmount = 0f;
        isHovered = false;
        isPressed = false;
        isKeyHolding = false;
        currentKeyHoldTime = 0f;
        keyTriggered = false;
    }

    #endregion
}