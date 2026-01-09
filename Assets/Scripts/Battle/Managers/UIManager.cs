using UnityEngine;
using UnityEngine.UI;


public class UIManager : MonoBehaviour
    {
        public TurnPanel turnPanel;
        public Button endTurnButton;
        public InfoBox infoBox;
        public Button burstButton;
        public TeamPanel teamPanel;

        public void Init()
        {
            teamPanel.gameObject.SetActive(true);
            teamPanel.Init();
        }
        public void ShowBurstReady(bool option)
        {
            burstButton.gameObject.SetActive(option);
        }
    }
