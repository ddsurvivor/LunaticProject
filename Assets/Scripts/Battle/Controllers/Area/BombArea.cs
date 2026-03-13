using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 轨道轰炸触发区
/// </summary>
public class BombArea : InteractArea
{
    //public ItemType bombType;// 爆炸特效预制体
    public int coolDown = 2; // 冷却回合数
    [SerializeField][ReadOnly]
    private int currentCoolDown = 0; // 当前冷却回合数
    [Header("爆炸技能数据")] public SkillPack bombSkillPack; // 爆炸技能数据

    
    [Button("测试轨道轰炸")]
    public override void TriggerAction(PieceController piece = null)
    {
        if(currentCoolDown > 0)
        {
            Debug.Log("轨道轰炸冷却中，剩余回合数：" + currentCoolDown);
            ableToTrigger = false;
            return;
        }
        //piece = BattleScene.Ins.BM.PlayerController.pieces[0]; // 测试用
        base.TriggerAction(piece);
        piece.StartSkillAttack(bombSkillPack);
        currentCoolDown = coolDown;// 设置冷却回合数
        ableToTrigger = false;
    }

    public void OnTurnStart()
    {
        if (currentCoolDown > 0)
        {
            currentCoolDown--;
            if (currentCoolDown == 0)
            {
                ableToTrigger = true;
                Debug.Log("轨道轰炸冷却结束，可以再次使用");
            }
            else
            {
                Debug.Log("轨道轰炸冷却中，剩余回合数：" + currentCoolDown);
            }
        }
    }
}