using UnityEngine;
using UnityEngine.UI;


public class UIManager : MonoBehaviour
    {
        public TurnPanel turnPanel;
        public Button endTurnButton;
        public InfoBox infoBox;
        public Button burstButton;

        public void ShowBurstReady(bool option)
        {
            burstButton.gameObject.SetActive(option);
        }
    }
