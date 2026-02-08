using System.Collections.Generic;
using UnityEngine;


    public class PickableItem: InteractArea
    {
        public List<ItemPack> itemPackList = new();
        
        public void SetItems(List<ItemPack> items)
        {
            itemPackList = items;
        }
        public override void TriggerAction(PieceController piece = null)
        {
            base.TriggerAction(piece);
            foreach (var itemPack in itemPackList)
            {
                // 直接获得物品
                GM.Ins.PLAYERPROFILE.AddItem(itemPack.itemName, itemPack.itemNum);
            }
            gameObject.SetActive(false);
        }
    }
