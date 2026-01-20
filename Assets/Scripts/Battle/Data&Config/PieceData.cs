using System.Collections.Generic;

[System.Serializable]
/// <summary>
/// 棋子数据
/// </summary>
public class PieceData
{
    public int pieceId;
    public List<SkillPack> skillPacks = new();
}