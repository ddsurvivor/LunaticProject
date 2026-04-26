
using System;
using UnityEngine;
using UnityEngine.UI;

public class PieceStatePanel : UIPanel
{
    public PieceController pc;
    
    //public Text nameText;
    //public Text healthText;
    //public Text mpText;
    public Text ammoText;
    public Text mpText;
    public Image healthBar;
    //public Image mpBar;
    public Image manaBar;

    public Transform upRoot;
    public float upRootBaseY = 50f; // upRoot的基础Y位置
    public float upRootScaleFactor = 50f; // upRoot随相机缩放的调整系数
    //public Text manaText;

    public void Update()
    {
        if (pc != null)
        {
            // 计算棋子在屏幕中的位置，更新行动面板的位置
            Vector3 screenPos = Camera.main.WorldToScreenPoint(pc.transform.position);
            transform.position = screenPos + new Vector3(0, 100, 0);
            
            // upRoot 根据当前相机的视角缩放来调整位置，相机缩放越大，upRoot离屏幕中心越远，反之亦然
            float scaleFactor = 1f/ Camera.main.orthographicSize; // 根据相机的正交大小计算缩放因子
            upRoot.localPosition = new Vector3(0,  upRootBaseY + upRootScaleFactor * scaleFactor, 0);
        }
    }

    public void OpenPanel(PieceController pc)
    {
        if(!pc.isPlayerPiece) return;
        this.pc = pc;
        gameObject.SetActive(true);
        UpdateDisplay();
    }
    public override void UpdateDisplay()
    {
        base.UpdateDisplay();
        if (pc != null)
        {
            // 更新显示pc的状态信息
            //nameText.text = pc.pieceData.pieceName;
            //healthText.text = $"{pc.unitAttrCenter.CurHealth}/{pc.unitAttrCenter.MaxHealth}";
            //mpText.text = $"{pc.unitAttrCenter.CurMovePoint}/{pc.unitAttrCenter.MaxMovePoint}";
            healthBar.fillAmount = (float)pc.unitAttrCenter.CurHealth / pc.unitAttrCenter.MaxHealth;
            //mpBar.fillAmount = (float)pc.unitAttrCenter.CurMovePoint / pc.unitAttrCenter.MaxMovePoint;
            ammoText.text =  $"{pc.unitAttrCenter.AmmoCount}/{pc.unitAttrCenter.MaxAmmoCount}";
            mpText.text = $"{pc.unitAttrCenter.CurMovePoint}/{pc.unitAttrCenter.MaxMovePoint}";
            manaBar.fillAmount = (float)pc.unitAttrCenter.ManaPoint / pc.unitAttrCenter.MaxManaPoint;
            //manaText.text = $"{pc.unitAttrCenter.ManaPoint}/{pc.unitAttrCenter.MaxManaPoint}";
        }
    }
}