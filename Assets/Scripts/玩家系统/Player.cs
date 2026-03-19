
using Sirenix.OdinInspector;
public enum AttrOp { Get, Add, Set } // 定义三种操作：读取、累加、赋值
[System.Serializable]
/// <summary>
/// 玩家棋子存档数据
/// </summary>
public struct Player
{
    [ShowInInspector][ReadOnly]private string name;
    [ShowInInspector][ReadOnly]private int hp;
    [ShowInInspector][ReadOnly]private int _hpmax;
    [ShowInInspector][ReadOnly]private int staying;
    [ShowInInspector][ReadOnly]private int stayingMax;
    [ShowInInspector][ReadOnly]private int yizhi;
    [ShowInInspector][ReadOnly]private int tactics;//作战
    [ShowInInspector][ReadOnly]int Physique;//体能
    [ShowInInspector][ReadOnly]private int Talk;//沟通
    [ShowInInspector][ReadOnly]private int Recognition;//模式识别

    public int curHealth;// 当前血量
    public int curAmmo;// 当前弹药
    public int curMana;// 当前能量
    
    [ShowInInspector][ReadOnly]private int skillPoints; // 当前可用点数
    [ShowInInspector][ReadOnly]private int level;

    public int SkillPoints
    {
        get => skillPoints;
        set => skillPoints = value;
    }

    public int Level
    {
        get => level;
        set => level = value;
    }

    public int Exp
    {
        get => exp;
        set => exp = value;
    }

    [ShowInInspector][ReadOnly]private int exp;
    
    
    /// <summary>
    /// 统一属性访问器
    /// </summary>
    /// <param name="index">属性编号</param>
    /// <param name="op">操作类型</param>
    /// <param name="val">操作数值 (仅在Add和Set模式下有效)</param>
    /// <returns>返回操作后的最终数值</returns>
    public int AccessAttribute(int index, AttrOp op, int val = 0) {
        // 先获取目标属性的引用
        // 注意：c# 7.0+ 支持 ref local，但 struct 内部可以直接操作
        switch (index) {
            case 0: return Execute(ref hp, op, val);
            //case 1: return Execute(ref hpMax, op, val);
            case 2: return Execute(ref staying, op, val);
            case 3: return Execute(ref stayingMax, op, val);
            case 4: return Execute(ref yizhi, op, val);
            case 5: return Execute(ref tactics, op, val);
            case 6: return Execute(ref Physique, op, val);
            case 7: return Execute(ref Talk, op, val);
            case 8: return Execute(ref Recognition, op, val);
            //case 9: return Execute(ref Luck, op, val);
            default: return 0;
        }
    }

    // 内部私有处理函数，进一步精简逻辑
    private int Execute(ref int field, AttrOp op, int val) {
        if (op == AttrOp.Add) field += val;
        else if (op == AttrOp.Set) field = val;
        return field; // Get 模式直接返回
    }
    
    // 获取属性名称的快捷方法
    public string GetAttrName(int index) {
        string[] names = { "生命值", "最大生命", "耐力", "最大耐力", "意志", "作战", "体能", "沟通", "模式识别", "幸运" };
        return (index >= 0 && index < names.Length) ? names[index] : "未知";
    }

    public string NAME
    {
        get { return name; }
        set { name = value; }
    }
    public int HPMAX
    {
        get { return _hpmax; }
        set { _hpmax = value; }
    }


    public int HP
    {
        get { return hp; }
        set { hp = value; }
    }
    public int STAYING
    {
        get { return staying; }
        set { staying = value; }
    }
    public int STAYINGMAX
    {
        get { return stayingMax; }
        set { stayingMax = value; }
    }
    public int YIZHI
    {
        get { return yizhi; }
        set { yizhi = value; }
    }
    public int TACTICS
    {
        get { return tactics; }
        set { tactics = value; }
    }
    public int PHYSIQUE
    {
        get { return Physique; }
        set { Physique = value; }
    }
    public int TALK
    {
        get { return Talk; }
        set { Talk = value; }
    }

    public int RECOGNITION
    {
        get { return Recognition; }
        set { Recognition = value; }    
    }
}