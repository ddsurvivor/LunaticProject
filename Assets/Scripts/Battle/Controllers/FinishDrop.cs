using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗胜利后掉落
/// </summary>
public class FinishDrop : MonoBehaviour
{
    public List<ItemPack> dropList = new();
    public int finishExp = 100;//默认经验值
    public void FinishExp()
    {
        Debug.Log($"获得了{finishExp}经验值");
        // 添加经验值到存档里
    }
    public void DropItems()
    {
        // 根据掉落列表生成道具
        foreach (var itemPack in dropList)
        {
            Debug.Log($"掉落了{itemPack.itemNum}个{itemPack.itemName}");
            // 添加道具到存档里
            GM.Ins.PLAYERPROFILE.AddItem(itemPack.itemName, itemPack.itemNum);
        }
        FinishExp();
        
        // 显示掉落界面
        //BattleScene.Ins.UM.messagePanel.ShowItemGet(dropList);
    }
}
