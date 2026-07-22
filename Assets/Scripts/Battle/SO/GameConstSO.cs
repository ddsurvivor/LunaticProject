using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

[CreateAssetMenu(fileName = "GameConst", menuName = "Config/Game Const Config")]
public class GameConstSO : SerializedScriptableObject
{
    [InfoBox("游戏常数配置文件")]

    #region 游戏常数
    [Header("游戏常数")]
    [SerializeField, LabelText("初始金币数量")]
    private int initialCoins = 3000;
    public int InitialCoins => initialCoins;
    #endregion

    #region 战斗常数
    [Header("战斗常数")]
    [SerializeField, LabelText("攻击聚能增加量")]
    private int attackBurstCharge = 10;
    public int AttackBurstCharge => attackBurstCharge;

    [SerializeField, LabelText("受击聚能增加量")]
    private int hurtBurstCharge = 5;
    public int HurtBurstCharge => hurtBurstCharge;

    [SerializeField, LabelText("聚能发动时伤害加成")]
    private float burstDamageRate = 1.2f;
    public float BurstDamageRate => burstDamageRate;

    [SerializeField, LabelText("聚能发动后伤害增加比例")]
    private float burstAddDamageRate = 0.2f;
    public float BurstAddDamageRate => burstAddDamageRate;

    [SerializeField, LabelText("敌人使用技能概率(%)")]
    private int enemySkillRate = 35;
    public int EnemySkillRate => enemySkillRate;
    #endregion
    
    // 夹击伤害倍率
    [SerializeField, LabelText("夹击伤害倍率")]
    private float flankDamageRate = 0.7f;
    public float FlankDamageRate => flankDamageRate;
    

    #region 行动常数
    [Header("行动常数")]
    [SerializeField, LabelText("最大行动点数")]
    private int maxActionPoints = 3;
    public int MaxActionPoints => maxActionPoints;

    [OdinSerialize, LabelText("行动点消耗配置")]
    private Dictionary<ActionType, int> actionPointCosts = new Dictionary<ActionType, int>();
    /// <summary> 行动点消耗字典（只读接口保护） </summary>
    public IReadOnlyDictionary<ActionType, int> ActionPointCosts => actionPointCosts;
    #endregion

    #region 工具函数
    /// <summary>
    /// 成功率检定
    /// </summary>
    public static bool CheckRate(int rate)
    {
        int roll = UnityEngine.Random.Range(1, 101);
        return roll <= rate;
    }
    
    /// <summary>
    /// 获取指定行动类型的行动力消耗，未配置时默认返回 1
    /// </summary>
    /// <param name="actionType">行动类型</param>
    /// <returns>行动力消耗点数</returns>
    public int GetActionPointCost(ActionType actionType)
    {
        if (actionPointCosts != null && actionPointCosts.TryGetValue(actionType, out int cost))
        {
            return cost;
        }
    
        // 如果字典未配置该类型或字典为空，默认返回 1
        return 1;
    }
    #endregion
    
    
    /*/// <summary>
    /// 在 Unity 检查器中生成一个按钮，方便一键填充/重置行动力字典
    /// </summary>
    [Button("自动填充行动力消耗配置", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.8f, 1f)] // 调整按钮颜色为天蓝色，更易分辨
    public void AutoFillActionPointCosts()
    {
        if (actionPointCosts == null)
        {
            actionPointCosts = new Dictionary<ActionType, int>();
        }

        actionPointCosts.Clear();

        // 遍历所有枚举元素并根据规则填充
        foreach (ActionType type in System.Enum.GetValues(typeof(ActionType)))
        {
            int cost = GetDefaultCost(type);
            actionPointCosts[type] = cost;
        }

        Debug.Log("【GameConstSO】行动点消耗字典已成功填充！");
    }

    /// <summary>
    /// 根据规则获取默认消耗
    /// </summary>
    private int GetDefaultCost(ActionType type)
    {
        switch (type)
        {
            case ActionType.移动:
                return 2;

            case ActionType.近战攻击:
            case ActionType.远程攻击:
                return 3;

            case ActionType.技能:
                return 4;

            default:
                return 1; // 其余全部默认为 1 点消耗
        }
    }*/
}