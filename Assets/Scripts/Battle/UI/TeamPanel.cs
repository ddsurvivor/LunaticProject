using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class TeamPanel : SerializedMonoBehaviour
{
    [Header("UI设置")]
    //[SerializeField] private Transform headContainer; // 头像容器
    //[SerializeField] private GameObject pieceHeadPrefab; // 头像预制体
    
    [Header("布局设置")]
    [SerializeField] private float spacing = 10f; // 头像间距
    private float originWidth = 100f;
    //[SerializeField] private Vector2 headSize = new Vector2(80, 80); // 头像大小
    //[SerializeField] private Vector2 bottomLeftPosition = new Vector2(50, 50); // 左下角位置
    [OdinSerialize]
    private Dictionary<int, PieceHeadUI> pieceHeads = new Dictionary<int, PieceHeadUI>();
    [OdinSerialize]
    private List<PieceHeadUI> headTransforms = new();
    [OdinSerialize]
    private int currentSelectedId = -1;

    private void Start()
    {
        //InitializeLayout();
        OnSelectPiece(1);
    }
    
    // 初始化布局
    private void InitializeLayout()
    {
        /*// 清除现有头像
        foreach (Transform child in headContainer)
        {
            Destroy(child.gameObject);
        }
        pieceHeads.Clear();
        headTransforms.Clear();
        
        // 创建占位头像用于测试（实际中应该根据棋子数据动态创建）
        for (int i = 0; i < 3; i++)
        {
            CreatePieceHead(i, $"棋子{i + 1}");
        }
        
        UpdateLayout();*/
    }
    
    /*// 创建棋子头像
    private void CreatePieceHead(int id, string pieceName)
    {
        if (pieceHeadPrefab == null) return;
        
        GameObject headObj = Instantiate(pieceHeadPrefab, headContainer);
        headObj.name = $"PieceHead_{id}";
        
        // 设置RectTransform
        RectTransform rect = headObj.GetComponent<RectTransform>();
        rect.sizeDelta = headSize;
        
        PieceHeadUI headUI = headObj.GetComponent<PieceHeadUI>();
        if (headUI != null)
        {
            headUI.pieceId = id;
            
            // 设置随机血量用于测试
            float randomHealth = Random.Range(30f, 100f);
            headUI.UpdateHealth(randomHealth, 100f);
            
            pieceHeads[id] = headUI;
        }
        
        headTransforms.Add(headObj.transform);
    }*/
    
    
    // 更新布局（保持底边对齐）
    private void UpdateLayout()
    {
        if (headTransforms.Count == 0) return;
        float posX = 0f;
        for (int i = 0; i < headTransforms.Count; i++)
        {
            RectTransform rect = headTransforms[i].GetComponent<RectTransform>();
            if (rect == null) continue;
            // 如果是选中状态，调整位置以保持间距不变
            if (headTransforms[i].pieceId == currentSelectedId)
            {
                rect.localPosition = new Vector3(posX, 150f*0.1f, 0f);
                posX += (originWidth*1.1f+spacing);
            }
            else
            {
                rect.localPosition = new Vector3(posX, 0f, 0f);
                posX += (originWidth + spacing);
            }
        }
    }
    
    // 接口函数：选中棋子
    public void OnSelectPiece(int pieceId)
    {
        int previousSelectedId = currentSelectedId;
        currentSelectedId = pieceId;
        
        // 取消之前选中的头像
        if (previousSelectedId >= 0 && pieceHeads.ContainsKey(previousSelectedId))
        {
            pieceHeads[previousSelectedId].SetSelected(false);
        }
        
        // 选中新的头像
        if (pieceHeads.ContainsKey(pieceId))
        {
            pieceHeads[pieceId].SetSelected(true);
        }
        
        // 更新布局，保持间距
        UpdateLayout();
    }
    
    /*// 添加棋子头像（动态添加）
    public void AddPieceHead(int id, Sprite icon, float health = 100f)
    {
        if (pieceHeads.ContainsKey(id))
        {
            Debug.LogWarning($"棋子ID {id} 已存在！");
            return;
        }
        
        CreatePieceHead(id, $"棋子{id}");
        UpdateLayout();
    }*/
    
    /*// 移除棋子头像
    public void RemovePieceHead(int id)
    {
        if (pieceHeads.ContainsKey(id))
        {
            Destroy(pieceHeads[id].gameObject);
            pieceHeads.Remove(id);
            UpdateLayout();
        }
    }*/
    
    // 更新棋子血量
    public void UpdatePieceHealth(int id, float currentHealth, float maxHealth = 100f)
    {
        if (pieceHeads.ContainsKey(id))
        {
            pieceHeads[id].UpdateHealth(currentHealth, maxHealth);
        }
    }
    
    // 编辑器调试用
    [ContextMenu("测试选中第一个棋子")]
    private void TestSelectFirst()
    {
        if (pieceHeads.Count > 0)
        {
            OnSelectPiece(1);
        }
    }
    
    [ContextMenu("测试更新血量")]
    private void TestUpdateHealth()
    {
        foreach (var kvp in pieceHeads)
        {
            float randomHealth = Random.Range(10f, 100f);
            kvp.Value.UpdateHealth(randomHealth, 100f);
        }
    }
}
