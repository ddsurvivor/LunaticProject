using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) {} }
    public static class Debug { public static void Log(object value) {} public static void LogWarning(object value) {} public static void LogError(object value) {} }
    public class TextAsset { public string text; }
    public static class Resources { public static T Load<T>(string value) where T:class => null; }
}
namespace Sirenix.OdinInspector {}
namespace Sirenix.Serialization { public class OdinSerializeAttribute : Attribute {} }
public enum Daytime { 上午 }
public enum AttrOp { Add }
public enum ItemName { 能量包, 医疗单元I型 }
public class ItemPack
{
    public ItemName itemName;
    public int itemNum;
    public ItemPack(ItemName name,int number) { itemName=name;itemNum=number; }
}
public static class GameConst { public static int initialCoins=3000; }
public static class 剧本技能 { public static Dictionary<string,int> 当前技能=new Dictionary<string,int>(); }
public class Player
{
    public string NAME,spriteName;
    public int HP,HPMAX,STAYING,STAYINGMAX,PHYSIQUE,TACTICS,YIZHI,TALK,RECOGNITION,Level,SkillPoints,curHealth,curAmmo,curMana;
    public void AccessAttribute(int index,AttrOp operation,int value) {}
}
public class ComponentData {}
public class ComponentConfig { public ComponentData GetData(int id) => null; }
public class DataManager { public ComponentConfig componentConfig=new ComponentConfig(); }
public class GM { public static GM Ins=new GM();public PLAYERPROFILE PLAYERPROFILE=new PLAYERPROFILE();public DataManager DM=new DataManager(); }
public class BattleUI { public void ShowItemGet(ComponentData data) {} }
public class BattleScene { public static BattleScene Ins;public BattleUI UM=new BattleUI(); }
public class 大地图System { public static 大地图System instance;public 剧本System 剧情; }
public partial class 剧本System
{
    public string[][] 已储存剧本;
    public void Open(string file) { 已储存剧本=new string[2][];RecordReadingStory(file); }
    public void Missing(string file) { 已储存剧本=null;RecordReadingStory(file); }
}
