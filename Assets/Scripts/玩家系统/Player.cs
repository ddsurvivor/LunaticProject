
using Sirenix.OdinInspector;

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