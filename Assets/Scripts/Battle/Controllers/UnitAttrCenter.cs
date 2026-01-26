using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

    [SerializeField]
    private float _moveRange;
    public float MoveRange => _moveRange;
    [SerializeField][ReadOnly]
    // 嘲讽值
    private int _tauntValue;
    public  int TauntValue => _tauntValue;
    
    [SerializeField]
    // 弹药数量
    private int _ammoCount;
    public int AmmoCount => _ammoCount;
    private int _maxAmmoCount = 3;
    public int MaxAmmoCount => _maxAmmoCount;

    [Header("Buff")] 
    [SerializeField][ReadOnly]
    public List<BuffState> buffStates = new();
    [SerializeField][ReadOnly]
    public Dictionary<BuffAttrType, float> buffAttrDic = new();


    [Header("UI")]
    public Transform hpBarFill;

    public void Init()
    {
        _curHealth = _maxHealth;
        InitBuffAttrDic();
        FullAmmo();
    }
    
    private void InitBuffAttrDic()
    {
        buffAttrDic.Clear();
        foreach (BuffAttrType type in System.Enum.GetValues(typeof(BuffAttrType)))
        {
            if (type is BuffAttrType.HitRate or 
                        BuffAttrType.MoveRangePercent)// 默认为100
            {
                buffAttrDic[type] = 100f;
            }
            else if (type != BuffAttrType.None)// 其余数值默认为0
            {
                buffAttrDic[type] = 0f;
            }
        }
    }
    public void AddBuffAttr(BuffAttrType type, float value)
    {
        if (buffAttrDic.ContainsKey(type))
        {
            buffAttrDic[type] += value;
        }
        else
        {
            buffAttrDic[type] = value;
        }
    }
    

    public void FullMovePoint()
    {
        _curMovePoint = _maxMovePoint;
    }
    public void FullAmmo()
    {
        _ammoCount = _maxAmmoCount;
    }
    public void FullHealth()
    {
        _curHealth = _maxHealth;
        if(hpBarFill!=null)hpBarFill.localScale = new Vector3(1f, 1f,1f);
    }
    public void TakeDamage(AttackPack attackPack)
    {
        if (attackPack.damage <= 0) return;
        _curHealth -= attackPack.damage;
        // 伤害跳字
        DamageText damageText = ObjectPool.Ins.GenerateObject(
            ItemType.DAMAGE_TEXT,
            transform.position, transform.rotation
        ).GetComponent<DamageText>();
        damageText.JumpOutNum(attackPack.damage);
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
            if (pc == null) return; 
                
                    pc.Hurt();
                    ObjectPool.Ins.GenerateObject(
                        attackPack.damageType == DamageType.Melee
                            ? ItemType.KINETIC_ATTACK
                            : ItemType.ENERGY_ATTACK,
                        this.transform.position + Vector3.up*5f
                        , Quaternion.identity);
                
        }
        Debug.Log($"受到{attackPack.damageType}伤害{attackPack.damage}");
    }
    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;
        _curHealth += healAmount;
        if (_curHealth > _maxHealth) _curHealth = _maxHealth;
        if(hpBarFill!=null)hpBarFill.localScale = new Vector3((float)_curHealth / _maxHealth, 1f,1f);
        Debug.Log($"恢复生命{healAmount}");
    }
    
    public bool CostMP(int costPoint=1)
    {
        if (_curMovePoint>=costPoint)
        {
            _curMovePoint -= costPoint;
            Debug.Log($"{gameObject.name}消耗行动力{costPoint}，剩余行动力{_curMovePoint}");
            return true;
        }
        return false;
    }
    
    public bool CostAmmo(int costCount=1)
    {
        if (_ammoCount>=costCount)
        {
            _ammoCount -= costCount;
            return true;
        }
        return false;
    }
    
    public int GetBuffStacks(BuffType buffType)
    {
        foreach (var buff in buffStates)
        {
            if (buff.buffType == buffType)
            {
                return buff.stacks;
            }
        }
        return 0;
    }
}
