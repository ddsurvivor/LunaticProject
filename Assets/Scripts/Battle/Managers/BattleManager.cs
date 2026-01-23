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
    


    public void Init()
    {
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
    }

    public void PlayerStart()
    {
        AIController.isInTurn = false;
        PlayerController.isInTurn = true;
        AIController.TurnEnd();
        PlayerController.TurnStart();
        BattleScene.Ins.UM.endTurnButton.enabled = true;
        BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家回合");
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


        int addAtk = attacker.unitAttrCenter.attr.GetAddDamage(defender.unitAttrCenter.elementType);
        int realDamage = attackPack.damage + addAtk;
        realDamage -= defender.unitAttrCenter.attr.GetArmor(attackPack.damageType);
        // 减伤
        realDamage = (int)(realDamage *
                           (100 - defender.unitAttrCenter.buffAttrDic[
                               BuffAttrType.DamageReduction])/100f);
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
            
            int addAtk = attacker.unitAttrCenter.attr.GetAddDamage(target.unitAttrCenter.elementType);
            foreach (var attackPack in skillPack.attackPacks)
            {
                int realDamage = attackPack.damage + addAtk;
                realDamage -= target.unitAttrCenter.attr.GetArmor(attackPack.damageType);
                // 减伤
                realDamage = (int)(realDamage *
                    (100 - target.unitAttrCenter.buffAttrDic[
                        BuffAttrType.DamageReduction])/100f);
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
        foreach (var piece in AIController.pieces)
        {
            if (!piece.isDead)
            {
                return;
            }
        }

        // 敌方棋子全灭，玩家胜利
        BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家胜利！");
        DOVirtual.DelayedCall(1.0f, () => { BattleScene.Ins.BM.OnClickQuitBattle(); });
    }


    // ===== Test ======//
    public void OnClickQuitBattle()
    {
        GM.Ins.BattleEnd();
    }
}