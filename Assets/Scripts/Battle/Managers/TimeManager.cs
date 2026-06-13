using UnityEngine;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    [Header("全局默认设置")]
    [SerializeField] private float defaultTimeScale = 0.1f;
    [SerializeField] private float defaultDuration = 0.1f;

    private Coroutine _recoveryCoroutine;
    private float _originalFixedDeltaTime;

    // 暂停状态
    private bool _isPaused = false;
    private float _pausedTimeScale = 1.0f;       // 暂停前的 timeScale（可能是减缓中的值）
    private float _remainingDuration = 0f;        // 减缓剩余时间

    private void Awake()
    {
        _originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    /// <summary>
    /// 请求受击停顿
    /// </summary>
    public void RequestHitStop(float? customScale = null, float? customDuration = null)
    {
        float targetScale = customScale ?? defaultTimeScale;
        float targetDuration = customDuration ?? defaultDuration;

        // 如果当前处于暂停状态，覆盖暂存的减缓参数，等恢复后生效
        if (_isPaused)
        {
            _pausedTimeScale = targetScale;
            _remainingDuration = targetDuration;
            return;
        }

        // 停止已有恢复协程，实现"刷新计时"
        if (_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
        }

        ApplyTimeScale(targetScale);
        _recoveryCoroutine = StartCoroutine(ResetTimeScale(targetDuration));
    }

    /// <summary>
    /// 暂停时间（覆盖当前减缓效果，记录状态）
    /// </summary>
    public void PauseTime()
    {
        if (_isPaused) return;

        _isPaused = true;

        // 保存暂停前的 timeScale（减缓中则保存减缓值，否则保存 1.0）
        _pausedTimeScale = (_recoveryCoroutine != null) ? Time.timeScale : 1.0f;

        // 如果减缓协程正在运行，停止它并记录剩余时间
        if (_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
            _recoveryCoroutine = null;
            // 剩余时间由协程自己记录，见 ResetTimeScale
        }

        ApplyTimeScale(0f);
    }

    /// <summary>
    /// 恢复时间（若之前有减缓效果则继续执行剩余时间）
    /// </summary>
    public void ResumeTime()
    {
        if (!_isPaused) return;

        _isPaused = false;

        if (_remainingDuration > 0f)
        {
            // 继续执行剩余的减缓
            ApplyTimeScale(_pausedTimeScale);
            _recoveryCoroutine = StartCoroutine(ResetTimeScale(_remainingDuration));
            _remainingDuration = 0f;
        }
        else
        {
            // 没有待续的减缓，直接恢复正常
            ApplyTimeScale(1.0f);
            Time.fixedDeltaTime = _originalFixedDeltaTime;
        }
    }

    // ─── 私有辅助 ────────────────────────────────────────────

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        // timeScale 为 0 时 fixedDeltaTime 也置 0，避免物理异常
        Time.fixedDeltaTime = (scale == 0f) ? 0f : _originalFixedDeltaTime * scale;
    }

    private IEnumerator ResetTimeScale(float delay)
    {
        float elapsed = 0f;

        // 逐帧累加真实时间，以便随时读取剩余量
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;

            // 如果在等待中途被 PauseTime() 打断，记录剩余时间后退出
            if (_isPaused)
            {
                _remainingDuration = delay - elapsed;
                _recoveryCoroutine = null;
                yield break;
            }

            yield return null;
        }

        // 正常结束：恢复原始时间
        ApplyTimeScale(1.0f);
        Time.fixedDeltaTime = _originalFixedDeltaTime;
        _recoveryCoroutine = null;
    }
}