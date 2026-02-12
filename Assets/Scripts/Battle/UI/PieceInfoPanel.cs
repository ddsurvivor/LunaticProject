
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 棋子信息面板
/// </summary>
    public class PieceInfoPanel: UIPanel
{

    public Image head;
    public Text pieceName;
    public Transform hpBar;
    public List<GameObject> mpIcon;
    public Transform manaBar;
    public List<GameObject> ammoIcons;
    public Text ammoNumText;
    
    public Sprite[] headSprites;
    public string[] pieceNames;// 临时用，后期改成棋子数据
    
    [SerializeField][ReadOnly]
    private PieceController piece;
    
    public List<BuffCell> buffCells = new();
    public void OnSelectPiece(PieceController piece)
    {
        if(piece.isPlayerPiece== false) return;// 只显示玩家棋子信息
        this.piece = piece;
        UpdateDisplay();
    }
    
    public override void UpdateDisplay()
    {
        if(piece == null) return;
        int pieceId = piece.pieceID;
        if(pieceId < 1 || pieceId > headSprites.Length) return;
        // 更新头像和名称
        head.sprite = headSprites[pieceId-1];
        pieceName.text = pieceNames[pieceId-1];
        // 更新血量
        float hpPercent = (float)piece.unitAttrCenter.CurHealth / piece.unitAttrCenter.MaxHealth;
        hpBar.localScale = new Vector3(hpPercent, 1, 1);
        // 更新魔法值图标
        int curMP = piece.unitAttrCenter.CurMovePoint;
        for (int i = 0; i < mpIcon.Count; i++)
        {
            if (i < curMP)
            {
                mpIcon[i].SetActive(true);
            }
            else
            {
                mpIcon[i].SetActive(false);
            }
        }
        int curMana = piece.unitAttrCenter.ManaPoint;
        float manaPercent = (float)curMana / piece.unitAttrCenter.MaxManaPoint;
        manaBar.localScale = new Vector3(manaPercent, 1, 1);
        
        int ammo = piece.unitAttrCenter.AmmoCount;
        for (int i = 0; i < ammoIcons.Count; i++)
        {
            if (i < ammo)
            {
                ammoIcons[i].SetActive(true);
            }
            else
            {
                ammoIcons[i].SetActive(false);
            }
        }

        // 更新buff
        foreach (var buffCell in buffCells)
        {
            buffCell.gameObject.SetActive(false);
        }
        for (int i = 0; i < piece.unitAttrCenter.buffStates.Count; i++)
        {
            if (i < buffCells.Count)
            {
                buffCells[i].gameObject.SetActive(true);
                buffCells[i].SetData(piece.unitAttrCenter.buffStates[i]);
            }
            else
            {
                break;
            }
        }
    }

}
