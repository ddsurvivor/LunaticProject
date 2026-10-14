using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;


public class PlayerController : SerializedMonoBehaviour
{
    public List<PieceController> pieces = new();
    public List<PieceController> keyPieces = new(); // 关键棋子，全部死亡后直接失败
    [ReadOnly] public bool isInTurn;
    [ReadOnly] public bool isBursting; // 是否处于聚能状态

    public float burstCharge = 0f; // 聚能值
    [ReadOnly] public float maxBurstCharge = 100f; // 最大聚能值
    public bool ableBurst => burstCharge >= maxBurstCharge && !isBursting; // 是否可以发动聚能

    [ReadOnly] public int totalDamage; // 对单一敌人造成的总伤害数值
    [ReadOnly] public PieceController burstTarget; // 当前聚能回合攻击的单一目标敌人

    [Header("UI")] public Image burstChargeBarFill; // 聚能条填充部分

    private float originWidth = 1842.2f;

    [FoldoutGroup("事件")] public UnityEvent OnInit;
    [FoldoutGroup("事件")] public UnityEvent OnTurnStart;
    [FoldoutGroup("事件")] public UnityEvent OnTurnEnd;

    public virtual void Init()
    {
        OnInit.Invoke();
        burstCharge = 0f;
        UpdateBurstBar(true);
        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            piece.Init(this, BattleScene.Ins.BM.pieceDataListSO.GetPieceData(piece.pieceID));
        }

        BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(pieces[0]);
        BattleScene.Ins.UM.ShowBurstReady(false);
    }

    // 保留序列化事件入口，但不再刷新整个阵营。
    public virtual void TurnStart()
    {
        var piece = BattleScene.Ins.BM.ActivePiece;
        if (piece != null && pieces.Contains(piece)) BeginPieceTurn(piece);
    }

    public void BeginPieceTurn(PieceController piece)
    {
        isInTurn = true;
        OnTurnStart?.Invoke();
        BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(piece);
        BattleScene.Ins.UM.OnTurnStart();
    }

    public virtual void TurnEnd() => EndPieceTurn();

    public void EndPieceTurn()
    {
        isInTurn = false;
        OnTurnEnd?.Invoke();
        if (isBursting) EndBurstMode();
    }

    /// <summary>
    /// 聚能充能
    /// </summary>
    /// <param name="amount"></param>
    public void ChargeBurst(float amount)
    {
        if (isBursting) return;
        if (burstCharge >= maxBurstCharge) return;
        
        burstCharge += amount;
        if (burstCharge >= maxBurstCharge)
        {
            burstCharge = maxBurstCharge;
            Debug.Log("聚能已满，可以发动聚能！");
            BattleScene.Ins.UM.ShowBurstReady(true);
            GM.Ins.AM.PlayAudio(AudioCueType.ChargeFull);
            BattleScene.Ins.BM.battleDialogueManager.TriggerBurstReady();
        }

        UpdateBurstBar();
    }

    // 聚能拼点结果
    public bool CheckBurstSuccess()
    {
        return true;
    }

    /// <summary>
    /// 进入聚能状态
    /// </summary>
    public void EnterBurstMode()
    {
        isBursting = true;

        UpdateBurstBar();
        totalDamage = 0;
        burstTarget = null;
        // 聚能只恢复当前棋子的行动力，不触发第二次回合开始或影响其他棋子。
        var active = BattleScene.Ins.BM.ActivePiece;
        if (active != null) active.unitAttrCenter.FullMovePoint();
        // 所有敌人棋子暂定
        foreach (var aiPiece in BattleScene.Ins.BM.AIController.pieces)
        {
            aiPiece.pieceDisplay.StopAnimation();
        }

        // 用相机滤镜模式
        //BattleScene.Ins.BM.camera.ActiveBurstMode(true);
        //BattleScene.Ins.UM.burstBtnImage.gameObject.SetActive(true);
        BattleScene.Ins.UM.burstStart.color = new Color(1f, 1f, 1f, 1f);
        BattleScene.Ins.UM.burstStart.gameObject.SetActive(true);
        BattleScene.Ins.UM.burstStart.DOFade(0f, 0.5f).SetDelay(0.6f).OnComplete(() =>
            BattleScene.Ins.UM.burstStart.gameObject.SetActive(false));
        // 所有图片变色
        BattleScene.Ins.BM.ShowBurstGray(true);
        GM.Ins.AM.PlayAudio(AudioCueType.ChargeStart);
    }

    /// <summary>
    /// 结束聚能状态
    /// </summary>
    public void EndBurstMode()
    {
        if (isBursting)
        {
            BattleScene.Ins.BM.ShowBurstGray(false);
            BattleScene.Ins.UM.ShowBurstReady(false);
            //BattleScene.Ins.UM.burstBtnImage.gameObject.SetActive(false);
            BattleScene.Ins.UM.burstEnd.color = new Color(1f, 1f, 1f, 1f);
            BattleScene.Ins.UM.burstEnd.gameObject.SetActive(true);
            BattleScene.Ins.UM.burstEnd.DOFade(0f, 0.5f)
                .SetDelay(1f)
                .OnComplete(() =>
                    BattleScene.Ins.UM.burstEnd.gameObject.SetActive(false));
            GM.Ins.AM.PlayAudio(AudioCueType.ChargeExit);
            Debug.Log("聚能状态结束");
        }

        isBursting = false;
        burstCharge = 0f;
        UpdateBurstBar();
        totalDamage = 0;
        burstTarget = null;
        //BattleScene.Ins.BM.camera.ActiveBurstMode(false);
    }


    // UI
    // 旧按钮/快捷键事件不再负责结束回合，使用棋子的待机行动。
    public void OnClickTurnEnd() { }

    public void OnClickBurst()
    {
        if (!this.isInTurn) return; //只能在回合内发动聚能
        if (!ableBurst || !BattleScene.Ins.BM.CanControlPiece(BattleScene.Ins.BM.ActivePiece)) return;
        Debug.Log("发动聚能");
        //BattleScene.Ins.UM.ShowBurstReady(false);
        BattleScene.Ins.UM.burstButton.gameObject.SetActive(false);
        //BattleScene.Ins.UM.burstButton.interactable = false;
        if (BattleScene.Ins.BM.AIController.ableBurst)
        {
            Debug.Log("敌人也准备发动聚能，进入拼点环节");
            if (!CheckBurstSuccess())
            {
                Debug.Log("玩家聚能拼点失败，无法发动聚能");
                return;
            }
        }

        //BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家聚能发动！");
        // 进入聚能
        EnterBurstMode();
    }

    private void UpdateBurstBar(bool instant = false)
    {
        if (burstChargeBarFill != null)
        {
            float endWidth = (burstCharge / maxBurstCharge);
            if (instant)
            {
                burstChargeBarFill.fillAmount = endWidth;
            }
            else
            {
                burstChargeBarFill.DOFillAmount(endWidth, 0.3f).SetDelay(0.3f);
            }
        }
    }

    public int AddBurstDamage(PieceController enemy, int damage)
    {
        return BattleScene.Ins.BM.damageManager.AddBurstDamage(enemy, damage);
    }
}
