using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 棋子综合显示面板
/// </summary>
public class CharacterUIPage : UIPanel
{
    public Player player;
    private int unitID;
    
    public Text skillPointNumText;
    
    public UIDetailPanel detailPanel;
    public GameObject applyBtn;
    public GameObject resetBtn;

    public Image weaponImage;
    [Header("Pre-placed UI Slots")]
    // 在 Inspector 里手动拖入 3 个固定格子
    public List<UIItemSlot> normalUISlots = new List<UIItemSlot>();

    // 在 Inspector 里手动拖入 2 个固定格子
    public List<UIItemSlot> weaponUISlots = new List<UIItemSlot>();

    // 在 Inspector 里将背包区域预先放好的 N 个格子全部拖进来
    //public List<UIItemSlot> inventoryUISlots = new List<UIItemSlot>();
    [Header("预设格子")] [SerializeField] private ItemSlot[] itemSlots = new ItemSlot[30];
    public List<UIItemSlot> plugsUISlots = new List<UIItemSlot>();

    [SerializeField] private List<AttrModRow> rowList = new();
    [SerializeField] public List<DetailAttributeRow> battleRowList = new();
    public List<DetailAttributeRow> armorRowList = new();

    public List<SkillGroup> passiveSkillGroupList = new List<SkillGroup>();
    public List<SkillGroup> activeSkillGroupList = new List<SkillGroup>();


    //public SkillPointPanel skillPointPanel;
    private int tempPoints;
    private PieceData pieceData;
    
    public CharactorPanel charactorPanel;

    private void Start()
    {
        ShowPanel(charactorPanel.player, charactorPanel.unitID);
    }

    public void ShowPanel(Player player, int unitID)
    {
        this.player = player;
        this.unitID = unitID;
        // TODO: 这里可以根据 player数据 获取对应的 PieceData
        pieceData = GM.Ins.DM.pieceDataListSO.GetPieceData(unitID);
        RefreshUI();
        Open();
        //gameObject.SetActive(true);
        detailPanel.gameObject.SetActive(false);
        InitPanel();
        /*for (int i = 0; i < 10; i++)
        {
            if (i >= rowList.Count) break;
            AttrModRow row = rowList[i];
            // 直接通过编号读取属性名和当前值
            string attrName = player.GetAttrName(i);
            int currentVal = player.AccessAttribute(i, AttrOp.Get);

            row.Setup(attrName, currentVal, 200);
        }*/

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
            string attrName = ((DamageType)i + 1).ToChinese() + "防护";
            int currentVal = pieceData._armorDic[(DamageType)i + 1];
            row.UpdateInfo(attrName, currentVal, 100);
        }

        skillPointNumText.text = player.SkillPoints.ToString();


