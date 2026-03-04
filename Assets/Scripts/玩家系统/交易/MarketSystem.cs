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
    public InventoryPanel inventoryPanel;
    //public GameObject marketPanel;
    public MarketItemCell marketItemCellPrefab;
    public ShopListSO shopListSO;
    
    // 打开背包面板
    public void OpenInventoryPanel()
    {
        inventoryPanel.gameObject.SetActive(true);
        // 根据玩家存档刷新背包显示
    }
    
    [Button("打开交易面板")]
    /// <summary>
    /// 打开交易面板
    /// </summary>
    public void OpenMarketPanel(int shopId)
    {
        ShopData shopData = shopListSO.GetShopData(shopId);
        inventoryPanel.ShowShop(shopData);
        // 显示玩家背包界面，点击玩家物品可卖出
        // 显示商店界面，点击商店物品可购买
    }
}