using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector; // 确保项目导入了 Odin Inspector
#endif

namespace SkillSystem
{
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