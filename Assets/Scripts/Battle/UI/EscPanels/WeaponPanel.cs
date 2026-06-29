using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class WeaponPanel : UIPanel
{
    public Image weaponImage;
    [Header("Pre-placed UI Slots")]
    // 在 Inspector 里手动拖入 3 个固定格子
    public List<UIItemSlot> normalUISlots = new List<UIItemSlot>();

    // 在 Inspector 里手动拖入 2 个固定格子
    public List<UIItemSlot> weaponUISlots = new List<UIItemSlot>();
    public List<UIItemSlot> plugsUISlots = new List<UIItemSlot>();
    public CharactorPanel charactorPanel;
    public UIDetailPanel detailPanel;

    public void RefreshUI()
    {
        PieceData pieceData = charactorPanel.pieceData;
        Player player = charactorPanel.player;
        weaponImage.sprite = pieceData.weaponIcon;
        //nameText.text = player.NAME;

        // 1. 刷新普通装备槽
        for (int i = 0; i < normalUISlots.Count; i++)
        {
            int id = (i < player.normalSlots.Length) ? player.normalSlots[i] : 0;
            normalUISlots[i].Init(id, this);
        }

        // 2. 刷新武器装备槽
        for (int i = 0; i < weaponUISlots.Count; i++)
        {
            int id = (i < player.weaponSlots.Length) ? player.weaponSlots[i] : 0;
            weaponUISlots[i].Init(id, this);
        }

        // 3. 刷新插件背包 (预置格子循环利用)
        for (int i = 0; i < plugsUISlots.Count; i++)
        {
            // 如果玩家背包里的物品数量超过了预设格数，这里会截断（建议预设足够多）
            int id = (i < GM.Ins.PLAYERPROFILE.componentInventory.Count)
                ? GM.Ins.PLAYERPROFILE.componentInventory[i]
                : 0;
            plugsUISlots[i].Init(id, this);
        }
    }
    
    public void ShowDetail(int id, Vector3 pos)
    {
        detailPanel.gameObject.SetActive(true);
        // 3. 设定基础偏移量
        float offsetX = 220f;

        // 4. 边界判定逻辑：
        // 如果 点击位置 + 偏移量 + 面板一半宽度 > 屏幕宽度，说明右边放不下了
        if (pos.x + offsetX + 100f > 1920f)
        {
            offsetX = -220f;
        }

        detailPanel.transform.position = pos + new Vector3(offsetX, 0, 0);
        detailPanel.Setup(id, this);
    }
    public bool CheckIsEquipped(int id)
    {
        foreach (int i in charactorPanel.player.normalSlots)
            if (i == id)
                return true;
        foreach (int i in charactorPanel.player.weaponSlots)
            if (i == id)
                return true;
        return false;
    }
}