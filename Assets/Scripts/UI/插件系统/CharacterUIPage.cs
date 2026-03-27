using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterUIPage : MonoBehaviour
{
    public Player player;
    public Image avatarImage;
    public UIDetailPanel detailPanel;
    public GameObject slotPrefab;

    public Transform normalSlotsParent;
    public Transform weaponSlotsParent;
    public Transform inventoryGrid;

    void Start()
    {
        RefreshUI();
        detailPanel.gameObject.SetActive(false);
    }

    public void RefreshUI()
    {
        avatarImage.sprite  = Resources.Load<Sprite>("CG/" + player.spriteName);
        
        // 刷新普通槽位 (int[])
        UpdateSlots(normalSlotsParent, player.normalSlots);
        // 刷新武器槽位 (int[])
        UpdateSlots(weaponSlotsParent, player.weaponSlots);
        // 刷新背包 (List<int>)
        UpdateInventory();
    }

    private void UpdateSlots(Transform parent, int[] ids)
    {
        UIItemSlot[] uiSlots = parent.GetComponentsInChildren<UIItemSlot>();
        for (int i = 0; i < uiSlots.Length; i++)
        {
            uiSlots[i].Init(i < ids.Length ? ids[i] : 0, this);
        }
    }

    private void UpdateInventory()
    {
        foreach (Transform child in inventoryGrid) Destroy(child.gameObject);
        foreach (int id in player.componentInventory)
        {
            GameObject go = Instantiate(slotPrefab, inventoryGrid);
            go.GetComponent<UIItemSlot>().Init(id, this);
        }
    }

    public void ShowDetail(int id, Vector3 pos)
    {
        detailPanel.gameObject.SetActive(true);
        detailPanel.transform.position = pos + new Vector3(120, -60, 0);
        detailPanel.Setup(id, this);
    }

    public bool CheckIsEquipped(int id)
    {
        foreach (int i in player.normalSlots) if (i == id) return true;
        foreach (int i in player.weaponSlots) if (i == id) return true;
        return false;
    }
}