
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 可交互点位
    /// </summary>
    public class ActionSlot: MonoBehaviour
    {
        public Transform[] slotTransforms;
        private Dictionary<Transform, Transform> piecesDic = new();
        public GameObject highlightEffect;
        
        public bool isFull => piecesDic.Values.Count >= slotTransforms.Length;

        public void AddToSlot(Transform target)
        {
            if(isFull) return;
            foreach (var slotTransform in slotTransforms)
            {
                if (!piecesDic.ContainsKey(slotTransform))
                {
                    piecesDic[slotTransform] = target;
                    target.position = slotTransform.position;
                    return;
                }
            }
        }

        public void LeaveSlot(Transform target)
        {
            foreach (var kvp in piecesDic)
            {
                if (kvp.Value == target)
                {
                    piecesDic.Remove(kvp.Key);
                    return;
                }
            }

        }

        private void OnMouseEnter()
        {
            if(isFull) return;
            highlightEffect.SetActive(true);
        }
        private void OnMouseExit()
        {
            highlightEffect.SetActive(false);
        }
    }
