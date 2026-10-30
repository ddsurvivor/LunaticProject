using UnityEngine;

/// <summary>
/// 交互区域
/// </summary>
public class InteractArea : MonoBehaviour
{
    public ActionType actionType;
    public bool ableToTrigger = true;   // 是否可以触发（例如已被触发过一次的区域可能需要设置为 false）

    /// <summary>提交场景交互前清除移动撤销，剧情、迷雾和区域变化不由移动快照回滚。</summary>
    public virtual void TriggerAction(PieceController piece = null)
    {
        BattleScene.Ins?.BM?.ClearMoveUndo();
    }
}