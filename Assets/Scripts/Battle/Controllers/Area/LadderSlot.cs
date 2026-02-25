
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 已废弃
    /// </summary>
    public class LadderSlot: MonoBehaviour
    {
        public List<PieceController> pieces = new();
        public int maxCapacity = 2;
        public GameObject highlightEffect;

        public Transform upPos;
        public Transform downPos;
        
        public void AddToSlot(PieceController target)
        {
            if(pieces.Count >= maxCapacity) return;
            if(pieces.Contains(target))return;
            pieces.Add(target);
            Debug.Log($" Adding {target.name} to  Ladder");
        }
        public void LeaveSlot(PieceController target)
        {
            pieces.Remove(target);
        }

        /// <summary>
        /// 开始转移棋子
        /// </summary>
        public void StartMove()
        {
            
        }
        private void OnMouseEnter()
        {
            highlightEffect.SetActive(true);
        }
        private void OnMouseExit()
        {
            highlightEffect.SetActive(false);
        }
    }
