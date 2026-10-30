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


    /// <summary>显示结果和只读胜利奖励预告；本入口不发放奖励。</summary>
    public void ShowPanel(bool win, FinishDrop drop =null, int exp = 0)
    {
        gameObject.SetActive(true);
        titleText.text = win ? "战斗胜利" : "战斗失败";
        descriptionText.text = win ? "战斗胜利" : "战斗失败";
        if (win)
        {
            continueButton.SetActive(true);
            retryButton.SetActive(true);
            //exitButton.SetActive(true);
        }
        else
        {
            continueButton.SetActive(true);
            retryButton.SetActive(true);
            //exitButton.SetActive(true);
        }

        if (win && drop != null)
        {
            descriptionText.text += "\n" + drop.PreviewSummary();
        }

        if (win && exp > 0)
        {
            descriptionText.text += $"\n全体经验：{exp}（离开战斗时结算）";
        }
    }
}
