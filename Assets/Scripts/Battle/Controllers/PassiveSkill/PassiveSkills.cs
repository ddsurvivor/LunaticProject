using UnityEngine;

namespace SkillSystem
{
    // ==========================================
    // 1. 继承者
    // ==========================================
    public class SkillSuccessor : BasePassiveSkill
    {
        private const int HP_THRESHOLD_PERCENT = 40; // 严格低于40%时生效，等于门槛不生效。
        private const float ATTRIBUTE_BONUS = 0.2f; // 按触发时的属性快照增加20%。
        private bool active;
        private int attackBonus; // 保存实际加值，退出时不按已变化的属性重新计算。
        private float moveRangeBonus;
        private readonly int[] armorBonuses = new int[3]; // 动能、热能、火种护甲各自持有加值。
        private UnitAttrCenter bonusUnit;

        /// <summary>随宿主生命与上限变化切换低血量增益；死亡、恢复至40%或卸载时撤销。</summary>
        public override void OnHpChanged(GameObject instigator, float currentHp, float maxHp)
        {
            if (owner == null || instigator != owner) return;
            if (maxHp <= 0) { RemoveBonus(); return; }
            // 交叉乘法避免Mono浮点除法精度把恰好40%判成低于门槛。
            bool shouldActivate = currentHp > 0 && (double)currentHp * 100 < (double)maxHp * HP_THRESHOLD_PERCENT;
            if (!shouldActivate) { RemoveBonus(); return; }
            if (active) return;
            var unit = owner.GetComponent<UnitAttrCenter>();
            if (unit == null) return;
            active = true;
            bonusUnit = unit;
            // 保存实际加值，撤销时不按已经变化的攻击力重新计算。
            attackBonus = Mathf.RoundToInt(unit.ATK * ATTRIBUTE_BONUS);
            moveRangeBonus = Mathf.Max(0f, unit.BaseMoveRange) * ATTRIBUTE_BONUS;
            for (int i = 0; i < armorBonuses.Length; i++)
            {
                var type = (DamageType)(i + 1);
                armorBonuses[i] = Mathf.RoundToInt(unit.attr.GetArmor(type) * ATTRIBUTE_BONUS);
                unit.attr.SetArmor(type, unit.attr.GetArmor(type) + armorBonuses[i]);
            }
            unit.ModifyAttribute(UnitAttrType.ATK, attackBonus);
            unit.ModifyAttribute(UnitAttrType.MoveRange, moveRangeBonus);
            BattleScene.Ins.BM.tipTextManager.ShowTip(owner.transform, data.skillName);
            ObjectPool.Ins.GenerateObject(ItemType.SPECIALTY_ACTIVATE, owner.transform.position, Quaternion.identity);
        }

        private void RemoveBonus()
        {
            if (!active) return;
            active = false;
            var unit = bonusUnit;
            bonusUnit = null;
            if (unit == null) return;
            for (int i = 0; i < armorBonuses.Length; i++)
            {
                var type = (DamageType)(i + 1);
                unit.attr.SetArmor(type, unit.attr.GetArmor(type) - armorBonuses[i]);
                armorBonuses[i] = 0;
            }
            unit.ModifyAttribute(UnitAttrType.ATK, -attackBonus);
            unit.ModifyAttribute(UnitAttrType.MoveRange, -moveRangeBonus);
            attackBonus = 0;
            moveRangeBonus = 0;
        }

        public override void OnSkillUnequipped() => RemoveBonus();
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

