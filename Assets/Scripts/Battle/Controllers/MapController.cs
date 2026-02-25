using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图管理器
/// 用于敌人ai进行寻路计算
/// </summary>
public class MapController : MonoBehaviour
{
    /// <summary>
    /// 当前场景内的所有地块
    /// </summary>
    public List<GameObject> groundList = new();
    
    // 所有楼梯
    public List<LadderArea> ladderAreas = new();

    public LadderArea GetLadder(Vector3 pos)
    {
        // TODO: 寻找可以到达的楼梯
        if (ladderAreas.Count>0)
        {
            return ladderAreas[0];
        }
        return null;
    }
}