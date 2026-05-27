using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 插件道具格子
/// </summary>
public class UIItemSlot : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public int currentItemID; // 当前格子存储的ID
    private CharacterUIPage mainPage;

    public void Init(int id, CharacterUIPage page)
    {
        mainPage = page;
        currentItemID = id;
        
        // 从配置表获取数据来显示
        ComponentData data = GM.Ins.DM.componentConfig.GetData(id);
        if (data != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }
    }
    public void Clear()
    {
        currentItemID = 0;
        iconImage.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && currentItemID != 0)
        {
            mainPage.ShowDetail(currentItemID, transform.position);
        }
    }
}