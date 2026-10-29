using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class BattleManager
{
    [SerializeField, Min(1)] private int maxReactionDepth = 8; // 每根反应的最大派生深度。
    [SerializeField, Min(1)] private int maxReactionEvents = 64; // 每根总事件及全局待处理事件的上限。
    private readonly Queue<ReactionRequest> reactions = new();
    private readonly HashSet<Tween> displacements = new(); // 本场拥有的击退，禁用与胜负时取消。
    private readonly Dictionary<PieceController, Tween> pieceDisplacements = new();
    private Coroutine reactionRoutine;
    private PieceController reactionActor; // 队列当前持有的施法者，中止时一并取消在途命中。
    private bool flankReactionExecuting;
    private int reactionVersion; // 中止后旧回调不得重新提交碰撞或反应。

    /// <summary>反应及击退尚未结束，回合、输入和镜头必须等待。</summary>
    public bool HasPendingBattleReactions => reactionRoutine != null || reactions.Count > 0 || displacements.Count > 0;

    private sealed class ReactionRequest
    {
        internal BattleActionContext Parent;
        internal Func<IEnumerator> Resolve;
    }

    private bool EnqueueReaction(string key, BattleActionContext parent, Func<IEnumerator> resolve)
    {
        if (!isActiveAndEnabled || !inBattle || LastOutcome != BattleOutcome.None) return false;
        parent ??= BattleActionContext.Current ?? new BattleActionContext();
        var chain = parent.ReactionChain;
        if (parent.ReactionDepth >= Mathf.Max(1, maxReactionDepth) ||
            chain.Count >= Mathf.Max(1, maxReactionEvents) || reactions.Count >= Mathf.Max(1, maxReactionEvents) ||
            !chain.Keys.Add(key)) return false;
        chain.Count++;
        reactions.Enqueue(new ReactionRequest { Parent = parent, Resolve = resolve });
        if (reactionRoutine == null) reactionRoutine = StartCoroutine(DrainReactions());
        return true;
    }

    private IEnumerator DrainReactions()
    {
        // 先归属协程句柄；等待原攻击与击退完成，但不等待包含自身的表现锁。
        yield return null;
        try
        {
            while (reactions.Count > 0)
            {
                while (HasPendingRecognitionSettlement || displacements.Count > 0 ||
                    HasPresentation(PlayerController.pieces) || HasPresentation(AIController.pieces) ||
                    HasPresentation(summonPieces)) yield return null;
                var request = reactions.Dequeue();
                var routine = request.Resolve();
                // 父上下文只在同步推进迭代器时恢复，不能跨帧占用全局状态。
                try
                {
                    while (true)
                    {
                        bool next;
                        using (request.Parent.Enter()) next = routine.MoveNext();
                        if (!next) break;
                        yield return routine.Current;
                    }
                }
                finally { (routine as IDisposable)?.Dispose(); }
            }
        }
        finally { reactionRoutine = null; flankReactionExecuting = false; }
    }

    private static bool CanReact(PieceController piece) => piece != null && piece.isActiveAndEnabled &&
        !piece.isDead && piece.pieceData != null && piece.unitAttrCenter != null && piece.unitAttrCenter.CurHealth > 0;

    /// <summary>排队保留穿越警戒的事实；出手时重查角色、阵营、指令与资源，保留现行反应开关。</summary>
    public bool QueueOrderReaction(UnitOrderState state, PieceController target, BattleActionContext parent)
    {
        if (state?.guard == null || target == null || !BattleRelations.CanAttack(state.guard, target)) return false;
        return EnqueueReaction($"order:{state.guard.GetInstanceID()}:{target.GetInstanceID()}", parent,
            () => ResolveOrderReaction(state, target));
    }

    private IEnumerator ResolveOrderReaction(UnitOrderState state, PieceController target)
    {
        try
        {
            if (!CanReact(state.guard) || !CanReact(target) || !BattleRelations.CanAttack(state.guard, target) ||
                (state.guard is EnemyController enemy && !enemy.isActived) ||
                !orderManager.IsOrderActive(state) || !BuffManager.CanTarget(state.guard, target)) yield break;
            bool ranged = state.profile.type == OrderType.Ranged;
            var skill = ranged ? state.guard.pieceData.RangedAttack : state.guard.pieceData.MeleeAttack;
            if (skill == null || (ranged && state.guard.unitAttrCenter.AmmoCount <= 0) ||
                !buffManager.OnAction(state.guard.unitAttrCenter) || !CanReact(state.guard)) yield break;
            reactionActor = state.guard;
            if (state.guard.isPlayerPiece) state.guard.CastNormalAttack(target, ranged, true);
            else
            {
                state.guard.StartNormalAttack(ranged);
                ((EnemyController)state.guard).CastAttackOnTarget(target, isMemeReaction: true, capturedOrderTarget: true);
            }
            if (!state.guard.IsPerformingAction) yield break;
            orderManager.ConsumeOrder(state);
            tipTextManager.ShowTip(state.guard.transform, "触发指令");
            while (state.guard != null && state.guard.IsPerformingAction) yield return null;
        }
        finally { state.reactionQueued = false; reactionActor = null; }
    }

    /// <summary>中止本场反应与击退，不补发取消动作的伤害；胜负、退出和禁用入口共用。</summary>
    private void CancelBattleReactions()
    {
        reactionVersion++;
        reactions.Clear();
        reactionActor?.CancelBattleReaction();
        reactionActor = null;
        if (reactionRoutine != null) StopCoroutine(reactionRoutine);
        reactionRoutine = null;
        flankReactionExecuting = false;
        foreach (var tween in new List<Tween>(displacements)) tween.Kill();
        displacements.Clear();
        pieceDisplacements.Clear();
        orderManager?.CancelMovementTracking();
        orderManager?.ClearAll(true);
        orderManager?.ClearAll(false);
    }
}
