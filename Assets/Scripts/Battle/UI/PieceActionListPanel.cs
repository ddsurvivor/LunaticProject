using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class PieceActionListPanel : SerializedMonoBehaviour
{
    public PieceController pc;
    public Dictionary<ActionType, Button> actionButtonDic = new();
    //public Dictionary<ActionType, UnityAction> actionDic = new();

    public void Init(PieceController pc)
    {
        this.pc = pc;
        foreach (var pair in actionButtonDic)
        {
            pair.Value.onClick.AddListener(() => OnActionButtonClicked(pair.Key));
        }
    }

    private void OnEnable()
    {
        foreach (var button in actionButtonDic)
        {
            button.Value.gameObject.SetActive(
                pc.unitAttrCenter.CurMovePoint>=1 && pc.availableActions.Contains(button.Key));
        }
        foreach (var interactArea in pc.interactAreas)
        {
            if (actionButtonDic.ContainsKey(interactArea.actionType))
            {
                actionButtonDic[interactArea.actionType].gameObject.SetActive(true);
                actionButtonDic[interactArea.actionType].onClick.AddListener(interactArea.TriggerAction);
            }
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
                BattleScene.Ins.CM.StartDarg(pc);
                break;
            case ActionType.Attack:
                pc.StartNormalAttack();
                break;
            case ActionType.Skill:
                break;
            case ActionType.Defend:
                break;
            case ActionType.Idle:
                pc.isIdle = true;
                // 清除剩余行动力
                pc.unitAttrCenter.CostMP(pc.unitAttrCenter.CurMovePoint);
                break;
            case ActionType.Scan:
                break;
            case ActionType.Range_ATK:
                pc.StartNormalAttack(true);
                break;
            case ActionType.Reload:
                pc.ReloadAmmo();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
        }
    }
}