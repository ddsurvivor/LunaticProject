
    using System.Collections.Generic;
    using UnityEngine;

    // 菜单创建CreateAssetMenu
    [CreateAssetMenu(fileName = "ShopListSO", menuName = "BattleSO/ShopListSO", order = 1)]
    public class ShopListSO: ScriptableObject
    {
        public List<ShopData> shopDatas = new();
        
        public ShopData GetShopData(int shopId)
        {
            return shopDatas.Find(shop => shop.shopId == shopId);
        }
    }
