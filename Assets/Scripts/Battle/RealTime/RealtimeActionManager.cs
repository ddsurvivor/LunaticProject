using System;
using System.Collections.Generic;
using UnityEngine;

public class RealtimeActionManager : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private GameObject barPrefab;     // 进度条Prefab
    [SerializeField] private Transform barContainer;    // UI列表父容器

    // 行动力规则配置
    private const int MAX_MP = 6;               // 上限 6 点
    private const int INITIAL_MP = 0;           // 初始 3 点
    private const float TIME_PER_POINT = 4.0f;  // 每 2 秒充满 1 点

    // 每个棋子的内部充能数据
    private class PieceChargeData
    {
        public float chargeProgress = 0.0f;     // 0.0 ~ 1.0 充能计数器
        public bool isInitialized = false;     // 是否已进行过初始 3 点设置
        public ActionGaugeUIItem uiItem;
    }

    private Dictionary<PieceController, PieceChargeData> pieceDataDict = new Dictionary<PieceController, PieceChargeData>();

    private bool isInitialized = false;

    public void Start()
    {
        Init();
    }

    public void Init()
    {
        pieceDataDict.Clear();
        var pieces = BattleScene.Ins?.BM?.PlayerController?.pieces;
        pieces.AddRange(BattleScene.Ins?.BM?.AIController?.pieces);
        if (pieces == null) return;
        foreach (var piece in pieces)
        {
            // 获取或创建棋子数据
            if (!pieceDataDict.TryGetValue(piece, out PieceChargeData data))
            {
                data = new PieceChargeData();
                pieceDataDict[piece] = data;
            }
            
            // 1. 首次上场设置初始行动力 (3 点)
            if (!data.isInitialized)
            {
                piece.unitAttrCenter.SetMovePoint(INITIAL_MP);
                data.isInitialized = true;
            }
        }
    }
    
    private void Update()
    {
        
        foreach (var piece in pieceDataDict)
        {
            if (piece.Value == null || piece.Key.unitAttrCenter == null) continue;

            //if(!piece.Key.gameObject.activeInHierarchy) continue;
            // 读取当前整数行动力 (强转为 int 确保逻辑准确)
            int currentMP = (int)piece.Key.unitAttrCenter.CurMovePoint;

            // 2. 充能逻辑计算
            if (currentMP < MAX_MP)
            {
                // 增加充能进度（每秒增加 1.0 / TIME_PER_POINT）
                piece.Value.chargeProgress += Time.deltaTime / TIME_PER_POINT;

                // 当充能进度达到 1.0 时，增加 1 点行动力
                if (piece.Value.chargeProgress >= 1.0f)
                {
                    // 已达上限时，清空充能进度
                    piece.Value.chargeProgress = 0.0f;
                    piece.Key.unitAttrCenter.AddMP(1);
                }
            }
            else
            {
                piece.Value.chargeProgress = 0.0f;
            }

            // 3. 刷新 UI 显示
            RefreshGaugeUI(piece.Key, piece.Value, currentMP);
        }
    }

    private void RefreshGaugeUI(PieceController piece, PieceChargeData data, int currentMP)
    {
        // 动态生成缺失的 UI 项
        if (data.uiItem == null)
        {
            GameObject newObj = Instantiate(barPrefab, barContainer);
            data.uiItem = newObj.GetComponent<ActionGaugeUIItem>();
        }

        // 刷新 UI：传入棋子名、当前整数MP、最大MP(6)、单点充能进度(0~1)
        data.uiItem.UpdateView(piece.pieceData.pieceName, currentMP, MAX_MP, data.chargeProgress);
    }

    // 棋子移除/阵亡时清理 UI 及缓存
    public void RemovePiece(PieceController piece)
    {
        if (pieceDataDict.TryGetValue(piece, out PieceChargeData data))
        {
            if (data.uiItem != null)
            {
                Destroy(data.uiItem.gameObject);
            }
            pieceDataDict.Remove(piece);
        }
    }
}