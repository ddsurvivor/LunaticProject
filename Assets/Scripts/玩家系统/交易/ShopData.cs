using System.Collections.Generic;

[System.Serializable]
public struct ShopData
{
    public int shopId;// 商店ID
    public int coins;// 商店金币数量
    public List<ItemPack> itemPacks;// 商店出售的物品列表

    /// <summary>
    /// 创建一个浅拷贝副本（断开 List 的引用连接）
    /// </summary>
    public ShopData Clone()
    {
        var clonedPacks = new List<ItemPack>();
        
        if (this.itemPacks != null)
        {
            foreach (var pack in this.itemPacks)
            {
                if (pack != null)
                {
                    // 关键点：通过 new 构造函数，为每个物品创建全新对象
                    // 假设 ItemPack 的构造函数是 ItemPack(ItemName name, int num)
                    clonedPacks.Add(new ItemPack(pack.itemName, pack.itemNum));
                }
            }
        }

        return new ShopData
        {
            shopId = this.shopId,
            coins = this.coins,
            itemPacks = clonedPacks
        };
    }
}

public class ShopItemData
{
    ItemPack itemPack;// 商店出售的物品
    public int sellPrice;// 商店出售价格
}