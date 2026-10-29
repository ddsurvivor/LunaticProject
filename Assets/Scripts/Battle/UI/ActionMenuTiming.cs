using System;

/// <summary>计算单次菜单展开的等间隔时间轴，末行完整动画包含在总时长内。</summary>
public static class ActionMenuTiming
{
    /// <summary>返回同批相邻行的固定开始间隔，保证末行动画包含在总预算内。</summary>
    public static float Interval(int count, float duration, float maximumInterval, float maximumTime)
    {
        if (count <= 1) return 0;
        duration = Math.Max(0, Math.Min(duration, maximumTime));
        return Math.Max(0, Math.Min(maximumInterval, (maximumTime - duration) / (count - 1)));
    }
}
