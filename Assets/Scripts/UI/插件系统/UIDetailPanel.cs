using UnityEngine;
using UnityEngine.UI;

public class UIDetailPanel : MonoBehaviour
{
    public Text titleText;
    public Text descText;
    public Button equipButton;
    public Button unequipButton;

    private int targetID;
    private CharacterUIPage mainPage;

    public void Setup(int id, CharacterUIPage page)
    {
        targetID = id;
        mainPage = page;
        
        ComponentData data = GM.Ins.DM.componentConfig.GetData(id);
        if (data == null) return;

        titleText.text = data.itemName;
        descText.text = data.description;

        bool isEquipped = mainPage.CheckIsEquipped(id);
        equipButton.gameObject.SetActive(!isEquipped);
        unequipButton.gameObject.SetActive(isEquipped);
    }

    public void OnEquipClick()
    {
        mainPage.player.Equip(targetID);
        mainPage.RefreshUI();
        gameObject.SetActive(false);
    }

    public void OnUnequipClick()
    {
        mainPage.player.Unequip(targetID);
        mainPage.RefreshUI();
        gameObject.SetActive(false);
    }
}