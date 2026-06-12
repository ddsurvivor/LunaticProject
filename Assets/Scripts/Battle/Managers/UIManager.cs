using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


public class UIManager : SerializedMonoBehaviour
{
    public TurnPanel turnPanel;
    public List<GameObject> turnImage;
    public CustomAdvancedButton endTurnButton;
    public InfoBox infoBox;
    public Button burstButton;
    public Image burstBtnImage;
    public TeamPanel teamPanel;
    public PieceInfoPanel pieceInfoPanel;
    public GameObject restartButton; // 重启战斗
    public Text turnNumberText;
    public PieceActionListPanel pieceActionListPanel;
    public PieceStatePanel pieceStatePanel;

    public RectTransform skillNameDisplay;
    public Text skillNamText;

    public 剧本System logSystem;

    public MessagePanel messagePanel;
    public ItemGetPanel itemGetPanel;
    public PlayerLogPanel logPanel;
    public BattleFinishPanel battleFinishPanel;
    public CheckDicePanel checkDicePanel;
    public BattleStartUIPanel battleStartUIPanel;// 复用为战斗胜利提示
    public Image burstStart;
    public Image burstEnd;
    public SkillTooltipUI skillTooltipUI;
    public CustomAdvancedButton skipButton;

    public Dictionary<KeyCode, GameObject> keyPanelDic = new();
    

    public void Init()
    {
        //teamPanel.gameObject.SetActive(true);
        //teamPanel.Init();
        pieceActionListPanel.Init();
    }

    private void Update()
    {
        if (Input.anyKeyDown)
        {
            foreach (var o in keyPanelDic)
            {
                if (Input.GetKeyDown(o.Key))
                {
                    o.Value.SetActive(true);
                }
            }
        }
    }


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
        // 计算棋子在屏幕中的位置，更新行动面板的位置
        Vector3 screenPos = Camera.main.WorldToScreenPoint(piece.transform.position);
        pieceActionListPanel.transform.position = screenPos + new Vector3(150, 0, 0);
        // 显示棋子行动面板
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

    public void PopSkillName(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName)) return;
        skillNamText.text = skillName;
        skillNameDisplay.gameObject.SetActive(true);
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);
        seq.Append(skillNameDisplay.DOMoveX(120, 0.3f));
        seq.AppendInterval(1f);
        seq.Append(skillNameDisplay.DOMoveX(-120, 0.3f));
    }

    public void OnClickRestartButton()
    {
        // 重新加载当前场景
        UnityEngine.SceneManagement.SceneManager
            .LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void OnClickQuit()
    {
        // 加载开始场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
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
}