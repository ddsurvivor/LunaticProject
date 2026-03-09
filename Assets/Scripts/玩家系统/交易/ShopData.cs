using System.Collections.Generic;

[System.Serializable]
public class ShopData
{
    public int shopId;// 商店ID
    public int coins;// 商店金币数量
    public List<ItemPack> itemPacks = new();// 商店出售的物品列表
}

public class ShopItemData
{
    ItemPack itemPack;// 商店出售的物品
    public int sellPrice;// 商店出售价格
}