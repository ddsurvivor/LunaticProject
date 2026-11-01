using UnityEngine;
using System.Collections.Generic;

public class TimeManager : MonoBehaviour
{
    [Header("命中停顿默认设置")]
    [SerializeField] private float defaultTimeScale = 0.1f;
    [SerializeField] private float defaultDuration = 0.1f;

    private float originalFixedDeltaTime;
    private const float normalTimeScale = 1f;
    private bool fastForward; // 仅属于本场战斗，不写入玩家设置。
    private float hitScale = 1f;
    private float remainingDuration;
    private readonly HashSet<UnityEngine.Object> pauseOwners = new HashSet<UnityEngine.Object>(); // 每个窗口独立持有暂停。
    private bool manualPause;
    /// <summary>包含显式暂停和所有窗口持有者的合并暂停状态。</summary>
    public bool IsPaused => manualPause || pauseOwners.Count > 0;
    public bool IsHitStopped => remainingDuration > 0f;

    /// <summary>战斗快进开关；暂停与命中停顿始终优先于3倍速。</summary>
    public bool IsFastForward => fastForward;
    private float BattleTimeScale => fastForward ? 3f : normalTimeScale;
    private float EffectiveTimeScale => IsPaused ? 0f : IsHitStopped ? hitScale : BattleTimeScale;

    /// <summary>切换本场战斗的1倍/3倍时间；保留正在持有的暂停和命中停顿。</summary>
    public void SetFastForward(bool enabled)
    {
        fastForward = enabled;
        ApplyTimeScale(EffectiveTimeScale);
    }

    private void Awake()
    {
        originalFixedDeltaTime = Time.fixedDeltaTime;
        ApplyTimeScale(normalTimeScale);
    }

    /// <summary>命中停顿使用真实秒数，暂停时冻结余量；叠加请求不缩短已有停顿，也不乘快进倍率。</summary>
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
        bool wasPaused = IsPaused;
        pauseOwners.RemoveWhere(owner => owner == null);
        if (wasPaused && !IsPaused) ApplyTimeScale(EffectiveTimeScale);
        if (IsPaused || !IsHitStopped) return;
        remainingDuration = Mathf.Max(0f, remainingDuration - Time.unscaledDeltaTime);
        if (!IsHitStopped) ApplyTimeScale(EffectiveTimeScale);
    }

    /// <summary>兼容显式暂停；窗口应使用独立持有者接口，避免相互解除暂停。</summary>
    public void PauseTime() { manualPause = true; ApplyTimeScale(0f); }

    /// <summary>仅解除显式暂停，不解除其他窗口持有的暂停。</summary>
    public void ResumeTime() { manualPause = false; ApplyTimeScale(EffectiveTimeScale); }

    /// <summary>窗口独占持有暂停；重复申请同一持有者不叠加。</summary>
    public void AcquirePause(UnityEngine.Object owner)
    {
        if (owner == null) return;
        pauseOwners.Add(owner);
        ApplyTimeScale(0f);
    }

    /// <summary>窗口关闭只释放自己的暂停，保留命中停顿及其他窗口状态。</summary>
    public void ReleasePause(UnityEngine.Object owner)
    {
        pauseOwners.Remove(owner);
        ApplyTimeScale(EffectiveTimeScale);
    }

    /// <summary>离开本场景前清理全部暂停、命中停顿和快进，恢复1倍时间及物理步长。</summary>
    public void ResetTime()
    {
        pauseOwners.Clear();
        manualPause = false;
        remainingDuration = 0f;
        fastForward = false;
        if (originalFixedDeltaTime > 0f) ApplyTimeScale(normalTimeScale);
    }

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        // timeScale=0 已暂停物理，不把物理步长设为无效的零。
        Time.fixedDeltaTime = scale > 0f
            ? originalFixedDeltaTime * scale / normalTimeScale : originalFixedDeltaTime;
    }

    private void OnDisable() { ResetTime(); }
}
