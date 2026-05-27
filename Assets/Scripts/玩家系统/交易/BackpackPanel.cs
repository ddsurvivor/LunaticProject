using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 物品标签类型
/// </summary>
public enum ItemTag
{
    All,        // 全部
    Consumables,// 消耗品
    Plugins,    // 插件
    Materials   // 材料（示例扩展）
}
public class BackpackPanel : MonoBehaviour
{
    [Header("背包滚动面板")]
    [SerializeField] private ScrollRect backpackScrollRect;

    [Header("预设的30个格子")]
    [SerializeField] private ItemSlot[] itemSlots = new ItemSlot[30];

    [Header("详情面板引用")]
    [SerializeField] private ItemDetailPanel detailPanel;

    [Header("分类标签按钮")]
    [SerializeField] private Button btnAll;
    [SerializeField] private Button btnConsumables;
    [SerializeField] private Button btnPlugins;

    private ItemTag currentTag = ItemTag.All; // 当前选中的标签

    private void Awake()
    {
        // 初始化所有格子并绑定点击事件
        foreach (var slot in itemSlots)
        {
            slot.Init(OnSlotSelected);
        }

        // 绑定标签按钮事件
        if (btnAll != null) btnAll.onClick.AddListener(() => SwitchTag(ItemTag.All));
        if (btnConsumables != null) btnConsumables.onClick.AddListener(() => SwitchTag(ItemTag.Consumables));
        if (btnPlugins != null) btnPlugins.onClick.AddListener(() => SwitchTag(ItemTag.Plugins));

        ClearAllSlots();
        if (detailPanel != null) detailPanel.Close();
    }

    public void OnEnable()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (detailPanel != null) detailPanel.Close();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 切换分类标签
    /// </summary>
    public void SwitchTag(ItemTag newTag)
    {
        currentTag = newTag;
        if (detailPanel != null) detailPanel.Close(); // 切换标签时关闭详情
        Refresh();
    }

    /// <summary>
    /// 刷新背包（核心过滤逻辑）
    /// </summary>
    public void Refresh()
    {
        // 1. 获取玩家原始背包数据
        List<ItemPack> rawPacks = GM.Ins.PLAYERPROFILE.itemPacks;
        
        // 2. 根据当前选中的 Tag 进行数据筛选
        List<ItemPack> filteredPacks = new List<ItemPack>();

        foreach (var pack in rawPacks)
        {
            if (currentTag == ItemTag.All)
            {
                filteredPacks.Add(pack);
            }
            else
            {
                // 获取配置检查类型是否匹配
                ItemData data = GM.Ins.marketSystem.marketItemListSO.GetData(pack.itemName);
                if (data != null && data.itemTag == currentTag)
                {
                    filteredPacks.Add(pack);
                }
            }
        }

        // 3. 渲染到 UI 格子上
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < filteredPacks.Count)
            {
                itemSlots[i].SetItem(filteredPacks[i]);
            }
            else
            {
                itemSlots[i].Clear();
            }
        }

        if (currentTag == ItemTag.All || currentTag == ItemTag.Plugins)// 插件显示特殊逻辑
        {
            for (int i = filteredPacks.Count; i < itemSlots.Length; i++)
            {
                int index = i - filteredPacks.Count;
                int id = (index < GM.Ins.PLAYERPROFILE.componentInventory.Count) ? GM.Ins.PLAYERPROFILE.componentInventory[index] : 0;
                itemSlots[i].SetPlugs(id);
                Debug.Log($"插件格子 {i} 显示组件 ID {id}");
            }
        }

        //backpackScrollRect.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// 格子点击的具体响应
    /// </summary>
    private void OnSlotSelected(string name, string desc, Vector2 mousePosition)
    {
        if (detailPanel != null)
        {
            detailPanel.Show(name, desc, mousePosition);
        }
    }

    private void ClearAllSlots()
    {
        foreach (var slot in itemSlots)
            slot.Clear();
    }
}