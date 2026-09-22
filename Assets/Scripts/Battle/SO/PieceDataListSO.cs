
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
        
        [Button("为敌人填充默认等级成长", ButtonSizes.Large)]
        [InfoBox("当前敌人编号从 1000 起。默认仅补齐未配置的属性；勾选覆盖已有值可重置。成长只对 EnemyController 生效。")]
        public void FillDefaultEnemyGrowth(int minimumEnemyId = 1000, bool overwriteExisting = false)
        {
            if (pieceDataList == null) return;
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "填充敌人等级成长");
#endif
            int count = 0;
            foreach (var data in pieceDataList)
            {
                if (data == null || data.pieceId < minimumEnemyId) continue;
                data.levelGrowth ??= new Dictionary<EnemyGrowthAttribute, float>();
                foreach (var pair in EnemyLevelGrowth.CreateDefaults(data))
                {
                    if (overwriteExisting || !data.levelGrowth.ContainsKey(pair.Key))
                        data.levelGrowth[pair.Key] = pair.Value;
                }
                count++;
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"已为 {count} 条敌人数据填充等级成长。");
        }

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

        /*[Button("测试修改数据")]
        public void TestSetData()
        {
            // 把所有棋子数据的暴击倍率，改为130
            foreach (var pieceData in pieceDataList)
            {
                pieceData.maxMovePoint = 6;
            }
            Debug.Log("已将所有棋子数据的最大移动点数改为6");
        }*/
    }
    
