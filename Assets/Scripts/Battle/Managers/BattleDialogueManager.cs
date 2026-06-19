using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 对应规则1e：使用旧版Text组件

namespace BattleDialogue
{
    public class BattleDialogueManager : MonoBehaviour
    {
        [Header("--- 当前场景特有对话配置 ---")]
        [SerializeField] private List<DialogueTriggerData> triggerList = new List<DialogueTriggerData>();
        
        //[Header("--- UI 引用 ---")]
        //[SerializeField] private Text dialogueDebugText; 

        // 缓存当前正在执行的对话结束回调
        private Action currentDialogueCompleteCallback;

        /// <summary>
        /// 核心触发接口：开始播放对话剧本
        /// </summary>
        /// <param name="logName">对话剧本名称</param>
        /// <param name="onComplete">对话完全结束时触发的回调</param>
        public void StartLog(string logName, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(logName))
            {
                Debug.LogWarning("[DialogueManager] 触发的 logName 为空，直接跳过对话并执行后续逻辑。");
                onComplete?.Invoke();
                return;
            }

            // 1. 缓存回调函数
            currentDialogueCompleteCallback = onComplete;

            // 2. 触发具体的表现层逻辑
            Debug.Log($"[DialogueManager] 正在播放本场景对话: <b>{logName}</b>");
            BattleScene.Ins.UM.StartLog(logName);
        }

        /// <summary>
        /// 关键回调接口：当UI面板完全关闭、玩家点完最后一句时，由外部UI控制脚本调用此方法
        /// </summary>
        public void OnDialogueFinished()
        {
            Debug.Log("[DialogueManager] 场景对话播放完毕，释放阻塞，继续后续逻辑。");

            Action callback = currentDialogueCompleteCallback;
            currentDialogueCompleteCallback = null;

            // 触发回调，推动外部串行逻辑（例如重置棋子状态）
            callback?.Invoke();
        }

        #region 通用事件快捷调用接口

        public void TriggerBattleStart(Action onComplete = null)
        {
            string logName = GetLogName(ETriggerType.BattleStart);
            StartLog(logName, onComplete);
        }

        public void TriggerBattleEnd(Action onComplete = null)
        {
            string logName = GetLogName(ETriggerType.BattleEnd);
            StartLog(logName, onComplete);
        }

        public void TriggerCharacterDeath(string characterId, Action onComplete = null)
        {
            string logName = GetLogName(ETriggerType.CharacterDeath, characterId);
            StartLog(logName, onComplete);
        }
        
        public void TriggerTurnNumStart(int turnNum, Action onComplete = null)
        {
            // 目前示例中未实现基于回合数的触发逻辑，但接口预留了turnNum参数以供未来扩展
            string logName = GetLogName(ETriggerType.TurnNumStart, turnNum);
            StartLog(logName, onComplete);
        }

        private bool hasBursted = false; // 示例中简单使用一个布尔值来模拟聚能状态，实际项目中可能需要更复杂的状态管理
        public void TriggerBurstReady(Action onComplete = null)
        {
            if (hasBursted)
            {
                return;
            }
            else
            {
                hasBursted = true;
            }
            string logName = GetLogName(ETriggerType.BustReady);
            StartLog(logName, onComplete);
        }

        #endregion

        #region 内部数据检索

        private string GetLogName(ETriggerType type, string characterId = "")
        {
            // 直接在本本地列表 (triggerList) 中进行快捷搜寻
            var target = triggerList.Find(data => 
                data.TriggerType == type && 
                (type != ETriggerType.CharacterDeath || data.CharacterId == characterId)
            );

            return target != null ? target.LogName : string.Empty;
        }
        private string GetLogName(ETriggerType type, int turnNum)
        {
            // 直接在本本地列表 (triggerList) 中进行快捷搜寻
            var target = triggerList.Find(data => 
                data.TriggerType == type && 
                (type != ETriggerType.TurnNumStart || data.turnNum == turnNum)
            );

            return target != null ? target.LogName : string.Empty;
        }

        #endregion
    }
}