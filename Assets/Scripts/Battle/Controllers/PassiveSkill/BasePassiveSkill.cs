using UnityEngine;

namespace SkillSystem
{
    public abstract class BasePassiveSkill
    {
        protected PassiveSkillData data;
        protected GameObject owner;
        protected CharacterSkillManager manager;

        public virtual void Initialize(PassiveSkillData data, GameObject owner)
        {
            this.data = data;
            this.owner = owner;
            this.manager = owner.GetComponent<CharacterSkillManager>();
            OnSkillEquipped();
        }

        // --- 全量事件钩子 ---
        public virtual void OnSkillEquipped() { }
        public virtual void OnSkillUnequipped() { }
        public virtual void OnHpChanged(float currentHp, float maxHp) { }
        public virtual void OnKillEnemy(GameObject victim) { }
        
        // 检定钩子：传入检定类型，通过 ref 修改额外机会和修正值
        public virtual void OnCheckInitiated(string checkType, ref int extraAttempts, ref int valueModifier) { }
        public virtual void OnTakeDamage(GameObject attacker) { }
        
        // 攻击前置钩子：通过 ref 动态修改最终伤害倍率
        public virtual void OnBeforeAttack(GameObject target, ref float damageMultiplier) { }
        
        // 特定机制钩子：当模式识别检定通过时触发
        public virtual void OnPatternRecognitionPassed() { }
        public virtual void OnCastActiveSkill() { }
        public virtual void OnTurnEnd() { } // 用于处理回合倒计时
    }
}