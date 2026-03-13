
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// 交易物品单元
    /// </summary>
    public class MarketItemCell: MonoBehaviour, IPointerClickHandler
    {
        public Image image;
        public Text nameText;
        public Text numText;
        public Text selectNumText;
        public ItemPack itemPack;
        
        private InventoryPanel inventoryPanel;
        private bool isMarketItem = false;// 是否为商店物品

        public void Init(InventoryPanel inventoryPanel, bool isMarketItem = false)
        {
            this.inventoryPanel = inventoryPanel;
            this.isMarketItem = isMarketItem;
            image.gameObject.SetActive(false);
            nameText.text = "";
            numText.text = "";
            selectNumText.text = "";
        }
        
        public void SetItem(ItemPack itemPack, bool market = false)
        {
            this.itemPack = itemPack;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            ItemData itemData = GM.Ins.marketSystem.marketItemListSO.GetData(itemPack.itemName);
            if (itemData.itemIcon != null)
            {
                image.gameObject.SetActive(true);
                image.sprite = itemData.itemIcon;
            }
            nameText.text = itemData.itemName.ToString();
            numText.text = "x" + itemPack.itemNum;
            int selectedCount = GetCurrentSelectedCount();
            selectNumText.gameObject.SetActive(selectedCount > 0);
            selectNumText.text = selectedCount.ToString();
        }


        public void OnClick()
        {
            if (!isMarketItem)
            {
                // 卖 → 加到 sellingList
                inventoryPanel.AddOrIncreaseCount(inventoryPanel.sellingList, itemPack.itemName, 1, itemPack.itemNum);
            }
            else
            {
                // 买 → 加到 buyingList
                inventoryPanel.AddOrIncreaseCount(inventoryPanel.buyingList, itemPack.itemName, 1, itemPack.itemNum);
            }
            UpdateDisplay();
        }
        
        public int GetCurrentSelectedCount()
        {
            if (!isMarketItem)
            {
                var pack = inventoryPanel.sellingList.Find(p => p.itemName == itemPack.itemName);
                return pack?.itemNum ?? 0;
            }
            else
            {
                var pack = inventoryPanel.buyingList.Find(p => p.itemName == itemPack.itemName);
                return pack?.itemNum ?? 0;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (itemPack == null)
            {
                return;
            }
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (!isMarketItem)
                {
                    // 卖 → 加到 sellingList
                    inventoryPanel.AddOrIncreaseCount(inventoryPanel.sellingList, itemPack.itemName, 1, itemPack.itemNum);
                }
                else
                {
                    // 买 → 加到 buyingList
                    inventoryPanel.AddOrIncreaseCount(inventoryPanel.buyingList, itemPack.itemName, 1, itemPack.itemNum);
                }
                UpdateDisplay();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (!isMarketItem)
                {
                    // 卖 → 加到 sellingList
                    inventoryPanel.AddOrIncreaseCount(inventoryPanel.sellingList, itemPack.itemName, -1, itemPack.itemNum);
                }
                else
                {
                    // 买 → 加到 buyingList
                    inventoryPanel.AddOrIncreaseCount(inventoryPanel.buyingList, itemPack.itemName, -1, itemPack.itemNum);
                }
                UpdateDisplay();
            }
        }
    }
