using System;
using UnityEngine;
using UnityEngine.UI;

public class CharactorPanel : UIPanel
{
    public Player player;
    public int unitID;
    public PieceData pieceData;
    public Text nameText;
    public Image avatarImage;
    
    public PieceInfoPanel pieceInfoPanel;

    private void Start()
    {
        player = GM.Ins.PLAYERPROFILE.GetPlayer(0);
        SetPlayer(player, 1);
    }

    public void SetPlayer(Player player, int unitID)
    {
        this.player = player;
        this.unitID = unitID;
        pieceData = GM.Ins.DM.pieceDataListSO.GetPieceData(unitID);
        //RefreshUI();
        avatarImage.sprite = Resources.Load<Sprite>("CG/" + player.spriteName);
        nameText.text = player.NAME;
        pieceInfoPanel.SetPlayer(player, pieceData);
    }
}