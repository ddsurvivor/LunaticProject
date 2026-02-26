using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// 玩家存档文件
/// </summary>
[System.Serializable]
public class PLAYERPROFILE
{
    //public static PLAYERPROFILE instance;
    [OdinSerialize]
    public Dictionary<string, int> finishNodeDic=new Dictionary<string, int>();
    
    [OdinSerialize]
    /// <summary>
    /// 棋子角色数据
    /// </summary>
    public Player[] player = new Player[20];

    [Header("进度存档")]
    public int date = 0;
    public Daytime daytime = Daytime.上午;
    public string currentMap = "TEST";
    public int curSmallMapIndex = 0;

    [Header("道具存档")] 
    public List<ItemPack> itemPacks = new();
    public int coins;// 金币数量
    
    [Header("存档信息")]
    public DateTime lastSaveTime;
    public bool isNewGame = false;

    // private void Awake()
    // {
    //     //instance = this;
    //     新游戏初始化数值();
    // }

    public void 新游戏初始化数值()
    {
        Debug.Log("检测为新游戏,初始化数值");
        coins = GameConst.initialCoins; 
        
        player = new Player[20];
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
        
        
        // 道具
        itemPacks.Clear();
        itemPacks.Add(new ItemPack(ItemName.能量包, 6));
        itemPacks.Add(new ItemPack(ItemName.医疗单元I型, 3));
        
        //初始化
        isNewGame = true;
    }
    

    public void 保存任务进度(string 任务, int 进度)
    {
        if (finishNodeDic.ContainsKey(任务))
        {
            finishNodeDic[任务] = 进度;
        }
        else
        {
            finishNodeDic.Add(任务, 进度);
        }
        if(isNewGame) isNewGame = false; // 只要保存过一次任务进度，就不再是新游戏了
    }
    
    public void 修改属性(int index, string fieldName, int value)
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

    public T 获取数据<T>(string fieldName, int index)
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
        if (finishNodeDic.ContainsKey(t))
        {
            rt = finishNodeDic[t];  
        }
        // try
        // {
        // }
        // catch (IndexOutOfRangeException e)
        // {
        //     Debug.LogError("此任务没有做过或者任务名输入错误"+e);
        // }
        // catch (KeyNotFoundException e)
        // {
        //     Debug.LogError("此任务没有做过或者任务名输入错误"+e);
        // }

        return rt;
    }


    #region 仓库存档
    public int GetItemNum(ItemName itemName)
    {
        var item = GM.Ins.PLAYERPROFILE.itemPacks
            .Find(t => t.itemName == itemName);
        return item != null ? item.itemNum : 0;
    }
    public void CostItem(ItemName itemName, int num)
    {
        var item = GM.Ins.PLAYERPROFILE.itemPacks
            .Find(t => t.itemName == itemName);
        if (item != null)
        {
            item.itemNum -= num;
            if (item.itemNum < 0)
                item.itemNum = 0;
        }
    }
    
    public void AddItem(ItemName itemName, int num)
    {
        Debug.Log($"添加道具: {itemName} 数量: {num}");
        var item = GM.Ins.PLAYERPROFILE.itemPacks
            .Find(t => t.itemName == itemName);
        if (item != null)
        {
            item.itemNum += num;
        }
        else
        {
            GM.Ins.PLAYERPROFILE.itemPacks
                .Add(new ItemPack(itemName, num));
        }
    }
    #endregion 
}