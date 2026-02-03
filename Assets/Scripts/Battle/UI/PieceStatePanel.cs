
using UnityEngine.UI;

public class PieceStatePanel : UIPanel
{
    public PieceController pc;
    
    public Text nameText;
    public Text healthText;
    public Text mpText;
    
    public Image healthBar;
    public Image mpBar;

    public void OpenPanel(PieceController pc)
    {
        this.pc = pc;
        gameObject.SetActive(true);
    }
    public override void UpdateDisplay()
    {
        base.UpdateDisplay();
        if (pc != null)
        {
            // 更新显示pc的状态信息
            nameText.text = pc.pieceData.pieceName;
            healthText.text = $"{pc.unitAttrCenter.CurHealth}/{pc.unitAttrCenter.MaxHealth}";
            mpText.text = $"{pc.unitAttrCenter.CurMovePoint}/{pc.unitAttrCenter.MaxMovePoint}";
            healthBar.fillAmount = (float)pc.unitAttrCenter.CurHealth / pc.unitAttrCenter.MaxHealth;
            mpBar.fillAmount = (float)pc.unitAttrCenter.CurMovePoint / pc.unitAttrCenter.MaxMovePoint;
        }
    }
}