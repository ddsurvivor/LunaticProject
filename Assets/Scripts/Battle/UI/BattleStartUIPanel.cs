using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 确保引入旧版 UI 命名空间

[RequireComponent(typeof(CanvasGroup))]
public class BattleStartUIPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform bgBlock;       // 长方形色块
    [SerializeField] private RectTransform startIcon;     // 战斗开始 Icon
    
    [Header("Animation Settings")]
    [SerializeField] private float iconMoveDistance = 150f; // Icon 向右移动的距离

    [SerializeField]private CanvasGroup canvasGroup;
    private Vector2 originalIconAnchoredPos;

    private void Awake()
    {
        // 缓存 CanvasGroup 组件，避免运行时频繁 GetComponent
        canvasGroup = GetComponent<CanvasGroup>();
        
        // 记录 Icon 的初始位置，以便动效可以重复正确播放
        if (startIcon != null)
        {
            originalIconAnchoredPos = startIcon.anchoredPosition;
        }
    }

    /// <summary>
    /// 播放战斗开始动画的主入口
    /// </summary>
    /// <param name="totalDuration">动画总持续时间（秒）</param>
    public void PlayBattleStartAnimation(float totalDuration)
    {
        if (bgBlock == null || startIcon == null || canvasGroup == null)
        {
            Debug.LogError("BattleStartUIPanel: 核心组件未分配！", this);
            return;
        }

        // 确保面板激活，并停止之前可能正在运行的动画协程
        gameObject.SetActive(true);
        StopAllCoroutines();
        // 记录 Icon 的初始位置，以便动效可以重复正确播放
        if (startIcon != null)
        {
            originalIconAnchoredPos = startIcon.anchoredPosition;
        }
        StartCoroutine(AnimatePanelRoutine(totalDuration));
    }

    private IEnumerator AnimatePanelRoutine(float totalDuration)
    {
        // 策略分配时间：前 40% 时间做“放大和位移”，后 60% 时间做“渐变消失”
        float introDuration = totalDuration * 0.7f;
        float fadeDuration = totalDuration * 0.3f;

        // --- 阶段 1：初始化状态 ---
        canvasGroup.alpha = 1f;
        bgBlock.localScale = new Vector3(1f, 0.5f, 1f); // Y轴初始 Scale 为 0.5
        startIcon.anchoredPosition = originalIconAnchoredPos;

        // --- 阶段 2：色块变高 + Icon 横移 ---
        float elapsed = 0f;
        Vector3 initialScale = new Vector3(1f, 0.5f, 1f);
        Vector3 targetScale = new Vector3(1f, 1f, 1f);
        Vector2 iconTargetPos = originalIconAnchoredPos + new Vector2(iconMoveDistance, 0f);

        startIcon.anchoredPosition = originalIconAnchoredPos + new Vector2(-iconMoveDistance, 0f);// Icon 从左侧开始
        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / introDuration;
            
            // 使用 SmoothStep 让运动曲线更平滑（两头慢中间快）
            float tSmooth = Mathf.SmoothStep(0f, 1f, t);

            // 缩放与位移插值
            bgBlock.localScale = Vector3.Lerp(initialScale, targetScale, tSmooth);
            startIcon.anchoredPosition = Vector2.Lerp(originalIconAnchoredPos, iconTargetPos, tSmooth);
            
            yield return null; // 等待下一帧
        }
        
        // 确保第一阶段最终值精准到位
        bgBlock.localScale = targetScale;
        startIcon.anchoredPosition = iconTargetPos;

        // --- 阶段 3：面板整体渐变消失 ---
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            
            // CanvasGroup Alpha 渐变
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            
            yield return null;
        }

        // --- 阶段 4：动画结束 ---
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false); // 隐藏面板，释放 DrawCall
    }
}