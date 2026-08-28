using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 棋子行动管理器：基于面板手动配置，控制行动的解锁与临时禁用
/// </summary>
public class PieceActionManager : MonoBehaviour
{
    [Title("关卡行动配置")]
    [LabelText("本关卡允许的行动")]
    [Tooltip("在这里手动勾选/添加当前关卡该棋子能够使用的行动")]
    // 如果您更喜欢用勾选框的形式，可以加上 [EnumToggleButtons]
    public List<ActionType> configuredActions = new List<ActionType>()
    {
        ActionType.移动,
        ActionType.近战攻击,
        ActionType.远程攻击,
        ActionType.道具,
        ActionType.警戒指令,
        ActionType.技能,
        ActionType.待机
    };

    // 当前棋子临时禁用的行动（用于处理战场上的 Debuff，比如缴械、定身）
    private HashSet<ActionType> _tempDisabledActions = new HashSet<ActionType>();

    /// <summary>
    /// 状态控制：临时禁用某行动（例如受到“定身”时传入 ActionType.移动）
    /// </summary>
    public void DisableActionTemp(ActionType action)
    {
        _tempDisabledActions.Add(action);
    }

    /// <summary>
    /// 状态控制：恢复被禁用的行动（Debuff 结束）
    /// </summary>
    public void EnableActionTemp(ActionType action)
    {
        _tempDisabledActions.Remove(action);
    }

    /// <summary>
    /// 动态计算并返回当前真正可用的行动列表，供UI和逻辑调用
    /// </summary>
    public List<ActionType> GetAvailableActions()
    {
        List<ActionType> availableList = new List<ActionType>();
        
        foreach (var action in configuredActions)
        {
            // 只要在面板中配置了，且当前未被临时禁用，才放入最终可用列表
            if (!_tempDisabledActions.Contains(action))
            {
                availableList.Add(action);
            }
        }
        
        return availableList;
    }
    
    
    /// <summary>
    /// 输入一个棋子列表，将当前的行动配置批量应用到这些棋子上
    /// </summary>
    /// <param name="pieces">需要应用设置的棋子列表</param>
    public void ApplySettingsToPieces(List<PieceController> pieces)
    {
        if (pieces == null || pieces.Count == 0) return;

        // 获取当前通过过滤后的可用行动列表
        List<ActionType> currentAvailableActions = GetAvailableActions();

        foreach (var piece in pieces)
        {
            if (piece == null) continue;

            // 1. 关联该管理器引用
            //piece.actionManager = this;

            // 2. 赋予可用行动（实例化新 List 避免多个棋子引用同一个 List 发生冲突）
            piece.availableActions = new List<ActionType>(currentAvailableActions);
        }
    }

    /// <summary>
    /// 批量临时禁用某项行动，并立即刷新应用到传入的棋子列表
    /// </summary>
    public void DisableActionForPieces(List<PieceController> pieces, ActionType action)
    {
        DisableActionTemp(action);
        ApplySettingsToPieces(pieces);
    }

    /// <summary>
    /// 批量恢复某项行动，并立即刷新应用到传入的棋子列表
    /// </summary>
    public void EnableActionForPieces(List<PieceController> pieces, ActionType action)
    {
        EnableActionTemp(action);
        ApplySettingsToPieces(pieces);
    }
}