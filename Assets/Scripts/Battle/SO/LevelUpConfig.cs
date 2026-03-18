using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

/// <summary>
/// 单个等级的数据定义（使用结构体，避免堆分配）
/// </summary>
[System.Serializable]
public struct LevelUpData
{
    public int level; // 等级（1~30）
    [Tooltip("升到该等级所需经验值（从上一级升到本级所需的经验）")]
    //[LabelText("升级经验")]
    public int requiredExp;

    [Tooltip("升到该等级时奖励的属性点数量（等级1通常为0）")]
    //[LabelText("奖励点数")]
    public int rewardAttributePoints;
}

/// <summary>
/// 等级配置资源，在 Project 视图中右键创建：Create/Game/LevelUpConfig
/// </summary>
[CreateAssetMenu(fileName = "LevelUpConfig", menuName = "Game/LevelUpConfig", order = 1)]
public class LevelUpConfig : ScriptableObject
{
    [TableList]
    [OdinSerialize]
    public List<LevelUpData> levels = new List<LevelUpData>();

    /// <summary>
    /// 在编辑器修改资源时自动确保列表长度固定为30
    /// </summary>
    private void OnValidate()
    {
        // 补全至30项
        while (levels.Count < 30)
            levels.Add(new LevelUpData());

        // 截断超出部分
        if (levels.Count > 30)
            levels.RemoveRange(30, levels.Count - 30);
    }

    /*[Button("初始化等级")]
    /// <summary>
    /// 初始化调试数据（线性增长示例）
    /// 等级1所需经验为0，等级2起每级增加100，每级奖励1属性点
    /// </summary>
    public void InitializeDebugData()
    {
        //levels = new List<LevelUpData>(30);
        for (int i = 0; i < 30; i++)
        {
            // 注意：列表索引器返回的是结构体的副本，需整体赋值
            LevelUpData data = new LevelUpData
            {
                level = i + 1
            };
            if (i == 0)
            {
                // 等级1：从0升到1通常不需要经验，但为了统一逻辑，可设为0
                data.requiredExp = 0;
                data.rewardAttributePoints = 0; // 等级1无奖励
            }
            else
            {
                // 示例：每级所需经验 = 100 * 等级（等级2需200，等级3需300...）
                data.requiredExp = 100 * (i + 1);
                data.rewardAttributePoints = 1; // 每级奖励1点
            }
            levels[i] = data;
        }
    }*/

    /// <summary>
    /// 根据等级（1~30）获取对应的升级数据
    /// </summary>
    public LevelUpData GetLevelData(int level)
    {
        if (level < 1 || level > 30)
        {
            Debug.LogError($"等级 {level} 超出有效范围 (1-30)");
            return default;
        }
        return levels[level - 1];
    }

    /// <summary>
    /// 获取指定等级升级所需的经验值（从上一级升到本级所需的经验）
    /// </summary>
    public int GetRequiredExpForLevel(int level)
    {
        if (level < 1 || level > 30)
        {
            Debug.LogError($"等级 {level} 超出有效范围 (1-30)");
            return -1;
        }
        return levels[level - 1].requiredExp;
    }

    /// <summary>
    /// 根据当前累积经验值计算角色等级（1~30）
    /// </summary>
    public int CalculateLevel(int totalExp)
    {
        int accumulatedExp = 0;
        for (int i = 0; i < levels.Count; i++)
        {
            // 等级1的 requiredExp 通常为0，直接跳过累积判断
            if (i > 0)
                accumulatedExp += levels[i - 1].requiredExp;

            // 如果总经验小于等于当前等级的累积经验，则当前等级为 i
            // 但注意：等级1时 accumulatedExp 仍为0，totalExp >=0 即满足
            if (totalExp < accumulatedExp + levels[i].requiredExp)
                return i + 1;
        }
        return 30; // 超过30级封顶
    }
}