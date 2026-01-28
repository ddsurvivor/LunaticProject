using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// 敌人增援波次控制器
/// </summary>
public class WaveController : SerializedMonoBehaviour
{
    [OdinSerialize]
    // 按照回合数刷新
    public Dictionary<int, List<EnemyController>> waveDict = new();

    public void RefreshEnemies(int turnNumber)
    {
        if (waveDict.ContainsKey(turnNumber))
        {
            foreach (var enemyController in waveDict[turnNumber])
            {
                enemyController.gameObject.SetActive(true);// 敌人出现
                enemyController.isActived = true;// 敌人激活
            }
        }
    }
}