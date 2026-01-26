
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
            return pieceDataList.Find(pieceData => pieceData.pieceId == pieceId);
        }
    }
    
