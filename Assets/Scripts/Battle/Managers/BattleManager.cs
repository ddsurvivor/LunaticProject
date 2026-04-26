using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public AIController AIController;
    public PlayerController PlayerController;
    public CameraController camera;
    public BuffManager buffManager;
    public SkillManager skillManager;
    public SpriteManager spriteManager;

    public PieceDataListSO pieceDataListSO;

    //public List<LadderArea> ladderAreas = new();
    public List<HealArea> areaList = new(); // 
    public FinishDrop finishDrop;
    [LabelText("战斗胜利经验值")] public int finishExp = 100;
    [LabelText("坚持回合胜利")]public int winTurnCondition = 999; // 胜利条件：在多少回合内获胜，999表示不限制

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
        CheckAllArea();
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
        , SkillPack skillPack, Vector3 targetPos = default, ActionType actionType = 0)
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
                if (hitRoll <= hitRate - evade)
                {
                    isHit = true;
                }
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


            int addAtk =
                attacker.unitAttrCenter.attr.GetAddDamage(target.unitAttrCenter.elementType);
            List<DamageInfo> damageInfos = new();
            foreach (var attackPack in skillPack.attackPacks)
            {
                Debug.Log($"依次计算伤害");
                int realDamage = attackPack.damage + attacker.unitAttrCenter.ATK;
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
                        BuffAttrType.DamageReduction]) / 100f); // 伤害减免
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
                    SpaceBombEffect(skillPack, attacker, target, targetPos); // 去除假死
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
        if (isCrit && targets.Count > 0) BattleScene.Ins.BM.camera.FocusShake(targets[0].transform);
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

        //Debug.Log($"判定敌人棋子全灭：{allEnemyDead}");
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
        BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家胜利！");
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

        PlayerController.EndBurstMode(); // 结束聚能状态
        // 延迟后退出战斗
        DOVirtual.DelayedCall(1.0f, () => { BattleScene.Ins.BM.OnClickQuitBattle(); });
    }

    public void PlayerLoss()
    {
        inBattle = false;
        AIController.isInTurn = false;
        Debug.Log("我方棋子全灭，玩家失败");
        // 我方棋子全灭，玩家失败
        BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家失败！");
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
        , Vector3 targetPos = default)
    {
        if (target.gameObject.activeInHierarchy)
        {
            // 检测目标是否有假死技能, 如果有且进入了假死状态，则彻底杀死
            SelfRevive revive = target.GetComponent<SelfRevive>();
            if (revive != null)
            {
                if (revive.isFakeDead)
                {
                    revive.TrueDeath();
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
                    HealArea healArea = ObjectPool.Ins.GenerateObject(ItemType.HEAL_AREA,
                        targetPos,
                        Quaternion.identity).GetComponent<HealArea>();
                    healArea.SetData(skillPack.buffPacks[0], 1);
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

    /*private void HitBackEffect(HitBackEffect hitBackEffect, PieceController caster = null
        , PieceController target = null, Vector3 targetPos = default)
    {
        if (target.ableMove == false) return;
        Vector3 dir = (target.transform.position - caster.transform.position);
        dir.y = 0;
        dir.Normalize();
        Vector3 hitBackPos = target.transform.position + dir * hitBackEffect.dis;
        target.transform.DOMove(hitBackPos, 0.2f);
    }*/
    private void HitBackEffect(HitBackEffect hitBackEffect, PieceController caster = null,
        PieceController target = null, Vector3 targetPos = default)
    {
        if (target.ableMove == false) return;

        Vector3 startPos = target.transform.position;
        Vector3 dir = (startPos - caster.transform.position);
        dir.y = 0;
        dir.Normalize();

        float totalDis = hitBackEffect.dis;
        float step = 0.5f;
        Vector3 lastValidPos = startPos;
        bool hasCollision = false; // 是否触发了碰撞

        List<PieceController> hitPieces = new List<PieceController>(); // 记录被击退路径上碰到的棋子，避免重复伤害
        for (float i = step; i <= totalDis; i += step)
        {
            Vector3 nextStepPos = startPos + dir * i;

            // 1. 前向碰撞检测 (墙壁、单位、边界)
            // 建议使用 Raycast 或 SphereCast (更厚实)
            Ray forwardRay = new Ray(nextStepPos + Vector3.up * 50f, Vector3.down);
            if (Physics.Raycast(forwardRay, out RaycastHit wallHit, 100f))
            {
                if (wallHit.collider.CompareTag("Wall") ||
                    wallHit.collider.CompareTag("Piece") &&
                    wallHit.collider.gameObject != target.gameObject)
                {
                    if (wallHit.collider.CompareTag("Piece"))
                    {
                        PieceController hitPiece = wallHit.collider.GetComponent<PieceController>();
                        if (hitPiece != null && !hitPieces.Contains(hitPiece))
                        {
                            hitPieces.Add(hitPiece);
                            Debug.Log($"{target.name} 击退时撞到了 {hitPiece.name}，造成碰撞伤害");
                        }
                    }

                    lastValidPos = wallHit.point - dir * 0.2f; // 撞到硬物，退后一点
                    hasCollision = true;
                    Debug.Log($"{target.name} 击退时撞到了 {wallHit.collider.name}");
                    break;
                }
            }

            // 2. 地面检测 (垂直向下检测)
            Ray groundRay = new Ray(nextStepPos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit groundHit, 30f))
            {
                if (groundHit.collider.CompareTag("Ground"))
                {
                    // 是合法地面，更新落点并进入下一次循环
                    lastValidPos = groundHit.point;
                }
                else if (groundHit.collider.gameObject != target.gameObject)
                {
                    // 检测到了碰撞体但不是 Ground (比如悬崖外的装饰物)，视为碰撞
                    hasCollision = true;
                    Debug.Log($"{target.name} 击退路径出现非地面物体{groundHit.collider.name}，停止移动");
                    break;
                }
            }
            else
            {
                // 完全没有检测到物体 (虚空)，视为碰撞
                hasCollision = true;
                Debug.Log($"{target.name} 击退至地图边缘/虚空，停止移动");
                break;
            }
        }

        lastValidPos.y = target.transform.position.y; // 保持原有高度，避免被击退技能弄到空中
        // 3. 执行位移
        target.transform.DOMove(lastValidPos, 0.2f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            // 4. 碰撞结果处理
            if (hasCollision)
            {
                hitPieces.Add(target); // 碰撞伤害也作用于被击退的目标自身
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