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
    public float MoveRange => Mathf.Max(0f, _moveRange) *
        (buffAttrDic.TryGetValue(BuffAttrType.MoveRangePercent, out float percent)
            ? Mathf.Max(0f, percent) / 100f : 1f) * (pc != null ? pc.MemeState.TimedMoveMultiplier : 1f);

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
        buffStates.Clear();
        _curHealth = _maxHealth;
        //_manaPoint = _maxManaPoint;
        InitBuffAttrDic();
        FullAmmo();
    }

    // 保存场景/预制体的原始属性，重复初始化时不把上次等级或 Buff 当作基础值。
    private AttrCenter enemyBaseAttributes;
    private int enemyBaseAttack, enemyBaseConstitution, enemyBaseTaunt;

    public void SetEnemyData(PieceData data, int level)
    {
        // 1. 首次保存原始值；以后每次都恢复基础值，再加载 SO。
        if (enemyBaseAttributes == null)
        {
            enemyBaseAttributes = _attr.Copy();
            enemyBaseAttack = ATK;
            enemyBaseConstitution = CON;
            enemyBaseTaunt = _tauntValue;
        }
        _attr = enemyBaseAttributes.Copy();
        ATK = enemyBaseAttack;
        CON = enemyBaseConstitution;
        _tauntValue = enemyBaseTaunt;
        buffStates.Clear();
        SetData(data, null);

        // 2. 在已经加载的基础属性上应用等级加成，不回写共享 SO。
        if (data.levelGrowth != null)
        {
            foreach (var pair in data.levelGrowth)
                ApplyEnemyGrowth(pair.Key, pair.Value, level);
        }

        // 3. 限制非法属性，并以最终上限初始化生命、行动力、能量和弹药。
        _maxHealth = Mathf.Max(1, _maxHealth);
        _maxMovePoint = Mathf.Max(0, _maxMovePoint);
        _maxManaPoint = Mathf.Max(0, _maxManaPoint);
        _maxAmmoCount = Mathf.Max(0, _maxAmmoCount);
        _moveRange = Mathf.Max(0f, _moveRange);
        critRate = Mathf.Clamp(critRate, 0, 100);
        buffAttrDic[BuffAttrType.EvasionRate] = Mathf.Clamp(buffAttrDic[BuffAttrType.EvasionRate], 0f, 100f);
        buffAttrDic[BuffAttrType.DamageReduction] = Mathf.Clamp(buffAttrDic[BuffAttrType.DamageReduction], 0f, 100f);
        _curHealth = _maxHealth;
        _curMovePoint = _maxMovePoint;
        _manaPoint = _maxManaPoint;
        _ammoCount = _maxAmmoCount;
    }

    private void ApplyEnemyGrowth(EnemyGrowthAttribute attribute, float growth, int level)
    {
        int Grow(float value) => EnemyLevelGrowth.Calculate(value, growth, level);
        switch (attribute)
        {
            case EnemyGrowthAttribute.MaxHealth: _maxHealth = Grow(_maxHealth); break;
            case EnemyGrowthAttribute.MaxActionPoints: _maxMovePoint = Grow(_maxMovePoint); break;
            case EnemyGrowthAttribute.MoveRange: _moveRange = Grow(_moveRange); break;
            case EnemyGrowthAttribute.MaxMana: _maxManaPoint = Grow(_maxManaPoint); break;
            case EnemyGrowthAttribute.MaxAmmo: _maxAmmoCount = Grow(_maxAmmoCount); break;
            case EnemyGrowthAttribute.Attack: ATK = Grow(ATK); break;
            case EnemyGrowthAttribute.Constitution: CON = Grow(CON); break;
            case EnemyGrowthAttribute.Taunt: _tauntValue = Grow(_tauntValue); break;
            case EnemyGrowthAttribute.CriticalRate: critRate = Grow(critRate); break;
            case EnemyGrowthAttribute.CriticalDamage: critDamageRate = Grow(critDamageRate); break;
            case EnemyGrowthAttribute.KineticAttack: _attr.SetAtk(DamageType.Melee, Mathf.Max(0, Grow(_attr.GetAtk(DamageType.Melee)))); break;
            case EnemyGrowthAttribute.ThermalAttack: _attr.SetAtk(DamageType.Ranged, Mathf.Max(0, Grow(_attr.GetAtk(DamageType.Ranged)))); break;
            case EnemyGrowthAttribute.SparkAttack: _attr.SetAtk(DamageType.Electric, Mathf.Max(0, Grow(_attr.GetAtk(DamageType.Electric)))); break;
            case EnemyGrowthAttribute.KineticArmor: _attr.SetArmor(DamageType.Melee, Mathf.Max(0, Grow(_attr.GetArmor(DamageType.Melee)))); break;
            case EnemyGrowthAttribute.ThermalArmor: _attr.SetArmor(DamageType.Ranged, Mathf.Max(0, Grow(_attr.GetArmor(DamageType.Ranged)))); break;
            case EnemyGrowthAttribute.SparkArmor: _attr.SetArmor(DamageType.Electric, Mathf.Max(0, Grow(_attr.GetArmor(DamageType.Electric)))); break;
            case EnemyGrowthAttribute.HitRate: buffAttrDic[BuffAttrType.HitRate] = Grow(buffAttrDic[BuffAttrType.HitRate]); break;
            case EnemyGrowthAttribute.EvasionRate: buffAttrDic[BuffAttrType.EvasionRate] = Grow(buffAttrDic[BuffAttrType.EvasionRate]); break;
            case EnemyGrowthAttribute.DamageIncrease: buffAttrDic[BuffAttrType.DamageIncrease] = Grow(buffAttrDic[BuffAttrType.DamageIncrease]); break;
            case EnemyGrowthAttribute.DamageReduction: buffAttrDic[BuffAttrType.DamageReduction] = Grow(buffAttrDic[BuffAttrType.DamageReduction]); break;
        }
    }

    private int playerAttackBonus, playerConstitutionBonus;

    public void SetData(PieceData pieceData, Player playerData = null)
    {
        ATK -= playerAttackBonus; CON -= playerConstitutionBonus;
        playerAttackBonus = playerConstitutionBonus = 0;
        _maxAmmoCount = pieceData.maxAmmoCount;
        _maxHealth = pieceData.maxHealth;
        _maxMovePoint = pieceData.maxMovePoint;
        _moveRange = pieceData.moveRange;
        elementType = pieceData.elementType;
        _maxManaPoint = pieceData.maxMana;
        //_manaPoint = pieceData.initialMana;
        critRate = pieceData.critRate;
        critDamageRate = pieceData.critDamageRate;
        if (playerData != null)
        {
            // 根据玩家属性调整单位属性
            _maxHealth += playerData.AccessAttribute(2, AttrOp.Get) * 2; // 体能每点增加2点生命
            _maxManaPoint += playerData.AccessAttribute(0, AttrOp.Get) * 2; // 意志每点增加2点能量
            playerAttackBonus = (int)(playerData.AccessAttribute(1, AttrOp.Get) * 0.5f);
            playerConstitutionBonus = (int)(playerData.AccessAttribute(4, AttrOp.Get) * 0.5f);
            ATK += playerAttackBonus;
            CON += playerConstitutionBonus;
            // 其他属性调整可以在这里添加
            critRate += (playerData.AccessAttribute(3, AttrOp.Get) + playerData.AccessAttribute(4, AttrOp.Get) ) * 2; // 技巧每点增加1%暴击率
        }
        _curHealth = _maxHealth;
        _manaPoint = _maxManaPoint;
        
        // 显式配置的分类型攻击力覆盖场景基础值，缺省保留原有值。
        if (pieceData.attackDic != null)
            foreach (var pair in pieceData.attackDic) _attr.SetAtk(pair.Key, pair.Value);

        // 护甲值
        if (pieceData._armorDic != null && pieceData._armorDic.Count > 0)
        {
            foreach (var pair in pieceData._armorDic)
            {
                _attr.SetArmor(pair.Key, pair.Value);
            }
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
        SetMovePoint(_maxMovePoint);
    }

    public void SetMovePoint(int num)
    {
        _curMovePoint = GetBuffStacks(BuffType.Stun) != 0 ? 0 : Mathf.Clamp(num, 0, _maxMovePoint);
    }

    public void FullAmmo()
    {
        _ammoCount = _maxAmmoCount;
    }

    public void FullHealth()
    {
        int previousHealth = _curHealth;
        _curHealth = _maxHealth;
        if (hpBarFill != null) hpBarFill.localScale = new Vector3(1f, 1f, 1f);
        UpdateHpBar();
        NotifyHealthChanged(previousHealth);
    }

    public void SetHealth(float healthPercent)
    {
        int previousHealth = _curHealth;
        _curHealth = Mathf.CeilToInt(_maxHealth * Mathf.Clamp(healthPercent, 0f, 100f) / 100f);
        UpdateHpBar();
        NotifyHealthChanged(previousHealth);
    }

    private void NotifyHealthChanged(int previousHealth, int previousMaxHealth = -1)
    {
        if (pc == null || (previousHealth == _curHealth &&
            (previousMaxHealth < 0 || previousMaxHealth == _maxHealth))) return;
        BattleScene.Ins?.BM?.ClearMoveUndo();
        BattleScene.Ins?.BM?.characterSkillManager?.NotifyHpChanged(pc.gameObject, _curHealth, _maxHealth);
    }
    /// <summary>每次有效生命伤害提交后发送一次结果；观察者不得再次扣除本次伤害，屏障全吸收不发送。</summary>
    public event System.Action<BattleDamageResult> DamageApplied;

    /// <summary>兼容旧伤害入口；未提供来源时标记Unknown，仍只提交一次伤害。</summary>
    public void TakeDamage(AttackPack attackPack) => TakeDamage(attackPack, new BattleDamageSource(BattleDamageKind.Unknown));

    /// <summary>提交有来源的伤害并返回实际生命损失；负伤害和死亡目标不发送结果事件。</summary>
    public BattleDamageResult TakeDamage(AttackPack attackPack, BattleDamageSource source)
    {
        source ??= new BattleDamageSource(BattleDamageKind.Unknown);
        if (pc.isDead || attackPack.damage < 0)
            return new BattleDamageResult(pc, source, attackPack.damage, _curHealth, _curHealth, true);
        var battle = BattleScene.Ins.BM;
        if (battle.PlayerController.isBursting &&
            (source.Kind == BattleDamageKind.Unknown || source.Kind == BattleDamageKind.Component || source.Kind == BattleDamageKind.Collision ||
             source.Kind == BattleDamageKind.Skill && !battle.IsBurstActor(source.Action.Actor)) &&
            battle.TryDeferBurstEffect(this, () => TakeDamage(attackPack, source), source.Action))
            return new BattleDamageResult(pc, source, attackPack.damage, _curHealth, _curHealth, true);
        battle.ClearMoveUndo();
        using var sourceScope = source.Action.Enter();
        // 1. 负数不作为伤害处理；零伤害命中仍继续播放受击反馈。
        int previousHealth = _curHealth;
        _curHealth -= attackPack.damage;
        // 2. 所有有效命中都显示伤害跳字，包括 0。
        System.Action showDamage = () => battle.tipTextManager.ShowDamage(transform, attackPack.damage, attackPack.isCritical);
        if (pc.isPlayerPiece || !battle.PlayerController.isBursting || !battle.TryDeferBurstEffect(this, showDamage, source.Action)) showDamage();
        if (_curHealth <= 0) _curHealth = 0;
        // 在生命变化回调与死亡事件前固定本段结果，嵌套被动不能篡改这次实际掉血。
        var result = new BattleDamageResult(pc, source, attackPack.damage, previousHealth, _curHealth);
        BattleScene.Ins.BM.timedMemeStatuses.OnDamageCommitted(result);
        UpdateHpBar();
        NotifyHealthChanged(previousHealth);
        // 按真实掉血充能，放在死亡分支前以覆盖致死伤害，零伤害不计入。
        if (pc.isPlayerPiece && _curHealth < previousHealth)
            BattleScene.Ins.BM.PlayerController.ChargeBurst(GameConst.hurtBurstCharge);
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
            if (pc == null) return result;
            
            pc.Hurt();
            
            // 3. 零伤害保留受击动画和特效，但不发送生命值变化通知。

            System.Action impact = () => ObjectPool.Ins.GenerateObject(
                attackPack.damageType == DamageType.Melee ? ItemType.KINETIC_ATTACK : ItemType.ENERGY_ATTACK,
                transform.position + Vector3.up * 5f, Quaternion.identity);
            if (pc.isPlayerPiece || !battle.PlayerController.isBursting || !battle.TryDeferBurstEffect(this, impact, source.Action)) impact();
        }

        BattleScene.Ins.UM.OnPieceStateChance(pc);
        DamageApplied?.Invoke(result);
        return result;
    }

    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;
        int previousHealth = _curHealth;
        _curHealth += healAmount;
        if (_curHealth > _maxHealth) _curHealth = _maxHealth;
        UpdateHpBar();
        NotifyHealthChanged(previousHealth);
        Debug.Log($"恢复生命{healAmount}");
        BattleScene.Ins.UM.OnPieceStateChance(pc);
        BattleScene.Ins.BM.tipTextManager.ShowHeal(pc.transform,healAmount);
        ObjectPool.Ins.GenerateObject(ItemType.HEAL_EFFECT, pc.transform.position+Vector3.up*2f, Quaternion.identity);
        GM.Ins.AM.PlayAudio(AudioCueType.Heal);
    }

    private void UpdateHpBar()
    {
        float healthPercent = (float)_curHealth / _maxHealth;
        if (hpBarFill != null)
            hpBarFill.DOScale( new Vector3(healthPercent, 1f, 1f), 0.5f);
        if (pc is EnemyController enemy)
        {
            enemy.UpdateHpBar(healthPercent);
        }
    }

    public bool CostMP(ActionType actionType)
    {
        if (pc.isDead || GetBuffStacks(BuffType.Stun) != 0) return false;
        if ((actionType == ActionType.MeleeAttack || actionType == ActionType.RangedAttack || actionType == ActionType.OrbitalStrike || actionType == ActionType.GuardOrder) &&
            !BattleRelations.CanInitiateAttack(pc)) return false;
        if ((actionType == ActionType.Move || actionType == ActionType.Climb) &&
            GetBuffStacks(BuffType.Bind) != 0) return false;
        int costPoint = GM.Ins.DM.gameConstSO.GetActionPointCost(actionType);
        if (_curMovePoint >= costPoint)
        {
            BattleScene.Ins.BM.RecordMovePayment(pc, actionType, costPoint);
            _curMovePoint -= costPoint;
            BattleScene.Ins.UM.NotifyActionCommitted(pc);
            if (costPoint > 0 && actionType != ActionType.Wait && !BattleScene.Ins.BM.timedMemeStatuses.BeforePaidAction(pc)) return false;
            if (!BattleScene.Ins.BM.buffManager.OnAction(this)) return false;
            if ((actionType == ActionType.Move || actionType == ActionType.Climb) &&
                GetBuffStacks(BuffType.Bind) != 0) return false;
            Debug.Log($"{gameObject.name}执行{actionType}消耗行动力{costPoint}，剩余行动力{_curMovePoint}");
            BattleScene.Ins.UM.OnPieceStateChance(pc);
            BattleScene.Ins.UM.ShowUndoMoveButton(false);
            return true;
        }
        return false;
    }
    /// <summary>旧固定费用入口不推断行动类型；结束回合沿用此入口，不触发新的主动行动状态。</summary>
    public bool CostMP(int costPoint) => CostMP(costPoint, false);

    /// <summary>固定费用主动行动显式传true；先扣AP再结算精确状态，致死不退AP、不执行后续动作。</summary>
    public bool CostMP(int costPoint, bool activeAction)
    {
        if (pc.isDead || GetBuffStacks(BuffType.Stun) != 0) return false;
        if (_curMovePoint >= costPoint)
        {
            BattleScene.Ins.BM.ClearMoveUndo();
            _curMovePoint -= costPoint;
            BattleScene.Ins.UM.NotifyActionCommitted(pc);
            if (costPoint > 0 && activeAction && !BattleScene.Ins.BM.timedMemeStatuses.BeforePaidAction(pc)) return false;
            if (!BattleScene.Ins.BM.buffManager.OnAction(this)) return false;
            Debug.Log($"{gameObject.name}消耗行动力{costPoint}，剩余行动力{_curMovePoint}");
            BattleScene.Ins.UM.OnPieceStateChance(pc);
            BattleScene.Ins.UM.ShowUndoMoveButton(false);
            return true;
        }
        return false;
    }

    public bool HasMP(ActionType actionType)
    {
        int costPoint = GM.Ins.DM.gameConstSO.GetActionPointCost(actionType);
        return _curMovePoint >= costPoint;
    }

    public void AddMP(int mpAmount)
    {
        if (mpAmount <= 0 || GetBuffStacks(BuffType.Stun) != 0) return;
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
        if (costPoint < 0) return false;
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
        return costPoint >= 0 && _manaPoint >= costPoint;
    }

    public void AddMana(int manaAmount)
    {
        if (manaAmount <= 0) return;
        _manaPoint += manaAmount;
        if (_manaPoint > _maxManaPoint) _manaPoint = _maxManaPoint;
        Debug.Log($"恢复能量{manaAmount}");
        BattleScene.Ins.UM.OnPieceStateChance(pc);
        ObjectPool.Ins.GenerateObject(ItemType.CHARGE_EFFECT, pc.transform.position+Vector3.up*2f, Quaternion.identity);
    }

    public bool CostItem(List<ItemPack> itemPacks)
    {
        if (!TryGetItemCosts(itemPacks, out var costs) || !HasItemCosts(costs)) return false;
        CommitItemCosts(costs);
        return true;
    }

    /// <summary>战斗道具确认时支付 Item 行动点和一件库存；提交前资源不足不扣费，行动钩子中断或使库存失效时只消耗 AP。</summary>
    public bool TryCostBattleItem(ItemName itemName)
    {
        var costs = new Dictionary<ItemName, int> { [itemName] = 1 };
        if (!HasItemCosts(costs) || !CostMP(ActionType.Item)) return false;
        // 行动钩子可能改变背包；不在库存已失效时继续生效或扣成负数。
        if (!HasItemCosts(costs)) return false;
        CommitItemCosts(costs);
        return true;
    }

    /// <summary>只读查询材料是否足够；合并同名费用，不分配集合、不输出日志或扣费，适用于持续 UI 刷新。</summary>
    public bool HasItem(List<ItemPack> itemPacks)
    {
        if (itemPacks == null) return true;
        foreach (var item in itemPacks)
            if (item == null || item.itemNum < 0) return false;
        for (int i = 0; i < itemPacks.Count; i++)
        {
            var item = itemPacks[i];
            bool counted = false;
            for (int j = 0; j < i; j++)
                if (itemPacks[j].itemName == item.itemName) { counted = true; break; }
            if (counted) continue;
            int total = 0;
            // 材料列表通常很短，直接合计避免每帧为每个技能分配临时字典。
            for (int j = i; j < itemPacks.Count; j++)
            {
                if (itemPacks[j].itemName != item.itemName) continue;
                int amount = itemPacks[j].itemNum;
                if (total > int.MaxValue - amount) return false;
                total += amount;
            }
            if (total == 0) continue;
            int owned = 0;
            foreach (var pack in GM.Ins.PLAYERPROFILE.itemPacks)
                if (pack.itemName == item.itemName) { owned = pack.itemNum; break; }
            if (owned < total) return false;
        }
        return true;
    }

    /// <summary>普通及延迟技能共用：资源不足不扣费；行动异常中断只消耗行动点。</summary>
    public bool TryCostSkill(SkillPack skill)
    {
        int manaCost = GetSkillManaCost(skill);
        if (skill == null || BattleRelations.IsOffensive(skill) && !BattleRelations.CanInitiateAttack(pc) || !HasMana(manaCost) ||
            !TryGetItemCosts(skill.consumeItems, out var costs) || !HasItemCosts(costs)) return false;
        // CostMP 内部处理死亡、眩晕、行动点校验以及行动触发的 Buff。
        if (!CostMP(ActionType.Skill)) return false;
        _manaPoint -= manaCost;
        CommitItemCosts(costs);
        BattleScene.Ins.UM.OnPieceStateChance(pc);
        return true;
    }

    public int GetSkillManaCost(SkillPack skill) => ComponentEquipment.ManaCost(pc?.playerData, skill);

    // 换装仅调整属性差值，不重置生命、能量、弹药、行动点或 Buff。
    public void RefreshPlayerAttributes(int[] previous, int[] current)
    {
        int oldHealth = _curHealth, oldMaxHealth = _maxHealth;
        _maxHealth = Mathf.Max(1, _maxHealth + (current[2] - previous[2]) * 2);
        _curHealth = Mathf.Min(_curHealth, _maxHealth);
        _maxManaPoint = Mathf.Max(0, _maxManaPoint + (current[0] - previous[0]) * 2);
        _manaPoint = Mathf.Min(_manaPoint, _maxManaPoint);
        int attackDelta = (int)(current[1] * 0.5f) - (int)(previous[1] * 0.5f);
        int conDelta = (int)(current[4] * 0.5f) - (int)(previous[4] * 0.5f);
        ATK += attackDelta; playerAttackBonus += attackDelta;
        CON += conDelta; playerConstitutionBonus += conDelta;
        critRate += (current[3] + current[4] - previous[3] - previous[4]) * 2;
        UpdateHpBar();
        NotifyHealthChanged(oldHealth, oldMaxHealth);
    }

    private static bool TryGetItemCosts(List<ItemPack> items, out Dictionary<ItemName, int> costs)
    {
        costs = new Dictionary<ItemName, int>();
        if (items == null) return true;
        foreach (var item in items)
        {
            if (item == null || item.itemNum < 0) return false;
            if (item.itemNum == 0) continue;
            costs.TryGetValue(item.itemName, out int previous);
            // 同一种材料可出现多次，必须按合计数量校验。
            if (previous > int.MaxValue - item.itemNum) return false;
            costs[item.itemName] = previous + item.itemNum;
        }
        return true;
    }

    private static bool HasItemCosts(Dictionary<ItemName, int> costs)
    {
        foreach (var cost in costs)
        {
            if (GM.Ins.PLAYERPROFILE.GetItemNum(cost.Key) < cost.Value)
            {
                Debug.LogWarning($"道具{cost.Key}数量不足！");
                return false;
            }
        }
        return true;
    }

    private void CommitItemCosts(Dictionary<ItemName, int> costs)
    {
        foreach (var cost in costs)
            GM.Ins.PLAYERPROFILE.CostItem(cost.Key, cost.Value);
        // 全部扣除后再发布表现，避免只扣除了列表中的第一项。
        foreach (var cost in costs)
            BattleScene.Ins.BM.tipTextManager.ShowTip(transform, $"消耗道具{cost.Key}x{cost.Value}");
    }

    public int GetBuffStacks(BuffType buffType)
    {
        foreach (var buff in buffStates)
        {
            if (buff.buffType == buffType)
            {
                return buff.sources.Count > 0 ? -1 : buff.stacks;
            }
        }

        return 0;
    }

    public void SetValues(int health, int mana, int ammo)
    {
        int previousHealth = _curHealth;
        _curHealth = health;
        if (_curHealth > _maxHealth)
        {
            _curHealth = _maxHealth;
        }
        _manaPoint = mana;
        _ammoCount = _maxAmmoCount;// 弹药数量直接设置为满
        NotifyHealthChanged(previousHealth);
    }
    
    public void SetHurtState()
    {
        int previousHealth = _curHealth;
        Debug.Log($"{gameObject.name} 进入受伤状态，生命值和能量都设置为30%");
        // 生命值和能量都设置为30%
        _curHealth = Mathf.RoundToInt(_maxHealth * 0.3f);
        _manaPoint = Mathf.RoundToInt(_maxManaPoint * 0.3f);
        NotifyHealthChanged(previousHealth);
    }
    
    /// <summary>
    /// 通过枚举统一修改属性数值
    /// </summary>
    /// <param name="type">要修改的属性类型</param>
    /// <param name="value">修改的增量（可以是负数）</param>
    public void ModifyAttribute(UnitAttrType type, float value)
    {
        int previousHealth = _curHealth;
        int previousMaxHealth = _maxHealth;
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
                _curHealth = _maxHealth;
                UpdateHpBar();
                BattleScene.Ins.UM.OnPieceStateChance(pc);
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

        NotifyHealthChanged(previousHealth, previousMaxHealth);
        // 每次修改属性后，同步战斗管理器的状态
        if (BattleScene.Ins != null && BattleScene.Ins.UM != null)
        {
            BattleScene.Ins.UM.OnPieceStateChance(pc);
        }
    
        Debug.Log($"属性 {type} 已修改，改变量为: {value}，当前值见检视面板。");
    }
    
}
