using Sirenix.OdinInspector;

/// <summary>
/// 伤害类别
/// </summary>
public enum DamageType
{
    [LabelText("动能")]
    Melee = 1,// 近战
    [LabelText("热能")]Ranged = 2,// 远程
    [LabelText("火能")]Electric = 3,// 电子
}

/// <summary>
/// 棋子行动类型
/// </summary>
public enum ActionType
{
    移动 = 1,// 移动
    近战攻击 = 2,// 攻击
    远程攻击 = 3,// 远程攻击
    重新装填 = 4,// 装填
    待机 = 5,// 待机
    扫描 = 6,// 扫描
    解除束缚 = 7,// 解除束缚
    攀爬 = 8,// 攀爬
    技能 = 10,// 技能
    道具 = 11,// 道具
    
}

// 棋子属性种类
public enum PieceElementType
{
    None = 0,
    // 机械
    Mechanical = 1,
    // 生物
    Biological = 2,
}

public enum BuffType
{
    [LabelText("充能")]// 充能
    Charge = 1,
    [LabelText("自动治疗")]// 自动治疗
    AutoHeal = 2,
    [LabelText("防护")]// 防护
    Shield = 3,
    [LabelText("隐蔽")]// 隐蔽
    Conceal = 4,
    
    // 100以后为Debuff
    [LabelText("干扰")]// 干扰
    Disrupt = 101,
    [LabelText("过载")]// 过载
    Overload = 102,
    [LabelText("束缚")]// 束缚
    Bind = 103,
    [LabelText("燃烧")]
    Burn = 104,
    [LabelText("脆弱")]
    Frail = 105,
}

/// <summary>
/// buff加成属性类型
/// </summary>
public enum BuffAttrType
{
    None = 0,
    // 闪避率
    EvasionRate = 1,
    // 命中率
    HitRate = 2,
    // 伤害减免
    DamageReduction = 3,
    // 伤害增加
    DamageIncrease = 4,
    // 移动范围百分比
    MoveRangePercent = 5,
    // 动能护甲百分比
    MeleeArmorPercent = 6,
}

public enum BattleItemType
{
    HealKit = 1,// 治疗包
    // 能量包
    EnergyPack = 2,
}

public enum EnemyAIType
{
    // 优先攻击最近的单位
    [LabelText("攻击型")]
    AttackNearest = 1,
    // 远离目标
    [LabelText("射击型")]
    Shoot = 2,
    [LabelText("技能型")]
    SkillUser = 3,
}

#region 技能枚举

public enum SkillTarget
{
    Enemy = 1,// 单体敌人
    Ally = 2,// 单体友军
    Self = 3,// 自身
    EnemyAll = 4,// 全体敌人
    Area = 5,// 区域
    All = 6,// 全体单位
    FarthestEnemy = 7,// 最远敌人
}

public enum RangeType
{
    [LabelText("圆形")]
    Circle = 1,// 圆形
    
    [LabelText("扇形")]// 扇形
    Fan = 2,
    [LabelText("手雷")]// 手雷
    Grenade = 3,
}

public enum SkillEffect
{
    HealArea = 1,// 区域治疗
}

#endregion

