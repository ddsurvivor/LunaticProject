using System.Collections.Generic;

[System.Serializable]
public class ShopData
{
    public int shopId;
    public int coins;
    public List<ItemPack> itemPacks = new();
}