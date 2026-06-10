using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector; // 确保项目导入了 Odin Inspector
#endif

namespace SkillSystem
{
    /// <summary>
    /// 专长技能类型枚举
    /// </summary>
    public enum PassiveSkillType
    {
        [LabelText("1. 继承者")] Successor
        , [LabelText("2. 鼓舞")] Inspiration
        , [LabelText("3. 生存智慧")] SurvivalWisdom
        , [LabelText("4. 反射性电子对抗")] ReflectiveECM
        , [LabelText("5. 进攻分析")] OffensiveAnalysis
        , [LabelText("6. 转账拦截")] TransferInterception
        , [LabelText("7. 边缘求生")] EdgeSurvival
        , [LabelText("8. 微机械损害管制")] MicromechanicalDamageControl
        , [LabelText("9. 维护保障")] MaintenanceSupport
    }

    /// <summary>
    /// 纯 C# 数据类：被动技能基础配置数据
    /// </summary>
    [System.Serializable]
    public class PassiveSkillData
    {

        [Header("技能类型 (代替原ID)")]
        public PassiveSkillType skillType;

        [Header("技能基础信息")] public string skillName;

        [TextArea(2, 4)] public string description;
        public Sprite skillIcon;

        [Header("弹性数值参数配置")] public float[] floatParams;
        public int[] intParams;
    }


    /// <summary>
    /// 全局唯一的被动技能配置总表
    /// </summary>
    [CreateAssetMenu(fileName = "PassiveSkillConfig"
        , menuName = "Skill System/Passive Skill Config")]
    public class PassiveSkillConfigSO : ScriptableObject
    {
        [SerializeField] private List<PassiveSkillData> allSkills = new List<PassiveSkillData>();

        // 运行时缓存字典，键改为枚举类型
        private Dictionary<PassiveSkillType, PassiveSkillData> _skillCache;

        public void InitializeCache()
        {
            if (_skillCache != null) return;

            _skillCache = new Dictionary<PassiveSkillType, PassiveSkillData>();
            foreach (var skill in allSkills)
            {
                if (!_skillCache.ContainsKey(skill.skillType))
                {
                    _skillCache.Add(skill.skillType, skill);
                }
                else
                {
                    Debug.LogWarning($"[PassiveSkillConfigSO] 发现了重复配置的技能类型: {skill.skillType}");
                }
            }
        }

        /// <summary>
        /// 根据枚举类型快速获取技能数据
        /// </summary>
        public PassiveSkillData GetSkillData(PassiveSkillType type)
        {
            InitializeCache();
            if (_skillCache.TryGetValue(type, out var data))
            {
                return data;
            }

            Debug.LogError($"[PassiveSkillConfigSO] 技能总表中未找到类型为 【{type}】 的配置！");
            return null;
        }
    }
}