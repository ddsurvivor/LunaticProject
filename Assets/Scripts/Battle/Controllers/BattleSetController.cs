using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 战斗预设管理器
/// </summary>
public class BattleSetController : MonoBehaviour
{
    public List<PresetData> presetDatas = new();
    public void ApplyAllPreset(int presetID)
    {
        List<PieceController> targetList = new();
        foreach (var presetData in presetDatas)
        {
            if(presetData.presetID != presetID) continue;
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
            foreach (var target in targetList)
            {
                // 1.应用属性修改
                if (presetData.AttrID != UnitAttrType.None)
                {
                    target.unitAttrCenter.ModifyAttribute(presetData.AttrID, presetData.attrValue);
                }
                // 2.应用buff状态
                if (presetData.buffState != null)
                {
                    BattleScene.Ins.BM.buffManager.AddBuff(target.unitAttrCenter, presetData.buffState.buffType,
                        presetData.buffState.stacks);
                }
                // 3.应用buff属性修改
                if (presetData.buffAttrType != BuffAttrType.None)
                {
                    target.unitAttrCenter.AddBuffAttr(presetData.buffAttrType,
                        presetData.buffAttrValue);
                }
            }
            
            Debug.Log($"应用预设{presetID}，目标数量{targetList.Count}");
        }
    }
}

[System.Serializable]
public class PresetData
{
    public int presetID;
    
    public TargetType targetType;
    public int targetID;
    
    public BuffState buffState;

    public UnitAttrType AttrID;
    public int attrValue;
    
    public BuffAttrType buffAttrType;
    public int buffAttrValue;
}

public enum TargetType
{
    ALL = 0,
    ALLY_ALL = 1,
    ENEMY_ALL = 2,
    ID = 3,
}
