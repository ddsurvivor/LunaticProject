using UnityEngine;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    [Header("全局默认设置")]
    [SerializeField] private float defaultTimeScale = 0.1f;
    [SerializeField] private float defaultDuration = 0.1f;

    private Coroutine _recoveryCoroutine;
    private float _originalFixedDeltaTime;

    private void Awake()
    {
        // 记录原始物理步长，用于后续按比例还原
        _originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    /// <summary>
    /// 请求受击停顿
    /// </summary>
    /// <param name="customScale">可选：自定义缩放倍率</param>
    /// <param name="customDuration">可选：自定义持续时间</param>
    public void RequestHitStop(float? customScale = null, float? customDuration = null)
    {
        float targetScale = customScale ?? defaultTimeScale;
        float targetDuration = customDuration ?? defaultDuration;

        // 如果已有恢复协程，直接停止，实现“刷新计时”而非叠加
        if (_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
        }

        // 应用时间缩放
        Time.timeScale = targetScale;
        
        // 必须调整 fixedDeltaTime，否则物理模拟（如某些位移）会产生明显的卡顿感
        Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;

        // 开启恢复协程
        _recoveryCoroutine = StartCoroutine(ResetTimeScale(targetDuration));
    }

    private IEnumerator ResetTimeScale(float delay)
    {
        // 必须使用 Realtime，因为 timeScale 变小时，普通的 WaitForSeconds 会被拉长
        yield return new WaitForSecondsRealtime(delay);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = _originalFixedDeltaTime;
        _recoveryCoroutine = null;
    }
}