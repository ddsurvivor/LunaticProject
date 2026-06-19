using UnityEngine;

namespace SkillSystem
{
    [System.Serializable]
    public abstract class BasePassiveSkill
    {
        protected PassiveSkillData data;
        [SerializeField]
        protected GameObject owner; // 技能的宿主（谁拥有这个技能）
        protected CharacterSkillManager manager;

        public virtual void Initialize(PassiveSkillData data, GameObject owner)
        {
            this.data = data;
            this.owner = owner;
            this.manager = owner.GetComponent<CharacterSkillManager>();
            OnSkillEquipped();
        }

        // --- 加上 instigator 后的全量钩子 ---
        public virtual void OnSkillEquipped() { }
        public virtual void OnSkillUnequipped() { }
        
        // instigator: 谁的血量变了
        public virtual void OnHpChanged(GameObject instigator, float currentHp, float maxHp) { }
        
        // instigator: 谁完成了击杀
        public virtual void OnKillEnemy(GameObject instigator, GameObject victim) { }
        
        // instigator: 谁发起了检定
        public virtual void OnCheckInitiated(GameObject instigator, string checkType, ref int extraAttempts, ref int valueModifier) { }
        
        // instigator: 谁受到了伤害
        public virtual void OnTakeDamage(GameObject instigator, GameObject attacker) { }
        
        // instigator: 谁在发起攻击
        public virtual void OnBeforeAttack(GameObject instigator, GameObject target, ref float damageMultiplier) { }
        
        // instigator: 谁通过了模式识别
        public virtual void OnPatternRecognitionPassed(GameObject instigator) { }
        
        // instigator: 谁释放了主动技能
        public virtual void OnCastActiveSkill(GameObject instigator) { }
        
        public virtual void OnTurnEnd() { } 
    }
}