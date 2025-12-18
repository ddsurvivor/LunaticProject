
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 角色战斗属性合集
/// </summary>
public class AttrCenter
{
    private Dictionary<DamageType,int> _atkDic;// 攻击力
    private Dictionary<DamageType, int> _armorDic;// 护甲值

    private int _moveRange;// 移动距离
    private int _atkRange;// 攻击距离

    
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
}