using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectPoolSO", menuName = "Game/ObjectPoolSO", order = 1)]
public class ObjectPoolSO : SerializedScriptableObject
{
    [SerializeField]
    /// <summary>
    /// 物品字典
    /// </summary>
    public Dictionary<ItemType, GameObject> itemPrefabDic = new();

}