            if (BattleRandom.Value <= TRIGGER_CHANCE)
            {
                BattleScene.Ins.BM.tipTextManager.ShowTip(instigator.transform
                    , $"{data.skillName}");
                ObjectPool.Ins.GenerateObject(ItemType.SPECIALTY_ACTIVATE, instigator.transform.position, Quaternion.identity);
                PlayerController player = instigator.GetComponent<PieceController>().player;

                foreach (var piece in player.pieces)
                {           
                    BattleScene.Ins.BM.buffManager.AddBuff(piece.unitAttrCenter, BuffType.Charge, CHARGE_LAYERS);
                }
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
        private const float TRIGGER_CHANCE = 0.90f;    // 保留现有 90% 配置
        private const int OVERLOAD_LAYERS = 2;         // 过载层数 2层

        public override void OnTakeDamage(GameObject instigator, GameObject attacker)
        {
            if (instigator != owner) return;
            if (attacker == null) return;
            
            UnitAttrCenter targetUnitAttrCenter = attacker.GetComponent<UnitAttrCenter>();
            if (BattleRandom.Value <= TRIGGER_CHANCE)
            {
                BattleScene.Ins.BM.tipTextManager.ShowTip(instigator.transform
                    , $"{data.skillName}");
                ObjectPool.Ins.GenerateObject(ItemType.SPECIALTY_ACTIVATE, instigator.transform.position, Quaternion.identity);
                Debug.Log($"[被动触发] {owner.name} 遭受 {attacker.name} 攻击，触发【反射性电子对抗】：反向对攻击者施加 {OVERLOAD_LAYERS} 层 [过载]！");
                BattleScene.Ins.BM.buffManager.AddBuff(targetUnitAttrCenter, BuffType.Overload
                    , OVERLOAD_LAYERS);
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

        public override float PreviewDamageMultiplier(GameObject target) => _nextAttackEmpowered ? DAMAGE_MULTIPLIER : 1f;

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
            if (BattleRandom.Value <= CHECK_CHANCE)
            {
                Debug.Log($"[被动判定] {owner.name} 攻击时触发【进攻分析】，正在进行自身的 [模式识别检定]...");
                
                var caster = owner.GetComponent<PieceController>();
                var defender = target != null ? target.GetComponent<PieceController>() : null;
                bool isCheckPassed = caster != null && defender != null &&
                    BattleScene.Ins.BM.diceCheckManager.ModeRecognitionCheck(caster, defender) is
                        CheckResult.DamageIncreased or CheckResult.MustCrit;
                
                if (isCheckPassed)
                {
                    Debug.Log($"[被动检定成功] {owner.name} 通过了模式识别检定！下一次攻击将被强化。");
                    _nextAttackEmpowered = true;


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

            if (BattleRandom.Value <= TRIGGER_CHANCE)
            {
                var battle = BattleScene.Ins != null ? BattleScene.Ins.BM : null;
                // 收益先留在本次尝试，沿用胜利离场提交与重开丢弃的规则。
                if (battle != null && battle.TryAddBattleCurrencyReward(owner.GetComponent<PieceController>(), REWARD_CURRENCY))
                    Debug.Log($"[被动触发] {owner.name}【转账拦截】：获得 {REWARD_CURRENCY} 标准货币，胜利离开战斗时结算。");
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

        private int _hitCounter = 0; // 增益生效期间不累计下一轮受击。
        private int _remainingTurns = 0; // 仅由宿主普通回合结束递减。
        private bool _isBuffActive = false;
        private PieceController bonusOwner;
        private Player bonusPlayer; // 卸载时撤销原持有者，避免复用棋子后误改新档案。

        public override void OnTakeDamage(GameObject instigator, GameObject attacker)
        {
            if (owner == null || instigator != owner) return;
            var piece = owner.GetComponent<PieceController>();
            if (piece == null || piece.playerData == null || piece.isDead || piece.unitAttrCenter.CurHealth <= 0) return;
            if (_isBuffActive) return; 

            _hitCounter++;
            Debug.Log($"[被动计数] {owner.name} 受到攻击，【边缘求生】自身计数器: {_hitCounter}/{REQUIRED_HIT_COUNT}");

            if (_hitCounter >= REQUIRED_HIT_COUNT)
            {
                _hitCounter = 0;
                _isBuffActive = true;
                _remainingTurns = BUFF_DURATION_TURNS;
                bonusOwner = piece;
                bonusPlayer = piece.playerData;
                // 只叠加运行时属性，刷新使用既有换算与资源裁剪，不改存档基础值。
                bonusPlayer.SetTemporaryAttributeBonuses(this, new[] { ATTRIBUTE_BONUS, ATTRIBUTE_BONUS, ATTRIBUTE_BONUS, 0, 0 });
                bonusOwner.RefreshPlayerAttributeBonuses();
                BattleScene.Ins.BM.tipTextManager.ShowTip(instigator.transform
                    , $"{data.skillName}");
                ObjectPool.Ins.GenerateObject(ItemType.SPECIALTY_ACTIVATE, instigator.transform.position, Quaternion.identity);
                
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
                    RemoveBonus();
                    Debug.Log($"[状态解除] {owner.name} 的【边缘求生】持续回合结束，自身属性恢复正常。");
                }
            }
        }

        public override void OnHpChanged(GameObject instigator, float currentHp, float maxHp)
        {
            if (instigator == owner && currentHp <= 0) OnSkillUnequipped();
        }

        private void RemoveBonus()
        {
            if (!_isBuffActive) return;
            _isBuffActive = false;
            _remainingTurns = 0;
            bonusPlayer?.RemoveTemporaryAttributeBonuses(this);
            if (bonusOwner != null && ReferenceEquals(bonusOwner.playerData, bonusPlayer))
                bonusOwner.RefreshPlayerAttributeBonuses();
            bonusOwner = null;
            bonusPlayer = null;
        }

        public override void OnSkillUnequipped()
        {
            _hitCounter = 0;
            RemoveBonus();
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

            if (BattleRandom.Value <= TRIGGER_CHANCE)
            {
                BattleScene.Ins.BM.tipTextManager.ShowTip(instigator.transform
                    , $"{data.skillName}");
                UnitAttrCenter targetUnitAttrCenter = instigator.GetComponent<UnitAttrCenter>();
                BattleScene.Ins.BM.buffManager.AddBuff(targetUnitAttrCenter, BuffType.AutoHeal
                    , HEAL_LAYERS);
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

        /// <summary>由正式施法成功通知调用，按当前支援关系为有效参战友方概率施加防护。</summary>
        public override void OnCastActiveSkill(GameObject instigator)
        {
            if (owner == null || instigator != owner) return;
            var battle = BattleScene.Ins != null ? BattleScene.Ins.BM : null;
            var caster = owner.GetComponent<PieceController>();
            if (battle == null || !battle.IsBattleRunning || battle.LastOutcome != BattleOutcome.None || caster == null) return;
            // 沿用参战资格和当前支援关系，目标快照去重，排除尸体、退场及尚未激活的单位。
            var targets = battle.SpeedParticipants().FindAll(piece => BattleRelations.CanSupport(caster, piece));
            if (!targets.Contains(caster)) return;

            if (BattleRandom.Value <= TRIGGER_CHANCE)
            {
                foreach (var piece in targets)
                    battle.buffManager.AddBuff(piece.unitAttrCenter, BuffType.Shield, PROTECT_LAYERS);
                battle.tipTextManager.ShowTip(owner.transform, data.skillName);
                Debug.Log($"[被动触发] {owner.name} 释放了主动技能，触发【维护保障】：成功为全队施加 {PROTECT_LAYERS} 层 [防护]！");
            }
        }
    }
}
