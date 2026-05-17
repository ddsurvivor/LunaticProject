using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 单位属性中心
/// </summary>
public class UnitAttrCenter : SerializedMonoBehaviour
{
    public PieceController pc;

    public PieceElementType elementType;

    [OdinSerialize] [ReadOnly] private AttrCenter _attr = new();
    public AttrCenter attr => _attr;

    // 生命值 hp
    //[ReadOnly]
    [SerializeField]  private int _curHealth;

    public int CurHealth => _curHealth;

    public int MaxHealth => _maxHealth;

    public int CurMovePoint => _curMovePoint;

    public int MaxMovePoint => _maxMovePoint;

    [SerializeField] [ReadOnly] private int _maxHealth;

    // 怒气值 AP
    private int _curAP;
    private int _maxAP;

    // 行动点数 MP
    [SerializeField] [ReadOnly] private int _curMovePoint;
    [SerializeField] [ReadOnly] private int _maxMovePoint;

    private int _tempShield;

    [SerializeField] [ReadOnly] private float _moveRange;
    public float MoveRange => _moveRange;

    [SerializeField] [ReadOnly]
    // 嘲讽值
    private int _tauntValue;

    public int TauntValue => _tauntValue;

    [SerializeField] [ReadOnly]
    // 弹药数量
    private int _ammoCount;

    public int AmmoCount => _ammoCount;
    [SerializeField] [ReadOnly]private int _maxAmmoCount = 3;
    public int MaxAmmoCount => _maxAmmoCount;

    [SerializeField] [ReadOnly] private int _manaPoint;
    [SerializeField] [ReadOnly]private int _maxManaPoint;
    public int ManaPoint => _manaPoint;
    public int MaxManaPoint => _maxManaPoint;
    
    // 暴击率
    public int critRate;
    // 暴击伤害
   public int critDamageRate;

   public int ATK;// 攻击力
   
   // 对抗
   public int CON;

   [Header("Buff")] [SerializeField] [ReadOnly]
    public List<BuffState> buffStates = new();

    [SerializeField] [ReadOnly] public Dictionary<BuffAttrType, float> buffAttrDic = new();


    [Header("UI")] 
    public Transform hpBarFill;
    public HpBarUI hpBarUI;
    [Header("Events")]
    public UnityEvent OnDead;

    public void Init()
    {
        _curHealth = _maxHealth;
        //_manaPoint = _maxManaPoint;
        InitBuffAttrDic();
        FullAmmo();
    }

    public void SetData(PieceData pieceData, Player playerData = null)
    {
        _maxAmmoCount = pieceData.maxAmmoCount;
        _maxHealth = pieceData.maxHealth;
        _maxMovePoint = pieceData.maxMovePoint;
        _moveRange = pieceData.moveRange;
        elementType = pieceData.elementType;
        _maxManaPoint = pieceData.maxMana;
        _manaPoint = pieceData.initialMana;
        critRate = pieceData.critRate;
        critDamageRate = pieceData.critDamageRate;
        if (playerData != null)
        {
            // 根据玩家属性调整单位属性
            _maxHealth += playerData.AccessAttribute(2, AttrOp.Get) * 2; // 体能每点增加2点生命
            _maxManaPoint += playerData.AccessAttribute(0, AttrOp.Get) * 2; // 意志每点增加2点能量
            ATK += (int)(playerData.AccessAttribute(1, AttrOp.Get) * 0.5f); // 作战每点增加0.5点攻击力
            CON += (int)(playerData.AccessAttribute(4, AttrOp.Get) * 0.5f); // 模式识别每点增加0.5点对抗
            // 其他属性调整可以在这里添加
            critRate += (playerData.AccessAttribute(3, AttrOp.Get) + playerData.AccessAttribute(4, AttrOp.Get) ) * 2; // 技巧每点增加1%暴击率
        }
        // 应用特殊修改数值
        if (pieceData.attrDic != null && pieceData.attrDic.Count > 0)
        {
            foreach (var pair in pieceData.attrDic)
            {
                ModifyAttribute(pair.Key, pair.Value);
            }
        }
        
        Init();
        buffAttrDic[BuffAttrType.EvasionRate] = pieceData.evasionRate;
    }

