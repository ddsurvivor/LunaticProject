using UnityEngine;
using UnityEngine.UI; // 确保引用，支持旧版 Text 所在的 UI 系统
using DG.Tweening;
using Sirenix.OdinInspector;

public class UIMovement : MonoBehaviour
{
    [Header("旋转设置")]
    [SerializeField] private bool enableRotation = true;
    [ShowIf("@enableRotation == true")] [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 0, 50f); // 每秒旋转度数

    [Header("浮动设置")]
    [SerializeField] private bool enableFloating = true;
    [ShowIf("@enableFloating == true")][SerializeField] private float floatDistance = 20f;   // 浮动上下位移像素
    [ShowIf("@enableFloating == true")][SerializeField] private float floatDuration = 2f;    // 循环周期（秒）
    [ShowIf("@enableFloating == true")] [SerializeField] private Ease floatEase = Ease.InOutQuad;
    
    [Header("闪烁设置")]
    [SerializeField] private bool enableFlicker = false;
    [SerializeField] private SpriteRenderer targetRenderer; // 需要闪烁的 SpriteRenderer
    [ShowIf("@enableFlicker == true")][SerializeField] private float flickerDuration = 0.5f; // 闪烁周期

    // 缓存 Tween 引用以便销毁
    private Tween _rotateTween;
    private Tween _floatTween;
    private Tween _flickerTween;

    private void OnEnable()
    {
        StartMotion();
    }

    private void StartMotion()
    {
        // 1. 清理正在运行的动画
        KillTweens();

        // 2. 处理旋转逻辑
        if (enableRotation)
        {
            // 使用 Incremental 模式实现无缝旋转
            _rotateTween = transform.DOLocalRotate(rotationSpeed, 1f, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        // 3. 处理浮动逻辑
        if (enableFloating)
        {
            // 基于当前的 localPosition 进行偏移
            _floatTween = transform.DOLocalMoveY(transform.localPosition.y + floatDistance, floatDuration)
                .SetEase(floatEase)
                .SetLoops(-1, LoopType.Yoyo);
        }

        if (enableFlicker)
        {
            _flickerTween = targetRenderer.DOFade(0f, flickerDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void KillTweens()
    {
        // 显式停止并销毁动画，防止内存泄漏或逻辑冲突
        _rotateTween?.Kill();
        _floatTween?.Kill();
    }
}