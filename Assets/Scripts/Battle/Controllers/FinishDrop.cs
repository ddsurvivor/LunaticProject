using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗胜利后掉落
/// </summary>
public class FinishDrop : MonoBehaviour
{

    public List<ItemPack> dropList = new();
    public void DropItems()
    {
        // 根据掉落列表生成道具
        foreach (var itemPack in dropList)
        {
            Debug.Log($"掉落了{itemPack.itemNum}个{itemPack.itemName}");
            // 添加道具到存档里
            GM.Ins.PLAYERPROFILE.AddItem(itemPack.itemName, itemPack.itemNum);
        }
    }
}
