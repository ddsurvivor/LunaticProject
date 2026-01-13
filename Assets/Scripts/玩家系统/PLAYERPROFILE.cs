using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

public class PLAYERPROFILE : SerializedMonoBehaviour
{
    public static PLAYERPROFILE instance;
    public Dictionary<string, int> 已完成任务=new Dictionary<string, int>();
    public struct Player
    {
        private string name;
        private int hp;
        private int _hpmax;
        private int staying;
        private int stayingMax;
        private int yizhi;
		private int tactics;//作战
        int Physique;//体能
        private int Talk;//沟通
        private int Recognition;//模式识别

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

    public static Player[] player = new Player[20];

    private void Awake()
    {
        instance = this;
        新游戏初始化数值();
    }

    public void 新游戏初始化数值()
    {
        Debug.Log("检测为新游戏,初始化数值");
        player[0] = new Player();
        player[0].NAME = "QIUWU";
        player[0].HP = 15;//体力
        player[0].HPMAX = 15;
        player[0].STAYING = 30;//耐力
        player[0].STAYINGMAX = 30;
        player[0].PHYSIQUE = 4;//体能
        player[0].TACTICS = 5;//作战
        player[0].YIZHI = 5;
        player[0].TALK = 5;
        player[0].RECOGNITION = 3;//模式识别
        player[1] = new Player(); 
        // player[2] ── 绿
        player[1].NAME        = "LV";
        player[1].PHYSIQUE    = 2;          // 体能
        player[1].YIZHI       = 4;          // 意志
        player[1].TALK        = 3;      // 沟通 
        player[1].TACTICS     = 4;      // 作战
        player[1].RECOGNITION = 7;      // 模式识别 
        player[1].HP          = 10;         // 当前生命
        player[1].HPMAX       = 10;         // 生命上限
        player[1].STAYING     = 20;         // 耐力
        player[1].STAYINGMAX = 20;
// player[2] ── 马赛
        player[2] = new Player();
        player[2].NAME        = "MASAI";
        player[2].PHYSIQUE    = 4;      // 体能 
        player[2].YIZHI       = 3;      // 意志 
        player[2].TALK        = 5;          // 沟通
        player[2].TACTICS     = 6;      // 作战 
        player[2].RECOGNITION = 2;          // 模式识别
        player[2].HP          = 30;         // 当前生命
        player[2].HPMAX       = 30;         // 生命上限
        player[2].STAYING     = 20;       
        player[2].STAYINGMAX  = 20;   
    }
    

    public void 保存任务进度(string 任务, int 进度)
    {
        已完成任务[任务] = 进度;
    }
    
    public static void 修改属性(int index, string fieldName, int value)
    {
        if (index < 0 || index >= player.Length)
        {
            Debug.LogError($"修改属性时index越界!");
            return;
        }
        PropertyInfo propertyInfo = typeof(Player).GetProperty(fieldName,
            BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo == null)
        {
            Debug.LogError($"修改属性失败：Player 中不存在属性 {fieldName}!");
            return;
        }
        if (propertyInfo.PropertyType != typeof(int))
        {
            Debug.LogError($"修改属性失败：{fieldName} 不是 int 类型");
            return;
        }
        Player p = player[index];
        int oldVal = (int)propertyInfo.GetValue(p);
        int newVal = oldVal + value;
        propertyInfo.SetValue(p, newVal);
        player[index] = p; 
        Debug.Log($"修改属性测试: {index} 的 {fieldName} 从 {oldVal} 改为 {newVal}");
    }

    public static T 获取数据<T>(string fieldName, int index)
    {
        Player[] players = player;

        if (index < 0 || index >= players.Length)
            throw new IndexOutOfRangeException($"Index {index} is out of range for the players array.");

        Player selectedPlayer = players[index];
        PropertyInfo propertyInfo = typeof(Player).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);

        if (propertyInfo == null)
            throw new ArgumentException($"Property '{fieldName}' not found in Player.");

        if (propertyInfo.PropertyType != typeof(T))
            throw new InvalidOperationException($"Property '{fieldName}' is not of type {typeof(T).Name}.");
        object baseValue = propertyInfo.GetValue(selectedPlayer);
        
        if (typeof(T) == typeof(int))
        {
            int intValue = (int)baseValue;

            #region 主角技能

            if (index == 0)
            {
                bool isTargetField =
                    fieldName == nameof(Player.YIZHI)     ||
                    fieldName == nameof(Player.PHYSIQUE)  ||
                    fieldName == nameof(Player.TALK);
                int skillLevel = 0;
                剧本技能.当前技能.TryGetValue("继承者", out skillLevel);
                if (isTargetField && skillLevel > 0)
                {
                    intValue += 3;
                }
            }

            #endregion
           
            return (T)(object)intValue;
        }

        // 对非 int 直接返回
        return (T)baseValue;
    }

    public int 获取任务进度(string t)
    {
        int rt = 0;
        try
        {
            rt = 已完成任务[t];
        }
        catch (IndexOutOfRangeException e)
        {
            //Debug.LogError("此任务没有做过或者任务名输入错误"+e);
        }
        catch (KeyNotFoundException e)
        {
            //Debug.LogError("此任务没有做过或者任务名输入错误"+e);
        }

        return rt;
    }
}