using UnityEngine;
using UnityEngine.UI;
//using TMPro;

public class ItemSlot : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private Image iconImage;
    //[SerializeField] private TextMeshProUGUI countText;

    /// <summary>
    /// 设置道具（空道具传入 null 或 itemNum <= 0）
    /// </summary>
    public void SetItem(ItemPack pack)
    {
        if (pack == null || pack.itemNum <= 0)
        {
            Clear();
            return;
        }

        // ★★★★★ 根据 ItemName 获取 ItemData ★★★★★
        ItemData data = GM.Ins.marketSystem.marketItemListSO.GetData(pack.itemName);
        if (data != null && data.itemIcon != null)
        {
            iconImage.sprite = data.itemIcon;
            iconImage.enabled = true;
            //countText.text = pack.itemNum.ToString();   // 数量始终显示（包括1）
        }
        else
        {
            Clear();
            Debug.LogWarning($"ItemName {pack.itemName} 没有找到对应的 ItemData");
        }
    }

    /// <summary>
    /// 清空格子（显示空状态）
    /// </summary>
    public void Clear()
    {
        iconImage.sprite = null;
        iconImage.enabled = false;
        //countText.text = "";
    }

    
}