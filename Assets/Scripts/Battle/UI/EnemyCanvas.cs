
    using System.Collections.Generic;
    using UnityEngine;

    public class EnemyCanvas: MonoBehaviour
    {
        // 生命值显示组件
        public HpBarUI hpBarUI;
        // 与 PieceInfoPanel 共用 BuffCell 的图标和层数显示
        [SerializeField] private RectTransform buffRoot;
        [SerializeField] private BuffCell buffCellPrefab;
        private readonly List<BuffCell> buffCells = new();
        
        // 受击信息显示组件
        public HitInfoPanel hitInfoPanel;

        public void UpdateBuffs(IReadOnlyList<BuffState> buffStates)
        {
            if (buffRoot == null || buffCellPrefab == null) return;

            for (int i = buffCells.Count; i < buffStates.Count; i++)
            {
                var cell = Instantiate(buffCellPrefab, buffRoot);
                cell.transform.localScale = Vector3.one * 0.3f;
                buffCells.Add(cell);
            }

            for (int i = 0; i < buffCells.Count; i++)
            {
                bool visible = i < buffStates.Count;
                buffCells[i].gameObject.SetActive(visible);
                if (visible) buffCells[i].SetData(buffStates[i]);
            }
        }
        
    }
