using System.Collections.Generic;
using DG.Tweening;
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

    public int TunrNumber => _turnNumber;
    private int _turnNumber = 0;

    public void Init()
    {
        _turnNumber = 0;
        PlayerController.Init();
        AIController.Init();
        PlayerStart();
    }

    public void ChangeTurn()
    {
        HandleDelaySkill(); // 处理回合结束时的延时技能效果
        if (PlayerController.isInTurn)
        {
            PlayerController.isInTurn = false;
            PlayerController.TurnEnd();
            AIController.isInTurn = true;
            AIController.TurnStart();
            BattleScene.Ins.UM.turnPanel.ShowTurnChange("敌人回合");
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
        BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家回合");
        _turnNumber++;
        BattleScene.Ins.UM.turnNumberText.text = TunrNumber.ToString();
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
        , SkillPack skillPack, Vector3 targetPos = default)
    {
        BattleScene.Ins.UM.PopSkillName(skillPack.skillName);

        foreach (var target in targets)
        {
            Debug.Log($"Skill Attack: Attacker={attacker.name}, Target={target.name}");
            // 命中判定
            bool isHit = false;
            if (attacker.player.isBursting)
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

            int addAtk =
                attacker.unitAttrCenter.attr.GetAddDamage(target.unitAttrCenter.elementType);
            foreach (var attackPack in skillPack.attackPacks)
            {
                Debug.Log($"依次计算伤害");
                int realDamage = attackPack.damage + addAtk;
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
                if (attacker.player.isBursting)
                {
                    realDamage = attacker.player.AddBurstDamage(target, realDamage);
                }

                if (realDamage < 0) realDamage = 0;
                Debug.Log(
                    $"Skill Attack: BaseDamage={attackPack.damage}, AddAtk={addAtk}, Armor={armor}, RealDamage={realDamage}");
                // TODO: 临时护盾功能
                target.unitAttrCenter.TakeDamage(new AttackPack(realDamage, attackPack.damageType));

                if (target is EnemyController enemy)
                {
                    enemy.AddDamageRecord(attacker, realDamage);
                }
            }

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
        }

        onceEffect = false;
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

        Debug.Log($"判定敌人棋子全灭：{allEnemyDead}");
        if (allEnemyDead)
        {
            // 敌方棋子全灭，玩家胜利
            BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家胜利！");
            // 处理胜利事件
            if (finishDrop != null) finishDrop.DropItems();
            // 延迟后退出战斗
            DOVirtual.DelayedCall(1.0f, () => { BattleScene.Ins.BM.OnClickQuitBattle(); });
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

        Debug.Log($"检查我方棋子全灭：{allPlayerDead}");
        if (allPlayerDead)
        {
            Debug.Log("我方棋子全灭，玩家失败");
            // 我方棋子全灭，玩家失败
            BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家失败！");
            // 激活重新开始按钮
            if (BattleScene.Ins.UM.restartButton != null)
            {
                BattleScene.Ins.UM.restartButton.gameObject.SetActive(true);
            }
        }
    }

    /*public void CheckAllLadderMove(bool isPlayerTurn)
    {
        foreach (var ladder in ladderAreas)
        {
            ladder.StartMove(isPlayerTurn);
        }
    }*/

    // 只处理一次的效果
    private bool onceEffect = false;

    public void ApplySKillEffect(SkillPack skillPack, PieceController caster = null
        , PieceController target = null
        , Vector3 targetPos = default)
    {
        foreach (var effect in skillPack.skillEffects)
        {
            switch (effect)
            {
                case SkillEffect.Blink:
                    if (onceEffect) continue;
                    onceEffect = true;
                    caster.transform.position =
                        targetPos + new Vector3(-0.5f, 0, -0.5f);
                    break;
                case SkillEffect.HealArea:
                    if (onceEffect) continue;
                    onceEffect = true;
                    Debug.Log("生成治疗区");
                    HealArea healArea = ObjectPool.Ins.GenerateObject(ItemType.HEAL_AREA,
                        targetPos,
                        Quaternion.identity).GetComponent<HealArea>();
                    healArea.SetData(skillPack.buffPacks[0], 1);
                    break;
                case SkillEffect.SpaceBomb:
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

                    break;
                default:
                    break;
            }
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
        if (_delaySkillPack != null)
        {
            float explodeRadius = _delaySkillPack.explodeRadius;
            // 检测球体范围内的所有敌人
            Collider[] hitColliders =
                Physics.OverlapSphere(_delaySkillTargetPos, explodeRadius);
            List<PieceController> newTargets = new();
            foreach (var collider in hitColliders)
            {
                PieceController piece = collider.transform.GetComponent<PieceController>();
                if (piece == null) continue;
                if(!piece.gameObject.activeInHierarchy) continue;
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
        }
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