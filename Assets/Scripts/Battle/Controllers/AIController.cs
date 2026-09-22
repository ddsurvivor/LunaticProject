using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 关卡ai敌人总管理器
/// </summary>
public class AIController : PlayerController
{
    [Header("敌人解锁顺序")]
    /// <summary>
    /// 战争迷雾管理
    /// </summary>
    public FogController fogController;

    // 增援波次管理
    public WaveController waveController;

    // 地图寻路管理器
    public MapController mapController;


    private float _timer;
    private float _actionInterval = 2.0f; // 每个动作之间的间隔时间

    [SerializeField] private bool realTimeMode;
    private int _lastActedIndex = -1;
    private float _realTimeInterval = 0.8f;
    [SerializeField]
    private RealtimeActionManager RAM;

    public void OnScanFog(GameObject fog)
    {
        if (fogController == null)
        {
            return;
        }

        if (fogController.enemyPiecesDict.ContainsKey(fog))
        {
            foreach (var piece in fogController.enemyPiecesDict[fog])
            {
                piece.isActived = true;
                piece.gameObject.SetActive(true);
                piece.StartNormalAttack(true);
            }
        }
    }

    public override void TurnStart()
    {
        pendingAttacks.Clear();
        retreatAfterShot.Clear();
        previousPositions.Clear();
        _timer = 0f;
        base.TurnStart();

        if (fogController != null)
        {
            // 激活迷雾敌人棋子
            foreach (var pair in fogController.enemyPiecesDict)
            {
                if (!pair.Key.gameObject.activeInHierarchy)
                {
                    foreach (var piece in pair.Value)
                    {
                        if (!piece.isDead)
                        {
                            piece.isActived = true;
                            piece.gameObject.SetActive(true);
                            piece.StartNormalAttack(true);
                        }
                    }
                }
            }
        }

        if (waveController != null)
        {
            // 刷新增援波次敌人棋子
            waveController.RefreshEnemies(BattleScene.Ins.BM.TunrNumber);
        }

        foreach (EnemyController piece in pieces)
        {
            if (piece.isActived && !piece.isDead)
            {
                piece.StartNormalAttack(true);
                // 每回合清空仇恨值
                // piece.damageDic = new();
            }
        }
    }

    public void Update()
    {
        if (isInTurn && !BattleScene.Ins.BM.moveManager.IsMoving)
        {
            if (realTimeMode)
            {
                if (_timer < _realTimeInterval)
                {
                    _timer += Time.deltaTime;
                    return;
                }

                //EnemyAction_AttackRandom();
                //EnemyAction_AttackNearestTarget();
                if (TryExecuteEnemyAction())
                    _timer = 0f;
            }
            else
            {
                if (_timer < _actionInterval)
                {
                    _timer += Time.deltaTime;
                    return;
                }
                else
                {
                    //EnemyAction_AttackRandom();
                    //EnemyAction_AttackNearestTarget();
                    EnemyAction_Calculate();
                    _timer = 0f;
                }
            }
        }
    }


    private sealed class AttackPlan
    {
        public PieceController Target;
        public SkillPack Skill;
        public ActionType Action;
    }

    private struct MoveCandidate
    {
        public Vector3 Position;
        public float PathLength;
        public float Score;
    }

    private readonly Dictionary<EnemyController, AttackPlan> pendingAttacks = new();
    private readonly HashSet<EnemyController> retreatAfterShot = new();
    private readonly Dictionary<EnemyController, Vector3> previousPositions = new();
    private bool actionAttempted;
    private const int PositionSamples = 24;
    private const float MinimumMove = 0.4f;

