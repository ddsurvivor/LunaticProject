using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using XLua;

/// <summary>
/// 暂时弃用
/// </summary>
[LuaCallCSharp]     
public class 背包系统 : MonoBehaviour
{
    public static 背包系统 Instance;
    public static Dictionary<string, int> 当前背包  = new Dictionary<string, int>();
    public static Dictionary<string, int> 准备使用道具 = new Dictionary<string, int>();
    public static List<道具> 道具列表 = new List<道具>();
    [LuaCallCSharp]
    public struct 道具
    {
        public string Name;//技术名词
        public string[] Title;//各语言显示名称
        public string[] Info;//各语言介绍
        public int Time;//回合数
        public string[] Check;//可使用阶段
    }
    private LuaEnv luaEnv;
    private void Awake()
    {
        Instance=this;
        luaEnv=new LuaEnv();
        luaEnv.AddLoader((ref string moduleName) =>
        {
            //把点号换成路径，如  Item.Item  ->  Item/Item
            string relativePath = moduleName.Replace('.', '/');
            string absPath = Path.Combine(Application.streamingAssetsPath,
                relativePath + ".lua.txt");

            if (File.Exists(absPath))
                return File.ReadAllBytes(absPath);
            return null;
        });
        luaEnv.DoString("require 'Item.Item'");
        luaEnv.Global.Get<LuaFunction>("InitItemList")?.Call();
    }

   
    [LuaCallCSharp]
    public static void AddItem(string name, string[] title, string[] info, int time, string[] check)
    {
        道具 it=new 道具();
        it.Name = name;
        it.Title = title;
        it.Info  = info;
        it.Time  = time;
        it.Check = check;

        道具列表.Add(it);
    }
    public static void 背包添加(string 道具名, int 数量 = 1)
    {
        if (数量 <= 0) return;

        if (当前背包.ContainsKey(道具名))
            当前背包[道具名] += 数量;
        else
            当前背包[道具名] = 数量;
    }

    public static void 背包减少(string 道具名, int 数量 = 1)
    {
        if (数量 <= 0) return;

        if (当前背包.TryGetValue(道具名, out int value))
        {
            value -= 数量;
            if (value > 0)
                当前背包[道具名] = value;
            else
                当前背包.Remove(道具名);
        }
    }
    public static void 背包全部减少()
    {
        List<string> keysToRemove = new List<string>();
        foreach (var kvp in 当前背包)
        {
            int newValue = kvp.Value - 1;
            if (newValue > 0)
                当前背包[kvp.Key] = newValue;
            else
                keysToRemove.Add(kvp.Key);
        }
        foreach (string key in keysToRemove)
            当前背包.Remove(key);
    }
    
    public static int 获取数量(string 道具名)
    {
        return 当前背包.TryGetValue(道具名, out int num) ? num : 0;
    }
    public static bool 使用道具(string 道具名)
    {
       int 回合数 = Get道具ByName(道具名).Time;
       Debug.Log($"{道具名}的回合数是{回合数},name是{ Get道具ByName(道具名).Name}");
        int cur = 获取数量(道具名);
        if (cur < 1)
        {
            Debug.LogWarning($"背包中 [{道具名}] 数量不足，无法消耗！");
            return false;
        }
        背包减少(道具名, 1);
        // 时间戳
        string timeSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        string keyWithTime = $"{道具名}_{timeSuffix}";
        
        准备使用道具[keyWithTime] = 回合数;

        return true;
    }
    public static 道具 Get道具ByName(string targetName)
    {
        return 道具列表.FirstOrDefault(p => p.Name == targetName);
    }
    
    public static void 道具倒数(string t)
    {
        if (string.IsNullOrEmpty(t)) return;

        var keys = 准备使用道具.Keys.Where(k => k.Contains(t)).ToList();
        foreach (var key in keys)
        {
            int newValue = 准备使用道具[key] - 1;
            if (newValue > 0)
                准备使用道具[key] = newValue;
            else
                准备使用道具.Remove(key);
        }
    }
    public static int 检查已使用道具(string t)
    {
        if (string.IsNullOrEmpty(t)) return 0;

        int cnt = 0;
        foreach (var key in 准备使用道具.Keys)
        {
            if (key.Contains(t))
                cnt++;
        }
        return cnt;
    }
    [ContextMenu("TEST_添加10测试道具")]
    void TEST_ADD()
    {
        背包添加("Test", 10);
        Debug.Log($"Test数量 = {获取数量("Test")}");
    }

    [ContextMenu("TEST_使用")]
    void TEST_USE()
    {
        使用道具("Test");
        Debug.Log("准备使用道具表：" + DictionaryToString(准备使用道具));
    }

    [ContextMenu("TEST_倒数一次")]
    void TEST_COUNTDOWN()
    {
        道具倒数("Test");
        Debug.Log("倒数后表：" + DictionaryToString(准备使用道具));
    }
    
    private string DictionaryToString(Dictionary<string,int> dict)
    {
        string s = "";
        foreach(var kv in dict)
            s += $"{kv.Key}:{kv.Value} ";
        return s;
    }
}
