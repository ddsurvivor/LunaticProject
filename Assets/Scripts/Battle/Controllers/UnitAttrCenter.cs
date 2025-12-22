using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// 单位属性中心
/// </summary>
public class UnitAttrCenter: SerializedMonoBehaviour
{
    [OdinSerialize]
    private AttrCenter _attr = new();
    public AttrCenter attr => _attr;
    
    // 生命值 hp
    private int _curHealth;
    [SerializeField]
    private int _maxHealth;
    
    // 怒气值 AP
    private int _curAP;
    private int _maxAP;
    
    // 行动点数 MP
    private int _curMovePoint;
    private int _maxMovePoint;

    private int _tempShield;

    public void Init()
    {
        _curHealth = _maxHealth;
    }
    public void TakeDamage(int atk, DamageType damageType, int addAtk)
    {
        int realDamage = atk;
        if (_tempShield>0)
        {
            realDamage -= _tempShield;
            _tempShield = 0;
        }
        realDamage -= addAtk + _attr.GetArmor(damageType);
        if (realDamage <= 0) return;
        _curHealth -= realDamage;
        if (_curHealth <= 0)
        {
            // 触发单位死亡事件
        }
        Debug.Log($"造成{damageType}伤害{realDamage}");
    }
}
