using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

public partial class BattleManager
{
    private sealed class MoveSnapshot
    {
        internal PieceController Actor;
        internal int Life; // 初始化版本，隔离对象池复用。
        internal int AP; // 移动付款前的实际 AP。
        internal int PaidAP; // 本次付款后的预期 AP。
        internal int Frame; // 未提交导航的记录最多保留至当前帧。
        internal Vector3 Origin, Destination; // 付款前位置及实际到达位置。
        internal Quaternion Rotation; // 移动前朝向。
        internal ulong RandomVersion; // 发生规则抽样后禁止撤销。
        internal string World; // 只作一致性校验，不用于回滚世界。
        internal bool Started, Ready; // 导航已提交、纯移动已完成。
    }

    private MoveSnapshot moveUndo; // 本行动段唯一的移动记录；不可逆结果直接丢弃。

    /// <summary>正式扣费前记录移动原值；其他已提交动作立即清除旧撤销，拒绝扣费不调用。</summary>
    internal void RecordMovePayment(PieceController actor, ActionType action, int cost)
    {
        ClearMoveUndo();
        if (action != ActionType.Move || !CanControlPiece(actor)) return;
        moveUndo = new MoveSnapshot { Actor = actor, Life = actor.BattleLifeVersion,
            AP = actor.unitAttrCenter.CurMovePoint, PaidAP = actor.unitAttrCenter.CurMovePoint - cost,
            Origin = actor.transform.position, Rotation = actor.transform.rotation,
            RandomVersion = BattleRandom.DrawVersion, Frame = Time.frameCount };
        moveUndo.World = ReadMoveWorld(actor);
    }

    /// <summary>导航成功提交后关联终点；没有移动付款快照的 AI 移动不生成撤销。</summary>
    internal void StartUndoableMove(PieceController actor, Vector3 destination)
    {
        if (moveUndo == null || moveUndo.Actor != actor) return;
        moveUndo.Started = true;
        moveUndo.Destination = destination;
    }

    /// <summary>到达回调及警戒检查后确认纯移动；超时、偏离终点及状态变化不提供撤销。</summary>
    internal void CompleteUndoableMove(PieceController actor, bool reached)
    {
        var snapshot = moveUndo;
        if (snapshot == null || snapshot.Actor != actor) return;
        var nav = actor.GetComponent<NavMeshAgent>();
        if (!reached || !snapshot.Started || Vector3.Distance(actor.transform.position, snapshot.Destination) >
            Mathf.Max(0.3f, (nav != null ? nav.stoppingDistance : 0f) + 0.15f) || !MoveSnapshotCurrent())
        { ClearMoveUndo(); return; }
        snapshot.Destination = actor.transform.position;
        snapshot.Ready = true;
        UpdateMoveUndo();
    }

    /// <summary>回合、聚能选人、伤害、反应或其他动作提交时销毁唯一记录。</summary>
    public void ClearMoveUndo()
    {
        moveUndo = null;
        BattleScene.Ins?.UM?.ShowUndoMoveButton(false);
    }

    private bool MoveSnapshotCurrent()
    {
        var s = moveUndo;
        return s != null && inBattle && s.Actor != null && !s.Actor.isDead &&
            ActivePiece == s.Actor && !s.Actor.isIdle && s.Actor.BattleLifeVersion == s.Life &&
            s.Actor.unitAttrCenter.CurMovePoint == s.PaidAP && BattleRandom.DrawVersion == s.RandomVersion &&
            ReadMoveWorld(s.Actor) == s.World;
    }

    private void UpdateMoveUndo()
    {
        if (moveUndo == null) return;
        if (!MoveSnapshotCurrent() || (!moveUndo.Started && Time.frameCount > moveUndo.Frame))
        { ClearMoveUndo(); return; }
        if (moveUndo.Ready && Vector3.Distance(moveUndo.Actor.transform.position, moveUndo.Destination) > 0.05f)
        { ClearMoveUndo(); return; }
        BattleScene.Ins.UM.ShowUndoMoveButton(moveUndo.Ready && CanControlPiece(moveUndo.Actor));
    }

