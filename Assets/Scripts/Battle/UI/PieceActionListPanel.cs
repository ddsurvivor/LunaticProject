using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


public class PieceActionListPanel : SerializedMonoBehaviour
{
    public PieceController pc;
    public Dictionary<ActionType, Button> actionButtonDic = new();
    
    

    public void Start()
    {
        foreach (var pair in actionButtonDic)
        {
            pair.Value.onClick.AddListener(() => OnActionButtonClicked(pair.Key));
        }
    }
    
    private void OnActionButtonClicked(ActionType actionType)
    {
        gameObject.SetActive(false);
        Debug.Log($"Action Button Clicked: {actionType}");
        // 在这里处理按钮点击事件
        switch (actionType)
        {
            case ActionType.Move:
                break;
            case ActionType.Attack:
                pc.StartNormalAttack();
                break;
            case ActionType.Skill:
                break;
            case ActionType.Defend:
                break;
            case ActionType.Idle:
                break;
            case ActionType.Scan:
                break;
            case ActionType.Range_ATK:
                pc.StartNormalAttack(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
        }
    }
}