using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public AIController AIController;
    public PlayerController PlayerController;
    public CameraController camera;
    public BuffManager buffManager;

    public PieceDataListSO pieceDataListSO;

    public List<LadderArea> ladderAreas = new();
    
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

        CheckAllLadderMove(PlayerController.isInTurn);
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

    public void PieceAttack(PieceController attacker, PieceController defender
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
    }

    public void PieceSkill(PieceController attacker, List<PieceController> targets
        , SkillPack skillPack)
    {
        foreach (var target in targets)
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
                return;
            }

            int addAtk =
                attacker.unitAttrCenter.attr.GetAddDamage(target.unitAttrCenter.elementType);
            foreach (var attackPack in skillPack.attackPacks)
            {
                int realDamage = attackPack.damage + addAtk;
                int armor = target.unitAttrCenter.attr.GetArmor(attackPack.damageType);
                if (attackPack.damageType == DamageType.Melee)
                {
                    armor = (int)(armor*(1f - target.unitAttrCenter.buffAttrDic[BuffAttrType.MeleeArmorPercent]/100f));
                }
                realDamage -= armor;
                // 减伤
                realDamage = (int)(realDamage
                    * (100 - attacker.unitAttrCenter.buffAttrDic[
                        BuffAttrType.DamageIncrease]) / 100f * // 伤害增加
                    (100 - target.unitAttrCenter.buffAttrDic[
                        BuffAttrType.DamageReduction]) / 100f);// 伤害减免
                if (realDamage < 0) realDamage = 0;
                // TODO: 临时护盾功能
                target.unitAttrCenter.TakeDamage(new AttackPack(realDamage, attackPack.damageType));

                if (target is EnemyController enemy)
                {
                    enemy.AddDamageRecord(attacker, realDamage);
                }
            }
        }
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

        if (allEnemyDead)
        {
            // 敌方棋子全灭，玩家胜利
            BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家胜利！");
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

        if (allPlayerDead)
        {
            // 我方棋子全灭，玩家失败
            BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家失败！");
            // 激活重新开始按钮
            if (BattleScene.Ins.UM.restartButton != null)
            {
                BattleScene.Ins.UM.restartButton.gameObject.SetActive(true);
            }
        }
    }

    public void CheckAllLadderMove(bool isPlayerTurn)
    {
        foreach (var ladder in ladderAreas)
        {
            ladder.StartMove(isPlayerTurn);
        }
    }


    // ===== Test ======//
    public void OnClickQuitBattle()
    {
        GM.Ins.BattleEnd();
    }
}