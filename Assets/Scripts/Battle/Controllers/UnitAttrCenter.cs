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
    public PieceController pc;
    
    public PieceElementType elementType;
    
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
    
    // 嘲讽值
    private int _tauntValue;
    public  int TauntValue => _tauntValue;
    
    
    [Header("UI")]
    public Transform hpBarFill;

    public void Init()
    {
        _curHealth = _maxHealth;
    }

    public void FullMovePoint()
    {
        _curMovePoint = _maxMovePoint;
    }
    public void TakeDamage(int realDamage)
    {
        if (realDamage <= 0) return;
        _curHealth -= realDamage;
        if (_curHealth <= 0) _curHealth = 0;
        if(hpBarFill!=null)hpBarFill.localScale = new Vector3((float)_curHealth / _maxHealth, 1f,1f);
        if (_curHealth <= 0)
        {
            if (pc != null)
            {
                pc.Dead();
            }
            else
            {
                // 触发单位死亡事件
                gameObject.SetActive(false);// 临时死亡
            }
        }
        else
        {
            if (pc != null) pc.Hurt();
        }
        Debug.Log($"受到伤害{realDamage}");
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
