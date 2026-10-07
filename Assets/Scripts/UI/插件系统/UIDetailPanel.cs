using UnityEngine;
using UnityEngine.UI;

public class UIDetailPanel : MonoBehaviour
{
    public Text titleText;
    public Text descText;
    public GameObject equipButton;
    public GameObject unequipButton;

    private int targetID;
    private WeaponPanel mainPage;

    public void Setup(int id, WeaponPanel page)
    {
        targetID = id;
        mainPage = page;
        
        ComponentData data = GM.Ins.DM.componentConfig.GetData(id);
        if (data == null || mainPage == null || mainPage.charactorPanel.player == null)
        {
            gameObject.SetActive(false);
            return;
        }

        titleText.text = data.itemName;
        descText.text = data.description;

        bool isEquipped = mainPage.CheckIsEquipped(id);
        equipButton.gameObject.SetActive(!isEquipped);
        unequipButton.gameObject.SetActive(isEquipped);
    }

    public void OnEquipClick()
    {
        if (!mainPage.charactorPanel.player.TryEquip(targetID))
        {
            descText.text = "装备失败：没有可用槽位，或该插件已不在背包中。";
            return;
        }
        mainPage.RefreshUI();
        gameObject.SetActive(false);
    }

    public void OnUnequipClick()
    {
        if (!mainPage.charactorPanel.player.TryUnequip(targetID))
        {
            descText.text = "卸下失败：该角色当前未装备此插件。";
            return;
        }
        mainPage.RefreshUI();
        gameObject.SetActive(false);
    }
}
