using UnityEngine;

/// <summary>
/// 交互区域
/// </summary>
public class InteractArea : MonoBehaviour
{
    public ActionType actionType;
    public bool ableToTrigger = true;   // 是否可以触发（例如已被触发过一次的区域可能需要设置为 false）

    public virtual void TriggerAction(PieceController piece = null)
    {
    }
}