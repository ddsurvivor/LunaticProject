
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    // 菜单创建CreateAssetMenu
    [CreateAssetMenu(fileName = "MarketItemListSO", menuName = "BattleSO/MarketItemListSO", order = 1)]
    public class MarketItemListSO: SerializedScriptableObject
    {
        public List<ItemData> itemDataList = new();
        
        public ItemData GetData(int itemId)
        {
            return itemDataList.Find(itemData => itemData.itemId == itemId);
        }
    }

    public class ItemData
    {
        // 道具数据
        public int itemId;
        public string itemName;
        public string itemDescription;
        public Sprite itemIcon;
    }
