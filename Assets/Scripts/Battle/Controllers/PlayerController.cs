using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;


public class PlayerController : SerializedMonoBehaviour
{
    public List<PieceController> pieces = new();
    public bool isInTurn;
    public bool isBursting; // 是否处于聚能状态

    public float burstCharge = 0f; // 聚能值
    public float maxBurstCharge = 100f; // 最大聚能值
    public bool ableBurst => burstCharge >= maxBurstCharge && !isBursting; // 是否可以发动聚能
    
    public int totalDamage; // 对单一敌人造成的总伤害数值
    public PieceController burstTarget; // 当前聚能回合攻击的单一目标敌人

    [Header("UI")] 
    public RectTransform burstChargeBarFill; // 聚能条填充部分
    private float originWidth = 500f;

    public virtual void Init()
    {
        burstCharge = 0f;
        UpdateBurstBar();
        foreach (var piece in pieces)
        {
            piece.Init(this, BattleScene.Ins.BM.pieceDataListSO.GetPieceData(piece.pieceID));
        }
        BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(pieces[0]);
        BattleScene.Ins.UM.ShowBurstReady(false);
    }
    public virtual void TurnStart()
    {
        // 所有棋子重置状态
        foreach (var piece in pieces)
        {
            piece.TurnStart();
        }

        BattleScene.Ins.UM.endTurnButton.enabled = false;
    }

    public virtual void TurnEnd()
    {
        // 可以在这里添加玩家回合结束时的逻辑
        foreach (var piece in pieces)
        {
            piece.TurnEnd();
            BattleScene.Ins.BM.buffManager.ProcessBuffs(piece.unitAttrCenter);
        }
        
    }

    /// <summary>
    /// 聚能充能
    /// </summary>
    /// <param name="amount"></param>
    public void ChargeBurst(float amount)
    {
        if (isBursting) return;

        burstCharge += amount;
        if (burstCharge >= maxBurstCharge)
        {
            burstCharge = maxBurstCharge;
            Debug.Log("聚能已满，可以发动聚能！");
            BattleScene.Ins.UM.ShowBurstReady(true);
        }

        UpdateBurstBar();
    }

    // 聚能拼点结果
    public bool CheckBurstSuccess()
    {
        return true;
    }

    public void EnterBurstMode()
    {
        isBursting = true;
        burstCharge = 0f;
        UpdateBurstBar();
        totalDamage = 0;
        burstTarget = null;
        // 所有棋子重置状态
        foreach (var piece in pieces)
        {
            piece.TurnStart();
        }
    }


    // UI
    public void OnClickTurnEnd()
    {
        BattleScene.Ins.BM.ChangeTurn();
        BattleScene.Ins.UM.endTurnButton.enabled  = false;
    }

    public void OnClickBurst()
    {
        Debug.Log("发动聚能");
        BattleScene.Ins.UM.ShowBurstReady(false);
        if (BattleScene.Ins.BM.AIController.ableBurst)
        {
            Debug.Log("敌人也准备发动聚能，进入拼点环节");
            if (!CheckBurstSuccess())
            {
                Debug.Log("玩家聚能拼点失败，无法发动聚能");
                return;
            }
        }

        BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家聚能发动！");
        // 进入聚能
        EnterBurstMode();
    }

    private void UpdateBurstBar()
    {
        if (burstChargeBarFill != null)
        {
            float endWidth = originWidth * (burstCharge / maxBurstCharge);
            DOVirtual.Float(burstCharge, endWidth, 0.3f, (value) =>
            {
                burstChargeBarFill.sizeDelta = new Vector2(value, burstChargeBarFill.sizeDelta.y);
            }).SetDelay(0.3f);
            /*burstChargeBarFill.sizeDelta = new Vector2(originWidth * (burstCharge / maxBurstCharge),
                burstChargeBarFill.sizeDelta.y);*/
        }
    }
    
    public int AddBurstDamage(PieceController enemy,  int damage)
    {
        //爆发状态下攻击同一目标增加额外伤害
        if (BattleScene.Ins.BM.PlayerController.burstTarget == enemy)
        {
            damage = (int)(GameConst.burstDamageRate * damage);
            damage += (int)(BattleScene.Ins.BM.PlayerController.totalDamage *
                                        GameConst.burstAddDamageRate);
            BattleScene.Ins.BM.PlayerController.totalDamage += damage;
        }
        else
        {
            BattleScene.Ins.BM.PlayerController.burstTarget = enemy;
            BattleScene.Ins.BM.PlayerController.totalDamage = damage;
        }
        return damage;
    }
}