
using System.Collections;
using System.Collections.Generic;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
/// <summary>
/// 角色战斗属性合集
/// </summary>
[System.Serializable]
public class AttrCenter
{
    [OdinSerialize]
    private Dictionary<DamageType,int> _atkDic = new();// 攻击力
    [OdinSerialize]
    private Dictionary<DamageType, int> _armorDic = new();// 护甲值
    [OdinSerialize]
    private int _normalAtkRange;// 近战攻击距离
    [OdinSerialize]
    private int _atkRange;// 远程攻击距离

    
    public int GetAtk(DamageType damageType)
    {
        if (_atkDic.ContainsKey(damageType))
        {
            return _atkDic[damageType];
        }
        return 0;
    }
    public int GetArmor(DamageType damageType)
    {
        if (_armorDic.ContainsKey(damageType))
        {
            return _armorDic[damageType];
        }
        return 0;
    }
    public int GetRange(bool isNormalAtk)
    {
        return isNormalAtk ? _normalAtkRange : _atkRange;
    }
}