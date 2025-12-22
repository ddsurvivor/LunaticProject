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
}
