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
    移动 = 1,// 移动
    近战攻击 = 2,// 攻击
    远程攻击 = 3,// 远程攻击
    重新装填 = 4,// 装填
    待机 = 5,// 待机
    扫描 = 6,// 扫描
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
    // 充能
    Charge = 1,
    // 自动治疗
    AutoHeal = 2,
    // 防护
    Shield = 3,
    // 隐蔽
    Conceal = 4,
    
    // 100以后为Debuff
    // 干扰
    Disrupt = 101,
    // 过载
    Overload = 102,
    // 束缚
    Bind = 103,
    // 燃烧
    Burn = 104,
    脆弱 = 105,
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
}