    /// <summary>只恢复最近一次纯移动的位置、朝向与实际 AP；不重放到达事件，不回滚世界或随机。</summary>
    public bool TryUndoMove()
    {
        if (moveUndo == null || !moveUndo.Ready || !CanControlPiece(moveUndo.Actor)) return false;
        if (!MoveSnapshotCurrent() || Vector3.Distance(moveUndo.Actor.transform.position, moveUndo.Destination) > 0.05f)
        { ClearMoveUndo(); return false; }
        var s = moveUndo;
        var nav = s.Actor.GetComponent<NavMeshAgent>();
        if (nav == null) { ClearMoveUndo(); return false; }
        var filter = new NavMeshQueryFilter { agentTypeID = nav.agentTypeID, areaMask = nav.areaMask };
        if (!NavMesh.SamplePosition(s.Origin, out var hit, 0.1f, filter) ||
            Vector3.Distance(hit.position, s.Origin) > 0.1f) { ClearMoveUndo(); return false; }
        foreach (var other in SpeedParticipants())
            if (other != s.Actor && Vector3.Distance(other.transform.position, s.Origin) <
                nav.radius + (other.GetComponent<NavMeshAgent>()?.radius ?? 0.5f))
            { ClearMoveUndo(); return false; }
        // 先消费快照，再同步导航；成功前不返还资源，也不调用 StopMove 的场景效果。
        BattleScene.Ins.CM.ClearTurnSelection();
        bool enabledBefore = nav.enabled;
        nav.enabled = true;
        bool restored = nav.Warp(hit.position);
        if (restored) { nav.ResetPath(); nav.velocity = Vector3.zero; }
        nav.enabled = enabledBefore;
        if (!restored) return false;
        s.Actor.transform.rotation = s.Rotation;
        s.Actor.unitAttrCenter.SetMovePoint(s.AP);
        s.Actor.RefreshAfterMoveUndo();
        if (!HasExternalPlayerControl) BattleScene.Ins.CM.SelectActivePiece(s.Actor);
        return true;
    }

    private string ReadMoveWorld(PieceController moving)
    {
        var text = new StringBuilder();
        var pieces = new List<PieceController>(PlayerController.pieces);
        pieces.AddRange(AIController.pieces);
        pieces.AddRange(summonPieces);
        foreach (var piece in pieces)
        {
            if (piece == null) { text.Append("null;"); continue; }
            var u = piece.unitAttrCenter;
            text.Append(piece.GetInstanceID()).Append(':').Append(piece.BattleLifeVersion).Append(':')
                .Append(piece.gameObject.activeInHierarchy).Append(':').Append(u.CurHealth).Append(':')
                .Append(u.MaxHealth).Append(':').Append(u.MaxMovePoint).Append(':')
                .Append(u.AmmoCount).Append(':').Append(u.ManaPoint).Append(':').Append(u.MoveRange);
            if (piece != moving) text.Append(':').Append(piece.transform.position.ToString("R")).Append(':').Append(u.CurMovePoint);
            var meme = piece.MemeState;
            text.Append(':').Append(u.MaxAmmoCount).Append(':').Append(u.MaxManaPoint).Append(':')
                .Append(meme.NetworkSourceCount).Append(':').Append(meme.Paralyzed).Append(':')
                .Append(meme.Violent).Append(':').Append(meme.Inverted).Append(':').Append(meme.AttackForbidden);
            foreach (var status in timedMemeStatuses.GetStatuses(piece))
                text.Append('|').Append(status.MemeId).Append(':').Append(status.Revision).Append(':')
                    .Append(status.RemainingTurns).Append(':').Append(status.Actor != null ? status.Actor.GetInstanceID() : 0);
            if (memeService.Catalog != null)
                foreach (var definition in memeService.Catalog.memes)
                    if (definition != null) text.Append('|').Append(definition.id).Append(':').Append(meme.GetResistance(definition.id));
            foreach (var buff in u.buffStates)
                if (buff != null) text.Append('|').Append(buff.buffType).Append(':').Append(buff.stacks)
                    .Append(':').Append(buff.barrierHealth).Append(':').Append(buff.retaliationTurns).Append(':').Append(buff.sources.Count);
            text.Append(';');
        }
        return text.ToString();
    }
}
