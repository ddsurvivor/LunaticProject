using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillPointPanel : MonoBehaviour {
    public Player playerData; // 数据源副本

    [Header("UI Reference")]
    public Text nameText;
    public Text pointsText;
    public Transform container;
    public GameObject rowPrefab;

    private List<AttributeRowUI> rowList = new List<AttributeRowUI>();
    private int tempPoints;

    void Start() {
        InitPanel();
    }

    void InitPanel() {
        nameText.text = playerData.NAME;
        tempPoints = playerData.SkillPoints;
        UpdatePointsUI();

        // --- 优雅的遍历初始化 ---
        for (int i = 0; i < 10; i++) {
            GameObject go = Instantiate(rowPrefab, container);
            AttributeRowUI row = go.GetComponent<AttributeRowUI>();
            
            // 直接通过编号读取属性名和当前值
            string attrName = playerData.GetAttrName(i);
            int currentVal = playerData.AccessAttribute(i, AttrOp.Get);
            
            row.Setup(attrName, currentVal, 200); 
            
            // 绑定按钮
            int index = i; // 闭包陷阱注意
            row.plusBtn.onClick.AddListener(() => { row.ChangePending(1, ref tempPoints); UpdatePointsUI(); });
            row.minusBtn.onClick.AddListener(() => { row.ChangePending(-1, ref tempPoints); UpdatePointsUI(); });
            
            rowList.Add(row);
        }
    }

    // --- 极简的 Apply 功能 ---
    public void ApplyPoints() {
        // 1. 修改副本数据
        playerData.SkillPoints = tempPoints;
        for (int i = 0; i < rowList.Count; i++) {
            int added = rowList[i].PendingAdd;
            playerData.AccessAttribute(i, AttrOp.Add, added); // 使用 Add 模式
            rowList[i].Commit();
        }

        // 2. 【重点：存档回写】
        // 在这里将修改后的 playerData 结构体存入你的存档系统或全局管理器
        // SaveToDisk(playerData); 
        
        Debug.Log("数据已更新至结构体并触发保存逻辑");
    }

    public void ResetPoints() {
        foreach (var row in rowList) row.ResetRow(ref tempPoints);
        UpdatePointsUI();
    }

    void UpdatePointsUI() => pointsText.text = "可用点数: " + tempPoints;
}