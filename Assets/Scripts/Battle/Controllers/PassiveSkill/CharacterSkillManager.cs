using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SkillSystem
{
    /// <summary>
    /// 全局被动技能裁判/管理器（统一管理场上所有棋子的被动技能）
    /// </summary>
    public class CharacterSkillManager : SerializedMonoBehaviour
    {
        // 采用单例模式，方便棋子或者战斗系统随时调用
        //public static CharacterSkillManager Instance { get; private set; }

        [Header("配置文件")]
        [SerializeField] private PassiveSkillConfigSO skillConfigSO;

        [OdinSerialize]
        // 【核心数据结构】Key: 棋子的 GameObject, Value: 该棋子当前运行时的被动技能实例列表
        private Dictionary<GameObject, List<BasePassiveSkill>> _runtimeRegistry = new Dictionary<GameObject, List<BasePassiveSkill>>();

        /// <summary>
        /// 主动触发：洗牌并初始化全场所有棋子的技能绑定
        /// </summary>
        /// <param name="piecesOnField">当前战场上存在的全量棋子 GameObject 列表</param>
        public void Init(List<PieceController> piecesOnField)
        {
            if (skillConfigSO == null)
            {
                Debug.LogError("[技能系统] 初始化失败！未配置 PassiveSkillConfigSO。");
                return;
            }

            // 1. 清理上一局或旧状态的残留数据
            //ClearAllRegistry();
            Debug.Log($"[技能系统] 开始主动触发全场技能绑定，当前共计 {piecesOnField.Count} 个棋子。");

            // 2. 主动遍历传入的棋子，提取技能并注册
            foreach (PieceController piece in piecesOnField)
            {
                if (piece == null) continue;
                
                // 尝试获取棋子身上挂载的技能数据组件（组件定义见下方第2小节）
                //ChessPiece pieceComponent = piece.GetComponent<ChessPiece>();
                
                // 主动调用注册，将棋子与对应枚举列表配对
                RegisterPiece(piece.gameObject, piece.pieceData.passiveSkillTypes);
                Debug.Log($"[技能系统] 已注册棋子 {piece.name} 的被动技能列表：{string.Join(", ", piece.pieceData.passiveSkillTypes)}");
            }
        }

        // ========================================================
        // 🧬 棋子注册与动态配对接口
        // ========================================================

        /// <summary>
        /// 当棋子在场上生成、或者对局开始时，将棋子及其被动技能列表注册到管理器中
        /// </summary>
        /// <param name="piece">棋子的 GameObject</param>
        /// <param name="skillTypes">该棋子拥有的被动技能枚举列表</param>
        public void RegisterPiece(GameObject piece, List<PassiveSkillType> skillTypes)
        {
            if (piece == null) return;
            if (skillConfigSO == null)
            {
                Debug.LogError("[技能系统] 未配置 PassiveSkillConfigSO，无法注册棋子技能！");
                return;
            }

            // 如果该棋子已经注册过，先清理，防止重复注册
            if (_runtimeRegistry.ContainsKey(piece))
            {
                UnregisterPiece(piece);
            }

            List<BasePassiveSkill> pieceRuntimeSkills = new List<BasePassiveSkill>();

            foreach (PassiveSkillType type in skillTypes)
            {
                PassiveSkillData data = skillConfigSO.GetSkillData(type);
                if (data == null) continue;

                string className = "SkillSystem.Skill" + type.ToString();
                System.Type classType = System.Type.GetType(className);
                
                if (classType != null)
                {
                    // 动态实例化技能逻辑类
                    BasePassiveSkill skillInstance = System.Activator.CreateInstance(classType) as BasePassiveSkill;
                    // 初始化技能，建立 技能 -> 棋子 的双向绑定
                    skillInstance.Initialize(data, piece); 
                    pieceRuntimeSkills.Add(skillInstance);
                }
                else
                {
                    Debug.LogWarning($"[技能系统] 未找到对应的技能逻辑脚本类: {className}");
                }
            }

            // 将棋子和其技能列表配对存入运行时字典
            _runtimeRegistry.Add(piece, pieceRuntimeSkills);
            Debug.Log($"[技能系统] 棋子 【{piece.name}】 成功注册了 {pieceRuntimeSkills.Count} 个被动技能。");
        }

        /// <summary>
        /// 当棋子死亡、退场或被销毁时，必须调用此接口移除，防止内存泄漏（Target Exception）
        /// </summary>
        public void UnregisterPiece(GameObject piece)
        {
            if (piece == null) return;

            if (_runtimeRegistry.TryGetValue(piece, out var skills))
            {
                foreach (var skill in skills)
                {
                    skill.OnSkillUnequipped(); // 触发卸载逻辑
                }
                _runtimeRegistry.Remove(piece);
                Debug.Log($"[技能系统] 棋子 【{piece.name}】 已从全局技能管理器中注销。");
            }
        }

        // ========================================================
        // ⚔️ 精准业务通知接口（利用 Dictionary 瞬间定位棋子，杜绝串味）
        // ========================================================

        public void NotifyHpChanged(GameObject instigator, float current, float max)
        {
            // 通过 instigator 瞬间找到这个棋子自己的技能，绝不影响别人
            if (_runtimeRegistry.TryGetValue(instigator, out var skills))
            {
                //Debug.Log($"[技能系统] 通知棋子 【{instigator.name}】 的技能：HP 变化了！当前 HP: {current}/{max}");
                foreach (var skill in skills) skill.OnHpChanged(instigator, current, max);
            }
        }

        public void NotifyKillEnemy(GameObject instigator, GameObject victim)
        {
            if (_runtimeRegistry.TryGetValue(instigator, out var skills))
            {
                foreach (var skill in skills) skill.OnKillEnemy(instigator, victim);
            }
        }

        public void NotifyTakeDamage(GameObject instigator, GameObject attacker)
        {
            // 注意：这里的 instigator 指的是“挨打的棋子”
            Debug.Log($"[技能系统] 通知棋子 【{instigator.name}】 的技能：挨打了！攻击者: {attacker.name}");
            if (_runtimeRegistry.TryGetValue(instigator, out var skills))
            {
                foreach (var skill in skills) skill.OnTakeDamage(instigator, attacker);
            }
        }

        public void NotifyCastActiveSkill(GameObject instigator)
        {
            if (_runtimeRegistry.TryGetValue(instigator, out var skills))
            {
                foreach (var skill in skills) skill.OnCastActiveSkill(instigator);
            }
        }

        public void EvaluateCheckSystem(GameObject instigator, string checkType, ref int extraAttempts, ref int valueModifier)
        {
            if (_runtimeRegistry.TryGetValue(instigator, out var skills))
            {
                foreach (var skill in skills) 
                    skill.OnCheckInitiated(instigator, checkType, ref extraAttempts, ref valueModifier);
            }
        }

        public float EvaluateDamageMultiplier(GameObject instigator, GameObject target)
        {
            float multiplier = 1.0f;
            if (_runtimeRegistry.TryGetValue(instigator, out var skills))
            {
                foreach (var skill in skills) 
                    skill.OnBeforeAttack(instigator, target, ref multiplier);
            }
            return multiplier;
        }

        public void NotifyPatternRecognitionPassed(GameObject instigator)
        {
            if (_runtimeRegistry.TryGetValue(instigator, out var skills))
            {
                foreach (var skill in skills) 
                    skill.OnPatternRecognitionPassed(instigator);
            }
        }

        /// <summary>
        /// 某特定棋子的回合结束
        /// </summary>
        public void NotifyTurnEnd(GameObject piece)
        {
            if (_runtimeRegistry.TryGetValue(piece, out var skills))
            {
                foreach (var skill in skills) skill.OnTurnEnd();
            }
        }
    }
}