    private void InitBuffAttrDic()
    {
        buffAttrDic.Clear();
        foreach (BuffAttrType type in System.Enum.GetValues(typeof(BuffAttrType)))
        {
            if (type is BuffAttrType.HitRate or
                BuffAttrType.MoveRangePercent or
                BuffAttrType.MeleeArmorPercent) // 默认为100
            {
                buffAttrDic[type] = 100f;
            }
            else if (type != BuffAttrType.None) // 其余数值默认为0
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
        if (hpBarFill != null) hpBarFill.localScale = new Vector3(1f, 1f, 1f);
    }

    public void SetHealth(float healthPercent)
    {
        _curHealth = Mathf.CeilToInt(_maxHealth * healthPercent / 100f);
        if (hpBarFill != null)
            hpBarFill.localScale = new Vector3((float)_curHealth / _maxHealth, 1f, 1f);
    }

    public void TakeDamage(AttackPack attackPack)
    {
        if (pc.isDead) return;
        if (attackPack.damage <= 0) return;
        _curHealth -= attackPack.damage;
        // 伤害跳字
        DamageText damageText = ObjectPool.Ins.GenerateObject(
            ItemType.DAMAGE_TEXT,
            transform.position, transform.rotation
        ).GetComponent<DamageText>();
        damageText.JumpOutNum(attackPack.damage);
        if (_curHealth <= 0) _curHealth = 0;
        UpdateHpBar();
        if (_curHealth <= 0)
        {
            if (pc != null)
            {
                pc.Dead();
            }
            else
            {
                // 触发单位死亡事件
                gameObject.SetActive(false); // 临时死亡
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
                this.transform.position + Vector3.up * 5f
                , Quaternion.identity);
        }

        BattleScene.Ins.UM.OnPieceStateChance(pc);
        Debug.Log($"受到{attackPack.damageType}伤害{attackPack.damage}");
    }

    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;
        _curHealth += healAmount;
        if (_curHealth > _maxHealth) _curHealth = _maxHealth;
        UpdateHpBar();
        Debug.Log($"恢复生命{healAmount}");
        BattleScene.Ins.UM.OnPieceStateChance(pc);
    }

    private void UpdateHpBar()
    {
        float healthPercent = (float)_curHealth / _maxHealth;
        if (hpBarFill != null)
            hpBarFill.localScale = new Vector3(healthPercent, 1f, 1f);
        if (pc is EnemyController enemy)
        {
            enemy.UpdateHpBar(healthPercent);
        }
    }

    public bool CostMP(int costPoint = 1)
    {
        if (_curMovePoint >= costPoint)
        {
            _curMovePoint -= costPoint;
            Debug.Log($"{gameObject.name}消耗行动力{costPoint}，剩余行动力{_curMovePoint}");
            BattleScene.Ins.UM.OnPieceStateChance(pc);
            return true;
        }

        return false;
    }

    public bool HasMP(int costPoint = 1)
    {
        return _curMovePoint >= costPoint;
    }

    public void AddMP(int mpAmount)
    {
        if (mpAmount <= 0) return;
        _curMovePoint += mpAmount;
        if (_curMovePoint > _maxMovePoint) _curMovePoint = _maxMovePoint;
        Debug.Log($"恢复行动力{mpAmount}");
        BattleScene.Ins.UM.OnPieceStateChance(pc);
    }

    public bool CostAmmo(int costCount = 1)
    {
        if (_ammoCount >= costCount)
        {
            _ammoCount -= costCount;
            BattleScene.Ins.UM.OnPieceStateChance(pc);
            return true;
        }

        return false;
    }

    public bool CostMana(int costPoint)
    {
        if (_manaPoint >= costPoint)
        {
            _manaPoint -= costPoint;
            Debug.Log($"{gameObject.name}消耗能量{costPoint}，剩余能量{_manaPoint}");
            BattleScene.Ins.UM.OnPieceStateChance(pc);
            return true;
        }

        return false;
    }

    public bool HasMana(int costPoint = 1)
    {
        return _manaPoint >= costPoint;
    }

