using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public partial class BattleManager
{
    private sealed class BurstEvent
    {
        internal Component Owner;
        internal PieceController Piece;
        internal int Life;
        internal float Remaining;
        internal Action Resolve;
        internal BattleActionContext Action;
        internal BattleEffectLifetime Lifetime;
        internal int Lease;
        internal bool IsValid => Owner != null && Owner.gameObject.activeInHierarchy &&
            (!(Owner is Behaviour behaviour) || behaviour.enabled) && Resolve != null &&
            Lifetime != null && Lifetime.Version == Lease &&
            (Piece == null || Piece.BattleLifeVersion == Life);
    }

    private readonly Queue<BurstEvent> burstEvents = new(); // 按触发顺序释放，连锁追加到队尾。
    private readonly List<BurstEvent> worldTimers = new(); // 引信使用可暂停的战斗时间，不用全局Tween计时。
    private Coroutine burstRelease;
    private bool releasingBurst;
    private BurstEvent activeBurstEvent;
    private BattleOutcome pendingBurstOutcome;
    private readonly HashSet<Tween> deathFades = new(); // 死亡淡出也须结束后才提交胜负。
    /// <summary>延期效果正在释放；阻止输入、下一回合和提前胜负提交。</summary>
    public bool HasPendingBurstRelease => releasingBurst;
    /// <summary>爆炸、死亡触发及其连锁须排队；不包含已确认的行动自伤与状态计时。</summary>
    public bool BurstEffectsDeferred => PlayerController != null && PlayerController.isBursting || releasingBurst;
    public int PendingBurstEvents => burstEvents.Count;

    private BurstEvent MakeBurstEvent(Component owner, Action resolve, float delay, BattleActionContext action)
    {
        var piece = owner != null ? owner.GetComponentInParent<PieceController>() : null;
        var lifetime = owner.GetComponent<BattleEffectLifetime>();
        if (lifetime == null) lifetime = owner.gameObject.AddComponent<BattleEffectLifetime>();
        return new BurstEvent { Owner = owner, Piece = piece, Life = piece != null ? piece.BattleLifeVersion : 0,
            Lifetime = lifetime, Lease = lifetime.Version,
            Resolve = resolve, Remaining = Mathf.Max(0, delay), Action = action ?? BattleActionContext.Current ?? new BattleActionContext(piece) };
    }

    /// <summary>聚能或释放连锁期间接纳一次效果；返回false时由调用者立即执行。回调须是实际实现，不能再次调用排队入口。</summary>
    public bool TryDeferBurstEffect(Component owner, Action resolve, BattleActionContext action = null)
    {
        if (!BurstEffectsDeferred || owner == null || resolve == null) return false;
        burstEvents.Enqueue(MakeBurstEvent(owner, resolve, 0, action));
        return true;
    }

    /// <summary>安排爆炸物引信、脉冲及回收；聚能期间暂停剩余时间，释放期连锁排到队尾。</summary>
    public void ScheduleWorldEffect(Component owner, float delay, Action resolve, BattleActionContext action = null)
    {
        if (owner == null || resolve == null || !inBattle || !isActiveAndEnabled) return;
        var entry = MakeBurstEvent(owner, resolve, delay, action);
        if (BurstEffectsDeferred) burstEvents.Enqueue(entry);
        else worldTimers.Add(entry);
    }

    /// <summary>组件停用时撤销其引信和待爆效果；重新启用不能恢复旧租期的回调。</summary>
    public void CancelWorldEffects(Component owner)
    {
        foreach (var entry in burstEvents) if (entry.Owner == owner) entry.Resolve = null;
        foreach (var entry in worldTimers) if (entry.Owner == owner) entry.Resolve = null;
        if (activeBurstEvent != null && activeBurstEvent.Owner == owner) activeBurstEvent.Resolve = null;
    }

    private void Update()
    {
        if (BurstEffectsDeferred || !inBattle) return;
        var ready = new List<BurstEvent>();
        for (int i = 0; i < worldTimers.Count;)
        {
            var entry = worldTimers[i];
            if (!entry.IsValid) { worldTimers.RemoveAt(i); continue; }
            entry.Remaining -= Time.deltaTime;
            if (entry.Remaining > 0) { i++; continue; }
            worldTimers.RemoveAt(i);
            ready.Add(entry);
        }
        foreach (var entry in ready)
        {
            if (!inBattle) break;
            if (!entry.IsValid) continue;
            if (BurstEffectsDeferred) burstEvents.Enqueue(entry);
            else { using (entry.Action.Enter()) entry.Resolve(); }
        }
    }

    /// <summary>支付成功后冻结已存在引信及爆炸物位置，不修改全局时间倍率。</summary>
    public void FreezeBurstWorld()
    {
        foreach (var entry in worldTimers) burstEvents.Enqueue(entry);
        worldTimers.Clear();
        FreezeExplosiveBodies();
    }

    /// <summary>退出聚能后按触发顺序释放；普通调度必须等待HasPendingBurstRelease变为false。</summary>
    public void ReleaseBurstEffects()
    {
        if (releasingBurst) return;
        RestoreExplosiveBodies();
        if (burstEvents.Count == 0) { CompleteBurstOutcome(); return; }
        releasingBurst = true;
        burstRelease = StartCoroutine(DrainBurstEffects());
    }

    private IEnumerator DrainBurstEffects()
    {
        yield return null;
        bool completed = false;
        try
        {
            while (burstEvents.Count > 0 && inBattle)
            {
                var entry = burstEvents.Dequeue();
                activeBurstEvent = entry;
                while (entry.IsValid && entry.Remaining > 0) { entry.Remaining -= Time.deltaTime; yield return null; }
                if (!entry.IsValid) continue;
                using (entry.Action.Enter()) entry.Resolve();
                // 不等待包含本队列的HasCombatPresentation，避免释放队列等待自身。
                while (inBattle && (HasActionPresentation || moveManager.IsMoving || cameraController.IsPresentingHit)) yield return null;
            }
            completed = true;
        }
        finally
        {
            activeBurstEvent = null;
            releasingBurst = false;
            burstRelease = null;
            if (!completed) { burstEvents.Clear(); worldTimers.Clear(); pendingBurstOutcome = BattleOutcome.None; }
        }
        if (inBattle) CompleteBurstOutcome();
    }

    private void CompleteBurstOutcome()
    {
        var outcome = pendingBurstOutcome;
        pendingBurstOutcome = BattleOutcome.None;
        if (!inBattle) return;
        if (outcome == BattleOutcome.PlayerVictory) PlayerWin();
        else if (outcome == BattleOutcome.PlayerDefeat) PlayerLoss();
        else PlayerCheckWin();
    }

    private bool DeferBurstOutcome(BattleOutcome outcome)
    {
        if (!BurstEffectsDeferred) return false;
        pendingBurstOutcome = outcome;
        cancelBurst = true;
        return true;
    }

    /// <summary>死亡回收排在已触发的爆炸之后，并将淡出纳入表现等待与退出清理。</summary>
    public void FadeDeadPiece(PieceController piece)
    {
        if (piece == null) return;
        Action fade = () =>
        {
            var renderer = piece.pieceDisplay?.pieceSpriteRenderer;
            if (renderer == null) { piece.gameObject.SetActive(false); return; }
            Tween tween = null;
            tween = renderer.DOFade(0f, 0.8f).OnComplete(() =>
            {
                deathFades.Remove(tween);
                if (piece != null) piece.gameObject.SetActive(false);
                PlayerCheckWin();
            }).OnKill(() => deathFades.Remove(tween));
            deathFades.Add(tween);
        };
        if (!TryDeferBurstEffect(piece, fade)) fade();
    }

    private void ClearBurstEffects()
    {
        var routine = burstRelease;
        burstRelease = null;
        if (routine != null) StopCoroutine(routine);
        releasingBurst = false;
        burstEvents.Clear();
        activeBurstEvent = null;
        worldTimers.Clear();
        pendingBurstOutcome = BattleOutcome.None;
        foreach (var tween in new List<Tween>(deathFades)) tween.Kill();
        deathFades.Clear();
        RestoreExplosiveBodies();
        foreach (var display in FindObjectsOfType<PieceDisplay>()) display.CancelBurstFreeze();
    }
}
