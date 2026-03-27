using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillPointPanel : MonoBehaviour
{
    public Player playerData; // 数据源副本

    [Header("UI Reference")] public Text nameText;
    public Text pointsText;
    public Image portraitImage;

    public Text levelText;
    //public Transform container;
    //public GameObject rowPrefab;

    [SerializeField]
    private List<AttrModRow> rowList = new List<AttrModRow>();
    private int tempPoints;

    private bool hasInited;
    private int unitID;
    void Start()
    {
        //InitPanel();
    }
    
    public void ShowPanel(Player playerData)
    {
        //this.unitID = unitID;
        this.playerData = playerData; //GM.Ins.PLAYERPROFILE.GetPlayer(unitID); // 接收外部传入的数据副本
        if(!hasInited) InitPanel();
        gameObject.SetActive(true);
        nameText.text = playerData.NAME;
        portraitImage.sprite = Resources.Load<Sprite>("CG/" + playerData.spriteName);
        tempPoints = playerData.SkillPoints;
        levelText.text = playerData.Level.ToString();
        pointsText.text = tempPoints.ToString();
        for (int i = 0; i < 10; i++)
        {
            if (i >= rowList.Count) break;
            AttrModRow row = rowList[i];
            // 直接通过编号读取属性名和当前值
            string attrName = playerData.GetAttrName(i);
            int currentVal = playerData.AccessAttribute(i, AttrOp.Get);

            row.Setup(attrName, currentVal, 200);
        }
    }

    void InitPanel()
    {
        hasInited = true;
        // --- 优雅的遍历初始化 ---
        for (int i = 0; i < 10; i++)
        {
            //GameObject go = Instantiate(rowPrefab, container);
            if (i >= rowList.Count) break;
            AttrModRow row = rowList[i];

            int index = i; // 闭包陷阱注意
            // 直接通过编号读取属性名和当前值
            string attrName = playerData.GetAttrName(index);
            int currentVal = playerData.AccessAttribute(index, AttrOp.Get);

            row.Setup(attrName, currentVal, 200);

            // 绑定按钮
            row.plusBtn.onClick.AddListener(() =>
            {
                row.ChangePending(1, ref tempPoints);
                pointsText.text = tempPoints.ToString();
            });
            row.minusBtn.onClick.AddListener(() =>
            {
                row.ChangePending(-1, ref tempPoints);
                pointsText.text = tempPoints.ToString();
            });

            //rowList.Add(row);
        }
    }
    
    

    // --- 极简的 Apply 功能 ---
    public void ApplyPoints()
    {
        // 1. 修改副本数据
        playerData.SkillPoints = tempPoints;
        for (int i = 0; i < rowList.Count; i++)
        {
            int added = rowList[i].PendingAdd;
            playerData.AccessAttribute(i, AttrOp.Add, added); // 使用 Add 模式
            rowList[i].Commit();
        }

        //GM.Ins.PLAYERPROFILE.player[unitID] = playerData;
        // 2. 【重点：存档回写】
        // 在这里将修改后的 playerData 结构体存入你的存档系统或全局管理器
        // SaveToDisk(playerData); 

        Debug.Log("数据已更新至结构体并触发保存逻辑");
    }

    public void ResetPoints()
    {
        foreach (var row in rowList) row.ResetRow(ref tempPoints);
        pointsText.text = tempPoints.ToString();
    }
    
}