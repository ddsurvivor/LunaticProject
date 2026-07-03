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

public class BattleManager : MonoBehaviour
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

    public PieceDataListSO pieceDataListSO;

    //public List<LadderArea> ladderAreas = new();
    public List<HealArea> areaList = new(); // 
    public FinishDrop finishDrop;
    [LabelText("战斗胜利经验值")] public int finishExp = 100;
    [LabelText("坚持回合胜利")] public int winTurnCondition = 999; // 胜利条件：在多少回合内获胜，999表示不限制

    public BattleSetController battleSetController;
    public int TunrNumber => _turnNumber;
    private int _turnNumber = 0;

    private bool inBattle = false; // 是否在战斗中，防止重复初始化

    [SerializeField] [LabelText("镜头移动等待时间")]
    float moveWaitTime = 1.0f; // 

    [SerializeField] [LabelText("镜头注视时间")] float gazeWaitTime = 0.5f; // 

    private Sequence startSequence; // 战斗开始时的镜头动画序列
    private Coroutine sweepCoroutine;

    public void Init()
    {
        inBattle = true;
        _turnNumber = 0;
        PlayerController.Init();
        AIController.Init();
        ApplySetting(GM.Ins.battleSetting); // 在完成所有棋子初始化以后，更新预设
        _delaySkillPack = null;
        //StartBattle();
        //gray = Resources.Load<Material>("Materials/Gray");
        //grayEnemy = Resources.Load<Material>("Materials/GrayEnemy");
    }

    public void StartBattle()
    {
        Debug.Log("战斗开始");
        // 初始化角色技能系统
        characterSkillManager.Init(PlayerController.pieces);

        startSequence?.Kill();
        startSequence = DOTween.Sequence();
        startSequence.AppendInterval(0.5f);

        // 触发扫视
        startSequence.AppendCallback(SweepActiveEnemies);

        startSequence.AppendInterval(CalculateTotalTime());
        startSequence.AppendCallback(() =>
        {
            if (sweepCoroutine != null)
            {
                EnterBattleSequence();
            }
        });
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

    public void ChangeTurn()
    {
        HandleDelaySkill(); // 处理回合结束时的延时技能效果
        if (PlayerController.isInTurn)
        {
            PlayerController.isInTurn = false;
            PlayerController.TurnEnd();
            DOVirtual.DelayedCall(1.5f, () =>
            {
                AIController.isInTurn = true;
                AIController.TurnStart();
            });
            //BattleScene.Ins.UM.turnPanel.ShowTurnChange("敌人回合");
            BattleScene.Ins.UM.ShowTurnChange(false);
        }
        else
        {
            PlayerStart();
        }

        //CheckAllLadderMove(PlayerController.isInTurn);
        //CheckAllArea();
    }

    public void PlayerStart()
    {
        AIController.isInTurn = false;
        PlayerController.isInTurn = true;
        AIController.TurnEnd();
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

    /*public void PieceAttack(PieceController attacker, PieceController defender
        , AttackPack attackPack)
    {
        // 命中判定
        bool isHit = false;
        if (attacker.player.isBursting)
        {
            isHit = true; // 聚能状态下必中
        }
        else
        {
            // 命中率计算公式，D100 <= (攻击方.命中率 - 防御方.闪避率)
            float hitRate = attacker.unitAttrCenter.buffAttrDic[BuffAttrType.HitRate];
            float evade = defender.unitAttrCenter.buffAttrDic[BuffAttrType.EvasionRate];
            int hitRoll = Random.Range(1, 101);
            if (hitRoll <= hitRate - evade)
            {
                isHit = true;
            }
        }

        if (isHit == false)
        {
            // 未命中
            defender.pieceDisplay.ChangeDisplayState(PieceDisplayState.Dodge, false, 0.5f);
            //BattleScene.Ins.BM.camera.FocusShake(defender.transform);
            return;
        }


        // 伤害计算公式
        int addAtk = attacker.unitAttrCenter.attr.GetAddDamage(defender.unitAttrCenter.elementType);
        int realDamage = attackPack.damage + addAtk;
        int armor = defender.unitAttrCenter.attr.GetArmor(attackPack.damageType);
        if (attackPack.damageType == DamageType.Melee)
        {
            armor = (int)(armor*(1f - defender.unitAttrCenter.buffAttrDic[BuffAttrType.MeleeArmorPercent]/100f));
        }
        realDamage -= armor;
        // 减伤
        realDamage = (int)(realDamage
            * (100 - attacker.unitAttrCenter.buffAttrDic[
                BuffAttrType.DamageIncrease]) / 100f * // 伤害增加
            (100 - defender.unitAttrCenter.buffAttrDic[
                BuffAttrType.DamageReduction]) / 100f);// 伤害减免
        if (realDamage < 0) realDamage = 0;
        // TODO: 临时护盾功能
        defender.unitAttrCenter.TakeDamage(new AttackPack(realDamage, attackPack.damageType));
        
        if (defender is EnemyController enemy)
        {
            enemy.AddDamageRecord(attacker, realDamage);
        }
    }*/

    public void PieceSkill(PieceController attacker, List<PieceController> targets
        , SkillPack skillPack, Vector3 targetPos = default, ActionType actionType = 0
        , CheckResult checkResult = CheckResult.None)
    {
        BattleScene.Ins.UM.PopSkillName(skillPack.skillName);
        List<List<DamageInfo>> damageInfoList = new();

        bool isCrit = false;
        foreach (var target in targets)
        {
            Debug.Log($"Skill Attack: Attacker={attacker.name}, Target={target.name}");

            // 楼层判定
            if (skillPack.layerSkill)
            {
                // 攻击者和被攻击者不在同一个y值则不受伤害
                if (Mathf.Abs(attacker.transform.position.y - target.transform.position.y) > 0.1f)
                {
                    Debug.Log("Skill Attack: Target is on a different layer, no damage applied");
                    return;
                }
            }

            // --- [掩体判定开始] ---
            CaverSlot activeCover = CheckCoverObstruction(attacker, target);
            int coverHitPenalty = 0;
            int coverDamageReduction = 0;

            if (activeCover != null)
            {
                // TODO: 在此处根据 activeCover.coverConfig.attribute 提取命中率和伤害修正数值
                coverHitPenalty = activeCover.evadeChance; // 示例：直接使用掩体的闪避率作为命中率惩罚
                coverDamageReduction = activeCover.damageReduction;
                Debug.Log($"掩体生效！命中率惩罚={coverHitPenalty}%，伤害减免={coverDamageReduction}%");
                SpriteEffectPlayer shieldEffect
                    = ObjectPool.Ins.GenerateObject(ItemType.SHIELD, target.transform.position
                            , Quaternion.identity)
                        .GetComponent<SpriteEffectPlayer>();
            }
            else
            {
                //Debug.Log("没有掩体，正常攻击");
            }
            // --- [掩体判定结束] ---

            // 命中判定
            bool isHit = false;
            if (attacker.player != null && attacker.player.isBursting)
            {
                isHit = true; // 聚能状态下必中
            }
            else if (skillPack.target is SkillTarget.EnemyAll
                     or SkillTarget.All or SkillTarget.Self or SkillTarget.AllyBody or SkillTarget.Ally)
            {
                isHit = true; // AOE必中
            }
            else
            {
                // 命中率计算公式，D100 <= (攻击方.命中率 - 防御方.闪避率)
                float hitRate = attacker.unitAttrCenter.buffAttrDic[BuffAttrType.HitRate];
                float evade = target.unitAttrCenter.buffAttrDic[BuffAttrType.EvasionRate];
                int hitRoll = Random.Range(1, 101);
                if (hitRoll <= (hitRate - evade - coverHitPenalty))
                {
                    isHit = true;
                }
                //Debug.Log($"Skill Attack: HitRoll={hitRoll}, HitRate={hitRate}, Evade={evade}, IsHit={isHit}");
            }

            if (isHit == false)
            {
                // 未命中
                target.pieceDisplay.ChangeDisplayState(PieceDisplayState.Dodge, false, 0.5f);
                BattleScene.Ins.BM.tipTextManager.ShowMiss(target.transform);
                //BattleScene.Ins.BM.camera.FocusShake(defender.transform);
                Debug.Log("Skill Attack: Missed");
                return;
            }

            // 暴击判定
            if (Random.Range(1, 101) <= attacker.unitAttrCenter.critRate)
            {
                isCrit = true;
            }

            // 模式识别判定
            float damageModifier = 1f;
            if (skillPack.isRecognitionCheck)
            {
                switch (checkResult)
                {
                    case CheckResult.DamageReduced:
                        damageModifier = 0.4f;
                        break;
                    case CheckResult.DamageIncreased:
                        damageModifier = 1.3f;
                        break;
                    case CheckResult.MustCrit:
                        damageModifier = 1.3f;
                        isCrit = true;
                        break;
                    case CheckResult.None:
                        damageModifier = 1f;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Debug.Log($"模式识别检定结果: {checkResult}, 伤害修正={damageModifier}, 必定暴击={isCrit}");
            }


            int addAtk =
                attacker.unitAttrCenter.attr.GetAddDamage(target.unitAttrCenter.elementType) +
                attacker.unitAttrCenter.ATK;
            List<DamageInfo> damageInfos = new();
            foreach (var attackPack in skillPack.attackPacks)
            {
                Debug.Log($"依次计算伤害");

                /*
                 // 旧伤害函数：伤害 = 基础伤害 - 护甲
                 int realDamage = attackPack.damage;
                
                if (coverDamageReduction > 0)
                    realDamage = (int)(realDamage * (100 - coverDamageReduction) / 100f); // 掩体伤害减免
                if (isCrit)
                    realDamage =
                        (int)(realDamage *
                              (attacker.unitAttrCenter.critDamageRate / 100f)); // 暴击伤害增加
                int armor = target.unitAttrCenter.attr.GetArmor(attackPack.damageType);
                if (attackPack.damageType == DamageType.Melee)
                {
                    armor = (int)(armor * (1f -
                                           target.unitAttrCenter.buffAttrDic[
                                               BuffAttrType.MeleeArmorPercent] / 100f));
                }

                realDamage -= armor;*/
                int realDamage = attackPack.damage;
                if (coverDamageReduction > 0)
                    realDamage = (int)(realDamage * (100 - coverDamageReduction) / 100f); // 掩体伤害减免
                int armor = target.unitAttrCenter.attr.GetArmor(attackPack.damageType);
                if (attackPack.damageType == DamageType.Melee)
                {
                    armor = (int)(armor * (1f -
                                           target.unitAttrCenter.buffAttrDic[
                                               BuffAttrType.MeleeArmorPercent] / 100f));
                }

                realDamage = DamageCalculator.CalculateActualDamage(realDamage, armor
                    , isCrit, attacker.unitAttrCenter.critDamageRate);
                // 减伤
                realDamage = (int)(realDamage
                                   * (100 + attacker.unitAttrCenter.buffAttrDic[
                                       BuffAttrType.DamageIncrease]) / 100f * // 伤害增加
                                   (100 - target.unitAttrCenter.buffAttrDic[
                                       BuffAttrType.DamageReduction]) / 100f // 伤害减免
                                   * damageModifier);

                // 聚能伤害
                if (attacker.player != null && attacker.player.isBursting)
                {
                    realDamage = attacker.player.AddBurstDamage(target, realDamage);
                }

                if (realDamage < 0) realDamage = 0;
                Debug.Log(
                    $"Skill Attack: BaseDamage={attackPack.damage}, AddAtk={addAtk}, Armor={armor}, RealDamage={realDamage}");
                // TODO: 临时护盾功能
                target.unitAttrCenter.TakeDamage(new AttackPack(realDamage, attackPack.damageType
                    , isCrit));
                BattleScene.Ins.BM.characterSkillManager.NotifyTakeDamage(target.gameObject
                    , attacker.gameObject);

                if (target.unitAttrCenter.CurHealth <= 0)
                {
                    // 触发击杀
                    BattleScene.Ins.BM.characterSkillManager.NotifyKillEnemy(attacker.gameObject
                        , target.gameObject);
                }

                if (target is EnemyController enemy)
                {
                    enemy.AddDamageRecord(attacker, realDamage);
                }

                if (realDamage > 0)
                {
                    damageInfos.Add(new DamageInfo(realDamage, attackPack.damageType.ToChinese()
                        , isCrit));
                }
            }

            damageInfoList.Add(damageInfos);


            // 处理buff
            foreach (var buffPack in skillPack.buffPacks)
            {
                if (buffPack.target == SkillTarget.Self)
                {
                    if (GameConst.CheckRate(buffPack.rate))
                    {
                        buffManager.AddBuff(attacker.unitAttrCenter, buffPack.buffType
                            , buffPack.stacks);
                    }
                }
                else if (buffPack.target == SkillTarget.All)
                {
                    if (GameConst.CheckRate(buffPack.rate))
                    {
                        buffManager.AddBuff(target.unitAttrCenter, buffPack.buffType
                            , buffPack.stacks);
                    }
                }
                else if (buffPack.target == SkillTarget.EnemyAll ||
                         buffPack.target == SkillTarget.Enemy)
                {
                    if (target.isPlayerPiece) continue; // 友军不受敌方buff影响
                    if (GameConst.CheckRate(buffPack.rate))
                    {
                        buffManager.AddBuff(target.unitAttrCenter, buffPack.buffType
                            , buffPack.stacks);
                    }
                }
                else if (buffPack.target == SkillTarget.Ally)
                {
                    if (!target.isPlayerPiece) continue; // 敌军不受友方buff影响
                    if (GameConst.CheckRate(buffPack.rate))
                    {
                        buffManager.AddBuff(target.unitAttrCenter, buffPack.buffType
                            , buffPack.stacks);
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

        // 操作记录系统
        BattleScene.Ins.UM.logPanel.PlayerLogAttack(attacker.pieceData.pieceName,
            actionType == 0 ? skillPack.skillName : actionType.ToString(),
            targets.Select(t => t.pieceData.pieceName).ToList(),
            damageInfoList
        );
        if (targets.Count > 0)
        {
            if (isCrit)
            {
                BattleScene.Ins.BM.cameraController.FocusShake(targets[0].transform);
            }
            else
            {
                BattleScene.Ins.BM.cameraController.FocusTarget(targets[0].transform);
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

        //BattleScene.Ins.BM.camera.FocusShake(targets[0].transform);
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

            PieceSkill(_delaySkillCaster, newTargets, _delaySkillPack, _delaySkillTargetPos);
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


    /// <summary>
    /// 扩展方法：在战斗开始时扫视所有已经激活的敌人（参数内嵌版）
    /// </summary>
    /// <param name="battleManager">BattleManager 实例</param>
    /// <param name="gameCamera">镜头控制组件</param>
    public void SweepActiveEnemies()
    {
        if (cameraController == null)
        {
            Debug.LogError("[BattleExtension] 扫视初始化失败：BattleManager 或 Camera 为空。");
            return;
        }

        // ====== 新增：开始扫视时激活跳过按钮 ======

        BattleScene.Ins.UM.skipButton?.gameObject.SetActive(true);

        // =========================================

        sweepCoroutine = StartCoroutine(SweepRoutine(moveWaitTime, gazeWaitTime));
    }

    private float CalculateTotalTime()
    {
        float totalTime = 0f;
        foreach (EnemyController enemy in AIController.pieces)
        {
            if (enemy != null && enemy.isActived)
            {
                totalTime += moveWaitTime + gazeWaitTime;
            }
        }

        return totalTime;
    }

    private IEnumerator SweepRoutine(float moveWaitTime, float gazeWaitTime)
    {
        if (AIController.pieces == null || AIController.pieces.Count == 0)
        {
            Debug.LogWarning("[BattleExtension] AIController.pieces 为空，跳过扫视。");
            yield break;
        }

        Debug.Log("【战前扫视】开始...");

        foreach (EnemyController enemy in AIController.pieces)
        {
            if (enemy != null && enemy.isActived)
            {
                // 1. 镜头追踪当前敌人
                cameraController.SetFollow(enemy.transform);

                // 2. 等待镜头移动到位
                yield return new WaitForSeconds(moveWaitTime);

                // 3. 镜头注视停留
                yield return new WaitForSeconds(gazeWaitTime);
            }
        }

        Debug.Log("【战前扫视】结束。");

        // 回调主管理器的开战函数
    }

    /// <summary>
    /// 提取出的后续核心战斗步骤
    /// </summary>
    private void EnterBattleSequence()
    {
        // ====== 新增：进入战斗阶段时，关闭跳过按钮 ======
        BattleScene.Ins.UM.skipButton?.gameObject.SetActive(false);
        // =============================================

        if (sweepCoroutine != null)
        {
            StopCoroutine(sweepCoroutine);
            sweepCoroutine = null;
        }

        startSequence?.Kill();

        if (tutorialManager.CheckAndShowTutorial())
        {
        }
        else
        {
            battleDialogueManager.TriggerBattleStart();
            PlayerStart();
        }
    }

    /// <summary>
    /// 跳过战前扫视，直接进入战斗
    /// </summary>
    public void OnClickSkip()
    {
        if (sweepCoroutine != null)
        {
            StopCoroutine(sweepCoroutine);
            Debug.Log("【战前扫视】玩家选择跳过。");

            // 强行把相机拉回玩家棋子
            if (PlayerController.pieces != null && PlayerController.pieces.Count > 0)
            {
                var firstPlayer =
                    PlayerController.pieces.FirstOrDefault(p => p != null && !p.isDead);
                if (firstPlayer != null)
                {
                    cameraController.SetFollow(firstPlayer.transform);
                }
            }

            // 直接执行后续（内部会自动关闭按钮）
            EnterBattleSequence();
        }
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
        if(attacker.isPlayerPiece == target.isPlayerPiece) return;// 排除：攻击者与目标属于同一阵营

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
            try
            {
                // 开启防重入锁：此时由这个协同攻击造成的任何伤害/击退，再次调用 CheckFlankAttack 时都会在开头被 return
                _isFlankAttacking = true;
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    // 触发协同普攻
                    if (closestPartner.isPlayerPiece)
                    {
                        Debug.Log(
                            $"【协同普攻】{closestPartner.pieceData.pieceName} 对 {target.pieceData.pieceName} 发动协同普攻！");
                        closestPartner.CastNormalAttack(target);
                        closestPartner.ableStrick = false;
                    }
                    else
                    {
                        closestPartner.StartNormalAttack();
                        ((EnemyController)closestPartner).CastAttackOnTarget(target);
                        closestPartner.ableStrick = false;
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"夹击协同攻击触发异常：{e.Message}");
            }
            finally
            {
                DOVirtual.DelayedCall(1f, () => { _isFlankAttacking = false; });
            }
        }
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
}