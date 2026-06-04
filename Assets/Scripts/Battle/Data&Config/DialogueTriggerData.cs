using System;
using UnityEngine;

namespace BattleDialogue
{
    /// <summary>
    /// 触发类型枚举
    /// </summary>
    public enum ETriggerType
    {
        BattleStart,     // 战斗开始
        BattleEnd,       // 战斗结束
        CharacterDeath,  // 关键角色死亡
        Custom           // 自定义/额外扩展调用
    }

    /// <summary>
    /// 纯C#配置数据类（可在Inspector中序列化展现）
    /// </summary>
    [Serializable]
    public class DialogueTriggerData
    {
        [SerializeField] private ETriggerType triggerType;
        [SerializeField] private string characterId; // 仅在角色死亡时使用
        [SerializeField] private string logName;     // 对应的对话剧本名称

        public ETriggerType TriggerType => triggerType;
        public string CharacterId => characterId;
        public string LogName => logName;
    }
}