using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public partial class PieceController
{
    /// <summary>训练关稳定回合边界快照；保留原棋子对象，不重新Init或触发生命事件。</summary>
    internal Action CaptureTurnState()
    {
        var restoreAttributes = unitAttrCenter.CaptureTurnState();
        var restoreMeme = MemeState.CaptureTurnState();
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        bool active = gameObject.activeSelf, faction = isPlayerPiece, idle = isIdle;
        bool control = cantControl, move = ableMove, strike = ableStrick, guarding = isGrauding;
        var cover = _curCaverSlot;
        var ladder = _curLadderArea;
        var areas = interactAreas.ToArray();
        var actions = availableActions.ToArray();
        var navigation = GetComponent<NavMeshAgent>();
        bool navigationEnabled = navigation != null && navigation.enabled;
        bool navigationStopped = navigation != null && navigation.isActiveAndEnabled && navigation.isOnNavMesh && navigation.isStopped;
        var renderers = GetComponentsInChildren<Renderer>(true).Select(r => (r, r.enabled)).ToArray();
        var sprites = GetComponentsInChildren<SpriteRenderer>(true).Select(r => (r, r.color, r.flipX)).ToArray();
        var colliders = GetComponentsInChildren<Collider>(true).Select(c => (c, c.enabled)).ToArray();
        bool enemyActive = this is EnemyController enemy && enemy.isActived;
        var damage = this is EnemyController e ? new System.Collections.Generic.Dictionary<PieceController, int>(e.damageDic) : null;
        string playerJson = playerData != null ? Newtonsoft.Json.JsonConvert.SerializeObject(playerData) : null;
        return () =>
        {
            StopAllCoroutines(); CancelCombatAction(); StopHitShake(); StopMovementAudio();
            _isAttacking = _isUsingSkill = false; isUsingOrder = false; burstDeathQueued = false;
            gameObject.SetActive(active);
            restoreAttributes(); restoreMeme();
            isPlayerPiece = faction; isIdle = idle; cantControl = control; ableMove = move; ableStrick = strike; isGrauding = guarding;
            _curCaverSlot = cover; _curLadderArea = ladder;
            interactAreas.Clear(); interactAreas.AddRange(areas);
            availableActions = actions.ToList();
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            transform.position = position;
            if (agent != null)
            {
                agent.enabled = navigationEnabled;
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                { agent.ResetPath(); agent.Warp(position); agent.isStopped = navigationStopped; }
            }
            transform.rotation = rotation;
            if (this is EnemyController restoredEnemy)
            { restoredEnemy.isActived = enemyActive; restoredEnemy.damageDic = new System.Collections.Generic.Dictionary<PieceController, int>(damage); }
            if (playerJson != null && playerData != null)
            {
                Newtonsoft.Json.JsonConvert.PopulateObject(playerJson, playerData, new Newtonsoft.Json.JsonSerializerSettings { ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace });
                skillLoadout = null; RefreshConfiguredSkills();
            }
            pieceDisplay?.StopAnimation();
            pieceDisplay?.ChangeDisplayState(isDead ? PieceDisplayState.Death : PieceDisplayState.Idle);
            foreach (var entry in renderers) if (entry.r != null) entry.r.enabled = entry.enabled;
            foreach (var entry in sprites) if (entry.r != null) { entry.r.color = entry.color; entry.r.flipX = entry.flipX; }
            foreach (var entry in colliders) if (entry.c != null) entry.c.enabled = entry.enabled;
        };
    }
}
