using UnityEngine;

/// <summary>窗口按自身生命周期持有暂停，不会恢复其他窗口或新场景的时间。</summary>
public class ActivePause : MonoBehaviour
{
    private TimeManager pauseClock;
    private void OnEnable() { Acquire(); }
    private void Update() { if (pauseClock == null) Acquire(); }
    private void Acquire()
    {
        pauseClock = BattleScene.Ins != null ? BattleScene.Ins.TM : null;
        if (pauseClock != null) pauseClock.AcquirePause(this);
    }
    private void OnDisable()
    {
        if (pauseClock != null) pauseClock.ReleasePause(this);
        pauseClock = null;
    }
}
