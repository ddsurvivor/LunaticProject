using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 棋子综合显示面板
/// </summary>
public class CharacterUIPage : MonoBehaviour
{
    public Player player;
    private int unitID;
    public Text nameText;
    public Text skillPointNumText;
    public Image avatarImage;
    public UIDetailPanel detailPanel;
    

    [Header("Pre-placed UI Slots")]
    // 在 Inspector 里手动拖入 3 个固定格子
    public List<UIItemSlot> normalUISlots = new List<UIItemSlot>();
    // 在 Inspector 里手动拖入 2 个固定格子
    public List<UIItemSlot> weaponUISlots = new List<UIItemSlot>();
    // 在 Inspector 里将背包区域预先放好的 N 个格子全部拖进来
    public List<UIItemSlot> inventoryUISlots = new List<UIItemSlot>();
    

    [SerializeField]
    private List<DetailAttributeRow> rowList = new ();
    [SerializeField]
    public List<DetailAttributeRow> armorRowList  = new ();
    public SkillPointPanel skillPointPanel;
    public void ShowPanel(Player player, int unitID)
    {
        this.player = player;
        this.unitID = unitID;
        RefreshUI();
        gameObject.SetActive(true);
        detailPanel.gameObject.SetActive(false);
        for (int i = 0; i < 10; i++)
        {
            if (i >= rowList.Count) break;
            DetailAttributeRow row = rowList[i];
            // 直接通过编号读取属性名和当前值
            string attrName = player.GetAttrName(i);
            int currentVal = player.AccessAttribute(i, AttrOp.Get);

            row.UpdateInfo(attrName, currentVal, 200);
        }

        PieceData piece = GM.Ins.DM.pieceDataListSO.GetPieceData(unitID);
        // for (int i = 0; i < 3; i++)
        // {
        //     if (i >= rowList.Count) break;
        //     DetailAttributeRow row = rowList[i];
        //     string attrName = piece._armorDic;
        // }

        //遍历enum DamageType
        for (int i = 0; i < 3; i++)
        {
            if (i >= armorRowList.Count) break;
            DetailAttributeRow row = armorRowList[i];
            string attrName = ((DamageType)i+1).ToChinese() + "防护";
            int currentVal = piece._armorDic[(DamageType)i+1];
            row.UpdateInfo(attrName, currentVal, 100);
        }
        skillPointNumText.text = player.SkillPoints.ToString();
    }

    
    public void RefreshUI() {
        
        avatarImage.sprite  = Resources.Load<Sprite>("CG/" + player.spriteName);
        nameText.text = player.NAME;
        
        // 1. 刷新普通装备槽
        for (int i = 0; i < normalUISlots.Count; i++) {
            int id = (i < player.normalSlots.Length) ? player.normalSlots[i] : 0;
            normalUISlots[i].Init(id, this);
        }

        // 2. 刷新武器装备槽
        for (int i = 0; i < weaponUISlots.Count; i++) {
            int id = (i < player.weaponSlots.Length) ? player.weaponSlots[i] : 0;
            weaponUISlots[i].Init(id, this);
        }

        // 3. 刷新背包 (预置格子循环利用)
        for (int i = 0; i < inventoryUISlots.Count; i++) {
            // 如果玩家背包里的物品数量超过了预设格数，这里会截断（建议预设足够多）
            int id = (i < GM.Ins.PLAYERPROFILE.componentInventory.Count) ? GM.Ins.PLAYERPROFILE.componentInventory[i] : 0;
            inventoryUISlots[i].Init(id, this);
        }
    }

    public void ShowDetail(int id, Vector3 pos) {
        detailPanel.gameObject.SetActive(true);
        // 3. 设定基础偏移量
        float offsetX = 220f;
    
        // 4. 边界判定逻辑：
        // 如果 点击位置 + 偏移量 + 面板一半宽度 > 屏幕宽度，说明右边放不下了
        if (pos.x + offsetX + 100f > 1920f) {
            offsetX = -220f;
        }
        detailPanel.transform.position = pos + new Vector3(offsetX, 0, 0);
        detailPanel.Setup(id, this);
    }

    public bool CheckIsEquipped(int id) {
        foreach (int i in player.normalSlots) if (i == id) return true;
        foreach (int i in player.weaponSlots) if (i == id) return true;
        return false;
    }
    
    public void OnClickSkillPoints() {
        // 打开技能点面板
        skillPointPanel.ShowPanel(player, unitID);
    }
}