using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public enum ItemType
{
    NONE=0,
    /*EMO_HAPPY=1,
    EMO_NORMAL=2,
    EMO_SAD=3,*/
    SHIELD=9,// 盾牌特效

    // 爆炸特效
    EXPLOSION=10,
    // 伤害跳字
    DAMAGE_TEXT=11,
    // 能量攻击特效
    ENERGY_ATTACK=12,
    // 动能攻击特效
    KINETIC_ATTACK=13,
    HEAL_SKILL=14,
    // 等离子爆炸
    PLASMA_EXPLOSION=15,
    // 燃烧特效
    FLAME_EFFECT=16,
    // 技能攻击区域标记
    SKILL_AREA=17,
    // 轨道轰炸特效
    ORBITAL_STRIKE=18,
    GEL_BOMB=19,// 粘性炸弹特效

    // 治疗区域
    HEAL_AREA=20,
    
    // 物品拾取
    PICKABLE_ITEM=30,
    
    // 电击弹道
    ELECTRIC_BOLT=40,
    // 火焰弹道
    FLAME_BOLT=41,
    
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
    
    // [SerializeField]
    // /// <summary>
    // /// 物品字典
    // /// </summary>
    // public Dictionary<ItemType, GameObject> itemPrefabDic = new();

    [InlineEditor]
    public ObjectPoolSO objectPoolSO;
    
    // Start is called before the first frame update
    // public  void Init()
    // {
    //     
    //
    //     // 初始化普通物体对象池
    //     foreach (var pair in itemPrefabDic)
    //     {
    //         itemPoolDic.Add(pair.Key, new List<GameObject>());
    //     }
    //
    //     
    // }

    


    /// <summary>
    /// 对象池普通生成物体
    /// </summary>
    /// <param name="_type">生成物体的种类</param>
    /// <param name="_transform">生成到节点</param>
    /// <returns>返回该物体</returns>
    public GameObject GenerateObject(ItemType _type, Transform _transform)
    {
        if (!objectPoolSO.itemPrefabDic.ContainsKey(_type)) return null;

        if (!itemPoolDic.ContainsKey(_type)) InitPool(_type, objectPoolSO.itemPrefabDic[_type]);

        foreach (var item in itemPoolDic[_type])
        {
            if (item.activeInHierarchy) continue;

            item.transform.SetParent(_transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.SetActive(true);
            return item;
        }

        GameObject go = Instantiate(objectPoolSO.itemPrefabDic[_type], _transform);
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
            Debug.LogError("生成物体失败，类型：" + _type);
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