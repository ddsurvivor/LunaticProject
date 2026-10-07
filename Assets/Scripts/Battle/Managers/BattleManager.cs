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
            inBattle = true;
            _turnNumber = 0;
            characterSkillManager.ClearAllRegistry();
            PlayerController.Init();
            AIController.Init();
            ApplySetting(GM.Ins.battleSetting); // 在完成所有棋子初始化以后，更新预设
            _delaySkillPack = null;
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
    public bool CanInspectEnemyTargets => inBattle && startupPhase == StartupPhase.Active && PlayerController.isInTurn;

    public bool HasCombatPresentation => _isFlankAttacking ||
        HasPresentation(PlayerController.pieces) || HasPresentation(AIController.pieces) || HasPresentation(summonPieces);

    private static bool HasPresentation(IEnumerable<PieceController> pieces)
    {
        foreach (var piece in pieces)
            if (piece != null && piece.gameObject.activeInHierarchy &&
                (piece.IsPerformingAction || (piece.pieceDisplay != null && piece.pieceDisplay.IsPlayingOneShot))) return true;
        return false;
    }

    public void ChangeTurn()
    {
        if (HasCombatPresentation) return;
        BattleScene.Ins?.CM?.ClearEnemyTargetLineInteraction();
        HandleDelaySkill(); // 处理回合结束时的延时技能效果
        if (PlayerController.isInTurn)
        {
            PlayerController.isInTurn = false;
            PlayerController.TurnEnd();

            float delayTime = 0.5f;
            if (summonPieces.Count > 0)
            {
                delayTime += summonPieces.Count * 3f;
            }

            StartCoroutine(SummonsActionPhaseRoutine());
            DOVirtual.DelayedCall(delayTime, () =>
            {
                BattleScene.Ins.UM.ShowTurnChange(false);
                AIController.isInTurn = true;
                AIController.TurnStart();
            });
            //BattleScene.Ins.UM.turnPanel.ShowTurnChange("敌人回合");
        }
        else
        {
            PlayerStart(false);
        }

        //CheckAllLadderMove(PlayerController.isInTurn);
        //CheckAllArea();
    }

    public void PlayerStart(bool firstTurn = true)
    {
        AIController.isInTurn = false;
        PlayerController.isInTurn = true;
        if(!firstTurn) AIController.TurnEnd();
        PlayerController.TurnStart();
        BattleScene.Ins.UM.endTurnButton.enabled = true;
        BattleScene.Ins.UM.ShowTurnChange(true);
        orderManager.ClearAll(true); //取消所有警戒状态
        //BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家回合");
        _turnNumber++;
        battleDialogueManager.TriggerTurnNumStart(_turnNumber);
        BattleScene.Ins.UM.turnNumberText.text = TunrNumber.ToString();
        // 胜利条件：坚持回合数
        if (TunrNumber >= winTurnCondition)
        {
            Debug.Log($"达到胜利回合数{TunrNumber}，玩家胜利");
            PlayerWin();
        }
    }

    public void PieceSkill(PieceController attacker, List<PieceController> targets
        , SkillPack skillPack, Vector3 targetPos = default, ActionType actionType = 0
        , CheckResult checkResult = CheckResult.None, bool isFlank = false)
    {
        if (attacker == null || attacker.isDead || skillPack == null || targets == null) return;
        List<List<DamageInfo>> damageInfoList = new();
        var validTargets = SkillTargeting.Filter(attacker, targets, skillPack, targetPos);
        // 每次施法只消费一次攻击被动，全部目标与伤害段共享本次倍率。
        var hostileTarget = validTargets.FirstOrDefault(t => !t.isDead && t.isPlayerPiece != attacker.isPlayerPiece);
        float passiveMultiplier = skillPack.attackPacks.Count > 0 && hostileTarget != null
            ? characterSkillManager.EvaluateDamageMultiplier(attacker.gameObject, hostileTarget.gameObject) : 1f;
        bool isCrit = false;
        var hitTargets = new List<PieceController>();
        foreach (var target in validTargets)
        {
            // 1. 伤害管理器统一处理目标校验、命中、暴击与各段扣血。
            var settlement = damageManager.ResolveSkillTarget(attacker, target, skillPack, checkResult, isFlank, passiveMultiplier);
            var damageInfos = settlement.DamageInfos;
            damageInfoList.Add(damageInfos);
            isCrit |= settlement.IsCritical;
            if (settlement.HitAttack && target.isPlayerPiece != attacker.isPlayerPiece) hitTargets.Add(target);

            // 2. 无效目标、未命中或屏障阻挡时，只跳过当前目标的附加效果。
            if (!settlement.CanApplyEffects) continue;

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
                    if (target.isPlayerPiece == attacker.isPlayerPiece) continue;
                    if (GameConst.CheckRate(buffPack.rate))
                    {
                        buffManager.AddBuff(target.unitAttrCenter, buffPack.buffType
                            , buffPack.stacks, buffPack.barrierHealth, buffPack.retaliationTurns);
                    }
                }
                else if (buffPack.target == SkillTarget.Ally || buffPack.target == SkillTarget.AllyBody)
                {
                    if (target.isPlayerPiece != attacker.isPlayerPiece) continue;
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
            if (attacker.player != null && attacker.player.isBursting) // 聚能状态下所有攻击附加击退效果
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

            // 判定夹击
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
            actionType == 0 ? skillPack.skillName : actionType.ToString(),
            validTargets.Select(t => t.pieceData.pieceName).ToList(),
            damageInfoList
        );
        if (validTargets.Count > 0)
        {
            if (isCrit)
            {
                BattleScene.Ins.BM.cameraController.FocusShake(validTargets[0].transform,attacker.transform);
            }
            else
            {
                BattleScene.Ins.BM.cameraController.FocusTarget(validTargets[0].transform,attacker.transform);
            }
        }

        // 处理聚能充能效果，多段伤害只充能一次
        if (attacker.isPlayerPiece)
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

    public void PlayerWin()
    {
        BattleScene.Ins?.CM?.ClearEnemyTargetLineInteraction();
        CancelBattleStartup();
        inBattle = false;
        AIController.isInTurn = false;
        // 敌方棋子全灭，玩家胜利
        //BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家胜利！");
        // 处理胜利事件
        if (finishDrop != null) finishDrop.DropItems();
        if (finishExp > 0)
        {
            foreach (var player in GM.Ins.PLAYERPROFILE.player)
            {
                if (player != null && player.HP > 0)
                {
                    player.AccessAttribute(22, AttrOp.Add, finishExp);
                }
            }
        }

        BattleScene.Ins.UM.battleStartUIPanel.PlayBattleStartAnimation(1);

        DOVirtual.DelayedCall(1.0f
            , () =>
            {
                OnClickQuitBattle();
                //BattleScene.Ins.UM.battleFinishPanel.ShowPanel(true, finishDrop, finishExp);
            });
        PlayerController.EndBurstMode(); // 结束聚能状态
        // 延迟后退出战斗
        //DOVirtual.DelayedCall(1.0f, () => { BattleScene.Ins.BM.OnClickQuitBattle(); });
    }

    public void PlayerLoss()
    {
        BattleScene.Ins?.CM?.ClearEnemyTargetLineInteraction();
        CancelBattleStartup();
        inBattle = false;
        AIController.isInTurn = false;
        Debug.Log("我方棋子全灭，玩家失败");
        // 我方棋子全灭，玩家失败
        BattleScene.Ins.UM.battleLossPanel.gameObject.SetActive(true);
        //BattleScene.Ins.UM.battleFinishPanel.ShowPanel(false);
        //BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家失败！");
        // 激活重新开始按钮
        if (BattleScene.Ins.UM.restartButton != null)
        {
            BattleScene.Ins.UM.restartButton.gameObject.SetActive(true);
        }
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
        if (target == null || target.ableMove == false) return;

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
        if (agent != null)
        {
            agent.enabled = false;
        }

        // 锁定Y轴，防止击退造成高度偏差
        finalPos.y = target.transform.position.y;

        // 执行位移
        target.transform.DOMove(finalPos, 0.2f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            // =================【核心优化：位移结束同步网格】=================
            if (agent != null)
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

                TriggerCollisionDamage(hitPieces, hitBackEffect.hitBackDamage);
            }
        });
    }

    /// <summary>
    /// 触发碰撞伤害
    /// </summary>
    private void TriggerCollisionDamage(List<PieceController> hitPieces, int damage = 10)
    {
        foreach (var piece in hitPieces)
        {
            // 所有被撞到的棋子都受伤
            piece.unitAttrCenter.TakeDamage(new AttackPack(damage, DamageType.Melee));
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
    private Vector3 _delaySkillTargetPos;
    private GameObject _delaySkillEffectObj;

    public void RestoreDelaySkill(PieceController caster, SkillPack skillPack
        , Vector3 targetPos = default)
    {
        // 存储技能数据，等待回合结束时触发攻击
        Debug.Log($"存储延时技能效果{skillPack.skillName}, 位置{targetPos}，将在回合结束时触发");
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

            PieceSkill(_delaySkillCaster, newTargets, _delaySkillPack, _delaySkillTargetPos, ActionType.技能);
            _delaySkillEffectObj.SetActive(false);
            _delaySkillPack = null;
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
    public void ShowBurstGray(bool option)
    {
        // 把 gray材质上的shader里的GREYSCALE_ON属性打开，所有使用这个材质的图片就会变灰
        if (gray != null)
        {
            if (option)
            {
                // 获取场景中所有这个材质的物品


                gray.EnableKeyword("GREYSCALE_ON");
                grayEnemy.EnableKeyword("GREYSCALE_ON");
            }
            else
            {
                gray.DisableKeyword("GREYSCALE_ON");
                grayEnemy.DisableKeyword("GREYSCALE_ON");
            }

            //Debug.Log("设置灰色滤镜: " + option);
        }
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


    // ===== 掩体判定 ========== //
    /// <summary>
    /// 判定掩体是否在攻击射线上生效
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="target">被攻击者</param>
    /// <returns>返回有效的掩体脚本，若未被遮挡则返回 null</returns>
    public CaverSlot CheckCoverObstruction(PieceController attacker, PieceController target)
    {
        // 1. 基础判定：目标当前是否有掩体引用
        if (target.CurCaverSlot == null) return null;

        // 2. 射线路径计算（从发射点到目标点，稍微抬高 y 轴模拟射击线）
        Vector3 start = attacker.transform.position + Vector3.up * 1.2f;
        Vector3 end = target.transform.position + Vector3.up * 1.2f;
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        // 3. 使用 RaycastAll 获取路径上所有的碰撞体（解决被其他物体遮挡的问题）
        RaycastHit[] hits = Physics.RaycastAll(start, direction.normalized, distance);

        foreach (var hit in hits)
        {
            // 判定击中的物体是否为目标关联的那个掩体
            if (hit.collider.gameObject == target.CurCaverSlot.gameObject)
            {
                // 找到匹配掩体，返回脚本
                return target.CurCaverSlot;
            }
        }

        return null;
    }


    // ===== 夹击判定 ========== //
    private Coroutine flankRoutine;
    private bool _isFlankAttacking = false; // 防重入锁：标记当前是否正在执行夹击结算

    /// <summary>
    /// 夹击判定：当目标受到攻击后，检查攻击者的队友是否也能触及该目标，若满足则触发协同普攻
    /// </summary>
    /// <param name="attacker">发起当前攻击的棋子</param>
    /// <param name="target">被攻击的受害者</param>
    /// 
    public void CheckFlankAttack(PieceController attacker, PieceController target)
    {
        if (_isFlankAttacking) return;
        if (target == null || attacker == null || target.unitAttrCenter.CurHealth <= 0) return;
        if (attacker.isPlayerPiece == target.isPlayerPiece) return; // 排除：攻击者与目标属于同一阵营

        // 获取攻击者的队友列表
        IEnumerable<PieceController> teammates = attacker.isPlayerPiece
            ? PlayerController.pieces
            : AIController.pieces.Cast<PieceController>();

        PieceController closestPartner = null;
        float minDistance = float.MaxValue; // 初始化一个极大值用于比对

        foreach (var partner in teammates)
        {
            // 排除：自身、空引用、已死亡的队友
            if (partner == attacker || partner == null ||
                partner.unitAttrCenter.CurHealth <= 0) continue;
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

            // 获取普攻数据
            SkillPack normalAttackPack = closestPartner.pieceData.meleeAtk;
            // 音效特效
            GM.Ins.AM.PlayAudio(AudioCueType.PincerTrigger);
            ObjectPool.Ins.GenerateObject(ItemType.PincerAttackFx
                , closestPartner.transform.position + Vector3.up * 1.2f, Quaternion.identity);
            if (normalAttackPack == null) return;
            _isFlankAttacking = true;
            closestPartner.ableStrick = false;
            flankRoutine = StartCoroutine(FlankAttackRoutine(attacker, closestPartner, target));
        }
    }

    private void OnDisable()
    {
        CancelBattleStartup();
        startupInitialized = false;
        startupRequested = false;
        if (flankRoutine != null) StopCoroutine(flankRoutine);
        flankRoutine = null;
        _isFlankAttacking = false;
    }

    private IEnumerator FlankAttackRoutine(PieceController attacker, PieceController partner, PieceController target)
    {
        try
        {
            // 等原攻击和队友当前动作收尾，再出手；锁覆盖整次夹击，含减速与弹道。
            while ((attacker != null && attacker.IsPerformingAction) ||
                   (partner != null && partner.IsPerformingAction)) yield return null;
            if (partner == null || !partner.isActiveAndEnabled || partner.isDead ||
                target == null || !target.isActiveAndEnabled || target.isDead) yield break;
            if (partner.isPlayerPiece) partner.CastNormalAttack(target);
            else
            {
                partner.StartNormalAttack();
                ((EnemyController)partner).CastAttackOnTarget(target);
            }
            while (partner != null && partner.IsPerformingAction) yield return null;
            tutorialManager.TriggerFirstTutorial(FirstTutorialType.FirstStrick);
        }
        finally { _isFlankAttacking = false; }
    }

    /// <summary>
    /// 辅助方法：判定目标棋子是否在指定棋子的攻击范围内
    /// </summary>
    private bool IsTargetInAttackRange(PieceController checker, PieceController target)
    {
        float distance = Vector3.Distance(checker.transform.position, target.transform.position);
        return distance <= checker.pieceData.meleeAtk.rangeValue;
    }


    // ===== Test ======//
    public void OnClickQuitBattle()
    {
        CancelBattleStartup();
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
