using UnityEngine;
using UnityEngine.UI;

public class BattleFinishPanel : MonoBehaviour
{
    // 标题
    public Text titleText;
    public Text descriptionText;
    
    public GameObject continueButton;
    public GameObject retryButton;
    //public GameObject exitButton;


    public void ShowPanel(bool win, FinishDrop drop =null, int exp = 0)
    {
        gameObject.SetActive(true);
        titleText.text = win ? "Victory!" : "Defeat!";
        descriptionText.text = win ? "战斗胜利" : "战斗失败";
        if (win)
        {
            continueButton.SetActive(true);
            retryButton.SetActive(false);
            //exitButton.SetActive(true);
        }
        else
        {
            continueButton.SetActive(false);
            retryButton.SetActive(true);
            //exitButton.SetActive(true);
        }

        if (drop != null)
        {
            descriptionText.text += "\n" + drop.dropSummary;
        }

        if (exp > 0)
        {
            descriptionText.text += $"\n全体获得了{exp}点经验值";
        }
    }
}