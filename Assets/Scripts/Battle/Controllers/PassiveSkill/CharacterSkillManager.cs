using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    public class CharacterSkillManager : MonoBehaviour
    {
        [SerializeField] private PassiveSkillConfigSO skillConfigSO;
        [SerializeField] private List<PassiveSkillType> equippedSkills = new List<PassiveSkillType>();

        private List<BasePassiveSkill> runtimeSkills = new List<BasePassiveSkill>();

        private void Start() { InitSkills(); }

        private void InitSkills()
        {
            if (skillConfigSO == null) return;
            foreach (PassiveSkillType type in equippedSkills)
            {
                PassiveSkillData data = skillConfigSO.GetSkillData(type);
                if (data == null) continue;

                string className = "SkillSystem.Skill" + type.ToString();
                System.Type classType = System.Type.GetType(className);
                if (classType != null)
                {
                    BasePassiveSkill skillInstance = System.Activator.CreateInstance(classType) as BasePassiveSkill;
                    skillInstance.Initialize(data, gameObject); 
                    runtimeSkills.Add(skillInstance);
                }
            }
        }

        // ==========================================
        // 外部战斗/生存系统的业务调用接口 (通知钩子)
        // ==========================================

        public void NotifyHpChanged(float current, float max)
        {
            foreach (var skill in runtimeSkills) skill.OnHpChanged(current, max);
        }

        public void NotifyKillEnemy(GameObject victim)
        {
            foreach (var skill in runtimeSkills) skill.OnKillEnemy(victim);
        }

        public void NotifyTakeDamage(GameObject attacker)
        {
            foreach (var skill in runtimeSkills) skill.OnTakeDamage(attacker);
        }

        public void NotifyCastActiveSkill()
        {
            foreach (var skill in runtimeSkills) skill.OnCastActiveSkill();
        }

        public void NotifyTurnEnd()
        {
            foreach (var skill in runtimeSkills) skill.OnTurnEnd();
        }

        /// <summary>
        /// 外部检定系统调用：传入初始检定参数，返回被动修正后的参数
        /// </summary>
        public void EvaluateCheckSystem(string checkType, ref int extraAttempts, ref int valueModifier)
        {
            foreach (var skill in runtimeSkills) 
                skill.OnCheckInitiated(checkType, ref extraAttempts, ref valueModifier);
        }

        /// <summary>
        /// 外部攻击系统调用：计算最终伤害加成倍率
        /// </summary>
        public float EvaluateDamageMultiplier(GameObject target)
        {
            float multiplier = 1.0f;
            foreach (var skill in runtimeSkills) 
                skill.OnBeforeAttack(target, ref multiplier);
            return multiplier;
        }

        /// <summary>
        /// 专门供【5. 进攻分析】内部检定成功时反向调用，从而驱动【6. 转账拦截】
        /// </summary>
        public void NotifyPatternRecognitionPassed()
        {
            foreach (var skill in runtimeSkills) 
                skill.OnPatternRecognitionPassed();
        }
    }
}