using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class BattleManager : MonoBehaviour
{
    public AIController AIController;
    public PlayerController PlayerController;
    public CameraController camera;
    public BuffManager buffManager;
    public SkillManager skillManager;
    public SpriteManager spriteManager;
    public DiceCheckManager diceCheckManager;

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

    public void Init()
    {
        inBattle = true;
        _turnNumber = 0;
        PlayerController.Init();
        AIController.Init();
        PlayerStart();
        ApplySetting(GM.Ins.battleSetting);
        _delaySkillPack = null;
        //gray = Resources.Load<Material>("Materials/Gray");
        //grayEnemy = Resources.Load<Material>("Materials/GrayEnemy");
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
        //BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家回合");
        _turnNumber++;
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
                     or SkillTarget.All or SkillTarget.Self)
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

                realDamage -= armor;
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
                else if (buffPack.target == SkillTarget.EnemyAll ||
                         buffPack.target == SkillTarget.Enemy)
                {
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
                if (skillPack.additionalEffects == null ||
                    !skillPack.additionalEffects.Any(e => e is HitBackEffect))
                {
                    SpaceBombEffect(skillPack, attacker, target, targetPos, true); // 去除假死
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
                BattleScene.Ins.BM.camera.FocusShake(targets[0].transform);
            }
            else
            {
                BattleScene.Ins.BM.camera.FocusTarget(targets[0].transform);
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

        BattleScene.Ins.UM.battleFinishPanel.ShowPanel(true, finishDrop, finishExp);

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
        BattleScene.Ins.UM.battleFinishPanel.ShowPanel(false);
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
                    caster.transform.position =
                        targetPos + new Vector3(-0.5f, 0, -0.5f);
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
                default:
                    break;
            }
        }
    }


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
        , GameObject selfObj)
    {
        MoveResult result = new MoveResult();
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
                bool isPiece = wallHit.collider.CompareTag("Piece") &&
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

                    // 记录碰撞并计算最终停留点（向后微调 0.2f 避免模型穿插）
                    //result.FinalPosition = wallHit.point - direction * 0.2f;
                    result.FinalPosition = lastValidPos;
                    result.IsCollided = true;
                    break;
                }
            }

            // 2. 地面检测 (垂直向下检测路径是否合法)
            Ray groundRay = new Ray(nextStepPos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit groundHit, 30f))
            {
                if (groundHit.collider.CompareTag("Ground"))
                {
                    lastValidPos = groundHit.point;
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
                        playerPiece.unitAttrCenter.CurHealth,
                        playerPiece.unitAttrCenter.AmmoCount,
                        playerPiece.unitAttrCenter.ManaPoint);
                }
            }
        }

        // 退出战斗，返回主界面
        GM.Ins.BattleEnd();
    }
}