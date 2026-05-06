
    using UnityEngine;

    public class DiceCheckManager: MonoBehaviour
    {
        
        /// <summary>
        /// 检定功能函数：模式识别对抗
        /// </summary>
        /// <param name="attacker">攻击方棋子</param>
        /// <param name="defender">目标方棋子</param>
        /// <returns>检定结果</returns>
        public CheckResult ModeRecognitionCheck(PieceController attacker, PieceController defender)
        {
            // 假设属性名为 ModeRecognition 和 CounterAttribute
            // 攻击方模式识别属性加成
            int modeRecognition = attacker.playerData.RECOGNITION;
            // 敌人对抗
            int counterAttr = defender.unitAttrCenter.CON;

            // 投掷3D6
            int diceSum = Random.Range(1, 7) + Random.Range(1, 7) + Random.Range(1, 7);

            //float total = modeRecognition + diceSum;// 方案1：属性加成 + 骰子
            float total = diceSum; // 方案2：只计算骰子

            if (total < counterAttr)
            {
                return CheckResult.DamageReduced;
            }
            else if (total >= 1.5f * counterAttr)
            {
                return CheckResult.MustCrit;
            }
            else // total > counterAttr && total < 1.5 * counterAttr
            {
                return CheckResult.DamageIncreased;
            }
        }
    }
    public enum CheckResult
    {
        DamageReduced,   // 伤害减少
        DamageIncreased, // 伤害增加
        MustCrit         // 必定暴击
    }