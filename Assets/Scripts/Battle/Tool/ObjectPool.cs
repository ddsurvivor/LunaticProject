using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

// 文件: Assets/Scripts/Battle/Tool/ObjectPool.cs
using Sirenix.OdinInspector;

public enum ItemType
{
    [LabelText("无")]
    NONE = 0,

    /*[LabelText("表情-高兴")]
    EMO_HAPPY = 1,
    [LabelText("表情-普通")]
    EMO_NORMAL = 2,
    [LabelText("表情-悲伤")]
    EMO_SAD = 3,*/

    [LabelText("盾牌特效")]
    SHIELD = 9, // 盾牌特效

    [LabelText("爆炸特效")]
    EXPLOSION = 10, // 爆炸特效

    [LabelText("伤害跳字")]
    DAMAGE_TEXT = 11, // 伤害跳字

    [LabelText("能量攻击特效")]
    ENERGY_ATTACK = 12, // 能量攻击特效

    [LabelText("动能攻击特效")]
    KINETIC_ATTACK = 13, // 动能攻击特效

    [LabelText("治疗技能")]
    HEAL_SKILL = 14,

    [LabelText("等离子爆炸")]
    PLASMA_EXPLOSION = 15, // 等离子爆炸

    [LabelText("燃烧特效")]
    FLAME_EFFECT = 16, // 燃烧特效

    [LabelText("技能攻击区域标记")]
    SKILL_AREA = 17, // 技能攻击区域标记

    [LabelText("轨道轰炸特效")]
    ORBITAL_STRIKE = 18, // 轨道轰炸特效

    [LabelText("粘性炸弹特效")]
    GEL_BOMB = 19, // 粘性炸弹特效

    [LabelText("治疗区域")]
    HEAL_AREA = 20, // 治疗区域

    [LabelText("物品拾取")]
    PICKABLE_ITEM = 30, // 物品拾取

    [LabelText("电击弹道")]
    ELECTRIC_BOLT = 40, // 电击弹道

    [LabelText("火焰弹道")]
    FLAME_BOLT = 41, // 火焰弹道

    [LabelText("扇形爆炸弹道")]
    FAN_EXPLOSION = 42, // 扇形爆炸弹道

    [LabelText("暗能量")]
    DENERGY = 43,

    [LabelText("目击之术")]
    EYESTRIKE = 44,

    [LabelText("火箭")]
    ROCKET = 45,

    [LabelText("高频")]
    HightFrequency = 46,

    [LabelText("光矛")]
    LightSpear = 47,

    [LabelText("光扫")]
    LightSweap = 48,

    [LabelText("通用子弹")]
    BulletFx = 49, // 通用子弹
    
    [LabelText("爆炸桶爆炸")]
    EXPLOSIVE_BARREL = 50, // 爆炸桶爆炸
    
    [LabelText("夹击特效")]
    PincerAttackFx = 51, // 夹击特效
    
    [LabelText("专长发动特效")]
    SPECIALTY_ACTIVATE = 52, // 专长发动特效
    [LabelText("回血特效")]
    HEAL_EFFECT = 53, // 回血特效
    
    [LabelText("打印机凝胶")]
    PRINTER_GEL = 54, // 打印机凝胶
    
    [LabelText("召唤无人机")]
    SUMMON_DRONE = 100, // 召唤无人机
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