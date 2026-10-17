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
    [ReadOnly] public bool isInTurn; // 普通或聚能行动期间是否允许玩家输入。
    [ReadOnly] public bool isBursting; // 是否处于聚能状态

    private readonly BurstGauge burstGauge = new(); // 统一持有聚能余额、容量与冷却规则。
    [ShowInInspector, ReadOnly] public float burstCharge => burstGauge.Charge;
    [ShowInInspector, ReadOnly] public float maxBurstCharge => burstGauge.Capacity;
    [ShowInInspector, ReadOnly] public int BurstCooldown => burstGauge.Cooldown; // 剩余普通棋子回合数。
    [Range(0, 3), LabelText("动力脊等级（待设施系统接入）")] public int powerSpineLevel;
    [LabelText("强制使用序章聚能容量")] public bool forceDefaultBurstCapacity;
    public bool ableBurst => burstGauge.Ready && !isBursting;
    public int AffordableBurstActors => burstGauge.AffordableActors;

    [ReadOnly] public int totalDamage; // 对单一敌人造成的总伤害数值
    [ReadOnly] public PieceController burstTarget; // 当前聚能回合攻击的单一目标敌人

    [Header("UI")] public Image burstChargeBarFill; // 聚能条填充部分

    private float originWidth = 1842.2f;

    [FoldoutGroup("事件")] public UnityEvent OnInit;
    [FoldoutGroup("事件")] public UnityEvent OnTurnStart;
    [FoldoutGroup("事件")] public UnityEvent OnTurnEnd;

    /// <summary>初始化我方棋子及本场聚能资源；按章节和动力脊配置确定容量。</summary>
    public virtual void Init()
    {
        OnInit.Invoke();
        burstGauge.Reset(powerSpineLevel, forceDefaultBurstCapacity || GM.Ins.PLAYERPROFILE.chapterNumber == 0);
        UpdateBurstBar(true);
        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            piece.Init(this, BattleScene.Ins.BM.pieceDataListSO.GetPieceData(piece.pieceID));
        }

        BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(pieces[0]);
        BattleScene.Ins.UM.ShowBurstReady(false);
    }

    /// <summary>兼容序列化回合事件，仅为当前行动的我方棋子开启操作。</summary>
    public virtual void TurnStart()
    {
        var piece = BattleScene.Ins.BM.ActivePiece;
        if (piece != null && pieces.Contains(piece)) BeginPieceTurn(piece);
    }

    /// <summary>开启一名我方棋子的普通回合输入与 UI；棋子和 Buff 的回合结算由调度器负责。</summary>
    public void BeginPieceTurn(PieceController piece)
    {
        isInTurn = true;
        OnTurnStart?.Invoke();
        BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(piece);
        BattleScene.Ins.UM.OnTurnStart();
    }

    /// <summary>兼容序列化回合结束事件，转交个人回合结束处理。</summary>
    public virtual void TurnEnd() => EndPieceTurn();

    /// <summary>关闭我方普通回合输入并发送结束事件，不结束聚能或批量结算队友。</summary>
    public void EndPieceTurn()
    {
        isInTurn = false;
        OnTurnEnd?.Invoke();

    }

    /// <summary>增加基础聚能量并刷新 UI；聚能中不自充能，首次达到可发动条件时提示。</summary>
    public void ChargeBurst(float amount)
    {
        if (isBursting) return;
        bool wasReady = ableBurst;
        burstGauge.Add(amount);
        if (!wasReady && ableBurst)
        {
            GM.Ins.AM.PlayAudio(AudioCueType.ChargeFull);
            BattleScene.Ins.BM.battleDialogueManager.TriggerBurstReady();
        }
        RefreshBurstUI();
    }

    /// <summary>按确认人数支付聚能费用并刷新 UI；失败时不扣费。</summary>
    public bool SpendBurst(int actors)
    {
        if (!burstGauge.Spend(actors)) return false;
        RefreshBurstUI();
        return true;
    }

    /// <summary>由调度器在可计入冷却的普通回合结束时调用，推进冷却并刷新 UI。</summary>
    public void CompleteNormalTurnForBurst()
    {
        burstGauge.CompleteNormalTurn();
        RefreshBurstUI();
    }

    /// <summary>开启聚能状态、重置累计伤害并播放入场表现；选人、支付和额外 AP 由调度器负责。</summary>
    public void EnterBurstMode()
    {
        isBursting = true;
        totalDamage = 0;
        burstTarget = null;
        BattleScene.Ins.BM.ShowBurstGray(true);
        var flash = BattleScene.Ins.UM.burstStart;
        if (flash != null)
        {
            flash.DOKill(); flash.color = Color.white; flash.gameObject.SetActive(true);
            flash.DOFade(0f, 0.5f).SetDelay(0.2f).OnComplete(() => flash.gameObject.SetActive(false));
        }
        GM.Ins.AM.PlayAudio(AudioCueType.ChargeStart);
        RefreshBurstUI();
    }

    /// <summary>退出聚能并开始冷却、恢复灰度表现；保留未花费能量，非聚能状态下不处理。</summary>
    public void EndBurstMode()
    {
        if (!isBursting) return;
        isBursting = false;
        burstGauge.BeginCooldown();
        totalDamage = 0;
        burstTarget = null;
        BattleScene.Ins.BM.ShowBurstGray(false);
        var flash = BattleScene.Ins.UM.burstEnd;
        if (flash != null)
        {
            flash.DOKill(); flash.color = Color.white; flash.gameObject.SetActive(true);
            flash.DOFade(0f, 0.5f).OnComplete(() => flash.gameObject.SetActive(false));
        }
        GM.Ins.AM.PlayAudio(AudioCueType.ChargeExit);
        RefreshBurstUI();
    }

    /// <summary>同步聚能槽、按钮可用状态与选人/退出/冷却文字。</summary>
    public void RefreshBurstUI()
    {
        UpdateBurstBar();
        var battle = BattleScene.Ins.BM;
        var button = BattleScene.Ins.UM.burstButton;
        if (button == null) return;
        button.gameObject.SetActive(true);
        button.interactable = isBursting || battle.IsChoosingBurst || (ableBurst && battle.SpeedTurnsActive);
        var label = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (label != null) label.text = isBursting ? "结束聚能" : battle.IsChoosingBurst
            ? $"发动聚能 ({battle.SelectedBurstCount})" : BurstCooldown > 0
            ? $"聚能冷却 {BurstCooldown}" : $"聚能 {burstCharge:0}/{maxBurstCharge:0}";
    }

    /// <summary>保留旧按钮序列化入口；速度模式自动交棒，此入口不执行操作。</summary>
    public void OnClickTurnEnd() { }
    /// <summary>将聚能按钮事件转交战斗调度器处理。</summary>
    public void OnClickBurst() => BattleScene.Ins.BM.OnBurstButton();

    private void UpdateBurstBar(bool instant = false)
    {
        if (burstChargeBarFill != null)
        {
            burstChargeBarFill.DOKill();
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
