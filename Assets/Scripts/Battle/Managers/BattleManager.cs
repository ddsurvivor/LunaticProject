using DG.Tweening;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public AIController AIController;
    public PlayerController PlayerController;
    public CameraController camera;
    public BuffManager buffManager;


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
        BattleScene.Ins.UM.endTurnButton.gameObject.SetActive(true);
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
        defender.unitAttrCenter.TakeDamage(realDamage);
        BattleScene.Ins.BM.camera.FocusShake(defender.transform);

        if (defender is EnemyController enemy)
        {
            enemy.AddDamageRecord(attacker, realDamage);
        }
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