using UnityEngine;


public class LogArea : InteractArea
{
    public string logName;
    public override void TriggerAction(PieceController piece = null)
    {
        base.TriggerAction(piece);
        // 开始战斗内剧情
        BattleScene.Ins.UM.StartLog(logName);
    }
}