    /// <summary>每次只执行一个动作；移动中的棋子完成导航后再继续攻击计划。</summary>
    private bool EnemyActionSelect(EnemyController aiPiece, PieceController target)
    {
        actionAttempted = false;
        if (aiPiece == null || aiPiece.isDead || !aiPiece.isActived ||
            aiPiece.unitAttrCenter.GetBuffStacks(BuffType.Stun) != 0) return false;

        // 1. 保留关卡专用撤退和跨层梯子逻辑。
        if (aiPiece.enemyAIType == EnemyAIType.Special)
        {
            var retreat = aiPiece.GetComponent<RetreatWin>();
            if (retreat == null || retreat.targetPoint == null) return false;
            if (TryApproach(aiPiece, retreat.targetPoint.position, 0.5f))
            {
                DOVirtual.DelayedCall(1f, () => { if (retreat != null) retreat.CheckTargetReached(); });
                return true;
            }
            retreat.CheckTargetReached();
            return actionAttempted;
        }
        if (!IsValidTarget(aiPiece, target)) return false;
        if (aiPiece.navigate && Mathf.Abs(target.transform.position.y - aiPiece.transform.position.y) > 1f)
            return EnemyMoveToLadder(aiPiece);

        // 2. 优先完成已承诺的“移动 + 攻击”，避免每移动一步就切换模式。
        if (pendingAttacks.TryGetValue(aiPiece, out var pending))
        {
            pendingAttacks.Remove(aiPiece);
            if (IsValidTarget(aiPiece, pending.Target) &&
                (TryAttackPlan(aiPiece, pending) || actionAttempted)) return true;
        }

        // 3. 射击型先开火，再寻找安全的侧向/斜向撤离点。
        if (aiPiece.enemyAIType == EnemyAIType.Shoot)
        {
            if (retreatAfterShot.Remove(aiPiece) &&
                (TryRetreat(aiPiece, target.transform.position, aiPiece.GetRange(false)) || actionAttempted))
                return true;
            if (TryAttackPlan(aiPiece, NormalPlan(aiPiece, target, true)) || actionAttempted) return true;
            return TryApproach(aiPiece, target.transform.position, aiPiece.GetRange(false));
        }

        // 4. 技能型优先本回合可施放的技能；无可用技能时按混合型处理。
        if (aiPiece.enemyAIType == EnemyAIType.SkillUser &&
            (TrySkillPlan(aiPiece, target) || actionAttempted)) return true;

        // 5. 混合型优先本回合能完成的近战，再考虑射击，最后接近目标。
        if (TryAttackPlan(aiPiece, NormalPlan(aiPiece, target, false)) || actionAttempted) return true;
        if (TryAttackPlan(aiPiece, NormalPlan(aiPiece, target, true)) || actionAttempted) return true;
        return TryApproach(aiPiece, target.transform.position, aiPiece.GetRange(true));
    }

    private static AttackPlan NormalPlan(EnemyController piece, PieceController target, bool ranged)
    {
        return new AttackPlan
        {
            Target = target,
            Skill = ranged ? piece.pieceData.rangedAtk : piece.pieceData.meleeAtk,
            Action = ranged ? ActionType.远程攻击 : ActionType.近战攻击
        };
    }

