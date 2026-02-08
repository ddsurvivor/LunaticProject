using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    public List<ItemPack> itemPackList = new();

    /// <summary>
    /// 掉落需拾取的物品
    /// </summary>
    public void DrapItem()
    {
        foreach (var itemPack in itemPackList)
        {
            Debug.Log($"掉落物品: {itemPack.itemName} x {itemPack.itemNum}");
        }

        // 在这里实现掉落物品的逻辑，比如生成物品在地面上
        PickableItem pickableItem = ObjectPool.Ins.GenerateObject(ItemType.PICKABLE_ITEM,
            transform.position, Quaternion.identity).GetComponent<PickableItem>();
        pickableItem.SetItems(itemPackList);
    }
    

    /// <summary>
    /// 直接将奖励加到仓库中
    /// </summary>
    public void DirectGetItem()
    {
        foreach (var itemPack in itemPackList)
        {
            // 直接获得物品
            GM.Ins.PLAYERPROFILE.AddItem(itemPack.itemName, itemPack.itemNum);
        }
    }
}