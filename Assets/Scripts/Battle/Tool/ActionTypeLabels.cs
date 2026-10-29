using System;
using Newtonsoft.Json;

/// <summary>行动枚举的显示名称与旧中文存档兼容入口；整数值始终作为存档输出。</summary>
public static class ActionTypeLabels
{
    public static bool English => GM.Ins != null && GM.Ins.DM != null && GM.Ins.DM.settingsData != null
        ? GM.Ins.DM.settingsData.languageIndex == 1 : Center.Language == "EN";

}

public static partial class EnumExtensions
{
    /// <summary>返回与 Odin 标签一致的中文行动名称，不依赖运行时语言设置。</summary>
    public static string ToChinese(this ActionType type)
    {
        switch (type)
        {
            case ActionType.Move: return "移动";
            case ActionType.MeleeAttack: return "近战攻击";
            case ActionType.RangedAttack: return "远程攻击";
            case ActionType.Reload: return "重新装填";
            case ActionType.Scan: return "扫描";
            case ActionType.ReleaseBind: return "解除束缚";
            case ActionType.Climb: return "攀爬";
            case ActionType.Skill: return "技能";
            case ActionType.Item: return "道具";
            case ActionType.Interact: return "交互";
            case ActionType.OrbitalStrike: return "轨道轰炸";
            case ActionType.Talk: return "对话";
            case ActionType.GuardOrder: return "警戒指令";
            case ActionType.Wait: return "待机";
            case ActionType.UseItem: return "使用道具";
            default: return type.ToString();
        }
    }

    /// <summary>返回适合界面展示的英文词组，避免直接暴露复合枚举标识。</summary>
    public static string ToEnglish(this ActionType type)
    {
        switch (type)
        {
            case ActionType.MeleeAttack: return "Melee Attack";
            case ActionType.RangedAttack: return "Ranged Attack";
            case ActionType.ReleaseBind: return "Release Bind";
            case ActionType.OrbitalStrike: return "Orbital Strike";
            case ActionType.GuardOrder: return "Guard Order";
            case ActionType.UseItem: return "Use Item";
            default: return type.ToString();
        }
    }

    /// <summary>按当前游戏语言显示行动名称；配置与历史存档保持独立。</summary>
    public static string ToDisplayName(this ActionType type) => ActionTypeLabels.English ? type.ToEnglish() : type.ToChinese();
}

public sealed class ActionTypeJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(ActionType);
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue((int)(ActionType)value);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer) return (ActionType)Convert.ToInt32(reader.Value);
        if (reader.TokenType == JsonToken.String)
        {
            string text = (string)reader.Value;
            if (Enum.TryParse<ActionType>(text, true, out var parsed)) return parsed;
            foreach (ActionType type in Enum.GetValues(typeof(ActionType)))
                if (type.ToChinese() == text) return type;
        }
        throw new JsonSerializationException("Invalid ActionType: " + reader.Value);
    }
}
