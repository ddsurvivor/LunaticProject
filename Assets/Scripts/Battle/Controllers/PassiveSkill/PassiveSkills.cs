using UnityEngine;

namespace SkillSystem
{
    // ==========================================
    // 1. 继承者
    // ==========================================
    public class SkillSuccessor : BasePassiveSkill
    {
        // 逻辑变量本地化定义
        private const float HP_THRESHOLD = 0.3f;       // 触发血线 30%
        private const float ATTRIBUTE_BONUS = 0.2f;    // 属性提升 20%
        
        private bool _isTriggered = false;

        public override void OnHpChanged(GameObject instigator, float currentHp, float maxHp)
        {
            if (instigator != owner) return;

            float hpRatio = currentHp / maxHp;

            if (hpRatio < HP_THRESHOLD && !_isTriggered)
            {
                _isTriggered = true;
                Debug.Log($"[被动触发] {owner.name} 触发【继承者】：自身HP低于 {HP_THRESHOLD:P0}，防御力、攻击力和行动范围提升 {ATTRIBUTE_BONUS:P0}！");
            }
            else if (hpRatio >= HP_THRESHOLD && _isTriggered)
            {
                _isTriggered = false;
                Debug.Log($"[状态解除] {owner.name} 的【继承者】效果因自身血线回升而解除。");
            }
        }
    }

    // ==========================================
    // 2. 鼓舞
    // ==========================================
    public class SkillInspiration : BasePassiveSkill
    {
        private const float TRIGGER_CHANCE = 0.25f;    // 触发概率 25%
        private const int CHARGE_LAYERS = 3;           // 充能层数 3层

        public override void OnKillEnemy(GameObject instigator, GameObject victim)
        {
            if (instigator != owner) return;

            if (Random.value <= TRIGGER_CHANCE)
            {
                Debug.Log($"[被动触发] {owner.name} 击杀了 {victim.name}，成功触发【鼓舞】：为全队施加 {CHARGE_LAYERS} 层 [充能]！");
            }
        }
    }

    // ==========================================
    // 3. 生存智慧
    // ==========================================
    public class SkillSurvivalWisdom : BasePassiveSkill
    {
        private const int EXTRA_ATTEMPTS = 1;          // 额外检定机会 +1
        private const int VALUE_MODIFIER = -2;         // 检定数值减免 -2

        public override void OnCheckInitiated(GameObject instigator, string checkType, ref int extraAttempts, ref int valueModifier)
        {
            if (instigator != owner) return;

            if (checkType == "Communication" || checkType == "Combat")
            {
                extraAttempts += EXTRA_ATTEMPTS;
                valueModifier += VALUE_MODIFIER; // -2点
                Debug.Log($"[被动生效] {owner.name} 的【生存智慧】在自身的 [{checkType}] 检定中生效：获得 {EXTRA_ATTEMPTS} 次额外机会，减免 {-VALUE_MODIFIER} 点检定值。");
            }
        }
    }

    // ==========================================
    // 4. 反射性电子对抗
    // ==========================================
    public class SkillReflectiveECM : BasePassiveSkill
    {
        private const float TRIGGER_CHANCE = 0.20f;    // 触发概率 20%
        private const int OVERLOAD_LAYERS = 2;         // 过载层数 2层

        public override void OnTakeDamage(GameObject instigator, GameObject attacker)
        {
            if (instigator != owner) return;
            if (attacker == null) return;
            
            if (Random.value <= TRIGGER_CHANCE)
            {
                Debug.Log($"[被动触发] {owner.name} 遭受 {attacker.name} 攻击，触发【反射性电子对抗】：反向对攻击者施加 {OVERLOAD_LAYERS} 层 [过载]！");
            }
            else
            {
                Debug.Log($"[被动未触发] 【反射性电子对抗】{owner.name}");  
            }
        }
    }

    // ==========================================
    // 5. 进攻分析
    // ==========================================
    public class SkillOffensiveAnalysis : BasePassiveSkill
    {
        private const float CHECK_CHANCE = 0.20f;       // 攻击时触发模式识别的概率 20%
        private const float DAMAGE_MULTIPLIER = 2.5f;   // 强化伤害倍率 250%
        
        private bool _nextAttackEmpowered = false;

