using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("全局默认设置")]
    [SerializeField] private float defaultTimeScale = 0.1f;
    [SerializeField] private float defaultDuration = 0.1f;

    private float originalFixedDeltaTime;
    private float normalTimeScale;
    private float hitScale = 1f;
    private float remainingDuration;
    public bool IsPaused { get; private set; }
    public bool IsHitStopped => remainingDuration > 0f;

    private void Awake()
    {
        originalFixedDeltaTime = Time.fixedDeltaTime;
        normalTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
    }

    // 减速用真实时间计时，显式暂停则冻结剩余时间。叠加命中不缩短已有停顿。
    public void RequestHitStop(float? customScale = null, float? customDuration = null)
    {
        float duration = Mathf.Max(0f, customDuration ?? defaultDuration);
        if (duration <= 0f) return;
        float scale = Mathf.Clamp(customScale ?? defaultTimeScale, 0f, normalTimeScale);
        hitScale = IsHitStopped ? Mathf.Min(hitScale, scale) : scale;
        remainingDuration = Mathf.Max(remainingDuration, duration);
        ApplyTimeScale(IsPaused ? 0f : hitScale);
    }

    private void Update()
    {
        if (IsPaused || !IsHitStopped) return;
        remainingDuration = Mathf.Max(0f, remainingDuration - Time.unscaledDeltaTime);
        if (!IsHitStopped) ApplyTimeScale(normalTimeScale);
    }

    public void PauseTime()
    {
        if (IsPaused) return;
        IsPaused = true;
        ApplyTimeScale(0f);
    }

    public void ResumeTime()
    {
        if (!IsPaused) return;
        IsPaused = false;
        ApplyTimeScale(IsHitStopped ? hitScale : normalTimeScale);
    }

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        // timeScale=0 已暂停物理，不把物理步长设为无效的零。
        Time.fixedDeltaTime = scale > 0f
            ? originalFixedDeltaTime * scale / normalTimeScale : originalFixedDeltaTime;
    }

    private void OnDisable()
    {
        remainingDuration = 0f;
        IsPaused = false;
        if (originalFixedDeltaTime > 0f) ApplyTimeScale(normalTimeScale);
    }
}
