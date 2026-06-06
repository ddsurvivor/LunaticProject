using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 交易系统
/// </summary>
public class MarketSystem : MonoBehaviour
{
    /// <summary>
    /// 道具数据
    /// </summary>
    public MarketItemListSO marketItemListSO;
    
    //public GameObject marketPanel;
    public MarketItemCell marketItemCellPrefab;
    public ShopListSO shopListSO;
    
    
    [Button("打开交易面板")]
    /// <summary>
    /// 打开交易面板
    /// </summary>
    public void OpenMarketPanel(int shopId, int discountPercent = 100)
    {
        ShopData shopData = shopListSO.GetShopData(shopId);
        if(shopData.itemPacks == null)
        {
            Debug.LogError("商店数据不存在，商店ID：" + shopId);
            return;
        }
        大地图System.instance.inventoryPanel.ShowShop(shopData, discountPercent);
        // 显示玩家背包界面，点击玩家物品可卖出
        // 显示商店界面，点击商店物品可购买
    }
}