using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public enum ItemType
{
    EMO_HAPPY=1,
    EMO_NORMAL=2,
    EMO_SAD=3,
}
/// <summary>
/// 对象池
/// </summary>
public class ObjectPool : MonoSingleton<ObjectPool>
{


    [HideInInspector]
    /// <summary>
    /// 对象池
    /// </summary>
    public Dictionary<ItemType, List<GameObject>> itemPoolDic = new();
    
    [SerializeField]
    /// <summary>
    /// 物品字典
    /// </summary>
    public Dictionary<ItemType, GameObject> itemPrefabDic = new();
    
    
    // Start is called before the first frame update
    public  void Init()
    {
        

        // 初始化普通物体对象池
        foreach (var pair in itemPrefabDic)
        {
            itemPoolDic.Add(pair.Key, new List<GameObject>());
        }

        
    }

    


    /// <summary>
    /// 对象池普通生成物体
    /// </summary>
    /// <param name="_type">生成物体的种类</param>
    /// <param name="_transform">生成到节点</param>
    /// <returns>返回该物体</returns>
    public GameObject GenerateObject(ItemType _type, Transform _transform)
    {
        if (!itemPrefabDic.ContainsKey(_type)) return null;

        if (!itemPoolDic.ContainsKey(_type)) InitPool(_type, itemPrefabDic[_type]);

        foreach (var item in itemPoolDic[_type])
        {
            if (item.activeInHierarchy) continue;

            item.transform.SetParent(_transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.SetActive(true);
            return item;
        }

        GameObject go = Instantiate(itemPrefabDic[_type], _transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        itemPoolDic[_type].Add(go);
        //Debug.Log("生成了" + go);
        return go;
    }

    public GameObject GenerateObject(ItemType _type, Vector3 _position, Quaternion _quaternion)
    {
        GameObject go = GenerateObject(_type, null);
        if (go==null)
        {
            return null;
        }
        go.transform.position = _position;
        go.transform.rotation = _quaternion;
        return go;
    }

    public void PlayParticle(ItemType _type, Vector3 _position, Quaternion _quaternion)
    {
        GameObject go = GenerateObject(_type, null);
        if (go==null)
        {
            return;
        }
        ParticleSystem particle = go.GetComponent<ParticleSystem>();
        if (particle==null)
        {
            return;
        }
        particle.transform.position = _position;
        particle.transform.rotation = _quaternion;
        particle.Play();
        
    }

    
    
    public void InitPool(ItemType _type, GameObject _prefab)
    {
        if (!itemPrefabDic.ContainsKey(_type))
        {
            itemPrefabDic.Add(_type, _prefab);
        }

        if (!itemPoolDic.ContainsKey(_type))
        {
            itemPoolDic.Add(_type, new List<GameObject>());
            for (int i = 0; i < 4; i++)
            {
                GameObject go = Instantiate(_prefab, transform);
                go.SetActive(false);
                itemPoolDic[_type].Add(go);
            }
        }
    }
    

    /// <summary>
    /// 回收物体，直接设置为隐藏
    /// </summary>
    /// <param name="_gameObject"></param>
    public void HideObject(GameObject _gameObject)
    {
        if (_gameObject == null) return;
        _gameObject.SetActive(false);
        _gameObject.transform.SetParent(transform);
    }

    
}