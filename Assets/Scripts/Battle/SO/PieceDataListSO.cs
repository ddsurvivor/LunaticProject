
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    // 菜单创建CreateAssetMenu
    [CreateAssetMenu(fileName = "PieceDataListSO", menuName = "BattleSO/PieceDataListSO", order = 1)]
    public class PieceDataListSO: SerializedScriptableObject
    {
        //[TableList]
        public List<PieceData> pieceDataList = new();
        
        public PieceData GetPieceData(int pieceId)
        {
            return pieceDataList.Find(pieceData => pieceData.pieceId == pieceId);
        }
    }
    
