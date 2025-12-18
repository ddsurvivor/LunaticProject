using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
