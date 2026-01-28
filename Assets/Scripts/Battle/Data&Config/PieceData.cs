using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEngine;
using System;

[Serializable]
/// <summary>
/// 棋子数据
/// </summary>
public class PieceData
{
    public int pieceId;
    public string pieceName;
    public SkillPack meleeAtk;
    public SkillPack rangedAtk;
    [OdinSerialize]
    public Dictionary<ActionType, AudioClip> actionSounds = new();
    public List<SkillPack> skillPacks = new();
}