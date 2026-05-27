using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("组件引用")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text countText; // 使用旧版 Text

    private ItemPack currentPack;
    private ComponentData currentComponentData; // 当前格子存储的组件数据（如果是插件格子）
    private Action<string, string, Vector2> onSlotClicked; // 点击回调，传入数据和点击位置
    
    /// <summary>
    /// 初始化格子，绑定点击事件
    /// </summary>
    public void Init(Action<string, string, Vector2> onClickAction)
    {
        this.onSlotClicked = onClickAction;
    }

    public void SetItem(ItemPack pack)
    {
        this.currentPack = pack;

        if (pack == null || pack.itemNum <= 0)
        {
            Clear();
            return;
        }

        ItemData data = GM.Ins.marketSystem.marketItemListSO.GetData(pack.itemName);
        if (data != null && data.itemIcon != null)
        {
            iconImage.sprite = data.itemIcon;
            iconImage.enabled = true;
            countText.text = pack.itemNum.ToString();
            countText.enabled = true;
        }
        else
        {
            Clear();
            Debug.LogWarning($"ItemName {pack.itemName} 没有找到对应的 ItemData");
        }
    }

    public void SetPlugs(int id)
    {
        // 从配置表获取数据来显示
        ComponentData data = GM.Ins.DM.componentConfig.GetData(id);
        if (data != null)
        {
            this.currentComponentData = data;
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
            countText.text = "1";
            countText.enabled = true;
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        currentPack = null;
        iconImage.sprite = null;
        iconImage.enabled = false;
        countText.text = "";
        countText.enabled = false;
    }

    // 实现 UGUI 点击接口
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentPack != null && onSlotClicked != null)
        {
            ItemData data = GM.Ins.marketSystem.marketItemListSO.GetData(currentPack.itemName);
            if (data == null) return;
            // 传递当前物品数据和鼠标点击的屏幕坐标
            onSlotClicked.Invoke(data.itemName.ToString(),data.itemDescription, eventData.position);
        }
        else if (currentComponentData != null && onSlotClicked != null)
        {
            // 传递当前组件数据和鼠标点击的屏幕坐标
            onSlotClicked.Invoke(currentComponentData.itemName.ToString(), currentComponentData.description, eventData.position);
        }
    }
}