using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemGetPanel : MonoBehaviour
{
    public Image icon;
    public Text itemNameText;
    public Text itemDescText;
    public Text itemNumText;

    public void ShowPanel(ItemPack itemPack)
    {
        if(itemPack == null)
        {
            Debug.LogError("ItemPack is null!");
            return;
        }


        ItemData itemData = GM.Ins.marketSystem.marketItemListSO.GetData(itemPack.itemName);
        if(itemData == null)
        {
            Debug.LogError("ItemData not found for item: " + itemPack.itemName);
            return;
        }

        gameObject.SetActive(true);
        icon.sprite = itemData.itemIcon;
        itemNameText.text = itemData.itemName.ToString();
        itemDescText.text = itemData.itemDescription;
        itemNumText.text = itemPack.itemNum.ToString();
    }

    public void ShowPanel(ComponentData componentData)
    {
        if (componentData == null)
        {
            Debug.LogError("ComponentData is null!");
            return;
        }
        
        gameObject.SetActive(true);
        icon.sprite = componentData.icon;
        itemNameText.text = componentData.itemName.ToString();
        itemDescText.text = componentData.description;
        itemNumText.text = "1";
    }
}
