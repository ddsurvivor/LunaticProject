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
    [SerializeField][ReadOnly]
    private int _curHealth;

    public int CurHealth => _curHealth;

    public int MaxHealth => _maxHealth;

    public int CurMovePoint => _curMovePoint;

    public int MaxMovePoint => _maxMovePoint;

    [SerializeField]
    private int _maxHealth;
    
    // 怒气值 AP
    private int _curAP;
    private int _maxAP;
    
    // 行动点数 MP
    [SerializeField][ReadOnly]
    private int _curMovePoint;
    [SerializeField]
    private int _maxMovePoint;

    private int _tempShield;

    public void Init()
    {
        _curHealth = _maxHealth;
    }

    public void FullMovePoint()
    {
        _curMovePoint = _maxMovePoint;
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
            gameObject.SetActive(false);// 临时死亡
        }
        Debug.Log($"受到{damageType}伤害{realDamage}");
    }
    
    public bool CostMP()
    {
        if (_curMovePoint>=1)
        {
            _curMovePoint--;
            return true;
        }
        return false;
    }
}
