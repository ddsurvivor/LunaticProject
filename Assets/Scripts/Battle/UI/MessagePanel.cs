using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessagePanel : UIPanel
{
    [Header("UI Components")]

    [SerializeField] private Text titleText; // 标题文本
    [SerializeField] private Text contentText; // 旧版 Text

    [Header("Image Pool")] [SerializeField]
    private GameObject imageRoot;

    // 在 Inspector 面板中手动拖入预先放好的 10 个 Image 物体
    [SerializeField] private List<Image> imageList = new List<Image>();

    /// <summary>
    /// 接口1：仅显示文本
    /// </summary>
    public void ShowMessage(string text)
    {
        if (gameObject.activeInHierarchy)
        {
            contentText.text += "\n"+text;
        }
        else
        {
            
        ResetUI();
        contentText.text = text;
        ShowPanel();
        }
    }

    /// <summary>
    /// 接口2：显示文本及多个图片（上限为 imageList 的长度）
    /// </summary>
    public void ShowMessage(string text, Sprite[] sprites)
    {
        ResetUI();
        contentText.text = text;
        imageRoot.SetActive(true);
        if (sprites != null)
        {
            // 循环遍历已有的 Image 列表
            for (int i = 0; i < imageList.Count; i++)
            {
                if (i < sprites.Length)
                {
                    // 在有效范围内，赋值并显示
                    imageList[i].sprite = sprites[i];
                    imageList[i].gameObject.SetActive(true);
                }
                else
                {
                    // 超出传入 Sprite 数量的部分，保持隐藏
                    imageList[i].gameObject.SetActive(false);
                }
            }
        }

        ShowPanel();
    }

    /// <summary>
    /// 显示获得物品的消息，传入一个物品列表，自动构建文本和图片显示
    /// </summary>
    /// <param name="dropList"></param>
    public void ShowItemGet(List<ItemPack> dropList)
    {
        // 构建显示文本
        titleText.text = "物品获得";
        string text = "获得了以下物品：\n";
        foreach (var item in dropList)
        {
            text += $"{item.itemNum} x {item.itemName}\n";
        }

        // 显示图片
        Sprite[] sprites = new Sprite[dropList.Count];
        for (int i = 0; i < dropList.Count; i++)
        {
            // 这里假设有一个方法可以根据物品名称获取对应的 Sprite
            sprites[i] = GM.Ins.marketSystem.marketItemListSO.GetData(dropList[i].itemName)
                .itemIcon;
        }
        ShowMessage(text, sprites);
    }

    public void ShowBattleFinish(string battleName, List<ItemPack> dropList, int expGain)
    {
        // 构建显示文本
        titleText.text = $"通关{battleName}";
        string text = "获得了以下物品：\n";
        foreach (var item in dropList)
        {
            text += $"{item.itemNum} x {item.itemName}\n";
        }

        // 显示图片
        Sprite[] sprites = new Sprite[dropList.Count];
        for (int i = 0; i < dropList.Count; i++)
        {
            // 这里假设有一个方法可以根据物品名称获取对应的 Sprite
            sprites[i] = GM.Ins.marketSystem.marketItemListSO.GetData(dropList[i].itemName)
                .itemIcon;
        }

        text += $"获得了{expGain}经验值";
        ShowMessage(text, sprites);
    }
    
    public void ShowModifyMessage(int index,int attr,int opIndex,int value)
    {
        titleText.text = "属性变化";
        //棋子名字 0邱悟、1绿、2马赛
        string pieceName = GM.Ins.PLAYERPROFILE.player[index].NAME;
        
        string attrName = GM.Ins.PLAYERPROFILE.player[index].GetAttrName(attr);
        string opText = "";
        switch (opIndex)
        {
            // opIndex: 0读取，1增加，2赋值，3减小
            case 0: opText = "当前"; break;
            case 1: opText = "增加"; break;
            case 2: opText = "设置为"; break;
            case 3: opText = "减少"; break;
        }
        string text = $"{pieceName}的{attrName}{opText}{value}";
        ShowMessage(text);
    }


    // 清空状态
    private void ResetUI()
    {
        contentText.text = string.Empty;
        imageRoot.SetActive(false);
        foreach (var img in imageList)
        {
            if (img != null) img.gameObject.SetActive(false);
        }
    }
}