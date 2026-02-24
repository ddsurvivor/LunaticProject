using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轨道轰炸触发区
/// </summary>
public class BombArea : InteractArea
{
    //public ItemType bombType;// 爆炸特效预制体
    [Header("爆炸技能数据")]
    public SkillPack bombSkillPack;// 爆炸技能数据
    
    public override void TriggerAction(PieceController piece = null)
    {
        base.TriggerAction(piece);
        // 检测所有已激活的敌人，对所有敌人发动一次攻击
        foreach (var enemy in BattleScene.Ins.BM.AIController.pieces)
        {
            if (((EnemyController)enemy).isActived)
            {
                // 发动攻击
                BattleScene.Ins.BM.PieceSkill(piece, new List<PieceController>() { enemy }
                    , bombSkillPack);
                
                // 检测目标是否有假死技能, 如果有且进入了假死状态，则彻底杀死
                SelfRevive revive = enemy.GetComponent<SelfRevive>();
                if (revive != null && revive.isFakeDead)
                {
                    ((EnemyController)enemy).deadNotDelete = false; // 取消死亡后不删除的设置
                    enemy.Dead();
                }
            }
        }
    }
}