        public override void OnBeforeAttack(GameObject instigator, GameObject target, ref float damageMultiplier)
        {
            if (instigator != owner) return;

            // 1. 应用已被强化的击打
            if (_nextAttackEmpowered)
            {
                damageMultiplier *= DAMAGE_MULTIPLIER;
                _nextAttackEmpowered = false; 
                Debug.Log($"[被动生效] {owner.name} 消耗【进攻分析】加成，本次攻击造成 {DAMAGE_MULTIPLIER:P0} 伤害！");
                return;
            }

            // 2. 概率进行模式识别检定
            if (Random.value <= CHECK_CHANCE)
            {
                Debug.Log($"[被动判定] {owner.name} 攻击时触发【进攻分析】，正在进行自身的 [模式识别检定]...");
                
                bool isCheckPassed = true; // 模拟检定成功
                
                if (isCheckPassed)
                {
                    Debug.Log($"[被动检定成功] {owner.name} 通过了模式识别检定！下一次攻击将被强化。");
                    _nextAttackEmpowered = true;

                    if (manager != null)
                    {
                        manager.NotifyPatternRecognitionPassed(owner);
                    }
                }
            }
        }
    }

    // ==========================================
    // 6. 转账拦截
    // ==========================================
    public class SkillTransferInterception : BasePassiveSkill
    {
        private const float TRIGGER_CHANCE = 0.25f;    // 触发概率 25%
        private const int REWARD_CURRENCY = 100;       // 奖励货币 100

        public override void OnPatternRecognitionPassed(GameObject instigator)
        {
            if (instigator != owner) return;

            if (Random.value <= TRIGGER_CHANCE)
            {
                Debug.Log($"[被动触发] 侦测到宿主 {owner.name} 自身通过了模式识别！【转账拦截】触发：成功拦截并获得 {REWARD_CURRENCY} 标准货币！");
            }
        }
    }

    // ==========================================
    // 7. 边缘求生
    // ==========================================
    public class SkillEdgeSurvival : BasePassiveSkill
    {
        private const int REQUIRED_HIT_COUNT = 6;      // 需要挨打次数 6次
        private const int BUFF_DURATION_TURNS = 2;     // 持续回合数 2回合
        private const int ATTRIBUTE_BONUS = 3;         // 属性提升点数 3点

        private int _hitCounter = 0;
        private int _remainingTurns = 0;
        private bool _isBuffActive = false;

        public override void OnTakeDamage(GameObject instigator, GameObject attacker)
        {
            if (instigator != owner) return;
            if (_isBuffActive) return; 

            _hitCounter++;
            Debug.Log($"[被动计数] {owner.name} 受到攻击，【边缘求生】自身计数器: {_hitCounter}/{REQUIRED_HIT_COUNT}");

            if (_hitCounter >= REQUIRED_HIT_COUNT)
            {
                _hitCounter = 0;
                _isBuffActive = true;
                _remainingTurns = BUFF_DURATION_TURNS;
                
                Debug.Log($"[被动触发] {owner.name} 自身累积遭受 {REQUIRED_HIT_COUNT} 次攻击，触发【边缘求生】：自身体力、意志和作战属性提升 {ATTRIBUTE_BONUS} 点，持续 {BUFF_DURATION_TURNS} 回合！");
            }
        }

        public override void OnTurnEnd()
        {
            if (_isBuffActive)
            {
                _remainingTurns--;
                if (_remainingTurns <= 0)
                {
                    _isBuffActive = false;
                    Debug.Log($"[状态解除] {owner.name} 的【边缘求生】持续回合结束，自身属性恢复正常。");
                }
            }
        }
    }

    // ==========================================
    // 8. 微机械损害管制
    // ==========================================
    public class SkillMicromechanicalDamageControl : BasePassiveSkill
    {
        private const float TRIGGER_CHANCE = 0.20f;    // 触发概率 20%
        private const int HEAL_LAYERS = 3;             // 自动治疗层数 3层

        public override void OnTakeDamage(GameObject instigator, GameObject attacker)
        {
            if (instigator != owner) return;

            if (Random.value <= TRIGGER_CHANCE)
            {
                Debug.Log($"[被动触发] {owner.name} 遭受攻击，触发【微机械损害管制】：为自身施加 {HEAL_LAYERS} 层 [自动治疗]！");
            }
        }
    }

    // ==========================================
    // 9. 维护保障
    // ==========================================
    public class SkillMaintenanceSupport : BasePassiveSkill
    {
        private const float TRIGGER_CHANCE = 0.20f;    // 触发概率 20%
        private const int PROTECT_LAYERS = 3;          // 防护层数 3层

        public override void OnCastActiveSkill(GameObject instigator)
        {
            if (instigator != owner) return;

            if (Random.value <= TRIGGER_CHANCE)
            {
                Debug.Log($"[被动触发] {owner.name} 释放了主动技能，触发【维护保障】：成功为全队施加 {PROTECT_LAYERS} 层 [防护]！");
            }
        }
    }
}