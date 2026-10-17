using System;
using System.Collections.Generic;

/// <summary>离散速度时钟。满条后暂停时钟，按到达时刻依次行动，保留溢出进度。</summary>
public sealed class SpeedTurnQueue<T> where T : class
{
    private sealed class Entry
    {
        public T Unit;
        public double Progress; // 可超过 100，行动后保留溢出值。
        public double Arrival; // 推算的满条时刻，用于区分同一 tick 内的先后。
        public double Speed; // 最近一次 tick 的有效速度。
        public long Order; // 首次注册次序，用于满条时刻相同的稳定排序。
    }

    private readonly List<Entry> entries = new();
    private long nextOrder;
    public int Ticks { get; private set; } // 已推进的离散时间步数，不等于棋子行动次数。
    /// <summary>清空参战者、进度及时间，供新战斗初始化使用。</summary>
    public void Clear() { entries.Clear(); nextOrder = 0; Ticks = 0; }
    /// <summary>读取棋子累计进度；尚未注册时返回 0，已满条时可能超过 100。</summary>
    public double Progress(T unit) => entries.Find(e => ReferenceEquals(e.Unit, unit))?.Progress ?? 0;

    /// <summary>同步有效参战者，保留已有进度，新加入者从 0 开始。</summary>
    public void Sync(IEnumerable<T> units)
    {
        var live = new HashSet<T>(units);
        entries.RemoveAll(e => !live.Contains(e.Unit));
        foreach (var unit in units)
            if (!entries.Exists(e => ReferenceEquals(e.Unit, unit)))
                entries.Add(new Entry { Unit = unit, Order = nextOrder++ });
    }

    /// <summary>为所有棋子累加一次速度；调用方仅在无人待行动时推进，非法速度回退为 10。</summary>
    public void Tick(Func<T, double> speed)
    {
        foreach (var entry in entries)
        {
            double value = speed(entry.Unit);
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) value = 10;
            entry.Speed = value;
            // 用连续满条时刻排序，避免同一 tick 越过阈值时只按注册顺序行动。
            entry.Arrival = Ticks + (100 - entry.Progress) / value;
            entry.Progress += value;
        }
        Ticks++;
    }

    /// <summary>取出最早满条的棋子并扣除 100 进度；同刻按注册次序，无就绪单位时返回 null。</summary>
    public T TakeReady()
    {
        Entry first = null;
        foreach (var entry in entries)
            if (entry.Progress >= 100 && (first == null || entry.Arrival < first.Arrival ||
                (entry.Arrival == first.Arrival && entry.Order < first.Order))) first = entry;
        if (first == null) return null;
        // 只消费一条进度，高速棋子的溢出仍参与下一次就绪判断。
        first.Progress -= 100;
        first.Arrival += 100 / first.Speed;
        return first.Unit;
    }
}
