using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


public partial class UIManager : SerializedMonoBehaviour
{
    public TurnPanel turnPanel;
    public List<GameObject> turnImage;
    public CustomAdvancedButton endTurnButton;
    public InfoBox infoBox;
    public Button burstButton;
    //public Image burstBtnImage;
    public TeamPanel teamPanel;
    public PieceInfoPanel pieceInfoPanel;
    public GameObject restartButton; // 重启战斗
    public Text turnNumberText;
    public PieceActionListPanel pieceActionListPanel;
    public PieceStatePanel pieceStatePanel;

    public RectTransform skillNameDisplay;
    public Text skillNamText;

    public 剧本System logSystem;

    //public MessagePanel messagePanel;
    //public ItemGetPanel itemGetPanel;
    public PlayerLogPanel logPanel;
    public BattleFinishPanel battleFinishPanel;
    public GameObject battleLossPanel;
    public CheckDicePanel checkDicePanel;
    public BattleStartUIPanel battleStartUIPanel;// 复用为战斗胜利提示
    public Image burstStart;
    public Image burstEnd;
    public SkillTooltipUI skillTooltipUI;
    public CustomAdvancedButton skipButton;
    public Image checkSuccess;// 检定成功
    public Image checkFail;// 检定失败
    public NotificationManager notificationManager;
    [SerializeField] private Transform undoMoveBtn;

    public Dictionary<KeyCode, GameObject> keyPanelDic = new();
    

    [SerializeField] private SpeedTimelineUI speedTimelinePrefab;
    private SpeedTimelineUI speedTimeline;

    /// <summary>供角色跟随状态避让当前可见的顶部行动序列；未创建或隐藏时返回 null。</summary>
    public RectTransform ActionTimelineRect => speedTimeline != null && speedTimeline.gameObject.activeInHierarchy
        ? (RectTransform)speedTimeline.transform : null;
    /// <summary>为当前战斗创建速度条；已有实例时先销毁，避免重复显示。</summary>
    public void EnableSpeedTimeline(BattleManager battle)
    {
        if (endTurnButton != null) endTurnButton.gameObject.SetActive(false);
        foreach (var image in turnImage) if (image != null) image.SetActive(false);
        if (speedTimeline != null) Destroy(speedTimeline.gameObject);
        var canvas = pieceInfoPanel.GetComponentInParent<Canvas>();
        if (canvas != null) speedTimeline = SpeedTimelineUI.Create(canvas.rootCanvas, battle, speedTimelinePrefab);
    }

    public BattleOverheadStatusLayer OverheadStatuses { get; private set; }

    /// <summary>初始化行动、头顶状态、结果窗口与消息区；沿用现有场景引用。</summary>
    public void Init()
    {
        if (endTurnButton != null) endTurnButton.gameObject.SetActive(false);
        //teamPanel.gameObject.SetActive(true);
        //teamPanel.Init();
        pieceActionListPanel.Init();
        OverheadStatuses = BattleOverheadStatusLayer.Ensure(this);
        InitializeWindows();
        InitializeMessageLanes();
    }

    private void Update() { HandleWindowKeys(); }

    public void ShowBurstReady(bool option)
    {
        //burstButton.enabled = option;
        burstButton.gameObject.SetActive(option);
        burstButton.interactable = option;
    }

    /// <summary>
    /// 显示棋子行动面板
    /// </summary>
    /// <param name="piece"></param>
    public void ShowPieceActionPanel(PieceController piece)
    {
        pieceActionListPanel.ShowPanel(piece);
    }

    public void ShowPieceState(PieceController pc)
    {
        // 计算棋子在屏幕中的位置，更新行动面板的位置
        Vector3 screenPos = Camera.main.WorldToScreenPoint(pc.transform.position);
        pieceStatePanel.transform.position = screenPos + new Vector3(0, 100, 0);
        pieceStatePanel.OpenPanel(pc);
    }

    /// <summary>
    /// 更新棋子状态面板显示
    /// 在棋子受伤、消耗后调用
    /// </summary>
    /// <param name="pc"></param>
    public void OnPieceStateChance(PieceController pc)
    {
        pieceStatePanel.OpenPanel(pc);
        pieceStatePanel.UpdateDisplay();
        pieceInfoPanel.OnSelectPiece(pc);
    }

    /// <summary>技能提示按顺序进入独立消息区，不覆盖正在显示的提示。</summary>
    public void PopSkillName(string skillName) { EnqueueNotice(skillName); }

    /// <summary>恢复最初战前快照并重开到部署；胜败窗口和暂停重开共用。</summary>
    public void OnClickRestartButton()
    {
        GM.Ins.RestartBattle();
    }

    /// <summary>沿现有主菜单出口离开，先清理本场窗口、暂停和临时消息。</summary>
    public void OnClickQuit()
    {
        if (!GM.Ins.CanLoadScene("StartScene")) return;
        PrepareBattleTransition();
        GM.Ins.LoadScene("StartScene");
    }

    /// <summary>
    /// 开始战斗内剧情
    /// </summary>
    /// <param name="t"></param>
    public void StartLog(string t)
    {
        logSystem.gameObject.SetActive(true);
        logSystem.设置新剧本(t);
        logSystem.Next();
    }

    public void OnTurnStart()
    {
        if (pieceStatePanel.gameObject.activeInHierarchy)
        {
            pieceStatePanel.OpenPanel(pieceStatePanel.pc);
            pieceInfoPanel.OnSelectPiece(pieceStatePanel.pc);
        }
    }

    public void ShowTurnChange(bool playerTurn)
    {
        // turnImage[0] 是玩家回合图，turnImage[1] 是敌人回合图
        // 使用dotween从屏幕外移入再移出
        int index = playerTurn ? 0 : 1;
        //turnImage[index].SetActive(true);
        RectTransform imgRect = turnImage[index].GetComponent<RectTransform>();
        
        Sequence seq = DOTween.Sequence();
        seq.Append(imgRect.DOLocalMoveY(-110, 0.3f));
        seq.AppendInterval(1f);
        seq.Append(imgRect.DOLocalMoveY(0, 0.3f));
    }
    
    /// <summary>将检定结果排入消息区，关闭旧叠加图片。</summary>
    public void ShowCheckResult(bool success)
    {
        if (checkSuccess != null) checkSuccess.gameObject.SetActive(false);
        if (checkFail != null) checkFail.gameObject.SetActive(false);
        EnqueueNotice(success ? "检定成功" : "检定失败");
    }

    public void ShowItemGet(ItemPack itemPack)
    {
        notificationManager.PushNotification(itemPack);
    }

    public void ShowItemGet(ComponentData itemData)
    {
        notificationManager.PushNotification(itemData);
    }
    
    public void ShowUndoMoveButton(bool show)
    {
        undoMoveBtn.gameObject.SetActive(show);
    }
}
