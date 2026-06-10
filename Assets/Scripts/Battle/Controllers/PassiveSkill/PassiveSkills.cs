using UnityEngine;

namespace SkillSystem
{
    // ==========================================
    // 1. 继承者
    // ==========================================
    public class SkillSuccessor : BasePassiveSkill
    {
        private bool _isTriggered = false;

        public override void OnHpChanged(float currentHp, float maxHp)
        {
            float threshold = data.floatParams.Length > 0 ? data.floatParams[0] : 0.3f; // 默认 30%
            float hpRatio = currentHp / maxHp;

            if (hpRatio < threshold && !_isTriggered)
            {
                _isTriggered = true;
                Debug.Log($"[被动触发] {owner.name} 触发【继承者】：HP低于{threshold:P0}，防御力、攻击力和行动范围提升20%！");
            }
            else if (hpRatio >= threshold && _isTriggered)
            {
                _isTriggered = false;
                Debug.Log($"[被动失效] {owner.name} 的【继承者】效果因血线回升而解除。");
            }
        }
    }

    // ==========================================
    // 2. 鼓舞
    // ==========================================
    public class SkillInspiration : BasePassiveSkill
    {
        public override void OnKillEnemy(GameObject victim)
        {
            float chance = data.floatParams.Length > 0 ? data.floatParams[0] : 0.25f; // 默认 25%
            if (Random.value <= chance)
            {
                int layers = data.intParams.Length > 0 ? data.intParams[0] : 3; // 默认 3 层
                Debug.Log($"[被动触发] {owner.name} 击杀了 {victim.name}，成功触发【鼓舞】：全队施加 {layers} 层 [充能]！");
            }
        }
    }

    // ==========================================
    // 3. 生存智慧
    // ==========================================
    public class SkillSurvivalWisdom : BasePassiveSkill
    {
        public override void OnCheckInitiated(string checkType, ref int extraAttempts, ref int valueModifier)
        {
            if (checkType == "Communication" || checkType == "Combat")
            {
                extraAttempts += 1;
                valueModifier -= 2;
                Debug.Log($"[被动生效] {owner.name} 的【生存智慧】在 [{checkType}] 检定中生效：获得1次额外机会，减免2点检定值。");
            }
        }
    }

    // ==========================================
    // 4. 反射性电子对抗
    // ==========================================
    public class SkillReflectiveECM : BasePassiveSkill
    {
        public override void OnTakeDamage(GameObject attacker)
        {
            if (attacker == null) return;
            
            float chance = data.floatParams.Length > 0 ? data.floatParams[0] : 0.20f; // 默认 20%
            if (Random.value <= chance)
            {
                int layers = data.intParams.Length > 0 ? data.intParams[0] : 2; // 默认 2 层
                Debug.Log($"[被动触发] {owner.name} 遭受 {attacker.name} 攻击，触发【反射性电子对抗】：向对方施加 {layers} 层 [过载]！");
            }
        }
    }

    // ==========================================
    // 5. 进攻分析 (核心联动技能)
    // ==========================================
    public class SkillOffensiveAnalysis : BasePassiveSkill
    {
        private bool _nextAttackEmpowered = false;

