using System;
using System.Collections.Generic;

/// <summary>离散速度时钟。满条后暂停时钟，按到达时刻依次行动，保留溢出进度。</summary>
public sealed class SpeedTurnQueue<T> where T : class
{
    private sealed class Entry
    {
        public T Unit;
        public double Progress;
        public double Arrival;
        public double Speed;
        public long Order;
    }

    private readonly List<Entry> entries = new();
    private long nextOrder;
    public int Ticks { get; private set; }
    public void Clear() { entries.Clear(); nextOrder = 0; Ticks = 0; }
    public double Progress(T unit) => entries.Find(e => ReferenceEquals(e.Unit, unit))?.Progress ?? 0;

    public void Sync(IEnumerable<T> units)
    {
        var live = new HashSet<T>(units);
        entries.RemoveAll(e => !live.Contains(e.Unit));
        foreach (var unit in units)
            if (!entries.Exists(e => ReferenceEquals(e.Unit, unit)))
                entries.Add(new Entry { Unit = unit, Order = nextOrder++ });
    }

    public void Tick(Func<T, double> speed)
    {
        foreach (var entry in entries)
        {
            double value = speed(entry.Unit);
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) value = 10;
            entry.Speed = value;
            entry.Arrival = Ticks + (100 - entry.Progress) / value;
            entry.Progress += value;
        }
        Ticks++;
    }

    public T TakeReady()
    {
        Entry first = null;
        foreach (var entry in entries)
            if (entry.Progress >= 100 && (first == null || entry.Arrival < first.Arrival ||
                (entry.Arrival == first.Arrival && entry.Order < first.Order))) first = entry;
        if (first == null) return null;
        first.Progress -= 100;
        first.Arrival += 100 / first.Speed;
        return first.Unit;
    }
}