    public void AddMana(int manaAmount)
    {
        if (manaAmount <= 0) return;
        _manaPoint += manaAmount;
        if (_manaPoint > _maxManaPoint) _manaPoint = _maxManaPoint;
        Debug.Log($"恢复能量{manaAmount}");
        BattleScene.Ins.UM.OnPieceStateChance(pc);
    }

    public bool CostItem(List<ItemPack> itemPacks)
    {
        if (itemPacks == null || itemPacks.Count == 0)
        {
            return true;
        }

        foreach (var item in itemPacks)
        {
            if (GM.Ins.PLAYERPROFILE.GetItemNum(item.itemName) >= item.itemNum)
            {
                // 扣除道具
                GM.Ins.PLAYERPROFILE.CostItem(item.itemName, item.itemNum);
                Debug.Log($"{gameObject.name}消耗道具{item.itemName}，消耗数量{item.itemNum}");
                return true;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}道具{item.itemName}数量不足，无法消耗！");
                return false;
            }
        }

        return false;
    }

    public bool HasItem(List<ItemPack> itemPacks)
    {
        if (itemPacks == null || itemPacks.Count == 0)
        {
            return true;
        }

        foreach (var item in itemPacks)
        {
            if (GM.Ins.PLAYERPROFILE.GetItemNum(item.itemName) < item.itemNum)
            {
                Debug.LogWarning($"{gameObject.name}道具{item.itemName}数量不足！");
                return false;
            }
        }

        return true;
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

    public void SetValues(int health, int mana, int ammo)
    {
        _curHealth = health;
        if (_curHealth > _maxHealth)
        {
            _curHealth = _maxHealth;
        }
        _manaPoint = mana;
        _ammoCount = _maxAmmoCount;// 弹药数量直接设置为满
    }
    
    /// <summary>
    /// 通过枚举统一修改属性数值
    /// </summary>
    /// <param name="type">要修改的属性类型</param>
    /// <param name="value">修改的增量（可以是负数）</param>
    public void ModifyAttribute(UnitAttrType type, float value)
    {
        // 将 float 转换为 int 供整数属性使用
        int intValue = Mathf.RoundToInt(value);

        switch (type)
        {
            case UnitAttrType.CurHealth:
                _curHealth = Mathf.Clamp(_curHealth + intValue, 0, _maxHealth);
                //UpdateHealthUI();
                break;
            case UnitAttrType.MaxHealth:
                _maxHealth += intValue;
                //UpdateHealthUI(); // 最大生命变化通常也需要刷新血条比例
                break;
            case UnitAttrType.CurMana:
                _manaPoint = Mathf.Clamp(_manaPoint + intValue, 0, _maxManaPoint);
                break;
            case UnitAttrType.MaxMana:
                _maxManaPoint += intValue;
                break;
            case UnitAttrType.CurMovePoint:
                _curMovePoint = Mathf.Clamp(_curMovePoint + intValue, 0, _maxMovePoint);
                break;
            case UnitAttrType.MaxMovePoint:
                _maxMovePoint += intValue;
                break;
            case UnitAttrType.MoveRange:
                _moveRange += value; // 移动范围通常是 float
                break;
            case UnitAttrType.TauntValue:
                _tauntValue += intValue;
                break;
            case UnitAttrType.CurAmmo:
                _ammoCount = Mathf.Clamp(_ammoCount + intValue, 0, _maxAmmoCount);
                break;
            case UnitAttrType.MaxAmmo:
                _maxAmmoCount += intValue;
                break;
            case UnitAttrType.CritRate:
                critRate += intValue;
                break;
            case UnitAttrType.CritDamageRate:
                critDamageRate += intValue;
                break;
            case UnitAttrType.ATK:
                ATK += intValue;
                break;
            case UnitAttrType.CON:
                CON += intValue;
                break;
        }

        // 每次修改属性后，同步战斗管理器的状态
        if (BattleScene.Ins != null && BattleScene.Ins.UM != null)
        {
            BattleScene.Ins.UM.OnPieceStateChance(pc);
        }
    
        Debug.Log($"属性 {type} 已修改，改变量为: {value}，当前值见检视面板。");
    }
    
}