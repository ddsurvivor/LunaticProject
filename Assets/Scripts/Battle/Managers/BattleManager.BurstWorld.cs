using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class BattleManager
{
    private sealed class ExplosiveBody
    {
        internal Transform Transform;
        internal Vector3 Position, Velocity, AngularVelocity;
        internal Quaternion Rotation;
        internal Rigidbody Body;
        internal bool Kinematic, AgentStopped, AgentCaptured;
        internal NavMeshAgent Agent;
        internal Animator[] Animators;
        internal float[] AnimatorSpeeds;
        internal readonly List<ParticleSystem> PlayingParticles = new();
        internal PieceController Piece;
        internal int Life;
        internal bool IsCurrent => Transform != null && (Piece == null || Piece.BattleLifeVersion == Life);
    }
    private readonly Dictionary<Transform, ExplosiveBody> explosiveBodies = new(); // 本次冻结拥有的物理及动画快照。

    private void FreezeExplosiveBodies()
    {
        foreach (var target in EnemyExplosiveTargets.Active)
            if (target.Piece != null) FreezeExplosiveBody(target.Piece.transform);
        foreach (var summon in FindObjectsOfType<SelfExploreSummonPiece>()) FreezeExplosiveBody(summon.transform);
    }

    private void FreezeExplosiveBody(Transform root)
    {
        if (root == null || explosiveBodies.ContainsKey(root)) return;
        var state = new ExplosiveBody { Transform = root, Position = root.position, Rotation = root.rotation,
            Body = root.GetComponent<Rigidbody>(), Agent = root.GetComponent<NavMeshAgent>(),
            Piece = root.GetComponent<PieceController>(), Animators = root.GetComponentsInChildren<Animator>(true) };
        state.Life = state.Piece != null ? state.Piece.BattleLifeVersion : 0;
        if (state.Body != null)
        {
            state.Kinematic = state.Body.isKinematic;
            state.Velocity = state.Body.velocity;
            state.AngularVelocity = state.Body.angularVelocity;
            state.Body.isKinematic = true;
        }
        if (state.Agent != null && state.Agent.enabled && state.Agent.isOnNavMesh)
        { state.AgentCaptured = true; state.AgentStopped = state.Agent.isStopped; state.Agent.isStopped = true; }
        state.AnimatorSpeeds = new float[state.Animators.Length];
        for (int i = 0; i < state.Animators.Length; i++)
        { state.AnimatorSpeeds[i] = state.Animators[i].speed; state.Animators[i].speed = 0; }
        foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
            if (particle.isPlaying) { state.PlayingParticles.Add(particle); particle.Pause(false); }
        explosiveBodies.Add(root, state);
    }

    /// <summary>爆炸物停用或棋子重建前归还物理快照，下一次启用按新位置登记。</summary>
    public void ReleaseExplosiveBody(Transform root)
    {
        if (root == null || !explosiveBodies.TryGetValue(root, out var state)) return;
        RestoreExplosiveBody(state);
        explosiveBodies.Remove(root);
    }

    private void RestoreExplosiveBody(ExplosiveBody state)
    {
        if (!state.IsCurrent) return;
        if (state.Body != null)
        {
            state.Body.isKinematic = state.Kinematic;
            if (!state.Kinematic) { state.Body.velocity = state.Velocity; state.Body.angularVelocity = state.AngularVelocity; }
        }
        if (state.AgentCaptured && state.Agent != null)
        {
            var lifetime = state.Transform.GetComponent<BattleEffectLifetime>();
            if (lifetime == null) lifetime = state.Transform.gameObject.AddComponent<BattleEffectLifetime>();
            lifetime.RestoreAgent(state.Agent, state.AgentStopped);
        }
        for (int i = 0; i < state.Animators.Length; i++)
            if (state.Animators[i] != null) state.Animators[i].speed = state.AnimatorSpeeds[i];
        foreach (var particle in state.PlayingParticles) if (particle != null) particle.Play(false);
    }

    private void RestoreExplosiveBodies()
    {
        foreach (var state in explosiveBodies.Values) RestoreExplosiveBody(state);
        explosiveBodies.Clear();
    }

    private void LateUpdate()
    {
        if (PlayerController != null && PlayerController.isBursting)
        {
            FreezeExplosiveBodies();
            foreach (var state in explosiveBodies.Values)
                if (state.IsCurrent)
                {
                    state.Transform.SetPositionAndRotation(state.Position, state.Rotation);
                    if (state.Body != null) { state.Body.position = state.Position; state.Body.rotation = state.Rotation; }
                    if (state.Agent != null && state.Agent.enabled && state.Agent.isOnNavMesh) state.Agent.nextPosition = state.Position;
                }
        }
        UpdateBurstVisuals();
        UpdateMoveUndo();
    }
}
