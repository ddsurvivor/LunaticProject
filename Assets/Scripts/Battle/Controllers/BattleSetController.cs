using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 战斗预设管理器
/// </summary>
public class BattleSetController : MonoBehaviour
{
    public List<PresetData> presetDatas = new();
    public void ApplyAllPreset()
    {
        List<PieceController> targetList = new();
        foreach (var presetData in presetDatas)
        {
            switch (presetData.targetType)
            {
                case TargetType.ALL:
                    targetList.AddRange(BattleScene.Ins.BM.PlayerController.pieces);
                    targetList.AddRange(BattleScene.Ins.BM.AIController.pieces);
                    break;
                case TargetType.ALLY_ALL:
                    targetList.AddRange(BattleScene.Ins.BM.PlayerController.pieces);
                    break;
                case TargetType.ENEMY_ALL:
                    targetList.AddRange(BattleScene.Ins.BM.AIController.pieces);
                    break;
                case TargetType.ID:
                    var piece = BattleScene.Ins.BM.PlayerController.pieces.Find(t =>
                        t.pieceData.pieceId == presetData.targetID);
                    targetList.Add(piece);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

[System.Serializable]
public class PresetData
{
    public TargetType targetType;
    public int targetID;
    
    public BuffState buffState;

    public int AttrID;
    public int attrValue;
}

public enum TargetType
{
    ALL = 0,
    ALLY_ALL = 1,
    ENEMY_ALL = 2,
    ID = 3,
}