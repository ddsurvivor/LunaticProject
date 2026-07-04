using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// 菜单创建CreateAssetMenu
[CreateAssetMenu(fileName = "MarketItemListSO", menuName = "BattleSO/MarketItemListSO", order = 1)]
public class MarketItemListSO : SerializedScriptableObject
{
    public List<ItemData> itemDataList = new();

    public ItemData GetData(ItemName itemName)
    {
        return itemDataList.Find(itemData => itemData.itemName == itemName);
    }
}

public class ItemData
{
    // 道具数据
    public int itemId;
    public ItemName itemName;
    public string techName; // 技术名称
    public string itemDescription;
    public Sprite itemIcon;
    public ItemTag itemTag; // 道具标签
    public EquipType equipType; // 道具类别
    public UseType useType; // 使用类别
    [LabelText("技能名称")][ShowIf("@this.useType == UseType.ActiveInBattle")] 
    public string skillPack; // 关联技能包ID，0表示无关联
    public int price; // 价格
}

public enum ItemName
{
    通用作战平台_CW179=0,
    魔女兵器=1,
    UX210_枪骑兵=2,
    能量包=3,
    医疗单元I型=4,
    专速达=5,
    礼盒=6,
    巡飞雷=7,
    修复集群=8,
    应急医疗单元=9,
    能量包S=10,
    便携手榴弹 = 201,
    
}

// 装备类别
public enum EquipType
{
    [LabelText("装备")] Equipment = 1
    ,
    [LabelText("消耗品")]Consumable = 2
}

public enum UseType
{
    [LabelText("作战检定")] InBattle = 1
    ,
    [LabelText("战斗外使用")]OutOfBattle = 2,
    // 能量不满时
    [LabelText("能量不满时")]WhenEnergyNotFull =3
    // 生命不满时
    ,[LabelText("生命不满时")]WhenHpNotFull =4
    // 耐力不满时
    ,[LabelText("耐力不满时")]WhenStaminaNotFull =5
    // 在战斗中主动使用
    ,[LabelText("主动使用")]ActiveInBattle =6
}