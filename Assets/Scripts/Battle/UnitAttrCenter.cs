using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 伤害类别
/// </summary>
public enum DamageType
{
    Melee = 1,// 近战
    Ranged = 2,// 远程
    Electric = 3,// 电子
}

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

/// <summary>
/// 单位属性中心
/// </summary>
public class UnitAttrCenter: MonoBehaviour
{
    private AttrCenter _attr;
    
    // 生命值 hp
    private int _curHealth;
    private int _maxHealth;
    
    // 怒气值 AP
    private int _curAP;
    private int _maxAP;
    
    // 行动点数 MP
    private int _curMovePoint;
    private int _maxMovePoint;

    private int _tempShield;

    public void TakeDamage(int atk, DamageType damageType, int addAtk)
    {
        int realDamage = atk;
        if (_tempShield>0)
        {
            realDamage -= _tempShield;
            _tempShield = 0;
        }
        realDamage -= addAtk - _attr.GetArmor(damageType);
        if (realDamage <= 0) return;
        _curHealth -= realDamage;
        if (_curHealth <= 0)
        {
            // 触发单位死亡事件
        }
    }
}
