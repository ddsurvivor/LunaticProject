
[System.Serializable]
    public class ItemPack
    {
        public ItemName itemName;
        public int itemNum;
        public ItemPack(ItemName itemName, int itemNum)
        {
            this.itemName = itemName;
            this.itemNum = itemNum;
        }
    }
