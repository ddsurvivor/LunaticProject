using System.Collections;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 战斗胜利后掉落
/// </summary>
public class FinishDrop : MonoBehaviour
{
    [LabelText("掉落列表")]
    public List<ItemPack> dropList = new();
    //public int finishExp = 100;//默认经验值
    [LabelText("掉落组件ID列表")]
    public List<int> dropCompIds = new List<int>();//掉落组件ID列表
    
    [LabelText("掉落信息汇总")]
    [ReadOnly]
    public string dropSummary = "";
    public void DropItems()
    {
        StringBuilder sb = new StringBuilder();
        // 根据掉落列表生成道具
        foreach (var itemPack in dropList)
        {
            Debug.Log($"掉落了{itemPack.itemNum}个{itemPack.itemName}");
            sb.AppendLine($"掉落了{itemPack.itemNum}个{itemPack.itemName}");
            // 添加道具到存档里
            GM.Ins.PLAYERPROFILE.AddItem(itemPack.itemName, itemPack.itemNum);
        }

        foreach (var dropCompId in dropCompIds)
        {
            Debug.Log($"掉落了组件ID: {dropCompId}");
            ComponentData compData = GM.Ins.DM.componentConfig.GetData(dropCompId);
            sb.AppendLine($"掉落了组件: {compData.itemName}");
            // 添加组件到存档里
            GM.Ins.PLAYERPROFILE.AddComponentToInventory(dropCompId);
        }
        dropSummary = sb.ToString();
        // 显示掉落界面
        //BattleScene.Ins.UM.messagePanel.ShowItemGet(dropList);
    }
}
