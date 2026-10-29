
using System.Collections;
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
    public Image hpBar;
    //public List<GameObject> hpIcons;
    public Text hpNumText;
    public Text hpMaxText;
    //public List<GameObject> mpIcons;
    public Text mpNumText;
    public Text mpMaxText;
    [SerializeField] private ActionPointPipsUI actionPointPips;
    public Image manaBar;
    //public List<GameObject> manaIcons;
    public Text manaNumText;
    public Text manaMaxText;
    //public List<GameObject> ammoIcons;
    public Image ammoBar;
    public Text ammoNumText;
    
    //public Sprite[] headSprites;
    //public string[] pieceNames;// 临时用，后期改成棋子数据
    
    [SerializeField][ReadOnly]
    private PieceController piece;
    
    public UnityEngine.UI.Image healthShadowBar;

    private readonly HealthBarFillAnimation healthAnimation = new();

    private void OnDisable() => healthAnimation.Reset();

    public List<BuffCell> buffCells = new();
    public void OnSelectPiece(PieceController piece)
    {
        if(piece.isPlayerPiece== false) return;// 只显示玩家棋子信息
        this.piece = piece;
        GetComponent<ActionCostPreviewUI>()?.Bind(piece.unitAttrCenter);
        UpdateDisplay();
    }
    
    public void SetPlayer(Player playerData, PieceData pieceData)
    {
        if(playerData == null) return;
        GetComponent<ActionCostPreviewUI>()?.Bind(null);
        int pieceId = pieceData.pieceId;
        
        string name = "CG/PC0" + pieceId + "C1";
        if (pieceId>100)
        {
            name = "CG/PC0" + (pieceId-100).ToString() + "01";
        }
        head.sprite = Resources.Load<Sprite>(name);
        pieceName.text = playerData.NAME;
        // 更新血量
        float hpPercent = pieceData.maxHealth > 0 ? (float)playerData.curHealth / pieceData.maxHealth : 0f;
        healthAnimation.Update(hpBar, healthShadowBar, hpPercent, playerData);
        hpNumText.text = playerData.curHealth.ToString();
        hpMaxText.text = "/" + pieceData.maxHealth.ToString();
        // 更新魔法值图标
        //int curMP = 3;
        mpNumText.text = pieceData.maxMovePoint.ToString();
        mpMaxText.text = "/" + pieceData.maxMovePoint.ToString();
        if (actionPointPips != null) actionPointPips.SetPoints(pieceData.maxMovePoint, pieceData.maxMovePoint);
        /*for (int i = 0; i < mpIcons.Count; i++)
        {
            if (i < curMP)
            {
                mpIcons[i].SetActive(true);
            }
            else
            {
                mpIcons[i].SetActive(false);
            }
        }*/
        int curMana = playerData.curMana;
        manaNumText.text = curMana.ToString();
        manaMaxText.text = "/" + pieceData.maxMana.ToString();
        float manaPercent = pieceData.maxMana > 0 ? (float)curMana / pieceData.maxMana : 0f;
        manaBar.fillAmount = manaPercent;
        
        int ammo = playerData.curAmmo;
        ammoNumText.text = ammo.ToString();
        ammoBar.fillAmount = pieceData.maxAmmoCount > 0 ? (float)ammo / pieceData.maxAmmoCount : 0f;
    }
    
    public override void UpdateDisplay()
    {
        if(piece == null) return;
        StopMpIconsBlink();
        int pieceId = piece.pieceID;
        //if(pieceId < 1 || pieceId > headSprites.Length) return;
        // 更新头像和名称
        Player playerData = GM.Ins.PLAYERPROFILE.GetPlayer(pieceId-1);
        string name = "CG/PC0" + pieceId + "C1";
        if (pieceId>100)
        {
            name = "CG/PC0" + (pieceId-100).ToString() + "01";
        }
        head.sprite = Resources.Load<Sprite>(name);
        pieceName.text = playerData.NAME;
        // 更新血量
        float hpPercent = piece.unitAttrCenter.MaxHealth > 0
            ? (float)piece.unitAttrCenter.CurHealth / piece.unitAttrCenter.MaxHealth : 0f;
        healthAnimation.Update(hpBar, healthShadowBar, hpPercent, piece);
        hpNumText.text = piece.unitAttrCenter.CurHealth.ToString();
        hpMaxText.text = "/" + piece.unitAttrCenter.MaxHealth.ToString();
        // 更新魔法值图标
        int curMP = piece.unitAttrCenter.CurMovePoint;
        mpNumText.text = curMP.ToString();
        mpMaxText.text = "/" + piece.unitAttrCenter.MaxMovePoint.ToString();
        if (actionPointPips != null) actionPointPips.SetPoints(curMP, piece.unitAttrCenter.MaxMovePoint);
        /*for (int i = 0; i < mpIcons.Count; i++)
        {
            if (i < curMP)
            {
                mpIcons[i].SetActive(true);
            }
            else
            {
                mpIcons[i].SetActive(false);
            }
        }*/
        int curMana = piece.unitAttrCenter.ManaPoint;
        manaNumText.text = curMana.ToString();
        manaMaxText.text = "/" + piece.unitAttrCenter.MaxManaPoint.ToString();
        float manaPercent = piece.unitAttrCenter.MaxManaPoint > 0 ? (float)curMana / piece.unitAttrCenter.MaxManaPoint : 0f;
        manaBar.fillAmount = manaPercent;
        
        int ammo = piece.unitAttrCenter.AmmoCount;
        ammoNumText.text = ammo.ToString();
        ammoBar.fillAmount = piece.unitAttrCenter.MaxAmmoCount > 0 ? (float)ammo / piece.unitAttrCenter.MaxAmmoCount : 0f;
        //Debug.Log("Ammo: " + ammo);
        /*for (int i = 0; i < ammoIcons.Count; i++)
        {
            if (i < ammo)
            {
                ammoIcons[i].SetActive(true);
            }
            else
            {
                ammoIcons[i].SetActive(false);
            }
        }*/

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

    /// <summary>兼容旧调用，仅预览当前可用 AP 右侧的指定点数。</summary>
    public void StartMpIconsBlink(int count = 1)
    {
        if (actionPointPips != null) actionPointPips.SetPreview(count);
    }

    /// <summary>停止 AP 呼吸并清理本面板的资源消耗预览。</summary>
    public void StopMpIconsBlink()
    {
        if (actionPointPips != null) actionPointPips.SetPreview(0);
        var preview = GetComponent<ActionCostPreviewUI>();
        if (preview != null) preview.Clear();
    }
}
