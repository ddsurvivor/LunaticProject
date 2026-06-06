using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 交易面板
/// </summary>
public class InventoryPanel : UIPanel
{
    public List<MarketItemCell> cells = new();
    public List<MarketItemCell> marketItemCells = new();

    public List<ItemPack> buyingList = new(); // 玩家购买的物品列表
    public List<ItemPack> sellingList = new(); // 玩家出售的物品列表

    public Text playerMoneyText;

    private ShopData currentShopData;
    
    
    // 打折
    public int discountPercent = 100; // 默认不打折

    public void ShowShop(ShopData shopData, int discountPercent = 100)
    {
        currentShopData = shopData.Clone();;
        gameObject.SetActive(true);
        this.discountPercent = discountPercent;
        //UpdateDisplay();
    }

    public override void Init()
    {
        base.Init();
        foreach (var cell in cells)
        {
            cell.Init(this);
        }

        foreach (var cell in marketItemCells)
        {
            cell.Init(this, true);
        }

        buyingList.Clear();
        sellingList.Clear();
    }

    public override void UpdateDisplay()
    {
        base.UpdateDisplay();
        foreach (var cell in cells)
        {
            cell.Init(this);
        }

        foreach (var cell in marketItemCells)
        {
            cell.Init(this, true);
        }
        for (int i = 0; i < GM.Ins.PLAYERPROFILE.itemPacks.Count; i++)
        {
            if (cells.Count > i)
            {
                cells[i].SetItem(GM.Ins.PLAYERPROFILE.itemPacks[i]);
            }
        }

        for (int i = 0; i < currentShopData.itemPacks.Count; i++)
        {
            if (marketItemCells.Count > i)
            {
                marketItemCells[i].SetItem(currentShopData.itemPacks[i], true);
            }
        }

        playerMoneyText.text =  GM.Ins.PLAYERPROFILE.coins.ToString();
    }

    public void AddOrIncreaseCount(List<ItemPack> list, ItemName name, int addNum, int cap)
    {
        var pack = list.Find(p => p.itemName == name);
        if (addNum > 0)
        {
            if (pack != null)
            {
                if (pack.itemNum + addNum <= cap)
                {
                    pack.itemNum += addNum;
                }
            }
            else
            {
                if (addNum <= cap)
                {
                    list.Add(new ItemPack(name, addNum));
                }
            }
        }
        else
        {
            if (pack != null)
            {
                pack.itemNum += addNum;
                if (pack.itemNum <= 0)
                {
                    list.Remove(pack);
                }
            }
        }
    }

    public void OnConfirmButtonClicked()
    {
        // 1. 检查钱够不够买 buyingList 的东西
        int totalCost = CalculateAllCost();
        if (!GM.Ins.PLAYERPROFILE.HasEnoughMoney(totalCost))
        {
            Debug.Log("金币不足!");
            return;
        }

        // 2. 检查玩家背包空间是否够放 buyingList（可选）
        // if (!playerInventory.HasEnoughSpace(buyingList)) { ... }

        // 3. 执行交易
        foreach (var pack in buyingList)
        {
            GM.Ins.PLAYERPROFILE.AddItem(pack.itemName, pack.itemNum);
            ModifyItem(currentShopData.itemPacks, pack.itemName, -pack.itemNum);
        }

        foreach (var pack in sellingList)
        {
            GM.Ins.PLAYERPROFILE.CostItem(pack.itemName, pack.itemNum);
            ModifyItem(currentShopData.itemPacks, pack.itemName, pack.itemNum);
        }

        // 4. 扣钱
        GM.Ins.PLAYERPROFILE.CostMoney(totalCost);

        // 4. 清空暂存 & 刷新所有格子
        buyingList.Clear();
        sellingList.Clear();

        UpdateDisplay();
    }

    private int CalculateAllCost()
    {
        int sum = 0;
        foreach (var pack in buyingList)
        {
            sum += GetItemPriceWithDiscount(pack.itemName) * pack.itemNum;
        }

        foreach (var pack in sellingList)
        {
            sum -= GetItemPrice(pack.itemName) * pack.itemNum;
        }

        return sum;
    }

    private int GetItemPrice(ItemName itemName)
    {
        // 从市场数据获取物品价格
        var itemData = GM.Ins.marketSystem.marketItemListSO.GetData(itemName);
        return itemData != null ? itemData.price : 0;
    }
    private int GetItemPriceWithDiscount(ItemName itemName)
    {
        // 从市场数据获取物品价格并应用折扣
        int basePrice = GetItemPrice(itemName);
        return Mathf.RoundToInt(basePrice * discountPercent / 100f);
    }
    
    public void ModifyItem(List<ItemPack> targetPack, ItemName itemName, int num)
    {
        if (targetPack == null) return;

        var item = targetPack.Find(t => t.itemName == itemName);

        if (num > 0)
        {
            if (item != null)
            {
                item.itemNum += num;
            }
            else
            {
                targetPack.Add(new ItemPack(itemName, num));
            }
            Debug.Log($"添加道具: {itemName} 数量: {num}");
        }
        else if (num < 0)
        {
            if (item != null)
            {
                item.itemNum += num; // num为负数
                if (item.itemNum <= 0)
                {
                    item.itemNum = 0;
                    targetPack.Remove(item);
                }
                Debug.Log($"消耗道具: {itemName} 数量: {-num}");
            }
        }
        // num == 0 不做操作
    }
}