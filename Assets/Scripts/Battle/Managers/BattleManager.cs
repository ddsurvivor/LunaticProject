using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BattleDialogue;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using SkillSystem;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public partial class BattleManager : MonoBehaviour
{
    public AIController AIController;
    public PlayerController PlayerController;
    [FormerlySerializedAs("camera")] public CameraController cameraController;
    public BuffManager buffManager;
    public SkillManager skillManager;
    public SpriteManager spriteManager;
    public DiceCheckManager diceCheckManager;
    public TutorialManager tutorialManager;
    public TipTextManager tipTextManager;
    public MoveManager moveManager;
    public BattleDialogueManager battleDialogueManager;
    public CharacterSkillManager characterSkillManager;
    public OrderManager orderManager;

    // 伤害管理器随本场战斗持有，自动创建，无需给场景或预制体补挂组件。
    private DamageManager _damageManager;
    public DamageManager damageManager => _damageManager ??= new DamageManager(this);

    public MemeCatalogSO memeCatalog; // 留空时读取 DataManager 的全局模因配置。
    private MemeService _memeService;
    /// <summary>本场模因装配与触发入口，不需要额外挂载组件。</summary>
    public MemeService memeService => _memeService ??= new MemeService(this);

    private TimedMemeStatusService _timedMemeStatuses;
    /// <summary>本场精确模因状态与计时，不复用旧Buff层数。</summary>
    public TimedMemeStatusService timedMemeStatuses => _timedMemeStatuses ??= new TimedMemeStatusService();

    public PieceDataListSO pieceDataListSO;

    //public List<LadderArea> ladderAreas = new();
    public List<HealArea> areaList = new(); // 
    public FinishDrop finishDrop;
    [LabelText("战斗胜利经验值")] public int finishExp = 100;
    [LabelText("坚持回合胜利")] public int winTurnCondition = 999; // 胜利条件：在多少回合内获胜，999表示不限制

    public BattleSetController battleSetController;// 战斗预设
    public PieceActionManager pieceActionManager;
    public int TunrNumber => _turnNumber;
    private int _turnNumber = 0;

    private bool inBattle = false; // 是否在战斗中，防止重复初始化

    [SerializeField] [LabelText("镜头移动等待时间")]
    float moveWaitTime = 1.0f; // 

    [SerializeField] [LabelText("镜头注视时间")] float gazeWaitTime = 0.5f; // 


    public List<PieceController> summonPieces = new();

    public void Init()
    {
        // 同一场景的多个入口可能请求初始化；先占用，防止 OnInit 事件重入。
        if (startupInitialized || startupInitializing) return;
        startupInitializing = true;
        ResetBattleStartup();
        try
        {
            GM.Ins.BeginBattleSession(this);
            inBattle = true;
            LastOutcome = BattleOutcome.None;
            _turnNumber = 0;
            _timedMemeStatuses?.ClearAll();
            ClearNetworkConnections();
            characterSkillManager.ClearAllRegistry();
            PlayerController.Init();
            AIController.Init();
            ApplySetting(GM.Ins.battleSetting); // 在完成所有棋子初始化以后，更新预设
            _delaySkillPack = null;
            delayAction = null;
            if(pieceActionManager!=null) pieceActionManager.ApplySettingsToPieces(PlayerController.pieces);
            //StartBattle();
            //gray = Resources.Load<Material>("Materials/Gray");
            //grayEnemy = Resources.Load<Material>("Materials/GrayEnemy");
            startupInitialized = true;
        }
        finally { startupInitializing = false; }
        if (startupRequested) StartBattle();
    }

    /// <summary>
    ///  应用战斗设置, 在棋子初始化后调用
    /// </summary>
    /// <param name="setting"></param>
    public void ApplySetting(int setting = 0)
    {
        // 执行特定战斗设置，如特殊规则、初始状态等
        if (battleSetController != null)
        {
            Debug.Log($"应用战斗设置: {setting}");
            battleSetController.ApplyAllPreset(setting);
        }
    }

    // 只在真实播放/飞行期间阻止下一动作；不以序列帧数量估算锁定秒数。
    public bool CanInspectEnemyTargets => inBattle && !BattleScene.Ins.TM.IsPaused && startupPhase == StartupPhase.Active && PlayerController.isInTurn;

    // 回合、AI、输入和聚能交接共用此等待条件，镜头恢复也属于受击表现。
    public bool HasCombatPresentation => HasPendingBurstRelease || (cameraController != null && cameraController.IsPresentingHit) || HasActionPresentation;

    /// <summary>攻击、受击、死亡和结算仍在播放；不含镜头，供镜头等待而不形成循环依赖。</summary>
    public bool HasActionPresentation => deathFades.Count > 0 || HasPendingBattleReactions || HasPendingRecognitionSettlement ||
        HasPresentation(PlayerController.pieces) || HasPresentation(AIController.pieces) || HasPresentation(summonPieces);

    private static bool HasPresentation(IEnumerable<PieceController> pieces)
    {
        foreach (var piece in pieces)
            if (piece != null && piece.gameObject.activeInHierarchy &&
                (piece.IsPerformingAction || (piece.pieceDisplay != null && piece.pieceDisplay.IsPlayingOneShot))) return true;
        return false;
    }

    /// <summary>兼容旧交棒事件，标记结束当前棋子；实际切换由调度器等待动画与移动完成后执行。</summary>
    public void ChangeTurn()
    {
        if (SpeedTurnsActive && ActivePiece != null) finishPieceTurn = true;
    }

    /// <summary>兼容旧玩家回合入口并启动速度调度；保留 firstTurn 参数供原调用使用。</summary>
    public void PlayerStart(bool firstTurn = true) => StartSpeedTurns();

    /// <summary>在攻击动画开始前准备本次攻击的被动倍率；所有目标与伤害段共享，命中时必须复用该值。</summary>
    public float PrepareAttackMultiplier(PieceController attacker, List<PieceController> targets, SkillPack skill, Vector3 targetPos)
    {
        var validTargets = SkillTargeting.Filter(attacker, targets, skill, targetPos);
        var hostileTarget = validTargets.FirstOrDefault(t => !t.isDead && BattleRelations.CanAttack(attacker, t));
        return skill.attackPacks.Count > 0 && hostileTarget != null
            ? characterSkillManager.EvaluateDamageMultiplier(attacker.gameObject, hostileTarget.gameObject) : 1f;
    }

    /// <summary>在正常命中时统一结算技能，复用动画前已准备的被动倍率；无动画的旧入口仍等待检定结束。</summary>
    public void PieceSkill(PieceController attacker, List<PieceController> targets
        , SkillPack skillPack, Vector3 targetPos = default, ActionType actionType = 0
        , CheckResult checkResult = CheckResult.None, bool isFlank = false, MemeActionContext memeActionContext = null,
        float? preparedPassiveMultiplier = null)
    {
        if (attacker == null || skillPack == null || BattleRelations.IsOffensive(skillPack) && !BattleRelations.CanInitiateAttack(attacker) || (attacker.isDead && !skillPack.AllowDeadCaster) || targets == null) return;
        if (PlayerController.isBursting && !IsBurstActor(attacker) && skillPack.attackPacks.Count > 0)
        {
            var capturedTargets = new List<PieceController>(targets);
            if (TryDeferBurstEffect(attacker, () => PieceSkill(attacker, capturedTargets, skillPack, targetPos,
                actionType, checkResult, isFlank, memeActionContext, preparedPassiveMultiplier))) return;
        }
        List<List<DamageInfo>> damageInfoList = new();
        var memeAction = memeActionContext ?? memeService.BeginAction(attacker, skillPack, isFlank);
        using var actionScope = memeAction.EnsureAction(attacker, skillPack).Enter();
        var validTargets = SkillTargeting.Filter(attacker, targets, skillPack, targetPos);
        // 每次施法只消费一次攻击被动，全部目标与伤害段共享本次倍率。
        var hostileTarget = validTargets.FirstOrDefault(t => !t.isDead && BattleRelations.CanAttack(attacker, t));
        float passiveMultiplier = preparedPassiveMultiplier ?? PrepareAttackMultiplier(attacker, validTargets, skillPack, targetPos);
        if (skillPack.isRecognitionCheck && checkResult == CheckResult.None && hostileTarget != null)
            checkResult = diceCheckManager.ModeRecognitionCheck(attacker, hostileTarget);
        // 攻击被动也能弹出检定；先保留本次倍率和结果，展示结束后再进入扣血流程。
        if (diceCheckManager != null && diceCheckManager.IsPresenting)
        {
            StartCoroutine(WaitForRecognitionSettlement(attacker, new List<PieceController>(validTargets), skillPack,
                targetPos, actionType, checkResult, isFlank, memeAction, passiveMultiplier));
            return;
        }
        bool isCrit = false;
        var hitTargets = new List<PieceController>();
        foreach (var target in validTargets)
        {
            // 1. 伤害管理器统一处理目标校验、命中、暴击与各段扣血。
            var settlement = damageManager.ResolveSkillTarget(attacker, target, skillPack, checkResult, isFlank, passiveMultiplier, BattleActionContext.Current);
            var damageInfos = settlement.DamageInfos;
            damageInfoList.Add(damageInfos);
            isCrit |= settlement.IsCritical;
            if (settlement.HitAttack && BattleRelations.CanAttack(attacker, target)) hitTargets.Add(target);

            // 2. 无效目标、未命中或屏障阻挡时，只跳过当前目标的附加效果。
            if (!settlement.CanApplyEffects) continue;

            // 每目标只在整次伤害结算后触发一次，未命中、屏障和致死目标不会获得新状态。
            memeService.ResolveTarget(attacker, target, skillPack, checkResult, memeAction);

            // 处理buff
            foreach (var buffPack in skillPack.buffPacks)
            {
                if (buffPack.target == SkillTarget.Self)
                {
                    // 施法者状态在目标循环外只结算一次。
                    continue;
                }
                else if (buffPack.target == SkillTarget.All)
                {
                    if (GameConst.CheckRate(buffPack.rate))
                    {
                        buffManager.AddBuff(target.unitAttrCenter, buffPack.buffType
                            , buffPack.stacks, buffPack.barrierHealth, buffPack.retaliationTurns);
                    }
                }
                else if (buffPack.target == SkillTarget.EnemyAll ||
                         buffPack.target == SkillTarget.Enemy || buffPack.target == SkillTarget.FarthestEnemy)
                {
                    if (!BattleRelations.CanAttack(attacker, target)) continue;
                    if (GameConst.CheckRate(buffPack.rate))
                    {
                        buffManager.AddBuff(target.unitAttrCenter, buffPack.buffType
                            , buffPack.stacks, buffPack.barrierHealth, buffPack.retaliationTurns);
                    }
                }
                else if (buffPack.target == SkillTarget.Ally || buffPack.target == SkillTarget.AllyBody)
                {
                    if (!BattleRelations.CanSupport(attacker, target)) continue;
                    if (GameConst.CheckRate(buffPack.rate))
                    {
                        buffManager.AddBuff(target.unitAttrCenter, buffPack.buffType
                            , buffPack.stacks, buffPack.barrierHealth, buffPack.retaliationTurns);
                    }
                }
            }

            // 处理附加效果
            ApplySKillEffect(skillPack, attacker, target, targetPos);
            ApplyAddEffect(skillPack, attacker, target, targetPos);
            if (skillPack.memeCureTags != MemeCureTag.None) timedMemeStatuses.TryCure(target, skillPack.memeCureTags);
            if (IsBurstActor(attacker)) // 聚能状态下所有攻击附加击退效果
            {
                SpaceBombEffect(skillPack, attacker, target, targetPos, true); // 去除假死
                // 如果没有击退效果则添加默认击退效果，如果有击退效果则不添加
                if (skillPack.additionalEffects == null ||
                    !skillPack.additionalEffects.Any(e => e is HitBackEffect))
                {
                    // 击退距离为默认值加上每10点伤害增加0.5f
                    float dis = 1f + damageInfos.Sum(d => d.damageValue) / 10f * 0.5f;
                    HitBackEffect(
                        new HitBackEffect
                        {
                            dis = dis
                            , hitBackDamage = (int)(damageInfos.Sum(d => d.damageValue) * 0.4f)
                        }, attacker, target, targetPos);
                }
            }

            // 排队到击退落点与碰撞结算后再判定夹击。
            CheckFlankAttack(attacker, target);
        }

        foreach (var buffPack in skillPack.buffPacks)
        {
            if (buffPack.target == SkillTarget.Self && GameConst.CheckRate(buffPack.rate))
                buffManager.AddBuff(attacker.unitAttrCenter, buffPack.buffType,
                    buffPack.stacks, buffPack.barrierHealth, buffPack.retaliationTurns);
        }

        ComponentEquipment.OnAttackResolved(attacker, hitTargets, skillPack, actionType, targetPos);

        // 操作记录系统
        BattleScene.Ins.UM.logPanel.PlayerLogAttack(attacker.pieceData.pieceName,
            actionType == 0 ? skillPack.skillName : actionType.ToDisplayName(),
            validTargets.Select(t => t.pieceData.pieceName).ToList(),
            damageInfoList
        );
        if (validTargets.Count > 0)
        {
            var cameraTarget = validTargets[0];
            System.Action focus = () =>
            {
                if (cameraTarget == null || attacker == null) return;
                if (isCrit) cameraController.FocusShake(cameraTarget.transform, attacker.transform);
                else cameraController.FocusTarget(cameraTarget.transform, attacker.transform);
            };
            // 敌方受击首帧冻结时，命中镜头也须随表现释放，不能独占当前聚能输入。
            if (!PlayerController.isBursting || cameraTarget.isPlayerPiece || !TryDeferBurstEffect(cameraTarget, focus)) focus();
        }

        // 处理聚能充能效果，多段伤害只充能一次
        if (attacker.isPlayerPiece && hitTargets.Count > 0)
        {
            if (!PlayerController.isBursting)
            {
                // 攻击充能
                PlayerController.ChargeBurst(GameConst.attackBurstCharge);
            }
        }

        // 处理附加效果
        ApplySKillEffectOnce(skillPack, attacker, targetPos);

        //BattleScene.Ins.BM.camera.FocusShake(validTargets[0].transform);
    }

    public void PlayerCheckWin()
    {
        if (BurstEffectsDeferred)
        {
            if (PlayerController.pieces.All(p => p == null || p.isDead) || AIController.pieces.All(p => p == null || p.isDead)) cancelBurst = true;
            return;
        }
        // 检查敌方是否全灭
        bool allEnemyDead = true;
        foreach (var piece in AIController.pieces)
        {
            if (!piece.isDead)
            {
                allEnemyDead = false;
                break;
            }
        }

        if (AIController.keyPieces.Count > 0 && AIController.keyPieces.All(k => k.isDead))
        {
            // 如果有关键棋子，且所有关键棋子都死了，也算敌方全灭
            allEnemyDead = true;
        }

        Debug.Log($"判定敌人棋子全灭：{allEnemyDead}");
        if (allEnemyDead)
        {
            PlayerWin();
            return;
        }

        // 检查我方是否全灭
        bool allPlayerDead = true;
        foreach (var piece in PlayerController.pieces)
        {
            if (!piece.isDead)
            {
                allPlayerDead = false;
                break;
            }
        }

        if (PlayerController.keyPieces.Count > 0 && PlayerController.keyPieces.All(k => k.isDead))
        {
            allPlayerDead = true;
        }

        //Debug.Log($"检查我方棋子全灭：{allPlayerDead}");
        if (allPlayerDead)
        {
            PlayerLoss();
        }
    }

    /// <summary>记录一次胜利并显示结果；掉落和经验留待玩家离场时提交。</summary>
    public void PlayerWin()
    {
        if (ObserveBattleOutcome(BattleOutcome.PlayerVictory)) return;
        PresentBattleOutcome(true);
    }

    /// <summary>记录一次失败并停止调度，提供重试和主菜单出口。</summary>
    public void PlayerLoss()
    {
        if (ObserveBattleOutcome(BattleOutcome.PlayerDefeat)) return;
        PresentBattleOutcome(false);
    }

    public void ApplySKillEffect(SkillPack skillPack, PieceController caster = null
        , PieceController target = null
        , Vector3 targetPos = default)
    {
        foreach (var effect in skillPack.skillEffects)
        {
            switch (effect)
            {
                case SkillEffect.SpaceBomb:
                    SpaceBombEffect(skillPack, caster, target, targetPos);

                    break;
                default:
                    break;
            }
        }
    }

    private void SpaceBombEffect(SkillPack skillPack, PieceController caster = null
        , PieceController target = null
        , Vector3 targetPos = default, bool isBurstHit = false)
    {
        if (target.gameObject.activeInHierarchy)
        {
            // 检测目标是否有假死技能, 如果有且进入了假死状态，则彻底杀死
            SelfRevive revive = target.GetComponent<SelfRevive>();
            if (revive != null)
            {
                if (revive.isFakeDead)
                {
                    revive.TrueDeath(isBurstHit);
                }
            }
        }
    }

    /// <summary>
    /// 只处理一次的技能附加效果
    /// </summary>
    /// <param name="skillPack"></param>
    /// <param name="caster"></param>
    /// <param name="targetPos"></param>
    public void ApplySKillEffectOnce(SkillPack skillPack, PieceController caster = null
        , Vector3 targetPos = default)
    {
        //Debug.Log("处理一次性效果");
        foreach (var effect in skillPack.skillEffects)
        {
            switch (effect)
            {
                case SkillEffect.Blink:
                    // TODO: 优化为移动
                    moveManager.TeleportPawnSuccess(caster.gameObject
                        , targetPos + new Vector3(-1.5f, 0, -1.5f));
                    /*caster.transform.position =
                        targetPos + new Vector3(-1.5f, 0, -1.5f);*/
                    break;
                case SkillEffect.HealArea:
                    Debug.Log("生成治疗区");
                    // 直接范围回血
                    HealArea healArea = ObjectPool.Ins.GenerateObject(ItemType.HEAL_AREA,
                        targetPos,
                        Quaternion.identity).GetComponent<HealArea>();
                    healArea.SetData(skillPack.buffPacks[0], 1, skillPack.explodeRadius);
                    break;
                case SkillEffect.Summon:
                    foreach (var effectBase in skillPack.additionalEffects)
                    {
                        if (effectBase is SummonEffect summonEffect)
                        {
                            summonEffect.ApplyEffect(targetPos, caster.player);
                        }
                    }

                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// 每个目标都要处理的技能附加效果（如击退）
    /// </summary>
    /// <param name="skillPack"></param>
    /// <param name="caster"></param>
    /// <param name="target"></param>
    /// <param name="targetPos"></param>
    private void ApplyAddEffect(SkillPack skillPack, PieceController caster = null
        , PieceController target = null, Vector3 targetPos = default)
    {
        if (skillPack.additionalEffects == null || skillPack.additionalEffects.Count == 0)
        {
            return;
        }

        foreach (var effectBase in skillPack.additionalEffects)
        {
            switch (effectBase)
            {
                case HitBackEffect hitBackEffect:
                    HitBackEffect(hitBackEffect, caster, target, targetPos);
                    break;
                case ShootFxEffect shootFxEffect:
                    shootFxEffect.ApplyEffect(caster, targetPos);
                    break;
                case SelfExplosionEffect selfExplosionEffect:
                    selfExplosionEffect.ApplyEffect(caster);
                    break;
                case SummonEffect summonEffect:
                    summonEffect.ApplyEffect(targetPos, caster.player);
                    break;
                case ReviveEffect reviveEffect:
                    reviveEffect.ApplyEffect(target);
                    break;
                default:
                    break;
            }
        }
    }

    /*/// <summary>
    /// 击退效果
    /// </summary>
    /// <param name="hitBackEffect"></param>
    /// <param name="caster"></param>
    /// <param name="target"></param>
    /// <param name="targetPos"></param>
    private void HitBackEffect(HitBackEffect hitBackEffect, PieceController caster = null,
        PieceController target = null, Vector3 targetPos = default)
    {
        if (target == null || target.ableMove == false) return;

        Vector3 dir = (target.transform.position - caster.transform.position);
        dir.y = 0;
        dir.Normalize();

        // 调用提取出的算法函数
        MoveResult moveResult = CalculateValidMovePos(target.transform.position, dir
            , hitBackEffect.dis, target.gameObject);

        // 锁定Y轴，防止击退造成高度偏差
        Vector3 finalPos = moveResult.FinalPosition;
        finalPos.y = target.transform.position.y;

        // 执行位移
        target.transform.DOMove(finalPos, 0.2f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            if (moveResult.IsCollided)
            {
                // 将自身加入伤害列表，处理碰撞反馈
                if (!moveResult.HitPieces.Contains(target))
                    moveResult.HitPieces.Add(target);

                TriggerCollisionDamage(moveResult.HitPieces, hitBackEffect.hitBackDamage);
            }
        });
    }*/


    /// <summary>
    /// 【NavMesh优化版】击退效果
    /// </summary>
    private void HitBackEffect(HitBackEffect hitBackEffect, PieceController caster = null,
        PieceController target = null, Vector3 targetPos = default)
    {
        if (PlayerController.isBursting && target != null &&
            TryDeferBurstEffect(target, () => HitBackEffect(hitBackEffect, caster, target, targetPos))) return;
        if (!CanReact(target) || caster == null || target.ableMove == false) return;
        if (pieceDisplacements.TryGetValue(target, out var previousDisplacement)) previousDisplacement.Kill();

        // 获取目标棋子的 NavMeshAgent
        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();

        // 计算击退方向
        Vector3 dir = (target.transform.position - caster.transform.position);
        dir.y = 0;
        dir.Normalize();

        // 理论上的理想击退终点
        Vector3 desiredEndPos = target.transform.position + dir * hitBackEffect.dis;

        Vector3 finalPos = desiredEndPos;
        bool hitWall = false;

        // =================【核心优化：NavMesh 环境碰撞拦截】=================
        // NavMesh.Raycast 会沿着网格表面“扫射”，如果中途遇到烘焙的边缘/墙体，会返回 true
        if (NavMesh.Raycast(target.transform.position, desiredEndPos, out NavMeshHit navHit
                , NavMesh.AllAreas))
        {
            // 预撞墙！将终点精准截断在墙面边缘
            finalPos = navHit.position;
            hitWall = true;
        }
        else
        {
            // 如果没撞墙，为了保险起见（防止终点稍微悬空），在终点处向下安全采样一次网格点
            if (NavMesh.SamplePosition(desiredEndPos, out NavMeshHit sampleHit, 1.0f
                    , NavMesh.AllAreas))
            {
                finalPos = sampleHit.position;
            }
        }

        // =================【核心优化：与原有角色碰撞算法融合】=================
        // 此时根据 NavMesh 算出的安全距离，重新限制你原本的角色间寻路/碰撞算法
        float allowedDis = Vector3.Distance(target.transform.position, finalPos);
        MoveResult moveResult = CalculateValidMovePos(target.transform.position, dir, allowedDis
            , target.gameObject);

        // 最终位置取双重保险：谁近听谁的（防止穿墙，也防止穿人）
        if (Vector3.Distance(target.transform.position, moveResult.FinalPosition) < allowedDis)
        {
            finalPos = moveResult.FinalPosition;
        }

        // 综合碰撞判定：撞了烘焙墙体 OR 撞了其他角色棋子
        bool isCollided = hitWall || moveResult.IsCollided;

        // =================【核心优化：防止 Agent 与 Tween 打架】=================
        // 在用 DOTween 强行平移前，必须关闭 Agent 避障与定位，否则会发生剧烈抖动或无法位移
        bool restoreAgent = agent != null && agent.enabled;
        if (agent != null) agent.enabled = false;

        // 锁定Y轴，防止击退造成高度偏差
        finalPos.y = target.transform.position.y;

        // 在安排位移时捕获来源，延迟碰撞沿用原根行动。
        var collisionSource = new BattleDamageSource(BattleDamageKind.Collision, caster);
        int version = reactionVersion;
        Tween displacement = null;
        displacement = target.transform.DOMove(finalPos, 0.2f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            // =================【核心优化：位移结束同步网格】=================
            if (version != reactionVersion || !CanReact(target)) return;
            if (restoreAgent && agent != null)
            {
                agent.enabled = true;
                // 核心关键：使用 Warp 强行将 Agent 的内部坐标刷新到当前物理坐标，彻底根治“退回原点”Bug
                agent.Warp(finalPos);
            }

            if (isCollided)
            {
                // 初始化或获取伤害列表
                var hitPieces = moveResult.HitPieces ??
                                new System.Collections.Generic.List<PieceController>();

                // 将自身加入伤害列表，处理碰撞反馈
                if (!hitPieces.Contains(target))
                    hitPieces.Add(target);

                TriggerCollisionDamage(hitPieces, hitBackEffect.hitBackDamage, collisionSource);
            }
        }).OnKill(() =>
        {
            displacements.Remove(displacement);
            if (target != null) pieceDisplacements.Remove(target);
            if (restoreAgent && CanReact(target) && agent != null && !agent.enabled)
            {
                agent.enabled = true;
                agent.Warp(target.transform.position);
            }
        });
        displacements.Add(displacement);
        pieceDisplacements[target] = displacement;
    }

    /// <summary>
    /// 触发碰撞伤害
    /// </summary>
    private void TriggerCollisionDamage(List<PieceController> hitPieces, int damage = 10, BattleDamageSource source = null)
    {
        foreach (var piece in hitPieces)
        {
            if (!CanReact(piece)) continue;
            // 所有仍存活的碰撞参与者沿用原碰撞基数。
            piece.unitAttrCenter.TakeDamage(new AttackPack(damage, DamageType.Melee), source ?? new BattleDamageSource(BattleDamageKind.Collision));
            BattleScene.Ins.UM.logPanel.PlayerLog(
                $"{piece.pieceData.pieceName} 受到碰撞伤害 <color=red>{damage}</color>！");
            Debug.Log($"<color=red>{piece.name} 受到碰撞伤害{damage}！</color>");
        }
    }


    public void CheckAllArea()
    {
        for (var index = areaList.Count - 1; index >= 0; index--)
        {
            var area = areaList[index];
            area.AddBuff();
            area.turnsDuration--;
            if (area.turnsDuration <= 0)
            {
                area.gameObject.SetActive(false);
                areaList.RemoveAt(index);
            }
        }
    }

    // 存储延时技能
    private PieceController _delaySkillCaster;
    private SkillPack _delaySkillPack;
    private BattleActionContext delayAction; // 注册时捕获根行动，跨回合延迟不读取届时其他角色的上下文。
    private Vector3 _delaySkillTargetPos;
    private GameObject _delaySkillEffectObj;

    public void RestoreDelaySkill(PieceController caster, SkillPack skillPack
        , Vector3 targetPos = default)
    {
        // 存储技能数据，等待回合结束时触发攻击
        Debug.Log($"存储延时技能效果{skillPack.skillName}, 位置{targetPos}，将在回合结束时触发");
        delayAction = new BattleActionContext(caster, skillPack);
        _delaySkillCaster = caster;
        _delaySkillPack = skillPack;
        _delaySkillTargetPos = targetPos;

        if (skillPack.rangeType == RangeType.Grenade) // 爆炸范围锁定
        {
            // 生成一个标记物显示爆炸位置
            _delaySkillEffectObj =
                ObjectPool.Ins.GenerateObject(ItemType.SKILL_AREA, targetPos, Quaternion.identity);
            _delaySkillEffectObj.transform.localScale =
                skillPack.explodeRadius * 1f / 11f * Vector3.one;
        }
    }

    // 处理临时效果
    public void HandleDelaySkill()
    {
        if (_delaySkillPack != null && _delaySkillCaster != null)
        {
            Debug.Log($"处理延时技能效果{_delaySkillPack.skillName}");
            float explodeRadius = _delaySkillPack.explodeRadius;
            // 检测球体范围内的所有敌人
            Collider[] hitColliders =
                Physics.OverlapSphere(_delaySkillTargetPos, explodeRadius);
            List<PieceController> newTargets = new();
            foreach (var collider in hitColliders)
            {
                PieceController piece = collider.transform.GetComponent<PieceController>();
                if (piece == null) continue;
                if (!piece.gameObject.activeInHierarchy) continue;
                newTargets.Add(piece);
            }

            if (_delaySkillTargetPos != null && _delaySkillPack.skillVFXType != 0)
            {
                ObjectPool.Ins.GenerateObject(
                    _delaySkillPack.skillVFXType,
                    _delaySkillTargetPos + Vector3.up * 3f,
                    Quaternion.identity);
            }

            using (delayAction?.Enter())
                PieceSkill(_delaySkillCaster, newTargets, _delaySkillPack, _delaySkillTargetPos, ActionType.Skill);
            if (_delaySkillEffectObj != null) _delaySkillEffectObj.SetActive(false);
            _delaySkillPack = null;
            delayAction = null;
            _delaySkillCaster = null;
        }
    }

    [SerializeField] private Material gray;
    [SerializeField] private Material grayEnemy;

    /// <summary>
    /// 聚能状态下的特殊视觉效果（如屏幕变灰）
    /// </summary>
    // public void ShowBurstGray(bool option)
    // {
    //     if (gray != null && grayEnemy != null)
    //     {
    //         // 获取场景中所有Renderer
    //         var renderers = FindObjectsOfType<Renderer>();
    //         foreach (var renderer in renderers)
    //         {
    //             foreach (var mat in renderer.sharedMaterials)
    //             {
    //                 if (mat == null) continue;
    //
    //                 if (option)
    //                     mat.EnableKeyword("GREYSCALE_ON");
    //                 else
    //                     mat.DisableKeyword("GREYSCALE_ON");
    //             }
    //         }
    //
    //         Debug.Log("设置灰色滤镜: " + option);
    //     }
    // }
    /// <summary>用渲染器实例副本覆盖场景灰度，选中角色保留彩色；退出恢复原材质且不改共享资产。</summary>
    public void ShowBurstGray(bool option)
    {
        if (!option) { RestoreBurstVisuals(); return; }
        burstGrayVisible = true;
        RefreshBurstVisuals();
    }

    // 或者在程序退出时
    void OnApplicationQuit()
    {
        ShowBurstGray(false);
    }

    // ====== 移动判定 =========//
    /// <summary>
    /// 移动检测结果数据
    /// </summary>
    public class MoveResult
    {
        public Vector3 FinalPosition; // 最终可达到的位置
        public bool IsCollided; // 路径中是否发生了碰撞
        public List<PieceController> HitPieces = new List<PieceController>(); // 碰撞到的目标列表

        public MoveResult(Vector3 finalPosition)
        {
            FinalPosition = finalPosition;
            IsCollided = false;
        }
    }

    /// <summary>
    /// 计算有效的移动位置
    /// </summary>
    /// <param name="origin">起始点</param>
    /// <param name="direction">移动方向（已归一化）</param>
    /// <param name="maxDistance">最大移动距离</param>
    /// <param name="selfObj">调用者自身，用于排除碰撞</param>
    /// <returns>包含终点和碰撞信息的 MoveResult</returns>
    public MoveResult CalculateValidMovePos(Vector3 origin, Vector3 direction, float maxDistance
        , GameObject selfObj, bool ignorePieces = false)
    {
        MoveResult result = new MoveResult(origin);
        float step = 0.5f;
        Vector3 lastValidPos = origin;

        // 确保方向忽略Y轴
        direction.y = 0;
        direction.Normalize();

        for (float i = step; i <= maxDistance; i += step)
        {
            Vector3 nextStepPos = origin + direction * i;

            // 1. 前向障碍物/单位检测 (从高空向下探测，适配不平整地面)
            Ray forwardRay = new Ray(nextStepPos + Vector3.up * 50f, Vector3.down);
            if (Physics.Raycast(forwardRay, out RaycastHit wallHit, 100f))
            {
                // 判定是否撞到墙壁或非自身的棋子
                bool isWall = wallHit.collider.CompareTag("Wall");
                bool isPiece = !ignorePieces && wallHit.collider.CompareTag("Piece") &&
                               wallHit.collider.gameObject != selfObj;

                if (isWall || isPiece)
                {
                    if (isPiece)
                    {
                        PieceController pc = wallHit.collider.GetComponent<PieceController>();
                        if (pc != null && !result.HitPieces.Contains(pc))
                        {
                            result.HitPieces.Add(pc);
                        }
                    }

                    Debug.Log($"检测到碰撞: {wallHit.collider.name} at {wallHit.point}");

                    // 记录碰撞并计算最终停留点（向后微调 0.2f 避免模型穿插）
                    //result.FinalPosition = wallHit.point - direction * 0.2f;
                    result.FinalPosition = lastValidPos;
                    result.IsCollided = true;
                    break;
                }
            }

            // 2. 地面检测 (垂直向下检测路径是否合法)
            Ray groundRay = new Ray(nextStepPos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit groundHit, 30f
                    , LayerMask.GetMask("Ground", "Wall"))) //
            {
                if (groundHit.collider.CompareTag("Ground"))
                {
                    lastValidPos = new Vector3(groundHit.point.x, origin.y, groundHit.point.z);
                }
                else if (groundHit.collider.gameObject != selfObj)
                {
                    // 检测到非地面物体（如装饰物或阻挡层）
                    result.IsCollided = true;
                    break;
                }
            }
            else
            {
                // 虚空，停止移动
                result.IsCollided = true;
                break;
            }

            // 如果循环到最后一步仍正常，更新位置
            result.FinalPosition = lastValidPos;
        }

        // 补偿：如果循环因为步长未开始或初始就在边缘，确保有默认值
        if (result.FinalPosition == Vector3.zero && !result.IsCollided)
            result.FinalPosition = origin;

        return result;
    }


    /// <summary>读取攻击射线及保护距离内的最高等级掩体，与预览和 AI 共用；不依赖槽位绑定。</summary>
    public CaverSlot CheckCoverObstruction(PieceController attacker, PieceController target) =>
        attacker == null ? null : BattleCoverQuery.Find(attacker.transform.position, target);

    // ===== 夹击判定 ========== //

    /// <summary>命中后排队判定夹击；等待实际落点与碰撞后选择最近队友，同根同队友同目标只反应一次。</summary>
    public void CheckFlankAttack(PieceController attacker, PieceController target)
    {
        if (PlayerController.isBursting && attacker != null &&
            TryDeferBurstEffect(attacker, () => CheckFlankAttack(attacker, target))) return;
        if (flankReactionExecuting || !CanReact(attacker) || !CanReact(target) ||
            !BattleRelations.CanAttack(attacker, target)) return;
        var parent = BattleActionContext.Current ?? new BattleActionContext(attacker);
        EnqueueReaction($"flank-check:{attacker.GetInstanceID()}:{target.GetInstanceID()}", parent,
            () => ResolveFlankAttack(attacker, target));
    }

    private IEnumerator ResolveFlankAttack(PieceController attacker, PieceController target)
    {
        if (!CanReact(attacker) || !CanReact(target) || !BattleRelations.CanAttack(attacker, target)) yield break;

        // 获取攻击者的队友列表
        IEnumerable<PieceController> teammates = SpeedParticipants().Where(p => BattleRelations.CanSupport(attacker, p));

        PieceController closestPartner = null;
        float minDistance = float.MaxValue; // 初始化一个极大值用于比对

        foreach (var partner in teammates)
        {
            // 排除：自身、空引用、已死亡的队友
            if (partner == attacker || !CanReact(partner) || !BattleRelations.CanAttack(partner, target) || partner.pieceData.MeleeAttack == null) continue;
            if (!partner.ableStrick) continue; // 排除：无法夹击的队友

            // 如果是敌方单位，还需确保其已激活
            if (partner is EnemyController enemy && !enemy.isActived) continue;

            // 1. 判定目标是否在该队友的攻击范围内
            if (IsTargetInAttackRange(partner, target))
            {
                // 2. 计算该队友与【受击目标】之间的物理距离
                float currentDistance =
                    Vector3.Distance(partner.transform.position, target.transform.position);

                // 3. 筛选出距离最近的队友
                if (currentDistance < minDistance)
                {
                    minDistance = currentDistance;
                    closestPartner = partner;
                }
            }
        }

        // ====== 循环结束后，只有最近的那个队友触发夹击 ======
        if (closestPartner != null)
        {
            Debug.Log(
                $"【夹击触发】最近的队友 {closestPartner.pieceData.pieceName} 对 {target.pieceData.pieceName} 发动协同夹击！（距离：{minDistance:F2}米）");

            var chain = BattleActionContext.Current.ReactionChain;
            if (!chain.Keys.Add($"flank:{closestPartner.GetInstanceID()}:{target.GetInstanceID()}")) yield break;
            if (!BuffManager.CanTarget(closestPartner, target) ||
                closestPartner.unitAttrCenter.GetBuffStacks(BuffType.Stun) != 0) yield break;
            GM.Ins.AM.PlayAudio(AudioCueType.PincerTrigger);
            ObjectPool.Ins.GenerateObject(ItemType.PincerAttackFx,
                closestPartner.transform.position + Vector3.up * 1.2f, Quaternion.identity);
            flankReactionExecuting = true;
            closestPartner.ableStrick = false;
            try
            {
                reactionActor = closestPartner;
                if (closestPartner.isPlayerPiece) closestPartner.CastNormalAttack(target);
                else
                {
                    closestPartner.StartNormalAttack();
                    ((EnemyController)closestPartner).CastAttackOnTarget(target, isMemeReaction: true);
                }
                while (closestPartner != null && closestPartner.IsPerformingAction) yield return null;
                tutorialManager.TriggerFirstTutorial(FirstTutorialType.FirstStrick);
            }
            finally { flankReactionExecuting = false; reactionActor = null; }
        }
    }

    private void OnDisable()
    {
        CancelRecognitionSettlements();
        CancelBattleStartup();
        startupInitialized = false;
        startupRequested = false;
        CancelBattleReactions();
        _timedMemeStatuses?.ClearAll();
        ClearNetworkConnections();
    }

    /// <summary>
    /// 辅助方法：判定目标棋子是否在指定棋子的攻击范围内
    /// </summary>
    private bool IsTargetInAttackRange(PieceController checker, PieceController target)
    {
        float distance = Vector3.Distance(checker.transform.position, target.transform.position);
        return distance <= checker.pieceData.MeleeAttack.rangeValue;
    }


    /// <summary>胜利继续入口：校验返回场景、一次性提交奖励，再保存继承状态并进入原离场流程。</summary>
    public void OnClickQuitBattle()
    {
        if (leavingBattle || LastOutcome != BattleOutcome.PlayerVictory || GM.Ins == null || !GM.Ins.CanLoadScene("Playing")) return;
        leavingBattle = true;
        CommitBattleRewards();
        BattleScene.Ins.UM.PrepareBattleTransition();

        CancelBattleStartup();
        CancelBattleReactions();
        _timedMemeStatuses?.ClearAll();
        ClearNetworkConnections();
        // 保存所有棋子状态保存到存档内
        if (GM.Ins.pieceHPInherit)
        {
            for (int i = 0; i < 3; i++)
            {
                var playerPiece = PlayerController.pieces[i];
                if (playerPiece != null)
                {
                    GM.Ins.PLAYERPROFILE.SetPlayer(i,
                        playerPiece.unitAttrCenter.CurHealth > 0
                            ? playerPiece.unitAttrCenter.CurHealth
                            : -1,
                        playerPiece.unitAttrCenter.AmmoCount,
                        playerPiece.unitAttrCenter.ManaPoint);
                }
            }
        }

        // 退出战斗，返回主界面
        GM.Ins.BattleEnd();
    }

    // ==== 召唤物行动 ===== //

    // 所有召唤物依次行动
    private IEnumerator SummonsActionPhaseRoutine()
    {
        foreach (var summon in summonPieces)
        {
            if (summon != null && summon.gameObject.activeInHierarchy)
            {
                SelfExploreSummonPiece summonPiece = summon.GetComponent<SelfExploreSummonPiece>();
                if (summonPiece != null)
                {
                    Debug.Log($"召唤物 {summon.name} 开始行动！");
                    summonPiece.ExecuteAI();
                }
            }

            yield return new WaitForSeconds(3f);
        }
    }
}