        /*foreach (var group in passiveSkillGroupList)
        {
            group.gameObject.SetActive(false);
        }

        for (var index = 0; index < pieceData.passiveSkillTypes.Count; index++)
        {
            //var skillPack = pieceData.passiveSkillPacks[index];
            var skillData =
                GM.Ins.DM.passiveSkillConfigSO.GetSkillData(pieceData.passiveSkillTypes[index]);
            if (passiveSkillGroupList.Count > index)
            {
                passiveSkillGroupList[index].gameObject.SetActive(true);
                passiveSkillGroupList[index].skillTitle.text = skillData.skillName;
                passiveSkillGroupList[index].skillDesc.text = skillData.description;
            }
        }

        foreach (var group in activeSkillGroupList)
        {
            group.gameObject.SetActive(false);
        }

        for (var index = 0; index < pieceData.skillPacks.Count; index++)
        {
            var VARIABLE = pieceData.skillPacks[index];
            if (activeSkillGroupList.Count > index)
            {
                activeSkillGroupList[index].gameObject.SetActive(true);
                activeSkillGroupList[index].skillTitle.text = VARIABLE.skillName;
                activeSkillGroupList[index].skillDesc.text = VARIABLE.GetSkillDesc();
            }
        }*/
    }


    public void RefreshUI()
    {
        Debug.Log($"RefreshUI called for player: {player.NAME}, unitID: {unitID}");
        //avatarImage.sprite = Resources.Load<Sprite>("CG/" + player.spriteName);
        // weaponImage.sprite = pieceData.weaponIcon;
        // //nameText.text = player.NAME;
        //
        // // 1. 刷新普通装备槽
        // for (int i = 0; i < normalUISlots.Count; i++)
        // {
        //     int id = (i < player.normalSlots.Length) ? player.normalSlots[i] : 0;
        //     normalUISlots[i].Init(id, this);
        // }
        //
        // // 2. 刷新武器装备槽
        // for (int i = 0; i < weaponUISlots.Count; i++)
        // {
        //     int id = (i < player.weaponSlots.Length) ? player.weaponSlots[i] : 0;
        //     weaponUISlots[i].Init(id, this);
        // }
        //
        // // 3. 刷新插件背包 (预置格子循环利用)
        // for (int i = 0; i < plugsUISlots.Count; i++)
        // {
        //     // 如果玩家背包里的物品数量超过了预设格数，这里会截断（建议预设足够多）
        //     int id = (i < GM.Ins.PLAYERPROFILE.componentInventory.Count)
        //         ? GM.Ins.PLAYERPROFILE.componentInventory[i]
        //         : 0;
        //     plugsUISlots[i].Init(id, this);
        // }

        // 1. 获取玩家原始背包数据
        List<ItemPack> rawPacks = GM.Ins.PLAYERPROFILE.itemPacks;

        // 2. 根据当前选中的 Tag 进行数据筛选
        List<ItemPack> filteredPacks = new List<ItemPack>();

        foreach (var pack in rawPacks)
        {
            // 获取配置检查类型是否匹配
            ItemData data = GM.Ins.marketSystem.marketItemListSO.GetData(pack.itemName);
            if (data != null)
            {
                filteredPacks.Add(pack);
            }
        }

        // 3. 渲染到 UI 格子上
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < filteredPacks.Count)
            {
                itemSlots[i].SetItem(filteredPacks[i]);
            }
            else
            {
                itemSlots[i].Clear();
            }
        }

        tempPoints = player.SkillPoints;

        // 刷新属性面板

        // 0生命，1能量，2攻击，3暴击，4对抗
        battleRowList[0].UpdateInfo("生命", pieceData.maxHealth, 100);
        battleRowList[1].UpdateInfo("能量", pieceData.maxMana, 100);
        //ATK += 
        //CON += (int)(playerData.AccessAttribute(4, AttrOp.Get) * 0.5f);
        battleRowList[2].UpdateInfo("攻击", (int)(player.AccessAttribute(1, AttrOp.Get) * 0.5f), 100);
        battleRowList[3].UpdateInfo("暴击"
            , pieceData.critRate + (player.AccessAttribute(3, AttrOp.Get) +
                                    player.AccessAttribute(4, AttrOp.Get)) * 2, 100);
        battleRowList[4].UpdateInfo("对抗", (int)(player.AccessAttribute(4, AttrOp.Get) * 0.5f), 100);
        
    }

    void InitPanel()
    {
        //hasInited = true;
        // --- 优雅的遍历初始化 ---
        for (int i = 0; i < 10; i++)
        {
            //GameObject go = Instantiate(rowPrefab, container);
            if (i >= rowList.Count) break;
            AttrModRow row = rowList[i];

            int index = i; // 闭包陷阱注意
            // 直接通过编号读取属性名和当前值
            string attrName = player.GetAttrName(index);
            int currentVal = player.AccessAttribute(index, AttrOp.Get);

            row.Setup(attrName, currentVal, 200);
            row.ShowBtn(tempPoints > 0);


            // 绑定按钮
            row.plusBtn.onClick.RemoveAllListeners();
            row.plusBtn.onClick.AddListener(() =>
            {
                row.ChangePending(1, ref tempPoints);
                skillPointNumText.text = tempPoints.ToString();
            });
            row.minusBtn.onClick.RemoveAllListeners();
            row.minusBtn.onClick.AddListener(() =>
            {
                row.ChangePending(-1, ref tempPoints);
                skillPointNumText.text = tempPoints.ToString();
            });

            //rowList.Add(row);
        }

        applyBtn.SetActive(tempPoints > 0);
        resetBtn.SetActive(tempPoints > 0);
    }

    

    

    public void OnClickSkillPoints()
    {
        // 打开技能点面板
        //skillPointPanel.ShowPanel(player, unitID);
    }

    public void ApplyPoints()
    {
        // 1. 修改副本数据
        player.SkillPoints = tempPoints;
        for (int i = 0; i < rowList.Count; i++)
        {
            int added = rowList[i].PendingAdd;
            player.AccessAttribute(i, AttrOp.Add, added); // 使用 Add 模式
            rowList[i].Commit();
        }


        //GM.Ins.PLAYERPROFILE.player[unitID] = playerData;
        // 2. 【重点：存档回写】
        // 在这里将修改后的 playerData 结构体存入你的存档系统或全局管理器
        // SaveToDisk(playerData); 
        RefreshUI();
        //InitPanel(); // 重新初始化界面显示，确保数值更新
        //ShowPanel(player,unitID); // 刷新主界面显示
        Debug.Log("数据已更新至结构体并触发保存逻辑");
    }

    public void ResetPoints()
    {
        foreach (var row in rowList) row.ResetRow(ref tempPoints);
        skillPointNumText.text = tempPoints.ToString();
        //RefreshUI();
        //InitPanel();
    }
}

[System.Serializable]
public class SkillGroup
{
    public GameObject gameObject;
    public Text skillTitle;
    public Text skillDesc;
}