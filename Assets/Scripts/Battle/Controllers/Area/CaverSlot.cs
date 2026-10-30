
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// 掩体点位
    /// </summary>
    public class CaverSlot: MonoBehaviour
    {
        public Transform[] slotTransforms;
        private Dictionary<Transform, Transform> piecesDic = new();
        public GameObject highlightEffect;
        
        [LabelText("作为掩体")] public bool providesCover = true;
        [LabelText("掩体等级"), Range(1, 3)] public int coverLevel = 1; // 旧场景没有该字段时沿一级处理。
        [HideInInspector] public int evadeChance = 20; // 保留旧序列化字段，效果改用 GameConstSO。
        [HideInInspector] public int damageReduction = 20; // 保留旧序列化字段，效果改用等级配置。

        public int Level => Mathf.Clamp(coverLevel, 1, 3);
        /// <summary>只读判断实体是否提供掩体；搭建组件的纯障碍开关优先。</summary>
        public bool ProvidesCover => isActiveAndEnabled && providesCover &&
            (GetComponent<BattleObstacle>() == null || GetComponent<BattleObstacle>().providesCover);
        
        public bool isFull => slotTransforms == null || piecesDic.Values.Count >= slotTransforms.Length;

        public void AddToSlot(Transform target)
        {
            if(isFull) return;
            if(piecesDic.ContainsValue(target))return;
            Debug.Log($" Adding {target.name} to CaverSlot");
            foreach (var slotTransform in slotTransforms)
            {
                if (!piecesDic.ContainsKey(slotTransform))
                {
                    piecesDic[slotTransform] = target;
                    target.position = slotTransform.position;
                    // 槽位只兼容旧站位接口，保护效果由攻击射线查询，不能重复授予 Buff。
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
                    // 移除buff，减伤、加闪避
                    UnitAttrCenter unit = target.GetComponent<UnitAttrCenter>();
                    BattleScene.Ins.BM.buffManager.RemoveBuffSource(unit, BuffType.Shield, this);
                    BattleScene.Ins.BM.buffManager.RemoveBuffSource(unit, BuffType.Conceal, this);
                    return;
                }
            }

        }

        private void OnMouseEnter()
        {
            //if(isFull) return;
            highlightEffect.SetActive(true);
        }
        private void OnMouseExit()
        {
            highlightEffect.SetActive(false);
        }
    }
