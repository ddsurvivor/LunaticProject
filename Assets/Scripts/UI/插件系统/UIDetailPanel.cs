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
    private Player equippedOwner;

    public void Setup(int id, WeaponPanel page, Player owner = null)
    {
        targetID = id;
        mainPage = page;
        equippedOwner = owner;
        
        ComponentData data = GM.Ins.DM.componentConfig.GetData(id);
        if (data == null || mainPage == null || mainPage.charactorPanel.player == null)
        {
            gameObject.SetActive(false);
            return;
        }

        titleText.text = data.itemName;
        descText.text = owner == null ? data.description : $"{owner.NAME}已装配\n{data.description}";

        bool equippedByOther = owner != null && owner != mainPage.charactorPanel.player;
        bool isEquipped = mainPage.CheckIsEquipped(id);
        equipButton.gameObject.SetActive(!equippedByOther && !isEquipped);
        unequipButton.gameObject.SetActive(!equippedByOther && isEquipped);
    }

    public void OnEquipClick()
    {
        if (equippedOwner != null && equippedOwner != mainPage.charactorPanel.player) return;
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
        if (equippedOwner != null && equippedOwner != mainPage.charactorPanel.player) return;
        if (!mainPage.charactorPanel.player.TryUnequip(targetID))
        {
            descText.text = "卸下失败：该角色当前未装备此插件。";
            return;
        }
        mainPage.RefreshUI();
        gameObject.SetActive(false);
    }
}
