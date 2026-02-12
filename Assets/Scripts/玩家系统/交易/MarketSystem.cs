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
    public GameObject marketPanel;
    public MarketItemCell marketItemCellPrefab;
    
    // 打开背包面板
    public void OpenInventoryPanel()
    {
        inventoryPanel.gameObject.SetActive(true);
        // 根据玩家存档刷新背包显示
    }
    
    /// <summary>
    /// 打开交易面板
    /// </summary>
    /// <param name="shopData">交易对象商店</param>
    public void OpenMarketPanel(ShopData shopData)
    {
        marketPanel.SetActive(true);
    }
}