        public override void OnBeforeAttack(GameObject target, ref float damageMultiplier)
        {
            // 1. 如果上次成功锁定了强化，则在此次攻击应用伤害加成
            if (_nextAttackEmpowered)
            {
                float dmgBonus = data.floatParams.Length > 0 ? data.floatParams[0] : 2.5f; // 默认 250% 伤害
                damageMultiplier *= dmgBonus;
                _nextAttackEmpowered = false; // 消耗掉该次加成
                Debug.Log($"[被动生效] {owner.name} 消耗【进攻分析】加成，本次攻击造成 {dmgBonus:P0} 伤害！");
                return;
            }

            // 2. 常规攻击时，概率触发模式识别检定
            float triggerChance = data.floatParams.Length > 1 ? data.floatParams[1] : 0.20f; // 默认 20%
            if (Random.value <= triggerChance)
            {
                Debug.Log($"[被动判定] {owner.name} 攻击时触发【进攻分析】，正在进行 [模式识别检定]...");
                
                // 模拟检定通过 (这里假设总是通过，或者您可以在此加入骰子逻辑)
                bool isCheckPassed = true; 
                
                if (isCheckPassed)
                {
                    Debug.Log($"[被动检定成功] {owner.name} 通过了模式识别检定！下一次攻击将被强化。");
                    _nextAttackEmpowered = true;

                    // 【核心联动】：通知技能管理器，该角色成功通过了模式识别，以此触发技能 6
                    if (manager != null)
                    {
                        manager.NotifyPatternRecognitionPassed();
                    }
                }
            }
        }
    }

    // ==========================================
    // 6. 转账拦截 (与技能 5 产生联动)
    // ==========================================
    public class SkillTransferInterception : BasePassiveSkill
    {
        public override void OnPatternRecognitionPassed()
        {
            float chance = data.floatParams.Length > 0 ? data.floatParams[0] : 0.25f; // 默认 25%
            if (Random.value <= chance)
            {
                int currencyAmount = data.intParams.Length > 0 ? data.intParams[0] : 100; // 默认 100
                Debug.Log($"[被动触发] 检测到模式识别通过！{owner.name} 触发【转账拦截】：成功拦截并获得 {currencyAmount} 标准货币！");
            }
        }
    }

    // ==========================================
    // 7. 边缘求生
    // ==========================================
    public class SkillEdgeSurvival : BasePassiveSkill
    {
        private int _hitCounter = 0;
        private int _remainingTurns = 0;
        private bool _isBuffActive = false;

        public override void OnTakeDamage(GameObject attacker)
        {
            if (_isBuffActive) return; // 不可叠加

            _hitCounter++;
            Debug.Log($"[被动计数] {owner.name} 的【边缘求生】计数器: {_hitCounter}/6");

            if (_hitCounter >= 6)
            {
                _hitCounter = 0;
                _isBuffActive = true;
                _remainingTurns = data.intParams.Length > 0 ? data.intParams[0] : 2; // 默认 2 回合
                
                int attrBonus = data.intParams.Length > 1 ? data.intParams[1] : 3; // 默认 3 点
                Debug.Log($"[被动触发] {owner.name} 累积遭受6次攻击，触发【边缘求生】：体力、意志和作战属性提升 {attrBonus} 点，持续 {_remainingTurns} 回合！");
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
                    Debug.Log($"[被动失效] {owner.name} 的【边缘求生】持续回合结束，属性恢复正常。");
                }
            }
        }
    }

    // ==========================================
    // 8. 微机械损害管制
    // ==========================================
    public class SkillMicromechanicalDamageControl : BasePassiveSkill
    {
        public override void OnTakeDamage(GameObject attacker)
        {
            float chance = data.floatParams.Length > 0 ? data.floatParams[0] : 0.20f; // 默认 20%
            if (Random.value <= chance)
            {
                int layers = data.intParams.Length > 0 ? data.intParams[0] : 3; // 默认 3 层
                Debug.Log($"[被动触发] {owner.name} 遭受攻击，触发【微机械损害管制】：为自身施加 {layers} 层 [自动治疗]！");
            }
        }
    }

    // ==========================================
    // 9. 维护保障
    // ==========================================
    public class SkillMaintenanceSupport : BasePassiveSkill
    {
        public override void OnCastActiveSkill()
        {
            float chance = data.floatParams.Length > 0 ? data.floatParams[0] : 0.20f; // 默认 20%
            if (Random.value <= chance)
            {
                int layers = data.intParams.Length > 0 ? data.intParams[0] : 3; // 默认 3 层
                Debug.Log($"[被动触发] {owner.name} 释放了技能，触发【维护保障】：为全队施加 {layers} 层 [防护]！");
            }
        }
    }
}