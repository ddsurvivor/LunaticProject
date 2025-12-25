using UnityEngine;

/// <summary>
/// 交互区域
/// </summary>
    public class InteractArea : MonoBehaviour
{
    public ActionType actionType;
    
    public virtual void TriggerAction() {}
}
