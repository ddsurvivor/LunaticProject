using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public class UIManager : MonoBehaviour
    {
        public TurnPanel turnPanel;
        public Button endTurnButton;
        public InfoBox infoBox;
        public Button burstButton;
        public TeamPanel teamPanel;
        public PieceInfoPanel pieceInfoPanel;
        public Button restartButton;// 重启战斗
        public Text turnNumberText;
        public PieceActionListPanel pieceActionListPanel;
        public PieceStatePanel pieceStatePanel;

        public RectTransform skillNameDisplay;
        public Text skillNamText;

        public void Init()
        {
            //teamPanel.gameObject.SetActive(true);
            //teamPanel.Init();
            pieceActionListPanel.Init();
        }


        public void ShowBurstReady(bool option)
        {
            burstButton.enabled = option;
            //burstButton.gameObject.SetActive(option);
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

        public void PopSkillName(string skillName)
        {
            skillNamText.text = skillName;
            skillNameDisplay.gameObject.SetActive(true);
            Sequence seq = DOTween.Sequence();
            
            seq.Append(skillNameDisplay.DOMoveX(280, 0.3f));
            seq.AppendInterval(1f);
            seq.Append(skillNameDisplay.DOMoveX(-280, 0.3f));
        }
    }