    private bool TrySkillPlan(EnemyController piece, PieceController target)
    {
        if (piece.availableSkills == null) return false;
        // 先找原地可用技能，再找需要移动的技能；不再用概率跳过可用技能。
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (var skill in piece.availableSkills)
            {
                if (!IsOffensiveSkill(skill) || !piece.unitAttrCenter.HasMana(skill.mpCost)) continue;
                if (pass == 0 && !CanAttackFrom(piece.transform.position, target, skill)) continue;
                var plan = new AttackPlan { Target = target, Skill = skill, Action = ActionType.技能 };
                if (TryAttackPlan(piece, plan) || actionAttempted) return true;
            }
        }
        return false;
    }

    private bool TryAttackPlan(EnemyController piece, AttackPlan plan)
    {
        if (plan.Skill == null) return false;
        if (plan.Action == ActionType.技能 && !piece.unitAttrCenter.HasMana(plan.Skill.mpCost)) return false;
        bool needsReload = plan.Action == ActionType.远程攻击 && piece.unitAttrCenter.AmmoCount <= 0;
        if (needsReload && piece.unitAttrCenter.MaxAmmoCount <= 0) return false;

        // 1. 已在范围内就直接行动，不浪费移动点。
        if (CanAttackFrom(piece.transform.position, plan.Target, plan.Skill))
        {
            if (needsReload)
                return SpendAction(piece, ActionType.重新装填, () => piece.ReloadAmmo());
            return SpendAction(piece, plan.Action, () =>
            {
                if (plan.Action == ActionType.技能)
                {
                    if (!piece.unitAttrCenter.HasMana(plan.Skill.mpCost)) return;
                    piece.StartSkillAttack(plan.Skill);
                    if (!piece.unitAttrCenter.CostMana(plan.Skill.mpCost)) return;
                    piece.CastSkillOnTarget(plan.Target, plan.Skill);
                }
                else
                {
                    piece.StartNormalAttack(plan.Action == ActionType.远程攻击);
                    piece.CastAttackOnTarget(plan.Target);
                    if (piece.enemyAIType == EnemyAIType.Shoot && plan.Action == ActionType.远程攻击)
                        retreatAfterShot.Add(piece);
                }
            });
        }

        // 2. 预留攻击和必要装填的行动力，再用实际路径长度判断本回合能否打到。
        if (!CanAfford(piece, plan.Action) || !CanMove(piece)) return false;
        int reserved = ActionCost(plan.Action) + (needsReload ? ActionCost(ActionType.重新装填) : 0);
        int moves = (piece.unitAttrCenter.CurMovePoint - reserved) / Mathf.Max(1, ActionCost(ActionType.移动));
        if (moves <= 0) return false;
        var candidates = FindPositions(piece, plan.Target.transform.position, plan.Skill.rangeValue,
            moves * piece.unitAttrCenter.MoveRange, p => CanAttackFrom(p, plan.Target, plan.Skill));
        if (candidates.Count == 0) return false;

        // 3. 从所需移动次数最少的合法点中随机选择，移动后保留攻击计划。
        float moveRange = piece.unitAttrCenter.MoveRange;
        int fewestMoves = candidates.Min(c => Mathf.CeilToInt(c.PathLength / moveRange));
        candidates.RemoveAll(c => Mathf.CeilToInt(c.PathLength / moveRange) != fewestMoves);
        Shuffle(candidates);
        foreach (var candidate in candidates)
        {
            if (!TryMoveTo(piece, candidate.Position)) continue;
            if (!piece.isDead) pendingAttacks[piece] = plan;
            return true;
        }
        return false;
    }

    private static bool IsOffensiveSkill(SkillPack skill)
    {
        return skill != null && skill.rangeValue > 0 &&
            skill.target is SkillTarget.Enemy or SkillTarget.EnemyAll or SkillTarget.All or SkillTarget.FarthestEnemy;
    }

    private static bool CanAttackFrom(Vector3 position, PieceController target, SkillPack skill)
    {
        if (target == null || skill == null) return false;
        if (skill.layerSkill && Mathf.Abs(position.y - target.transform.position.y) > 0.1f) return false;
        float distance = Vector3.Distance(position, target.transform.position);
        return distance <= skill.rangeValue && (skill.rangeType != RangeType.Fan || distance >= 1f);
    }

    private static bool IsValidTarget(EnemyController piece, PieceController target)
    {
        return target != null && !target.isDead && target.gameObject.activeInHierarchy &&
            BuffManager.CanTarget(piece, target);
    }

    private static int ActionCost(ActionType action) => GM.Ins.DM.gameConstSO.GetActionPointCost(action);
    private static bool CanAfford(EnemyController piece, ActionType action) =>
        piece.unitAttrCenter.CurMovePoint >= ActionCost(action);

    private static bool CanMove(EnemyController piece)
    {
        return CanAfford(piece, ActionType.移动) && piece.unitAttrCenter.MoveRange > MinimumMove &&
            piece.unitAttrCenter.GetBuffStacks(BuffType.Bind) == 0;
    }

    private bool SpendAction(EnemyController piece, ActionType action, Action execute)
    {
        if (!CanAfford(piece, action)) return false;
        actionAttempted = true;
        // 行动触发的异常状态可能中止动作；本次仍已尝试行动，不继续执行第二个动作。
        if (piece.unitAttrCenter.CostMP(action)) execute();
        return true;
    }

    private List<MoveCandidate> FindPositions(EnemyController piece, Vector3 target, float range,
        float pathBudget, Func<Vector3, bool> accepts)
    {
        var result = new List<MoveCandidate>();
        var agent = piece.GetComponent<NavMeshAgent>();
        if (agent == null) return result;
        float margin = Mathf.Max(0.4f, agent.stoppingDistance + 0.1f);
        float radius = Mathf.Max(0f, range - margin);
        Vector3 facing = piece.transform.position - target;
        facing.y = 0;
        if (facing.sqrMagnitude < 0.01f) facing = Vector3.forward;
        facing.Normalize();

        // 环绕目标采样，验证真实路径、落点占用和攻击条件；不修改全局移动预览。
        for (int ring = 0; ring < 2; ring++)
        {
            for (int i = 0; i < PositionSamples; i++)
            {
                Vector3 point = target + Quaternion.Euler(0, i * 360f / PositionSamples, 0) *
                    facing * radius * (ring == 0 ? 1f : 0.65f);
                if (!BattleScene.Ins.BM.moveManager.TryGetAIMoveDestination(piece.gameObject, point,
                    pathBudget, out var destination, out float length, false)) continue;
                if (!accepts(destination) || IsOccupied(piece, destination)) continue;
                if (result.Any(c => Vector3.SqrMagnitude(c.Position - destination) < 0.04f)) continue;
                result.Add(new MoveCandidate { Position = destination, PathLength = length });
            }
        }
        return result;
    }

    private bool TryApproach(EnemyController piece, Vector3 target, float range)
    {
        if (!CanMove(piece)) return false;
        var candidates = FindPositions(piece, target, range, float.PositiveInfinity, _ => true);
        if (candidates.Count == 0) return false;
        // 即使本回合打不到，也沿完整导航路径接近，允许绕墙时暂时远离目标。
        float shortest = candidates.Min(c => c.PathLength);
        candidates.RemoveAll(c => c.PathLength > shortest + piece.unitAttrCenter.MoveRange * 0.2f);
        Shuffle(candidates);
        foreach (var candidate in candidates)
            if (TryMoveTo(piece, candidate.Position)) return true;
        return false;
    }

    private bool TryRetreat(EnemyController piece, Vector3 threat, float range)
    {
        if (!CanMove(piece)) return false;
        var candidates = new List<MoveCandidate>();
        var edgeEscapes = new List<MoveCandidate>();
        var agent = piece.GetComponent<NavMeshAgent>();
        if (agent == null) return false;
        Vector3 origin = piece.transform.position;
        float moveRange = piece.unitAttrCenter.MoveRange;
        float initialSafety = ChaseSafety(origin);
        float initialEdge = NavMesh.FindClosestEdge(origin, out var originEdge, agent.areaMask) ? originEdge.distance : 0f;
        float phase = UnityEngine.Random.Range(0f, 360f / PositionSamples);
        for (int ring = 1; ring <= 3; ring++)
        {
            for (int i = 0; i < PositionSamples; i++)
            {
                Vector3 point = origin + Quaternion.Euler(0, phase + i * 360f / PositionSamples, 0) *
                    Vector3.forward * moveRange * ring / 3f;
                if (!BattleScene.Ins.BM.moveManager.TryGetAIMoveDestination(piece.gameObject, point,
                    moveRange, out var destination, out float length, false) || IsOccupied(piece, destination)) continue;
                float safety = ChaseSafety(destination);
                // 不往更危险的位置撤退；允许沿侧方移动或从贴边位置向开阔处转移。
                bool safer = safety >= Mathf.Min(initialSafety, 0f) - 0.25f;
                float edgeSpace = NavMesh.FindClosestEdge(destination, out var edge, agent.areaMask) ? edge.distance : 0f;
                float distance = Vector3.Distance(destination, threat);
                float score = Mathf.Clamp(safety, -moveRange, moveRange * 0.5f)
                    + Mathf.Min(edgeSpace, 3f) * 1.5f
                    - Mathf.Max(0f, distance - range * 0.9f);
                if (previousPositions.TryGetValue(piece, out var previous) &&
                    Vector3.Distance(previous, destination) < 1f) score -= 2f;
                var candidate = new MoveCandidate { Position = destination, PathLength = length, Score = score };
                if (safer) candidates.Add(candidate);
                else if (edgeSpace > initialEdge + 0.5f && safety >= initialSafety - moveRange * 0.6f)
                    edgeEscapes.Add(candidate);
            }
        }
        // 被逼到角落时允许适度缩短距离以离开边缘，避免只能原地站住。
        if (candidates.Count == 0) candidates = edgeEscapes;
        if (candidates.Count == 0) return false;
        // 安全性相近的候选点随机选，不固定向玩家反方向走。
        float bestScore = candidates.Max(c => c.Score);
        candidates.RemoveAll(c => c.Score < bestScore - 0.75f);
        Shuffle(candidates);
        foreach (var candidate in candidates)
            if (TryMoveTo(piece, candidate.Position)) return true;
        return false;
    }

    private float ChaseSafety(Vector3 point)
    {
        float safety = float.PositiveInfinity;
        foreach (var player in BattleScene.Ins.BM.PlayerController.pieces)
        {
            if (player == null || player.isDead || !player.gameObject.activeInHierarchy) continue;
            float meleeRange = player.pieceData.meleeAtk != null ? player.pieceData.meleeAtk.rangeValue : 0f;
            float chase = player.unitAttrCenter.GetBuffStacks(BuffType.Bind) != 0 ? 0f : player.unitAttrCenter.MoveRange;
            safety = Mathf.Min(safety, Vector3.Distance(point, player.transform.position) - chase - meleeRange);
        }
        return float.IsPositiveInfinity(safety) ? 0f : safety;
    }

    private bool IsOccupied(EnemyController movingPiece, Vector3 position)
    {
        var agent = movingPiece.GetComponent<NavMeshAgent>();
        float radius = agent != null ? agent.radius : 0.5f;
        foreach (var other in pieces.Concat(BattleScene.Ins.BM.PlayerController.pieces))
        {
            if (other == null || other == movingPiece || other.isDead || !other.gameObject.activeInHierarchy) continue;
            if (Mathf.Abs(other.transform.position.y - position.y) > 1f) continue;
            var otherAgent = other.GetComponent<NavMeshAgent>();
            float clearance = radius + (otherAgent != null ? otherAgent.radius : 0.5f);
            Vector3 diff = other.transform.position - position;
            diff.y = 0;
            if (diff.sqrMagnitude < clearance * clearance) return true;
        }
        return false;
    }

    private static void Shuffle(List<MoveCandidate> candidates)
    {
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int index = UnityEngine.Random.Range(0, i + 1);
            (candidates[i], candidates[index]) = (candidates[index], candidates[i]);
        }
    }

    private bool TryMoveTo(EnemyController piece, Vector3 goal)
    {
        if (!CanMove(piece)) return false;
        var movement = BattleScene.Ins.BM.moveManager;
        if (!movement.TryGetAIMoveDestination(piece.gameObject, goal, piece.unitAttrCenter.MoveRange,
            out var destination, out _, true) || IsOccupied(piece, destination)) return false;
        return SpendAction(piece, ActionType.移动, () =>
        {
            Vector3 origin = piece.transform.position;
            float length = movement.ExecuteMove(piece.gameObject,
                () => { if (piece != null && !piece.isDead) piece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle); },
                destination);
            if (length <= 0f) return;
            previousPositions[piece] = origin;
            piece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Move);
            piece.CheckFace(destination - origin);
            piece.PlayAudio(ActionType.移动);
        });
    }

    // 保留原调用入口，所有 AI 移动都先选合法点，再扣行动力。
    public void EnemyMove(EnemyController aiPiece, Vector3 targetPos, float range, bool leave = false)
    {
        if (leave) TryRetreat(aiPiece, targetPos, range);
        else TryApproach(aiPiece, targetPos, range);
    }

    private bool EnemyMoveToLadder(EnemyController aiPiece)
    {
        LadderArea ladder = mapController?.GetLadder(aiPiece.transform.position);
        if (ladder == null || !CanMove(aiPiece)) return false;
        Vector3 target = ladder.GetNearPos(aiPiece.transform.position);
        if (Vector3.Distance(target, aiPiece.transform.position) < 2f)
            return SpendAction(aiPiece, ActionType.攀爬, () => ladder.TriggerAction(aiPiece));
        return TryApproach(aiPiece, target, 0f);
    }

    /// <summary>
    /// 敌人行动，根据所有目标的威胁值计算
    /// </summary>
    private void EnemyAction_Calculate()
    {
        foreach (EnemyController aiPiece in pieces)
        {
            if (aiPiece == null || !aiPiece.isActived || aiPiece.isDead) continue;
            if (aiPiece.unitAttrCenter.CurMovePoint <= 0) continue;
            PieceController target = CheckEnemyTarget(aiPiece);
            if (!EnemyActionSelect(aiPiece, target))
                continue; // 如果敌人没有行动，则继续下一个敌人
            BattleScene.Ins.BM.cameraController.SetFollow(aiPiece.transform);
            return;
        }

        BattleScene.Ins.BM.ChangeTurn();
    }

    /// <summary>
    /// 即时制下，轮询所有敌人并尝试让其中一个执行行动
    /// </summary>
    /// <returns>是否有敌人成功执行了行动</returns>
    private bool TryExecuteEnemyAction()
    {
        int totalPieces = pieces.Count;
        for (int i = 1; i <= totalPieces; i++)
        {
            int currentIndex = (_lastActedIndex + i) % totalPieces;
            EnemyController aiPiece = pieces[currentIndex] as EnemyController;
            if (aiPiece == null || !aiPiece.isActived || aiPiece.isDead) continue;

            // 行动力判空（适配之前的行动力属性，若不够1点行动力直接跳过）
            if (aiPiece.unitAttrCenter.CurMovePoint <= 0) continue;

            PieceController target = CheckEnemyTarget(aiPiece);

            // 尝试执行行动
            if (EnemyActionSelect(aiPiece, target))
            {
                _lastActedIndex = currentIndex;
                // 成功行动后跟随镜头（可根据需要保留或删减）
                //BattleScene.Ins?.BM?.GetComponent<CameraController>()?.SetFollow(aiPiece.transform);

                // 返回 true 触发全局 1 秒冷却，避免其他满足条件的敌人同时行动
                return true;
            }
        }

        // 即时制下，所有敌人均未行动时直接结束检测，不再调用 ChangeTurn()
        return false;
    }

    public PieceController CheckEnemyTarget(EnemyController aiPiece)
    {
        // 计算所有目标棋子的威胁值
        Dictionary<PieceController, int> threatValues = new();

        PieceController nearTarget = null; // 距离最近的棋子
        // 伤害最高的棋子
        PieceController highDamageTarget = null;
        float minDistance = float.MaxValue;
        int maxDamage = -1;

        foreach (var playerPiece in BattleScene.Ins.BM.PlayerController.pieces)
        {
            if (playerPiece == null || playerPiece.isDead || !playerPiece.gameObject.activeInHierarchy ||
                !BuffManager.CanTarget(aiPiece, playerPiece)) continue;
            threatValues.Add(playerPiece, 0);

            // 获取最近的玩家棋子
            float distance =
                Vector3.Distance(playerPiece.transform.position, aiPiece.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearTarget = playerPiece;
            }

            // 伤害最高的玩家棋子
            if (aiPiece.damageDic.ContainsKey(playerPiece))
            {
                int damage = aiPiece.damageDic[playerPiece];
                if (damage > maxDamage)
                {
                    maxDamage = damage;
                    highDamageTarget = playerPiece;
                }
            }

            // 计算嘲讽值
            threatValues[playerPiece] += playerPiece.unitAttrCenter.TauntValue;
        }

        if (nearTarget != null) threatValues[nearTarget] += 1; // 距离最近的地方棋子威胁值+1
        if (highDamageTarget != null)
        {
            threatValues[highDamageTarget] += 2; // 伤害最高的玩家棋子威胁值+2
        }

        if (threatValues.Count == 0) return null;
        // 选择威胁值最高的目标进行攻击
        PieceController target = threatValues.Aggregate((l, r)
            => l.Value > r.Value ? l : r).Key;
        return target;
    }
}
