
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
    
    public List<BuffCell> buffCells = new();
    public void OnSelectPiece(PieceController piece)
    {
        if(piece.isPlayerPiece== false) return;// 只显示玩家棋子信息
        this.piece = piece;
        UpdateDisplay();
    }
    
    public void SetPlayer(Player playerData, int pieceId)
    {
        if(playerData == null) return;
        string name = "CG/PC0" + pieceId + "C1";
        if (pieceId>100)
        {
            name = "CG/PC0" + (pieceId-100).ToString() + "01";
        }
        head.sprite = Resources.Load<Sprite>(name);
        pieceName.text = playerData.NAME;
        // 更新血量
        float hpPercent = (float)piece.unitAttrCenter.CurHealth / piece.unitAttrCenter.MaxHealth;
        hpBar.fillAmount = hpPercent;
        hpNumText.text = piece.unitAttrCenter.CurHealth.ToString();
        hpMaxText.text = "/" + piece.unitAttrCenter.MaxHealth.ToString();
        // 更新魔法值图标
        int curMP = piece.unitAttrCenter.CurMovePoint;
        mpNumText.text = curMP.ToString();
        mpMaxText.text = "/" + piece.unitAttrCenter.MaxMovePoint.ToString();
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
        float manaPercent = (float)curMana / piece.unitAttrCenter.MaxManaPoint;
        manaBar.fillAmount = manaPercent;
        
        int ammo = piece.unitAttrCenter.AmmoCount;
        ammoNumText.text = ammo.ToString();
        ammoBar.fillAmount = (float)ammo / piece.unitAttrCenter.MaxAmmoCount;
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
        float hpPercent = (float)piece.unitAttrCenter.CurHealth / piece.unitAttrCenter.MaxHealth;
        hpBar.fillAmount = hpPercent;
        hpNumText.text = piece.unitAttrCenter.CurHealth.ToString();
        hpMaxText.text = "/" + piece.unitAttrCenter.MaxHealth.ToString();
        // 更新魔法值图标
        int curMP = piece.unitAttrCenter.CurMovePoint;
        mpNumText.text = curMP.ToString();
        mpMaxText.text = "/" + piece.unitAttrCenter.MaxMovePoint.ToString();
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
        float manaPercent = (float)curMana / piece.unitAttrCenter.MaxManaPoint;
        manaBar.fillAmount = manaPercent;
        
        int ammo = piece.unitAttrCenter.AmmoCount;
        ammoNumText.text = ammo.ToString();
        ammoBar.fillAmount = (float)ammo / piece.unitAttrCenter.MaxAmmoCount;
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

    private List<Coroutine> mpIconBlinkCoroutines = new();
    // 启动前 count 个 mpIcons 的闪烁
    public void StartMpIconsBlink(int count=1)
    {
        StopMpIconsBlink();
        List<GameObject> activeIcons = new();
        //activeIcons.AddRange(mpIcons.FindAll(icon => icon.activeInHierarchy));
        int total = activeIcons.Count;
        for (int i = total - count; i < total; i++)
        {
            if (i >= 0 && i < total)
            {
                var coroutine = StartCoroutine(BlinkIcon(activeIcons[i]));
                mpIconBlinkCoroutines.Add(coroutine);
            }
        }
    }
    // 停止所有 mpIcons 的闪烁
    public void StopMpIconsBlink()
    {
        foreach (var coroutine in mpIconBlinkCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        mpIconBlinkCoroutines.Clear();

        // 恢复所有 mpIcons 的正常显示
        /*foreach (var icon in mpIcons)
        {
            var img = icon.GetComponent<Image>();
            if (img != null)
                img.color = Color.white;
        }*/
    }

    // 闪烁协程
    private IEnumerator BlinkIcon(GameObject icon)
    {
        var img = icon.GetComponent<Image>();
        if (img == null) yield break;
        bool visible = true;
        while (true)
        {
            img.color = visible ? Color.white : new Color(1, 1, 1, 0.3f);
            visible = !visible;
            yield return new WaitForSeconds(0.3f);
        }
    }
    
}
