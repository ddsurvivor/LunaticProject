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
/// 棋子行动类型
/// </summary>
public enum ActionType
{
    Move = 1,// 移动
    Attack = 2,// 攻击
    Skill = 3,// 技能
    Defend = 4,// 防御
    Idle = 5,// 待机
    Scan = 6,// 扫描
    Range_ATK = 7,// 远程攻击
    Reload = 8,// 装填
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
    // 充能
    Charge = 1,
    // 自动治疗
    AutoHeal = 2,
    // 防护
    Shield = 3,
    
    // 100以后为Debuff
    // 干扰
    Disrupt = 101,
    // 过载
    Overload = 102,
    // 束缚
    Bind = 103,
    // 燃烧
    Burn = 104,
}

