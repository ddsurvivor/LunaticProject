
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using Sirenix.Serialization;
    using UnityEngine;

    // 菜单创建CreateAssetMenu
    [CreateAssetMenu(fileName = "PieceDataListSO", menuName = "BattleSO/PieceDataListSO", order = 1)]
    public class PieceDataListSO: SerializedScriptableObject
    {
        [OdinSerialize]
        //[TableList]
        public List<PieceData> pieceDataList = new();
        
        public PieceData GetPieceData(int pieceId)
        {
            if (pieceDataList == null || pieceDataList.Count == 0)
            {
                throw new InvalidOperationException("Piece data list is empty or not initialized.");
            }

            var pieceData = pieceDataList.Find(pd => pd.pieceId == pieceId);
            if (pieceData == null)
            {
                Debug.LogError($"No PieceData found with pieceId: {pieceId}");
            }

            return pieceData;
        }
    }
    
