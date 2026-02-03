
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 交易物品单元
    /// </summary>
    public class MarketItemCell: MonoBehaviour
    {
        public Image image;
        public Text nameText;
        public Text numText;
        
        public ItemPack itemPack;

        public void Init(ItemPack itemPack)
        {
            this.itemPack = itemPack;
        }

        public void UpdateDisplay()
        {
            ItemData itemData = GM.Ins.marketSystem.marketItemListSO.GetData(itemPack.itemId);
            image.sprite = itemData.itemIcon;
            nameText.text = itemData.itemName;
            numText.text = "x" + itemPack.itemNum;
        }


        public void OnClick()
        {
            
        }
    }
