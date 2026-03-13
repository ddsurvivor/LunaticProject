using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class BackpackPanel : MonoBehaviour
{
    [Header("背包滚动面板")]
    [SerializeField] private ScrollRect backpackScrollRect;   // 拖入 BackpackScrollView

    [Header("预设的30个格子")]
    [SerializeField] private ItemSlot[] itemSlots = new ItemSlot[30];  // 在Inspector里全部拖入！

    private void Awake()
    {
        // 可选：确保初始全为空
        ClearAllSlots();
    }

    /// <summary>
    /// 打开背包并刷新
    /// </summary>
    public void OnEnable()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 刷新背包（每次打开或道具变化时调用）
    /// </summary>
    public void Refresh()
    {
        List<ItemPack> packs = GM.Ins.PLAYERPROFILE.itemPacks;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < packs.Count)
            {
                itemSlots[i].SetItem(packs[i]);   // 显示道具
            }
            else
            {
                itemSlots[i].Clear();             // 剩余格子显示空
            }
        }

        // 滚动到顶部（好习惯）
        backpackScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ClearAllSlots()
    {
        foreach (var slot in itemSlots)
            slot.Clear();
    }
}